using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MelonLoader;
using HarmonyLib;

using static Flatline.Flatline;
using static Flatline.FlatlinePlayer;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.PlayerConsumeDamage;
using static Flatline.DebugModule;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using ScheduleOne.UI;
using ScheduleOne.Property;
using ScheduleOne.Interaction;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Interaction;
#endif

namespace Flatline
{
    public static class FlatlineIngestibles
    {
        // which of the item instance ids have added consumption logic
        public static readonly List<string> addOnConsumption = new()
        {
            "donut", "chili", "megabean", "banana", "paracetamol", "flumedicine", "addy"
        };

        public static readonly List<string> chokeableFoods = new()
        {
            "donut", "megabean", "banana"
        };

        public static readonly List<Ingestible> ingestibles = new()
        {
            new IngestibleDonut(),
            new IngestibleChili(),
            new IngestibleBean(),
            new IngestibleBanana(),
            new IngestibleParacetamol(),
            new IngestibleFluMedicine(),
            new IngestibleAddy(),
            new IngestibleCuke(),
            new IngestibleEnergyDrink()
        };

        public static readonly float consumeDuration = 1.5f;

        public static Image consumeRadialIndicator;
        public static AudioClip munchClip;
        public static AudioClip drinkClip;
        public static AudioSource ingestorAudio;

        private static bool isCurrentItemChecked = false;
        private static bool isCurrentItemValid = false;
        private static float currentConsumeDuration = 0f;
        private static bool ingestingItem = false;
        private static bool isEvaluatingTapAccess = false;

        public static bool ingestibleModuleInitiated = false;

        public static List<Tap> tapsInUse = new();

        public static IEnumerator InitiateIngestibleModule()
        {
            
            if (Singleton<HUD>.Instance != null && Singleton<HUD>.Instance.radialIndicator != null)
            {
                GameObject newObj = UnityEngine.Object.Instantiate(Singleton<HUD>.Instance.radialIndicator.gameObject, Singleton<HUD>.Instance.radialIndicator.transform.parent);
                newObj.SetActive(true);
                newObj.name = "RadialIndicatorAdditional";
                RectTransform original = Singleton<HUD>.Instance.radialIndicator.gameObject.GetComponent<RectTransform>();
                if (original == null)
                    Log("Radial indicator has no rect trasform");
                RectTransform rt = newObj.GetComponent<RectTransform>();
                rt.position = original.position;
                rt.rotation = original.rotation;
                rt.localScale = original.localScale;
                consumeRadialIndicator = newObj.GetComponent<Image>();
                if (consumeRadialIndicator != null)
                    consumeRadialIndicator.enabled = true;
                else
                    Log("Radial indicator has no Image component");
            }
            else
            {
                Log("Failed to make radial indicator copy");
            }
            Log("Radial indicator copy made");
#if MONO
            PlayerInventory.Instance.onEquippedSlotChanged = (Action<int>)Delegate.Combine(PlayerInventory.instance.onEquippedSlotChanged, new Action<int>(OnSlotChanged));
#else
            PlayerInventory.Instance.onEquippedSlotChanged += (Il2CppSystem.Action<int>)OnSlotChanged;
#endif
            Log("Equipped slot change callback assigned");
            // Fetch required sound clips
            Func<string, ItemDefinition> GetItem;

#if MONO
            GetItem = ScheduleOne.Registry.GetItem;
#else
            GetItem = Il2CppScheduleOne.Registry.GetItem;
#endif
            ItemDefinition shroomItemDef = GetItem("shroom");
            ItemDefinition cukeItemDef = GetItem("cuke");

            ItemInstance shroomItemInst = shroomItemDef.GetDefaultInstance(1);

            ShroomInstance shroomInst = null;
            ShroomDefinition shroomDef = null;
            Equippable_Cuke cukeEquippable = null;
#if MONO
            shroomInst = shroomItemInst as ShroomInstance;
            shroomDef = shroomItemDef as ShroomDefinition;
            cukeEquippable = cukeItemDef.Equippable as Equippable_Cuke;
#else
            shroomInst = shroomItemInst.TryCast<ShroomInstance>();
            shroomDef = shroomItemDef.TryCast<ShroomDefinition>();
            cukeEquippable = cukeItemDef.Equippable.TryCast<Equippable_Cuke>();
#endif

            // its unassigned from def annd needs instantiate in the form of equip to get the clip?

            ProductConsumeAnimation consumeInstantiated = UnityEngine.Object.Instantiate<ProductConsumeAnimation>(shroomDef.ConsumeAnimation, null);

            if (consumeInstantiated.ConsumeSound == null)
            {
                Log("Failed to instantiate sound component");
            }
            else
            {
                if (consumeInstantiated.ConsumeSound.Clip == null)
                {
                    Log("Could not instantiate clip");
                }
                else
                {
                    munchClip = consumeInstantiated.ConsumeSound.Clip;
                }
            }

            UnityEngine.Object.Destroy(consumeInstantiated.gameObject);
            consumeInstantiated = null;

            yield return Wait01;
            Log($"Is null:  {munchClip == null}");
            if (cukeEquippable != null)
            {
                Log("Assiging drink clip");
                Equippable_Cuke equippable = UnityEngine.Object.Instantiate<Equippable_Cuke>(cukeEquippable, null);
                if (equippable != null && equippable.SlurpSound != null && equippable.SlurpSound.Clip != null)
                {
                    drinkClip = equippable.SlurpSound.Clip;
                }
                UnityEngine.Object.Destroy(equippable.gameObject);
                equippable = null;

                yield return Wait01;
                Log($"Is null:  {drinkClip == null}");
            }
            else
                Log("Could not assing drink clip");
            Log("Sound clips found");

            GameObject eatSound = new("FlatlineIngestorAudio");

            ingestorAudio = eatSound.AddComponent<AudioSource>();
            ingestorAudio.loop = false;
            ingestorAudio.playOnAwake = false;
            ingestorAudio.volume = 0.7f;
            eatSound.SetActive(true);
            Log("IngestorAudio made");
            //tap for water drinking
            Tap[] allTaps = UnityEngine.Object.FindObjectsOfType<Tap>(true);
            foreach (Tap tap in allTaps)
            {
                if (tap._interactable == null) continue;
                GameObject newIntObj = new("DrinkTapWater");
                InteractableObject intObj = newIntObj.AddComponent<InteractableObject>();
                BoxCollider bc = newIntObj.AddComponent<BoxCollider>();
                bc.size = new Vector3(0.45f, 0.45f, 0.45f);
                bc.isTrigger = true;
                intObj.message = "Drink tap water";
                intObj.interactionState = InteractableObject.EInteractableState.Default;
                intObj.MaxInteractionRange = 1.85f;
                newIntObj.transform.parent = tap._interactable.transform.parent;
                if (tap._interactable.displayLocationPoint != null)
                    newIntObj.transform.localPosition = tap._interactable.displayLocationPoint.localPosition;
                else if (tap._interactable.displayLocationCollider != null)
                    newIntObj.transform.localPosition = tap._interactable.displayLocationCollider.gameObject.transform.localPosition;
                else
                    newIntObj.transform.localPosition = tap._interactable.transform.localPosition;

                newIntObj.transform.localRotation = Quaternion.identity;

                void StartDrinking()
                {
                    coros.Add(MelonCoroutines.Start(DrinkTapWater(tap, intObj)));
                }
                intObj.onInteractStart.AddListener((UnityEngine.Events.UnityAction)StartDrinking);

                void OnHover()
                {
                    if (!isEvaluatingTapAccess) return;
                    isEvaluatingTapAccess = true;
                    coros.Add(MelonCoroutines.Start(EvaluateTapAccess(intObj, tap._interactable)));
                }
                intObj.onHovered.AddListener((UnityEngine.Events.UnityAction)OnHover);

                intObj.enabled = true;
                newIntObj.SetActive(true);

            }
            ingestibleModuleInitiated = true;
            Log("Initiated ingestible module");
            yield break;
        }

        public static void ResetIngestibleModule()
        {
            ingestibleModuleInitiated = false;

            consumeRadialIndicator = null;
            munchClip = null;
            drinkClip = null;
            ingestorAudio = null;

            isCurrentItemChecked = false;
            isCurrentItemValid = false;
            currentConsumeDuration = 0f;
            ingestingItem = false;
        }


        public static IEnumerator EvaluateTapAccess(InteractableObject drinkWater, InteractableObject fillCan)
        {
            if (!drinkWater.enabled || drinkWater.gameObject.activeSelf) yield break;

            if ((fillCan.interactionState == InteractableObject.EInteractableState.Default || fillCan.interactionState == InteractableObject.EInteractableState.Invalid) && drinkWater.interactionState == InteractableObject.EInteractableState.Default)
            {
                drinkWater.interactionState = InteractableObject.EInteractableState.Disabled;
            }
            else if (drinkWater.interactionState == InteractableObject.EInteractableState.Disabled && fillCan.interactionState == InteractableObject.EInteractableState.Disabled)
            {
                drinkWater.interactionState = InteractableObject.EInteractableState.Default;
            }

            yield return Wait05;
            isEvaluatingTapAccess = false;
        }

        // Unity OnUpdate
        public static void UpdateConsumption()
        {
            if (!PlayerSingleton<PlayerInventory>.Instance.isAnythingEquipped) return;
            if (!ingestibleModuleInitiated) return;
            if (!isCurrentItemChecked)
            {
                isCurrentItemChecked = true;
                isCurrentItemValid = CheckSlotItem();
            }

            if (!isCurrentItemValid) return;

            if (Input.GetMouseButtonDown(0))
            {
                ingestingItem = true;
                currentConsumeDuration = 0f;
                consumeRadialIndicator.gameObject.SetActive(true);
                consumeRadialIndicator.fillAmount = 0f;
            }
            if (Input.GetMouseButtonUp(0))
            {
                ResetEatingState();
            }

            // Left Click held
            if (Input.GetMouseButton(0) && ingestingItem)
            {
                consumeRadialIndicator.gameObject.SetActive(true);

                currentConsumeDuration += Time.deltaTime;
                float progress = Mathf.Clamp01(currentConsumeDuration / consumeDuration);
                consumeRadialIndicator.fillAmount = progress;
                if (currentConsumeDuration >= consumeDuration)
                {
                    IngestCurrentItem();
                    ResetEatingState();
                }
            }
            return;
        }

        private static void ResetEatingState()
        {
            if (!ingestibleModuleInitiated) return;
            isCurrentItemChecked = false;
            ingestingItem = false;
            currentConsumeDuration = 0f;
            consumeRadialIndicator.gameObject.SetActive(false);
            consumeRadialIndicator.fillAmount = 0f;
        }

        public static void IngestCurrentItem()
        {
            int current = PlayerSingleton<PlayerInventory>.Instance._equippedSlotIndex;
            ItemInstance item = Player.Local._inventory[current].ItemInstance;
            if (item != null)
            {
                AudioClip currentClip = (item.ID == "flumedicine") ? drinkClip : munchClip;
                ingestorAudio.PlayOneShot(currentClip);

                item.ChangeQuantity(-1);
                if (item.Quantity > 0)
                    PlayerSingleton<PlayerInventory>.Instance.Reequip();

                Player.Local.SendAnimationTrigger("Eat");
                ProcessIngestible(item.ID);
            }
            return;
        }

        public static void ProcessIngestible(string id, EQuality quality = EQuality.Trash, bool useQuality = false)
        {
            if (useQuality)
                OnProductConsumed(TypeFromID(id), quality, true);
            else
                OnProductConsumed(TypeFromID(id));

            if (ingestibles.Count == 0)
            {
                return;
            }
            Ingestible ingestible = null;
            for (int i = 0; i < ingestibles.Count; i++)
            {
                if (ingestibles[i].ItemID == id)
                {
                    ingestible = ingestibles[i];
                    break;
                }
            }
            if (ingestible == null)
            {
                // Log("No ingestible found with id: " + id);
                return;
            }
            else
            {
                // Random roll one in about 67 million to lethally choke on food
                if (UnityEngine.Random.Range(0, 67000000) == 0)
                {
                    Log("Player lethally choked on food: " + ingestible.ItemID);
                    FlatlineUIModule.causeOfDeath = $"Choked on a {ingestible.ItemID}";
                    if (Player.Local.Avatar.CurrentSettings.Gender > 0.5f)
                        FlatlinePlayer.flatlinePlayerAudio.PlayOneShot(ConfigLoader.loadedAudios["femalecough"]);
                    else
                        FlatlinePlayer.flatlinePlayerAudio.PlayOneShot(ConfigLoader.loadedAudios["malecough"]);

                    coros.Add(MelonCoroutines.Start(FlatlinePlayer.PrePlayerDied()));
                    return;
                }

                Player.Local.Health.CurrentHealth = Mathf.Clamp(Player.Local.Health.CurrentHealth + ingestible.HPRegen, 0f, loadedPlayerData.State.healthData.MaxHP);

                SetEnergy(Mathf.Clamp01(loadedPlayerData.State.Energy + ingestible.Energy));

                SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst + ingestible.Thirst));

                SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger + ingestible.Food));

                if ((ingestible.healIllness || ingestible.increaseSanity) && allDiseases.Count > 0)
                {
                    for (int i = 0; i < allDiseases.Count; i++)
                    {
                        if (!allDiseases[i].data.Active) continue;

                        if (allDiseases[i].data.DiseaseID == "fever" && ingestible.healIllness)
                        {
                            allDiseases[i].data.HealState += UnityEngine.Random.Range(0.33f, 0.66f);
                        }

                        if (allDiseases[i].data.DiseaseID == "bonebreak" && ingestible.healIllness)
                        {
                            allDiseases[i].data.HealState += UnityEngine.Random.Range(0.02f, 0.09f);
                        }

                        if (allDiseases[i].data.DiseaseID == "depression" && ingestible.increaseSanity)
                        {
                            allDiseases[i].data.HealState += UnityEngine.Random.Range(0.02f, 0.09f);
                        }
                    }
                }

                FlatlinePlayer.stateChanged = true;
            }
            return;
        }

        public static bool CheckSlotItem()
        {
            int current = PlayerSingleton<PlayerInventory>.Instance._equippedSlotIndex;
            if (current >= 0 && current < 8)
            {
                ItemInstance item = Player.Local._inventory[current].ItemInstance;
                if (item != null)
                {
                    if (addOnConsumption.Contains(item.ID))
                    {
                        return true;
                    }
                    else
                    {
                        ingestingItem = false;
                        return false;
                    }
                }
                else
                {
                    ingestingItem = false;
                    return false;
                }
            }
            else
            {
                ingestingItem = false;
                return false;
            }
        }

        public static void OnSlotChanged(int _)
        {
            ResetEatingState();
        }

        public static IEnumerator DrinkTapWater(Tap tap, InteractableObject intObj)
        {
            if (tap.NPCUserObject != null || tap.PlayerUserObject != null || tapsInUse.Contains(tap)) yield break;
            tapsInUse.Add(tap);
            intObj.enabled = false;
            if (!FlatlinePlayer.flatlinePlayerAudio.isPlaying)
                FlatlinePlayer.flatlinePlayerAudio.PlayOneShot(drinkClip);
            tap.SetHeldOpen(true);
            
            yield return Wait05;
            SetWater(Mathf.Clamp(loadedPlayerData.State.Thirst + 0.08f, 0f, 1f));
            yield return Wait1;
            tap.SetHeldOpen(false);
            yield return Wait1;
            tapsInUse.Remove(tap);
            intObj.enabled = true;
            yield break;
        }

    }

    // Harmony patch the cuke and energy drink to disable their default effects
    [HarmonyPatch(typeof(Equippable_Cuke), "ApplyEffects")]
    public static class Equippable_Cuke_ApplyEffects_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Equippable_Cuke __instance)
        {
            Log("Apply Cuke Effects");
            // Omitted pretty much everything from the function except the consume
            if (__instance.PseudoProduct != null)
            {
#if MONO
                ProductItemInstance product = __instance.PseudoProduct.GetDefaultInstance(1) as ProductItemInstance;
#else
                ProductItemInstance product = __instance.PseudoProduct.GetDefaultInstance(1).TryCast<ProductItemInstance>();
#endif
                if (product != null)
                {
                    Player.Local.ConsumeProduct(product);
                }
            }

            // Full patch so dont run the original
            return false;
        }
    }
}