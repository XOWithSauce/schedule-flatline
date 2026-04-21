using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

using static Flatline.ConfigLoader;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlineIngestibles;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.PlayerConsumeDamage;
using static Flatline.FlatlinePlayer;
using static Flatline.PlayerFallDamage;
using static Flatline.Player_ConsumeProduct_Patch;
using static Flatline.BedRotSimulator;
using static Flatline.DepressionSimulator;
using static Flatline.HospitalHealing;
using static Flatline.PropertyTemperatureController;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Persistence;
using ScheduleOne.UI.MainMenu;
using ScheduleOne.UI;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.UI.MainMenu;
using Il2CppScheduleOne.UI;
#endif

[assembly: MelonInfo(typeof(Flatline.Flatline), Flatline.BuildInfo.Name, Flatline.BuildInfo.Version, Flatline.BuildInfo.Author, Flatline.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
#if MONO
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
#else
// Because the load throws a warning for the Il2Cpp support module missing (gets loaded after the mod)
// Il2Cpp isnt optional dependency per say but this gets rid of the ugly message
[assembly: MelonOptionalDependencies("FishNet.Runtime", "Il2Cpp")]
#endif
[assembly: MelonGame("TVGS", "Schedule I")]

#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonLoader.VerifyLoaderVersion("0.7.0", true)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: MelonLoader.VerifyLoaderVersion("0.7.0", true)]
#endif

namespace Flatline
{
    public static class BuildInfo
    {
        public const string Name = "Flatline";
        public const string Description = "";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.0.1";
        public const string DownloadLink = "https://github.com/XOWithSauce/schedule-flatline";
    }

    public class Flatline : MelonMod
    {
        public static Flatline Instance { get; private set; }
        public static FlatlinePlayerData loadedPlayerData;
        public static FlatlineModConfig currentConfig;
        public static List<object> coros = new();
        public static bool registered = false;
        private bool firstTimeLoad = false;
        public static bool isSaving = false;
        public static bool haltExecution = false;
        public static bool setupCompleted = false;
        #region await
        public static readonly WaitForEndOfFrame WaitFrame = new WaitForEndOfFrame();
        public static readonly WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        public static readonly WaitForSeconds Wait025 = new WaitForSeconds(0.25f);
        public static readonly WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        public static readonly WaitForSeconds Wait1 = new WaitForSeconds(1f);
        public static readonly WaitForSeconds Wait2 = new WaitForSeconds(2f);
        public static readonly WaitForSeconds Wait5 = new WaitForSeconds(5f);
        public static readonly WaitForSeconds Wait10 = new WaitForSeconds(10f);
        public static readonly WaitForSeconds Wait30 = new WaitForSeconds(30f);
        public static readonly WaitForSeconds Wait60 = new WaitForSeconds(60f);
        #endregion

        #region Melon Prefs
        private MelonPreferences_Category category;
        private MelonPreferences_Entry<bool> PermanentDeath;
        private MelonPreferences_Entry<bool> DrugSideEffects;
        private MelonPreferences_Entry<bool> PropertyTemperatureChanges;
        private MelonPreferences_Entry<bool> WorldTemperatureChanges;
        private MelonPreferences_Entry<bool> FahrenheitTemp;

        private MelonPreferences_Entry<bool> DiseasesEnabled;
        private MelonPreferences_Entry<bool> BleedingEnabled;
        private MelonPreferences_Entry<bool> BoneBreakEnabled;
        private MelonPreferences_Entry<bool> CancerEnabled;
        private MelonPreferences_Entry<bool> DepressionEnabled;
        private MelonPreferences_Entry<bool> FeverEnabled;

        private MelonPreferences_Entry<bool> WaterRequired;
        private MelonPreferences_Entry<bool> FoodRequired;
        private MelonPreferences_Entry<bool> EnergyRequired;
        private MelonPreferences_Entry<bool> TemperatureRequired;

        private MelonPreferences_Entry<float> WaterConsumption;
        private MelonPreferences_Entry<float> FoodConsumption;
        private MelonPreferences_Entry<float> EnergyConsumption;
        private MelonPreferences_Entry<float> TemperatureConsumption;

        public override void OnPreferencesSaved()
        {
            currentConfig.PermanentDeath = PermanentDeath.Value;
            currentConfig.DrugSideEffects = DrugSideEffects.Value;
            currentConfig.PropertyTemperatureChanges = PropertyTemperatureChanges.Value;
            currentConfig.WorldTemperatureChanges = WorldTemperatureChanges.Value;
            currentConfig.FahrenheitTemp = FahrenheitTemp.Value;

            currentConfig.DiseasesEnabled = DiseasesEnabled.Value;
            currentConfig.BleedingEnabled = BleedingEnabled.Value;
            currentConfig.BoneBreakEnabled = BoneBreakEnabled.Value;
            currentConfig.CancerEnabled = CancerEnabled.Value;
            currentConfig.DepressionEnabled = DepressionEnabled.Value;
            currentConfig.FeverEnabled = FeverEnabled.Value;

            currentConfig.WaterRequired = WaterRequired.Value;
            currentConfig.FoodRequired = FoodRequired.Value;
            currentConfig.EnergyRequired = EnergyRequired.Value;
            currentConfig.TemperatureRequired = TemperatureRequired.Value;

            FlatlinePlayer.DefaultThirstConsumption = WaterConsumption.Value;
            FlatlinePlayer.DefaultFoodConsumption = FoodConsumption.Value;
            FlatlinePlayer.DefaultEnergyConsumption = EnergyConsumption.Value;
            FlatlinePlayer.TemperatureConsumptionPerMinutePerDegreeDiff = TemperatureConsumption.Value;
        }

        private void SetupMelonPreferences()
        {
            category = MelonPreferences.CreateCategory($"{BuildInfo.Name}_{BuildInfo.Author}", BuildInfo.Name);
            PermanentDeath = category.CreateEntry("PermanentDeath", true,
                display_name: "PermanentDeath",
                description: "Permanent Death Enabled"
            );

            DrugSideEffects = category.CreateEntry("DrugSideEffects", true,
                display_name: "DrugSideEffects",
                description: "Enables overdosing on drugs and medicine and adds effects to drugs"
            );

            PropertyTemperatureChanges = category.CreateEntry("PropertyTemperatureChanges", true,
                display_name: "PropertyTemperatureChanges",
                description: "Properties get cold if door is kept open and outside is colder"
            );

            WorldTemperatureChanges = category.CreateEntry("WorldTemperatureChanges", true,
                display_name: "WorldTemperatureChanges",
                description: "World temperature changes based on time and weather"
            );

            FahrenheitTemp = category.CreateEntry("FahrenheitTemp", false,
                display_name: "FahrenheitTemp",
                description: "Display Temperatures as Fahrenheit"
            );

            DiseasesEnabled = category.CreateEntry("DiseasesEnabled", true,
                display_name: "DiseasesEnabled",
                description: "Enables all diseases"
            );

            BleedingEnabled = category.CreateEntry("BleedingEnabled", true,
                display_name: "BleedingEnabled",
                description: "Enable Bleeding disease"
            );

            BoneBreakEnabled = category.CreateEntry("BoneBreakEnabled", true,
                display_name: "BoneBreakEnabled",
                description: "Enable Bone Break disease"
            );

            CancerEnabled = category.CreateEntry("CancerEnabled", true,
                display_name: "CancerEnabled",
                description: "Enable Cancer disease"
            );

            DepressionEnabled = category.CreateEntry("DepressionEnabled", true,
                display_name: "DepressionEnabled",
                description: "Enable Depression disease"
            );

            FeverEnabled = category.CreateEntry("FeverEnabled", true,
                display_name: "FeverEnabled",
                description: "Enable Fever disease"
            );

            WaterRequired = category.CreateEntry("WaterRequired", true,
                display_name: "WaterRequired",
                description: "Player needs to drink to survive"
            );

            FoodRequired = category.CreateEntry("FoodRequired", true,
                display_name: "FoodRequired",
                description: "Player needs to eat to survive"
            );

            EnergyRequired = category.CreateEntry("EnergyRequired", true,
                display_name: "EnergyRequired",
                description: "Player needs to rest and manage energy"
            );

            TemperatureRequired = category.CreateEntry("TemperatureRequired", true,
                display_name: "TemperatureRequired",
                description: "Player needs to stay warm to survive"
            );

            WaterConsumption = category.CreateEntry("WaterConsumption", 0.00087958f,
                display_name: "WaterConsumption",
                description: "Amount of water consumed per minute"
            );

            FoodConsumption = category.CreateEntry("FoodConsumption", 0.0015f,
                display_name: "FoodConsumption",
                description: "Amount of food consumed per minute"
            );

            EnergyConsumption = category.CreateEntry("EnergyConsumption", 0.0007f,
                display_name: "EnergyConsumption",
                description: "Amount of energy consumed per minute"
            );

            TemperatureConsumption = category.CreateEntry("TemperatureConsumption", 0.00022f,
                display_name: "TemperatureConsumption",
                description: "Amount of temperature lost per each degree difference"
            );
            Log("Diseases enabled: " + DiseasesEnabled.Value);
            Log("In config: " + currentConfig.DiseasesEnabled);

            OnPreferencesSaved();
            MelonPreferences.Save();
        }
        #endregion

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            Instance = this;

            currentConfig = new();
            SetupMelonPreferences();
            Log("Diseases enabled: " + DiseasesEnabled.Value);
            Log("In config: " + currentConfig.DiseasesEnabled);
            // Load images instantiate sprites
            ConfigLoader.LoadModResources();

            // Setup prefab
            MelonCoroutines.Start(InitBedRotModule());

            MelonLogger.Msg("Flatline Mod Loaded");
            return;
        }

        #region Unity Methods

        public override void OnUpdate()
        {
            if (!registered || isSaving || haltExecution) return;
            UpdateConsumption();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered && !firstTimeLoad)
                {
                    firstTimeLoad = true;
#if MONO
                    LoadManager.Instance.onLoadComplete.AddListener(OnLoadCompleteCb);
#else
                    LoadManager.Instance.onLoadComplete.AddListener((UnityEngine.Events.UnityAction)OnLoadCompleteCb);
#endif
                }
            }
            if (buildIndex != 1)
            {
                if (registered)
                {
                    ExitPreTask();
                }
            }

            return;
        }
        #endregion

        #region Mod Initialization and Coroutine load order

        private void OnLoadCompleteCb()
        {
            if (registered) return;
            registered = true;
            MelonCoroutines.Start(Setup());
            return;
        }

        public static IEnumerator Setup()
        {
#if MONO
            WaitUntil gameLoad =  new WaitUntil(() => LoadManager.Instance.IsGameLoaded);
#else
            WaitUntil gameLoad = new WaitUntil((Il2CppSystem.Func<bool>)(() => LoadManager.Instance.IsGameLoaded));
#endif
            yield return gameLoad;

#if MONO
            WaitUntil timeManagerExists = new WaitUntil(() => TimeManager.InstanceExists);
#else
            WaitUntil timeManagerExists = new WaitUntil((Il2CppSystem.Func<bool>)(() => TimeManager.InstanceExists));
#endif
            yield return timeManagerExists;

            Log("Starting Setup");
            loadedPlayerData = ConfigLoader.LoadPlayerData();
            InitiateSurvivalSliders();
            InitiateDiseasesHolder();
            yield return MelonCoroutines.Start(InitiateWorldTemperatureText());
            InitiateDeathScreen();
            InitiateDepressionSimulatorModule();
            yield return MelonCoroutines.Start(InitiateIngestibleModule());
            InitFlatlinePlayer();
            InitPlayerDiseaseDamage();
            InitConsumeDamageModule();
            InitHospitalHealing();
            AssignPseudoProductIDs();
            InitPropertyTemperatureController();

            // After inits
            ApplyLoadedDiseases();
            coros.Add(MelonCoroutines.Start(UpdateSliderState()));
            coros.Add(MelonCoroutines.Start(WhileSprinting()));
            coros.Add(MelonCoroutines.Start(EvaluatePlayerJumpMovement()));
            coros.Add(MelonCoroutines.Start(EnsureSystematicHomeostasis()));
            coros.Add(MelonCoroutines.Start(UpdateDiseasesHolder()));
            coros.Add(MelonCoroutines.Start(LazyUpdatePlayerWarmBuilding()));
            coros.Add(MelonCoroutines.Start(UpdateAllPropertyTemperatures()));
            TimeManager instance = NetworkSingleton<TimeManager>.Instance;

            var action = (Action)FlatlinePlayer.OnMinPass;
            var bedrotAction = (Action)BedRotSimulator.MinPassBedrotting;
#if MONO
            instance.onMinutePass.Add(action);
            instance.onMinutePass.Add(bedrotAction);
            instance.onHourPass = (Action)Delegate.Combine(instance.onHourPass, new Action(FlatlinePlayer.OnHourPass));
            instance.onHourPass = (Action)Delegate.Combine(instance.onHourPass, new Action(PlayerConsumeDamage.OnHourPass));
            instance.onHourPass = (Action)Delegate.Combine(instance.onHourPass, new Action(PlayerDiseaseDamage.OnHourPass));
            instance.onSleepEnd = (Action)Delegate.Combine(instance.onSleepEnd, new Action(PlayerDiseaseDamage.OnSleepEnd));
            instance.onSleepEnd = (Action)Delegate.Combine(instance.onSleepEnd, new Action(FlatlinePlayer.OnSleepEnd));
#else
            instance.onMinutePass += (Il2CppSystem.Action)action;
            instance.onMinutePass += (Il2CppSystem.Action)bedrotAction;
            instance.onHourPass += (Il2CppSystem.Action)FlatlinePlayer.OnHourPass;
            instance.onHourPass += (Il2CppSystem.Action)PlayerConsumeDamage.OnHourPass;
            instance.onHourPass += (Il2CppSystem.Action)PlayerDiseaseDamage.OnHourPass;
            instance.onSleepEnd += (Il2CppSystem.Action)PlayerDiseaseDamage.OnSleepEnd;
            instance.onSleepEnd += (Il2CppSystem.Action)FlatlinePlayer.OnSleepEnd;
#endif
            setupCompleted = true;
            Log("Setup finished");
            yield break;
        }


        #endregion

        #region Harmony Patches for Saving and Coroutine safety
        static void ExitPreTask()
        {
            registered = false;

            foreach (object coro in coros)
            {
                try
                {
                    if (coro != null)
                        MelonCoroutines.Stop(coro);
                }
                catch (Exception ex)
                {
                    Log("Something failed while stopping coroutines: " + ex);
                }
            }

            coros.Clear();

            if (allDiseases.Count > 0)
            {
                foreach (Disease disease in allDiseases)
                {
                    try
                    {
                        if (disease.diseaseCoroutine != null)
                            MelonCoroutines.Stop(disease.diseaseCoroutine);
                    }
                    catch (Exception ex)
                    {
                        Log("Something failed while stopping coroutines: " + ex);
                    }
                }
                allDiseases.Clear();
            }

            ResetFlatlineUIModule();
            ResetFlatlinePlayer();
            ResetIngestibleModule();
            ResetConsumeDamageModule();
            ResetBedRotModule();
            ResetHospitalHealing();
            ResetDepressionSimulatorModule();
            ResetPlayerDiseaseDamage();
            ResetPropertyTemperatureController();
            lastHighestFrameY = 0f;
            isSaving = false;
            loadedPlayerData = null;
            setupCompleted = false;
        }

        [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { typeof(string) })]
        public static class SaveManager_Save_String_Patch
        {
            public static bool Prefix(SaveManager __instance, string saveFolderPath)
            {
                if (!isSaving)
                {
                    isSaving = true;
                    UpdateDiseaseData();
                    ConfigLoader.Save(loadedPlayerData);
                }
                isSaving = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { })]
        public static class SaveManager_Save_Patch
        {
            public static bool Prefix(SaveManager __instance)
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(LoadManager), "ExitToMenu")]
        public static class LoadManager_ExitToMenu_Patch
        {
            public static bool Prefix(LoadManager __instance, SaveInfo autoLoadSave = null, MainMenuPopup.Data mainMenuPopup = null, bool preventLeaveLobby = false)
            {
                ExitPreTask();
                return true;
            }
        }

        [HarmonyPatch(typeof(DeathScreen), "LoadSaveClicked")]
        public static class DeathScreen_LoadSaveClicked_Patch
        {
            public static bool Prefix(DeathScreen __instance)
            {
                ExitPreTask();
                return true;
            }
        }

        [HarmonyPatch(typeof(DeathScreen), "Open")]
        public static class DeathScreen_Open_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(DeathScreen __instance)
            {
                if (currentConfig == null || !registered) return;
                // if the permanent death is not enabled->  disable the custom button
                if (!currentConfig.PermanentDeath)
                {
                    CustomButton.gameObject.SetActive(false);
                }
                // Else perma death enabled ->  disable the respawn/load buttons
                else if (currentConfig.PermanentDeath)
                {
                    if (__instance.respawnButton.gameObject.activeSelf)
                        __instance.respawnButton.gameObject.SetActive(false);
                    if (__instance.loadSaveButton.gameObject.activeSelf)
                        __instance.loadSaveButton.gameObject.SetActive(false);
                }
                return;
            }
        }
        #endregion


    }
}