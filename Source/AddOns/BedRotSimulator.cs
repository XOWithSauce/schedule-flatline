
using MelonLoader;
using UnityEngine;
using System.Collections;
using HarmonyLib;

using static Flatline.Flatline;
using static Flatline.FlatlinePlayer;
using static Flatline.DebugModule;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone;
using ScheduleOne.GameTime;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.GameTime;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#endif

namespace Flatline
{
    public static class BedRotSimulator
    {
        public static bool isBedrotting = false;
        public static bool isInitialized = false;
        public static bool isEvaluatingBedState = false;

        public static readonly float energyRegenPerMinute = DefaultEnergyConsumption + 0.003f;
        public static readonly float healthRegenPerMinuteTarget = DefaultHealthRegeneration * 5f;
        public static readonly float healthRegenT = SystematicHomeostasisSpeed * 10f;

        public static IEnumerator InitBedRotModule()
        {
            // Wait network manager exists
#if MONO
            WaitUntil waitObj = new WaitUntil(() => NetworkManager.Instances != null &&  NetworkManager.Instances.Count > 0);
#else
            WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => NetworkManager._instances != null && NetworkManager._instances.Count > 0));
#endif
            yield return waitObj;

            // Grab the instance and wait for it to init
#if MONO
            NetworkManager instance = Enumerable.ToList<NetworkManager>(NetworkManager.Instances)[0];
#else
            NetworkManager instance = NetworkManager._instances[0];
#endif
#if MONO
            WaitUntil waitObjInit = new WaitUntil(() => instance.Initialized);
#else
            WaitUntil waitObjInit = new WaitUntil((Il2CppSystem.Func<bool>)(() => instance.Initialized));
#endif
            yield return waitObjInit;

            PrepareBedRotInteractable();
            Log("Bed prefab changed succesfully");
            isInitialized = true;
            yield break;
        }

        public static void ResetBedRotModule()
        {
            isBedrotting = false;
            isEvaluatingBedState = false;
        }

        public static void PrepareBedRotInteractable()
        {
            // This has to change the network spawnable object before scene load?
            NetworkManager netManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);
            PrefabObjects spawnablePrefabs = netManager.SpawnablePrefabs;
            for (int i = 0; i < spawnablePrefabs.GetObjectCount(); i++)
            {
                NetworkObject prefab = spawnablePrefabs.GetObject(true, i);
                string name = prefab?.gameObject.name;
                if (name.Contains("SingleBed"))
                {
                    Bed bedComp = prefab.GetComponent<Bed>();
                    GameObject newInteractable = new("BedRotIntObj");
                    newInteractable.transform.parent = prefab.transform;
                    newInteractable.gameObject.name = "BedRotIntObj";
                    newInteractable.transform.position = new Vector3(0f, 0.264f, 0.8f);
                    BoxCollider bc = newInteractable.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    InteractableObject bedRotInt = newInteractable.AddComponent<InteractableObject>();
                    bedRotInt.message = "Rot in the bed";
                    bedRotInt.SetInteractableState(InteractableObject.EInteractableState.Default);

                    newInteractable.SetActive(true);
                    bc.enabled = true;
                    bedRotInt.enabled = true;
                }
            }
        }

        public static void BedRotInteracted(InteractableObject intObj, Bed bed)
        {
            if (!isBedrotting)
            {
                isBedrotting = true;
                coros.Add(MelonCoroutines.Start(BedRotSimulatorRunner(intObj, bed)));
            }
            return;
        }

        public static void BedRotInteractableHovered(InteractableObject intObj, Bed bed)
        {
            if (isEvaluatingBedState) return;
            isEvaluatingBedState = true; 
            // Because hovered runs on update but evaluate needs 1 foreach loop
            // therefore force evaluate speed to be max once a second
            coros.Add(MelonCoroutines.Start(EvaluateBedState(intObj, bed)));
            return;
        }

        public static IEnumerator EvaluateBedState(InteractableObject intObj, Bed bed)
        {

            if (Player.Local.IsSkating)
            {
                intObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
                intObj.message = "Can't bedrot while skateboarding";
            }
            else if (bed.AssignedEmployee != null)
            {
                intObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
                intObj.message = "Can't bedrot in an employee's bed";
            }
            else if (NetworkSingleton<TimeManager>.Instance.IsCurrentTimeWithinRange(359, 700))
            {
                intObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
                intObj.message = "Can't bedrot at late night";
            }
            else if (Player.PlayerList.Count > 1)
            {
                foreach (Player player in Player.PlayerList)
                {
                    if (player.CurrentBed == bed.NetworkObject)
                    {
                        intObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
                        intObj.message = "Someone is already bedrotting here";
                    }
                }
            }
            else
            {
                intObj.SetInteractableState(InteractableObject.EInteractableState.Default);
                intObj.message = "Rot in the bed";
            }
            yield return Wait1;
            if (!registered) yield break;
            isEvaluatingBedState = false;
            yield break;
        }

        public static IEnumerator BedRotSimulatorRunner(InteractableObject intObj, Bed bed)
        {
            intObj.enabled = false;
            bed.intObj.enabled = false;

            Player.Local.CurrentBed = bed.NetworkObject;
            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(false);
            PlayerSingleton<PlayerMovement>.Instance.CanMove = false;
            PlayerSingleton<PlayerMovement>.Instance.enabled = false;
            Player.Local.CapCol.enabled = false;

            Transform playerTrRoot = Player.Local.transform.root;
            playerTrRoot.parent = bed.transform;

            Vector3 originalStandPoint = playerTrRoot.localPosition;
            Quaternion originalRotation = playerTrRoot.localRotation;

            Vector3 targetLocalPosition = new Vector3(0.009f, 0.23f, 0.012f);
            Quaternion targetRotation = Quaternion.Euler(270f, 0f, 0f);

            float animationDuration = 3f;
            float elapsed = 0f;

            while (elapsed < animationDuration && registered)
            {
                float t = elapsed / animationDuration;
                playerTrRoot.localPosition = Vector3.Lerp(originalStandPoint, targetLocalPosition, t);
                playerTrRoot.localRotation = Quaternion.Slerp(originalRotation, targetRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerTrRoot.localPosition = targetLocalPosition;
            playerTrRoot.localRotation = targetRotation;

            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(true);
            
            // While in bed
            float maxY = 20f;
            while (registered)
            {
                yield return WaitFrame;
                float diff = Quaternion.Angle(playerTrRoot.localRotation, targetRotation);

                if (diff > maxY)
                {
                    playerTrRoot.localRotation = Quaternion.Slerp(playerTrRoot.localRotation, targetRotation, 0.05f);
                }

                if (!PlayerSingleton<Phone>.Instance.IsOpen)
                {
                    if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) break;
                }
                if (isPassedOut || isQueuedForDeath) break;
            }

            // Exit bed
            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(false);
            elapsed = 0f;
            Quaternion currentRotation = playerTrRoot.localRotation;
            while (elapsed < animationDuration && registered)
            {
                float t = elapsed / animationDuration;
                playerTrRoot.localPosition = Vector3.Lerp(targetLocalPosition, originalStandPoint, t);
                playerTrRoot.localRotation = Quaternion.Slerp(currentRotation, originalRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            playerTrRoot.localPosition = originalStandPoint;
            playerTrRoot.localRotation = originalRotation;
            playerTrRoot.parent = null;

            Player.Local.CapCol.enabled = true;
            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(true);
            PlayerSingleton<PlayerMovement>.Instance.enabled = true;
            PlayerSingleton<PlayerMovement>.Instance.CanMove = true;

            intObj.enabled = true;
            bed.intObj.enabled = true;

            isBedrotting = false;
            Player.Local.CurrentBed = null;

            AfterBedrotEnd();
            yield break;
        }

        public static void MinPassBedrotting()
        {
            if (!registered || isSaving || !isBedrotting || isQueuedForDeath || haltExecution) return;
            if (loadedPlayerData.State.Energy < 1f)
                loadedPlayerData.State.Energy = Mathf.Clamp01(loadedPlayerData.State.Energy + energyRegenPerMinute);

            if (loadedPlayerData.State.healthData.CurrentHP < loadedPlayerData.State.healthData.MaxHP)
            {
                if (HealthRegenPerMinute < healthRegenPerMinuteTarget)
                    HealthRegenPerMinute = Mathf.Lerp(HealthRegenPerMinute, healthRegenPerMinuteTarget, healthRegenT);

                if (UnityEngine.Random.Range(0f, 1f) > 0.90f)
                {
                    Player.Local.Health.CurrentHealth = Mathf.Clamp(Player.Local.Health.CurrentHealth + 1f, 0f, loadedPlayerData.State.healthData.MaxHP);

                    if (loadedPlayerData.State.Energy < 1f)
                        Mathf.Clamp01(loadedPlayerData.State.Energy + energyRegenPerMinute * 8f);
                }
            }

            if (loadedPlayerData.State.Hunger < 1f && loadedPlayerData.State.Hunger > 0.1f)
            {
                // While bedrotting decrease hunger consumption towards default / 2
                if (FoodConsumptionPerMinute > DefaultFoodConsumption / 2f)
                    FoodConsumptionPerMinute = Mathf.Lerp(FoodConsumptionPerMinute, DefaultFoodConsumption / 2f, SystematicHomeostasisSpeed * 1.55f);
            }

            if (loadedPlayerData.State.Thirst < 1f && loadedPlayerData.State.Thirst > 0.1f)
            {
                // While bedrotting decrease thirst consumption towards default / 2
                if (ThirstConsumptionPerMinute > DefaultThirstConsumption / 2f)
                    ThirstConsumptionPerMinute = Mathf.Lerp(ThirstConsumptionPerMinute, DefaultThirstConsumption / 2f, SystematicHomeostasisSpeed * 1.55f);
            }
            return;
        }

        public static void AfterBedrotEnd()
        {
            // reset the increased changes if applied
            if (HealthRegenPerMinute > DefaultHealthRegeneration)
                HealthRegenPerMinute = Mathf.Lerp(HealthRegenPerMinute, DefaultHealthRegeneration, 0.5f);

            if (FoodConsumptionPerMinute < DefaultFoodConsumption)
                FoodConsumptionPerMinute = Mathf.Lerp(FoodConsumptionPerMinute, DefaultFoodConsumption, 0.5f);

            if (ThirstConsumptionPerMinute < DefaultThirstConsumption)
                ThirstConsumptionPerMinute = Mathf.Lerp(ThirstConsumptionPerMinute, DefaultThirstConsumption, 0.5f);

            return;
        }
    }

    // To add the listeners once instantiated
    [HarmonyPatch(typeof(Bed), "Awake")]
    public static class Bed_Awake_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Bed __instance)
        {
            Transform bedRotTr = __instance.transform.Find("BedRotIntObj");
            if (bedRotTr == null)
            {
                return true;
            }
            InteractableObject bedRotInt = bedRotTr.gameObject.GetComponent<InteractableObject>();

            void OnBedRotInteraction()
            {
                if (!registered) return;
                BedRotSimulator.BedRotInteracted(bedRotInt, __instance);
            }
            bedRotInt.onInteractStart.AddListener((UnityEngine.Events.UnityAction)OnBedRotInteraction);

            void OnBedRotHovered()
            {
                if (!registered) return;
                BedRotSimulator.BedRotInteractableHovered(bedRotInt, __instance);
            }
            bedRotInt.onHovered.AddListener((UnityEngine.Events.UnityAction)OnBedRotHovered);
            return true;
        }
    }
}