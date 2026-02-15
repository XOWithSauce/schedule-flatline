
using System.Collections;
using UnityEngine;
using HarmonyLib;

using static Flatline.Flatline;
using static Flatline.FlatlinePlayer;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;

#if MONO
using ScheduleOne.Building.Doors;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.Temperature;
using ScheduleOne.Map;
using TMPro;
#else
using Il2CppScheduleOne.Building.Doors;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Temperature;
using Il2CppScheduleOne.Map;
using Il2CppTMPro;
#endif

namespace Flatline
{
    public static class PropertyTemperatureController
    {
        public static readonly float MaximumPropertyTemperatureDrop = 6f;

        public static readonly float OpenDoorHeatDissipationTMax = 0.125f;
        public static readonly float OpenDoorHeatDissipationTMin = 0.095f;

        public static readonly float OpenDoorHeatConcentrationTMax = 0.185f;
        public static readonly float OpenDoorHeatConcentrationTMin = 0.115f;

        public static readonly float ACHeaterHeatConcentrationAdd = 0.055f;
        public static readonly float ACHeaterHeatDissipationMin = 0.015f;

        public static readonly float ClosedDoorHeatConcentrationT = 0.195f;

        // key property code,
        // value float 0...1 (higher loses temp faster in smaller spaces)
        // Note: float 1 should equal to property temperature being equal to outside temp?
        public static readonly Dictionary<string, float> PropertyTemperatureChangeMultiplier = new()
        {
            { "motelroom", 0.80f },
            { "sweatshop", 0.75f },
            { "bungalow", 0.67f },
            { "storageunit", 0.80f },
            { "barn", 0.78f },
            { "manor", 0.60f },
            { "dockswarehouse", 1f },   // Open air door equal to world outside temp (unless ac)
            { "seweroffice", 0.1f },    // because its underground but a small space
        };

        // Property code, temp default In order of static List Property.Properties
        public static Dictionary<string, float> propertyTempDefaults = new();

        // key: property code, value text component that displays property temperature
        public static Dictionary<string, TextMeshProUGUI> propertyTemperatureDisplayTexts = new();

        // key: property code, List count is number of heater ac units, list content is object ids
        public static Dictionary<string, List<int>> propertyHeaterCounts = new();

        public static Dictionary<string, PropertyDoorController[]> propertyDoors = new();

        public static Dictionary<EMapRegion, List<BoxCollider>> warmBuildingsInRegion = new();

        public static Transform darkMarket;

        public static void InitPropertyTemperatureController()
        {
            // this finds only active so if manor is unlocked mid game it wont have it (next load will)
            SavePoint[] savePoints = UnityEngine.Object.FindObjectsOfType<SavePoint>();
            Property storageUnit = null;

            // list all properties + default temps and assign storage unit obj for save point check
            foreach (Property property in Property.Properties)
            {
                propertyTempDefaults.Add(property.propertyCode, property.AmbientTemperature);
                Log("Added Property temperature default value to list : " + property.propertyCode);

                if (property.propertyCode == "storageunit")
                    storageUnit = property;

                if (!propertyHeaterCounts.ContainsKey(property.propertyCode))
                    propertyHeaterCounts.Add(property.propertyCode, new());

                if (property.propertyCode != "dockswarehouse" && property.propertyCode != "rv")
                {
                    PropertyDoorController[] doors = property.GetComponentsInChildren<PropertyDoorController>(true);
                    if (doors.Length != 0)
                    {
                        Log($"Found {doors.Length} doors for {property.propertyCode}");
                        propertyDoors.Add(property.propertyCode, doors);
                    }
                }
            }


            // setup text components on save points
            foreach (SavePoint savePoint in savePoints)
            {
                Property parentProperty = savePoint.GetComponentInParent<Property>();
                if (parentProperty == null)
                {
                    // fucking ass but gonna just assign it like this because its not parented
                    Vector3 storageUnitSavePoint = new Vector3(-6.09f, 1.30f, 105.01f);
                    if (Vector3.Distance(savePoint.transform.position, storageUnitSavePoint) < 1f)
                        parentProperty = storageUnit;
                }

                string propertyCode = parentProperty.propertyCode;

                GameObject savePointDisplay = new("LocalAmbientTemp");
                savePointDisplay.SetActive(false);

                savePointDisplay.transform.parent = savePoint.transform;

                TextMeshProUGUI tmPro = savePointDisplay.AddComponent<TextMeshProUGUI>();
                tmPro.fontStyle = FontStyles.Bold;
                tmPro.fontSize = 26f;
                tmPro.color = Color.black;

                string sign = currentConfig.FahrenheitTemp ? fahrenheitSign : celsiusSign;
                int temp = currentConfig.FahrenheitTemp ?
                    Mathf.RoundToInt(CelsiusToFahrenheit((float)parentProperty.AmbientTemperature))
                    : Mathf.RoundToInt(parentProperty.AmbientTemperature);
                tmPro.text = $"{temp}{sign}";

                Canvas canvas = savePointDisplay.AddComponent<Canvas>();
                if (PlayerSingleton<PlayerCamera>.Instance == null || PlayerSingleton<PlayerCamera>.Instance.Camera == null)
                    Log("Player camer is null!");

                canvas.worldCamera = PlayerSingleton<PlayerCamera>.Instance.Camera;
                canvas.renderMode = RenderMode.WorldSpace;

                RectTransform rt = savePointDisplay.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 100f);
                rt.localPosition = new Vector3(-0.015f, -0.076f, -0.032f);
                rt.localRotation = Quaternion.Euler(0f, 90f, 0f);
                rt.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                savePointDisplay.SetActive(true);
                propertyTemperatureDisplayTexts.Add(propertyCode, tmPro);
            }

            // setup warm buildings bounds
            // Northtown
            List<BoxCollider> northtownColliders = new();

            Transform hardwareNorthtown = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Northtown/Hardware Store/Small hardware store/Shop/Collider");
            if (hardwareNorthtown != null && hardwareNorthtown.TryGetComponent<BoxCollider>(out var bc11))
                northtownColliders.Add(bc11);
            else
                Log("Failed to find TR hardwareNorthtown");

            Transform pawnShop = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Northtown/Pawn shop/Collider");
            if (pawnShop != null && pawnShop.TryGetComponent<BoxCollider>(out var bc12))
                northtownColliders.Add(bc12);
            else
                Log("Failed to find TR pawnShop");

            warmBuildingsInRegion.Add(EMapRegion.Northtown, northtownColliders);

            // Westville
            List<BoxCollider> westvilleColliders = new();

            Transform tattooShop = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Westville/Tattoo Parlour New/Collider");
            if (tattooShop != null && tattooShop.TryGetComponent<BoxCollider>(out var bc21))
                westvilleColliders.Add(bc21);
            else
                Log("Failed to find TR tattooShop");

            warmBuildingsInRegion.Add(EMapRegion.Westville, westvilleColliders);

            // Downtown
            List<BoxCollider> downtownColliders = new();

            Transform dealership = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Downtown/Dealership/Dealership/BoundingBox");
            if (dealership != null && dealership.TryGetComponent<BoxCollider>(out var bc31))
                downtownColliders.Add(bc31);
            else
                Log("Failed to find TR dealership");

            Transform hardwareDowntown = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Downtown/HardwardStore/BoundingBox");
            if (hardwareDowntown != null && hardwareDowntown.TryGetComponent<BoxCollider>(out var bc32))
                downtownColliders.Add(bc32);
            else
                Log("Failed to find TR hardwareDowntown");

            Transform boutique = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Downtown/Boutique Store/Collider");
            if (boutique != null && boutique.TryGetComponent<BoxCollider>(out var bc33))
                downtownColliders.Add(bc33);
            else
                Log("Failed to find TR boutique");

            Transform REOffice = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Downtown/RE Office/Collider");
            if (REOffice != null && REOffice.TryGetComponent<BoxCollider>(out var bc34))
                downtownColliders.Add(bc34);
            else
                Log("Failed to find TR REOffice");

            warmBuildingsInRegion.Add(EMapRegion.Downtown, downtownColliders);

            // Docks
            List<BoxCollider> docksColliders = new();

            Transform clothingShop = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Docks/Clothing store with interior/Collider");
            if (clothingShop != null && clothingShop.TryGetComponent<BoxCollider>(out var bc41))
                docksColliders.Add(bc41);
            else
                Log("Failed to find TR clothingShop");

            Transform barberShop = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Docks/Barbershop/Collider");
            if (barberShop != null && barberShop.TryGetComponent<BoxCollider>(out var bc42))
                docksColliders.Add(bc42);
            else
                Log("Failed to find TR clothingShop");

            warmBuildingsInRegion.Add(EMapRegion.Docks, docksColliders);

            darkMarket = Singleton<Map>.Instance.transform.Find("Hyland Point/Region_Docks/Dark Market Area/Docks Warehouse");


            Log("Finished initializing property temperature controller");
        }

        public static void ResetPropertyTemperatureController()
        {
            propertyTempDefaults.Clear();
            propertyTemperatureDisplayTexts.Clear();
            propertyHeaterCounts.Clear();
            propertyDoors.Clear();
            warmBuildingsInRegion.Clear();
            darkMarket = null;
        }

        public static IEnumerator UpdateAllPropertyTemperatures()
        {
            for (; ; )
            {
                yield return Wait2;
                if (!registered) yield break;
                if (isSaving || Singleton<SaveManager>.Instance.IsSaving) continue;
                if (haltExecution) continue;

                for (int i = 0; i < Property.Properties.Count; i++)
                {
                    yield return Wait2;
                    if (!registered) yield break;
                    if (isSaving || Singleton<SaveManager>.Instance.IsSaving) continue;
                    if (haltExecution) continue;

                    Property property = Property.Properties[i];

                    if (!property.IsOwned) continue;
                    // every other property uses doors except warehouse
                    bool useDoors = property.propertyCode == "dockswarehouse" ? false : true;
                    int openDoors = 0;
                    float sizeMult = float.NaN;
                    if (!PropertyTemperatureChangeMultiplier.TryGetValue(property.propertyCode, out sizeMult))
                    {
                        Log("Property does not support temp change: " + property.propertyCode);
                        continue;
                    }

                    PropertyDoorController[] doors = null;
                    if (useDoors && propertyDoors.TryGetValue(property.propertyCode, out doors))
                    {
                        if (doors.Length == 0)
                        {
                            Log("Property has doors length 0! " + property.propertyCode);
                            continue;
                        }
                        foreach (PropertyDoorController controller in doors)
                        {
                            if (controller.IsOpen)
                                openDoors++;
                        }
                    }

                    float result = float.NaN;

                    if (useDoors)
                    {
                        if (!propertyTempDefaults.ContainsKey(property.propertyCode))
                        {
                            Log("Property does not exist in temperature defaults: " + property.propertyCode);
                            continue;
                        }

                        if (openDoors > 0 && property.AmbientTemperature > (propertyTempDefaults[property.propertyCode] - MaximumPropertyTemperatureDrop))
                        {
                            // Based on open door count decrease OR Increase property ambient temperature

                            if (doors == null || doors.Length == 0)
                            {
                                Log("No listed doors for property!");
                                continue; // Do nothing
                            }
                            // skip if temp diff is not great enough
                            if (Mathf.Abs(OutsideTemperatureCelsius - property.AmbientTemperature) < 1.5f) 
                                continue;

                            float openDTMax = OpenDoorHeatDissipationTMax * sizeMult;
                            float openDTMin = OpenDoorHeatDissipationTMin * sizeMult;
                            float openCTMax = OpenDoorHeatConcentrationTMax * sizeMult;
                            float openCTMin = OpenDoorHeatConcentrationTMin * sizeMult;

                            float openDoorsT = Mathf.Clamp01(((float)openDoors / (float)doors.Length));
                            float heatDissipationT = Mathf.Lerp(openDTMin, openDTMax, openDoorsT);
                            float heatConcentrationT = Mathf.Lerp(openCTMin, openCTMax, openDoorsT);

                            if (property.AmbientTemperature <= OutsideTemperatureCelsius && property.AmbientTemperature < propertyTempDefaults[property.propertyCode])
                            {
                                // Increase when property temperature is smaller than or equal to outside
                                // and must be still lower than the default

                                int heaterCount = 0;
                                if (propertyHeaterCounts.Count > 0 && propertyHeaterCounts.ContainsKey(property.propertyCode))
                                {
                                    propertyHeaterCounts.TryGetValue(property.propertyCode, out List<int> heatersList);
                                    heaterCount = heatersList.Count;
                                    if (heaterCount > 0)
                                        heatConcentrationT = Mathf.Lerp(heatConcentrationT, heatConcentrationT + ACHeaterHeatConcentrationAdd, Mathf.Clamp01((float)heaterCount / 3f));
                                }

                                result = EaseOutCubic(property.AmbientTemperature, OutsideTemperatureCelsius, heatConcentrationT);
                                result = Mathf.Clamp(result, 0f, propertyTempDefaults[property.propertyCode]);
                                Log($"{property.propertyCode} heat accumulation: {result} (+{result - property.AmbientTemperature})");
                            }
                            else if (property.AmbientTemperature > OutsideTemperatureCelsius && property.AmbientTemperature > (propertyTempDefaults[property.propertyCode] - MaximumPropertyTemperatureDrop))
                            {
                                // decrease if diff is large enough and doesnt contain AC heating units
                                // And property temperature must be higher than the max change

                                int heaterCount = 0;
                                if (propertyHeaterCounts.Count > 0 && propertyHeaterCounts.ContainsKey(property.propertyCode))
                                {
                                    propertyHeaterCounts.TryGetValue(property.propertyCode, out List<int> heatersList);
                                    heaterCount = heatersList.Count;
                                    if (heaterCount > 0)
                                        heatDissipationT = Mathf.Lerp(heatDissipationT, ACHeaterHeatDissipationMin, Mathf.Clamp01((float)heaterCount / 3f));
                                }

                                result = EaseOutCubic(property.AmbientTemperature, OutsideTemperatureCelsius, heatDissipationT);
                                result = Mathf.Clamp(result, 0f, propertyTempDefaults[property.propertyCode]);
                                Log($"{property.propertyCode} heat dissipation: {result} (-{property.AmbientTemperature - result})");
                            }
                        }
                        else if (openDoors == 0 || property.AmbientTemperature <= (propertyTempDefaults[property.propertyCode] - MaximumPropertyTemperatureDrop))
                        {
                            float delta = Mathf.Abs(property.AmbientTemperature - propertyTempDefaults[property.propertyCode]);
                            // increase temp towards default if not at default OR at lower than lowest possible inside temperature
                            if (delta > 0.1f)
                            {
                                float closedT = ClosedDoorHeatConcentrationT * sizeMult;
                                // Is not approximately, ease towards default
                                int heaterCount = 0;
                                if (propertyHeaterCounts.Count > 0 && propertyHeaterCounts.ContainsKey(property.propertyCode))
                                {
                                    propertyHeaterCounts.TryGetValue(property.propertyCode, out List<int> heatersList);
                                    heaterCount = heatersList.Count;
                                    if (heaterCount > 0)
                                        closedT = Mathf.Lerp(closedT, ClosedDoorHeatConcentrationT + ACHeaterHeatConcentrationAdd, Mathf.Clamp01((float)heaterCount / 3f));
                                }

                                result = EaseOutCubic(property.AmbientTemperature, propertyTempDefaults[property.propertyCode], closedT);

                                Log($"{property.propertyCode} heat accumulation: {result} (+{result - property.AmbientTemperature})");
                            }
                            else if (property.AmbientTemperature != propertyTempDefaults[property.propertyCode])
                            {
                                // Is approximately and not equal to, set equal to defaults
                                result = propertyTempDefaults[property.propertyCode];
                            }
                            else
                            {
                                // is approximately and equal to default do nothing
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // for warehouse use the cubic but outside temp so t == 1 but can be negated towards 0 with heaters
                        float heatDissipationT = 1f;
                        float heatConcentrationT = 1f - ACHeaterHeatConcentrationAdd;
                        float targetDefault = propertyTempDefaults[property.propertyCode];
                        float newTempTarget = OutsideTemperatureCelsius;
                        int heaterCount = 0;
                        if (propertyHeaterCounts.Count > 0 && propertyHeaterCounts.ContainsKey(property.propertyCode))
                        {
                            propertyHeaterCounts.TryGetValue(property.propertyCode, out List<int> heatersList);
                            heaterCount = heatersList.Count;
                        }

                        if (heaterCount > 0)
                            newTempTarget = Mathf.Lerp(newTempTarget, targetDefault, Mathf.Clamp01((float)heaterCount / 3f));

                        if (property.AmbientTemperature > newTempTarget)
                        {
                            if (heaterCount > 0)
                            {
                                heatDissipationT = Mathf.Lerp(heatDissipationT, ACHeaterHeatDissipationMin, Mathf.Clamp01((float)heaterCount / 3f));
                            }

                            result = EaseOutCubic(property.AmbientTemperature, newTempTarget, heatDissipationT);
                            Log($"{property.propertyCode} heat dissipation: {result} (-{property.AmbientTemperature - result})");
                        }
                        else if (property.AmbientTemperature < newTempTarget)
                        {
                            if (heaterCount > 0)
                            {
                                heatConcentrationT = Mathf.Lerp(heatConcentrationT, heatConcentrationT + ACHeaterHeatConcentrationAdd, Mathf.Clamp01((float)heaterCount / 3f));
                            }

                            result = EaseOutCubic(property.AmbientTemperature, newTempTarget, heatConcentrationT);
                            Log($"{property.propertyCode} heat accumulation: {result} (+{result - property.AmbientTemperature})");
                        }
                    }

                    // Apply not continued
                    if (!float.IsNaN(result))
                    {
                        Property.Properties[i].AmbientTemperature = result;
                        // Not continued, temp must have changed update text
                        string sign = currentConfig.FahrenheitTemp ? fahrenheitSign : celsiusSign;
                        int temp = currentConfig.FahrenheitTemp ?
                            Mathf.RoundToInt(CelsiusToFahrenheit((float)result))
                            : Mathf.RoundToInt(result);
                        propertyTemperatureDisplayTexts[property.propertyCode].text = $"{temp}{sign}";
                    }
                    else
                    {
                        Log("Temperature result calculation failed for " + property.propertyCode);
                    }
                }
            }
        }

        public static float EaseOutCubic(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, 1f - Mathf.Pow(1f - t, 3f));
        }


        // Patch Air conditioner to add a listener on awake to update
        // property heater counts when needed
        [HarmonyPatch(typeof(AirConditioner), "Awake")]
        public static class AirConditioner_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(AirConditioner __instance)
            {
                if (__instance.ParentProperty != null)
                {
                    string code = __instance.ParentProperty.propertyCode;
                    if (__instance.CurrentMode == AirConditioner.EMode.Heating)
                    {
                        if (!propertyHeaterCounts.ContainsKey(code))
                            propertyHeaterCounts.Add(code, new());

                        if (!propertyHeaterCounts[code].Contains(__instance.GetInstanceID()))
                        {
                            propertyHeaterCounts[code].Add(__instance.GetInstanceID());
                            Log($"Property {code} heater count increased to {propertyHeaterCounts[code].Count}", "Awake");
                        }
                    }
                }

                if (__instance.TemperatureEmitter == null) return;

                void OnThisEmitterChanged()
                {
                    if (!registered) return;

                    string code = __instance.ParentProperty != null ? __instance.ParentProperty.propertyCode : "";
                    if (code == "") return;

                    if (__instance.CurrentMode == AirConditioner.EMode.Heating)
                    {
                        if (!propertyHeaterCounts.ContainsKey(code))
                            propertyHeaterCounts.Add(code, new());

                        if (!propertyHeaterCounts[code].Contains(__instance.GetInstanceID()))
                        {
                            propertyHeaterCounts[code].Add(__instance.GetInstanceID());
                            Log($"Property heater count increased to {propertyHeaterCounts[code].Count}", "EmitterChanged");
                        }
                    }
                    else
                    {
                        if (!propertyHeaterCounts.ContainsKey(code))
                            propertyHeaterCounts.Add(code, new());

                        if (propertyHeaterCounts[code].Count > 0)
                        {
                            if (propertyHeaterCounts[code].Contains(__instance.GetInstanceID()))
                            {
                                propertyHeaterCounts[code].Remove(__instance.GetInstanceID());
                                Log($"Property heater count decreased to {propertyHeaterCounts[code].Count}", "EmitterChanged");
                            }
                        }
                    }
                    return;
                }
#if MONO
                __instance.TemperatureEmitter.OnEmitterChanged = (Action)Delegate.Combine(__instance.TemperatureEmitter.OnEmitterChanged, new Action(OnThisEmitterChanged));
#else
                __instance.TemperatureEmitter.OnEmitterChanged += (Il2CppSystem.Action)OnThisEmitterChanged;
#endif
                return;
            }
        }

        // Patch Air conditioner Destroy function to update the heater counts when needed
        [HarmonyPatch(typeof(AirConditioner), "Destroy")]
        public static class AirConditioner_Destroy_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(AirConditioner __instance)
            {
                if (!registered) return true;
                if (__instance.CurrentMode != AirConditioner.EMode.Heating) return true;
                if (__instance.ParentProperty == null)
                {
                    Log("Failed to process destroy prefix", "Destroy");
                    return true;
                }
                string code = __instance.ParentProperty.propertyCode;
                if (propertyHeaterCounts.ContainsKey(code) && propertyHeaterCounts[code].Count > 0)
                {
                    if (propertyHeaterCounts[code].Contains(__instance.GetInstanceID()))
                    {
                        propertyHeaterCounts[code].Remove(__instance.GetInstanceID());
                        Log($"Property heater count decreased to {propertyHeaterCounts[code].Count}", "Destroy");
                    }
                }

                return true;
            }
        }

    }

}