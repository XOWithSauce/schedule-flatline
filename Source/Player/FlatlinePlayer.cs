using System.Collections;
using UnityEngine;
using HarmonyLib;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.FlatlineUIModule;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.FlatlineIngestibles;
using static Flatline.BedRotSimulator;
using static Flatline.ConfigLoader;
using static Flatline.PropertyTemperatureController;
using static Flatline.DebugModule;

#if MONO
using ScheduleOne.Audio;
using ScheduleOne.Core.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.PlayerScripts;
using ScheduleOne.PlayerScripts.Health;
using ScheduleOne.Combat;
using ScheduleOne.Product;
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.FX;
using ScheduleOne.UI;
using ScheduleOne.Weather;
using ScheduleOne.Core.Weather;
using ScheduleOne;
using ScheduleOne.Money;
using ScheduleOne.Map;
using ScheduleOne.Persistence;
using FishNet;
#else
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.Core.Audio;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.FX;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Weather;
using Il2CppScheduleOne.Core.Weather;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Persistence;
using Il2CppFishNet;
#endif

namespace Flatline
{
    public static class FlatlinePlayer
    {
        public static readonly float SystematicHomeostasisSpeed = 1f / ((60f * 24f) / 2f);

        public static float DefaultThirstConsumption = 0.00087958f;
        public static float ThirstConsumptionPerMinute = DefaultThirstConsumption;

        public static float DefaultFoodConsumption = 0.0015f;
        public static float FoodConsumptionPerMinute = DefaultFoodConsumption;

        public static float DefaultEnergyConsumption = 0.0007f;
        public static float EnergyConsumptionPerMinute = DefaultEnergyConsumption;
        public static readonly float PunchEnergyConsumption = 0.001f;
        public static readonly float JumpEnergyConsumption = 0.0032f;

        public static readonly float ParasympatheticActiveTempDecreasePerMinute = 0.00035f;
        public static float TemperatureConsumptionPerMinutePerDegreeDiff = 0.00022f;
        public static readonly float TemperatureIncreasePerMinuteInside = 0.0035f;
        public static float TemperatureConsumption = TemperatureConsumptionPerMinutePerDegreeDiff;

        public static readonly float DefaultHealthRegeneration = 0.0006944f;
        public static float HealthRegenPerMinute = DefaultHealthRegeneration;

        public static readonly float MaxSprintReserveSeconds = 15f;
        public static float sprintReserveSeconds = MaxSprintReserveSeconds;
        public static readonly float extraEnergyConsumptionWhileRunning = 0.0005f;
        public static readonly float extraTemperatureIncreaseWhileRunning = 0.000025f;
        public static readonly float DefaultSprintReserveRegenPerMinute = 0.66f;
        public static float sprintReserveRegenPerMinute = DefaultSprintReserveRegenPerMinute;
        public static bool isSprintExhausted = false;

        public static bool isPassedOut = false;
        public static bool isQueuedForDeath = false;
        public static bool isFPRagdollActive = false;

        public static bool stateChanged = false;

        #region Clothing and temperature
        public static readonly List<string> warmClothesPaths = new List<string>()
        {
            "Avatar/Layers/Bottom/Jeans",
            "Avatar/Layers/Top/Overalls",
            "Avatar/Layers/Bottom/CargoPants",
            "Avatar/Layers/Top/Buttonup",
            "Avatar/Layers/Top/FlannelButtonUp",
            "Avatar/Layers/Accessories/Gloves",

            "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
            "Avatar/Accessories/Chest/Blazer/Blazer",
            "Avatar/Accessories/Feet/Sneakers/Sneakers",
            "Avatar/Accessories/Feet/CombatBoots/CombatBoots",
            "Avatar/Accessories/Feet/DressShoes/DressShoes",
            "Avatar/Accessories/Head/Beanie/Beanie"
        };

        public static readonly int maxBodyLayerClothes = 3;
        public static readonly int maxAccessoryLayerClothes = 3;

        public static float OutsideTemperatureCelsius = 17f;
        public static float dayTempTarget = 20f;
        public static float nightTempTarget = 5f;
        public static readonly float maxOutTempCelsius = 23f;
        public static readonly float minOutTempCelsius = -1f;

        public static float playerClothingAmount = 0.33f;
        #endregion

        public static int minsInsideProperty = 0;
        public static int minsOutsideProperty = 0;
        public static int minsWithSunnyWeather = 0;

        public static bool insideWarmBuilding = false;

        public static AudioSource flatlinePlayerAudio;

        public static List<string> lastDamageSources = new();

        public static void InitFlatlinePlayer()
        {
            Player.Local.Health.CurrentHealth = loadedPlayerData.State.healthData.CurrentHP;
            // Player.Local.Energy.CurrentEnergy = Mathf.Lerp(1f, 100f, loadedPlayerData.State.Energy);
            ThirstSlider.value = loadedPlayerData.State.Thirst;
            HungerSlider.value = loadedPlayerData.State.Hunger;
            EnergySlider.value = loadedPlayerData.State.Energy;
            TemperatureSlider.value = loadedPlayerData.State.Temperature;

            GameObject audioObject = new GameObject("FlatlinePlayerAudio");
            audioObject.transform.parent = Player.Local.transform.root;
            audioObject.transform.localPosition = Vector3.zero;
            flatlinePlayerAudio = audioObject.AddComponent<AudioSource>();
            flatlinePlayerAudio.volume = 0.7f;
            flatlinePlayerAudio.loop = false;
            audioObject.SetActive(true);
            flatlinePlayerAudio.enabled = true;

            if (currentConfig.WorldTemperatureChanges)
            {
                float sineTemp = Mathf.Lerp(2f, 20f, Mathf.Sin(TimeManager.Instance.NormalizedTimeOfDay * 2f * Mathf.PI - (Mathf.PI / 2f)));
                OutsideTemperatureCelsius = Mathf.MoveTowards(sineTemp, GetWeatherAdjustedWorldTemp(sineTemp, currentMin: 5f, currentMax: 17f), 2.5f);
            }
            else
            {
                OutsideTemperatureCelsius = 20f;
            }

            if (WorldTemperature != null)
            {
                string sign = currentConfig.FahrenheitTemp ? fahrenheitSign : celsiusSign;
                int temp = currentConfig.FahrenheitTemp ?
                    Mathf.RoundToInt(CelsiusToFahrenheit((float)OutsideTemperatureCelsius))
                    : Mathf.RoundToInt(OutsideTemperatureCelsius);
                WorldTemperature.text = $"{temp}{sign}";
            }

            // add listener to update sfx volumes
            // TODO: Fixme onVolumeSettingsChanged is of type PreallocatedAction with source showing cappacit on it, ensure not being over cappacit after adding listener and ensure casting works both mono il2cpp without issues
#if MONO
            //Singleton<AudioManager>.Instance.onVolumeSettingsChanged = (Action)Delegate.Combine(Singleton<AudioManager>.Instance.onVolumeSettingsChanged, new Action(OnAudioSettingsChanged));
#else
            //Singleton<AudioManager>.Instance.onVolumeSettingsChanged += (Il2CppSystem.Action)OnAudioSettingsChanged;
#endif
            Log("Finished init player");
        }

        public static void ResetFlatlinePlayer()
        {
            ThirstConsumptionPerMinute = DefaultThirstConsumption;

            FoodConsumptionPerMinute = DefaultFoodConsumption;

            EnergyConsumptionPerMinute = DefaultEnergyConsumption;

            TemperatureConsumption = TemperatureConsumptionPerMinutePerDegreeDiff;

            HealthRegenPerMinute = DefaultHealthRegeneration;

            sprintReserveSeconds = MaxSprintReserveSeconds;
            sprintReserveRegenPerMinute = DefaultSprintReserveRegenPerMinute;

            isSprintExhausted = false;

            isPassedOut = false;
            isQueuedForDeath = false;
            isFPRagdollActive = false;

            stateChanged = false;

            OutsideTemperatureCelsius = 20f;
            playerClothingAmount = 0.33f;

            minsInsideProperty = 0;
            minsOutsideProperty = 0;
            minsWithSunnyWeather = 0;
            flatlinePlayerAudio = null;

            insideWarmBuilding = false;
        }

        public static IEnumerator PrePlayerDied()
        {
            Log("Started death routine");
            if (isQueuedForDeath) yield break;
            isQueuedForDeath = true;

            if (isBedrotting)
            {
                yield return Wait2;
                yield return Wait1;
                if (!registered) yield break;
            }

            if (isPassedOut)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || !isPassedOut);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || !isPassedOut));
#endif
                yield return waitObj;
            }

            if (isFPRagdollActive || Player.Local.IsRagdolled)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || (!isFPRagdollActive && !Player.Local.IsRagdolled));
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || (!isFPRagdollActive && !Player.Local.IsRagdolled)));
#endif
                yield return waitObj;
            }

            Player.Local.Health.PlayBloodMist();

            Singleton<EyelidOverlay>.Instance.AutoUpdate = false;

            float duration = 3.5f;
            float current = 0f;

            bool hasSetRagdolled = false;

            float moveSpeed = PlayerMovement.StaticMoveSpeedMultiplier;

            flatlinePlayerAudio.PlayOneShot(loadedAudios["flatline"]);

            float t = 0f;
            while (registered && current < duration)
            {
                current += Time.deltaTime;
                t = current / duration;
                Singleton<PostProcessingManager>.Instance.SetBlur(t);
                PlayerMovement.StaticMoveSpeedMultiplier = Mathf.Lerp(moveSpeed, 0f, t);
                Singleton<EyelidOverlay>.Instance.CurrentOpen = Mathf.Lerp(1f, 0f, t);
                if (!hasSetRagdolled && t > 0.1f && !(isFPRagdollActive || Player.Local.IsRagdolled))
                {
                    hasSetRagdolled = true;
                    Player.Local.IsUnconscious = true;
                    Singleton<GameInput>.Instance.ExitAll();
                    PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(false);
                    Singleton<HUD>.Instance.SetCrosshairVisible(false);
                    Singleton<HUD>.Instance.canvas.enabled = false;
                    coros.Add(MelonCoroutines.Start(FPRagdoll()));
                }
                yield return null;
            }

            Singleton<EyelidOverlay>.Instance.CurrentOpen = 0f;
            Singleton<PostProcessingManager>.Instance.SetBlur(1f);
            yield return Wait05;
            if (!registered) yield break;

            Player.Local.Health.SendDie();
            coros.Add(MelonCoroutines.Start(GenerateDeathScreenScore()));

            // For when multiplayer perma ddeath is disabled then its just respawn script and remove the custom ragdoll + reset states
            if (Player.PlayerList.Count > 1 && !currentConfig.PermanentDeath)
            {
                Log("Detected multiplayer instance where permanent death is disabled");
                Log("Wait until player presses respawn button");
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || Player.Local.Health.IsAlive);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || Player.Local.Health.IsAlive));
#endif
                yield return waitObj;

                // And after wait alive its 2 sec wait
                yield return Wait2;
                if (!registered) yield break;

                SetWater(0.99f);
                SetFood(0.99f);
                SetTemperature(0.99f);
                SetEnergy(0.99f);
                loadedPlayerData.State.healthData.MaxHP = 100f;
                loadedPlayerData.State.healthData.CurrentHP = 99f;

                current = 0f;
                t = 0f;
                while (current < duration)
                {
                    if (!registered) yield break;
                    current += Time.deltaTime;
                    t = current / duration;
                    Singleton<PostProcessingManager>.Instance.SetBlur(Mathf.Lerp(1f, 0f, t));
                    Singleton<EyelidOverlay>.Instance.CurrentOpen = t;
                    yield return null;
                }

                Singleton<EyelidOverlay>.Instance.CurrentOpen = 1f;
                Singleton<EyelidOverlay>.Instance.AutoUpdate = true;
                Singleton<PostProcessingManager>.Instance.SetBlur(0f);
                PlayerMovement.StaticMoveSpeedMultiplier = moveSpeed;
                if (hasSetRagdolled)
                {
                    PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(true);
                    Player.Local.IsUnconscious = false;
                    Singleton<HUD>.Instance.SetCrosshairVisible(true);
                }
                isQueuedForDeath = false;
                Log("Player respawned");
            }

            yield break;
        }

        public static IEnumerator PrePlayerPassOut()
        {
            if (isPassedOut || isQueuedForDeath) yield break;
            isPassedOut = true;
            if (isBedrotting)
            {
                yield return Wait2;
                yield return Wait1;
                if (!registered || isQueuedForDeath) yield break;
            }

            if (isFPRagdollActive || Player.Local.IsRagdolled)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || (!isFPRagdollActive && !Player.Local.IsRagdolled));
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || (!isFPRagdollActive && !Player.Local.IsRagdolled)));
#endif
                yield return waitObj;
            }

            Singleton<EyelidOverlay>.Instance.AutoUpdate = false;

            bool shouldRob = false;
            if (Player.Local.CurrentProperty == null)
                shouldRob = true;

            bool shouldSleep = false;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime > 30 && NetworkSingleton<TimeManager>.Instance.CurrentTime < 701)
            {
                if (!InstanceFinder.IsServer)
                    Log("Server client cannot sleep through the night during pass out");
                else
                    shouldSleep = true;
            }

            float duration = 6f;
            float current = 0f;

            bool hasSetRagdolled = false;

            bool shouldWakeNow = false;
            bool ShouldWake()
            {
                return shouldWakeNow;
            }

            float moveSpeed = PlayerMovement.StaticMoveSpeedMultiplier;

            float t = 0f;
            while (current < duration)
            {
                if (!registered || isQueuedForDeath) yield break;
                current += Time.deltaTime;
                t = current / duration;
                Singleton<PostProcessingManager>.Instance.SetBlur(t);
                PlayerMovement.StaticMoveSpeedMultiplier = Mathf.Lerp(1f, 0f, t);
                if (t > 0.375f)
                {
                    Singleton<EyelidOverlay>.Instance.CurrentOpen = Mathf.Lerp(1f, 0f, t);
                }
                if (!hasSetRagdolled && t > 0.1f)
                {
                    hasSetRagdolled = true;
                    Player.Local.IsUnconscious = true;
                    Singleton<GameInput>.Instance.ExitAll();
                    PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(false);
                    coros.Add(MelonCoroutines.Start(FPRagdoll(shouldSleep, ShouldWake)));
                }
                yield return null;
            }

            Singleton<EyelidOverlay>.Instance.CurrentOpen = 0f;
            Singleton<PostProcessingManager>.Instance.SetBlur(1f);
            if (!registered || isQueuedForDeath) yield break;

            if (shouldRob)
            {
                float cashLoss = 0f;
                if (MoneyManager.Instance.cashBalance > 50f)
                    cashLoss = Mathf.Round(NetworkSingleton<MoneyManager>.Instance.cashBalance * 0.85f);

                if (cashLoss != 0f)
                    NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-cashLoss, false, false);
            }

            
            if (shouldSleep)
            {
                NetworkSingleton<TimeManager>.Instance.StartSleep();
            }
            else
            {
                // Skip 3 hours only regen little bit energy
                int passOutEndTime = TimeManager.AddMinutesTo24HourTime(NetworkSingleton<TimeManager>.Instance.CurrentTime, 60 * 3);
                NetworkSingleton<TimeManager>.Instance.SkipForwardToTime(passOutEndTime);
                loadedPlayerData.State.Energy += 0.33f;
            }

            if (!shouldSleep)
            {
                yield return Wait5;
                if (!registered || isQueuedForDeath) yield break;
            }
            else
            {
                Log("Waiting until sleep ends to recover ragdoll...");
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || !NetworkSingleton<TimeManager>.Instance.IsSleepInProgress);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || !NetworkSingleton<TimeManager>.Instance.IsSleepInProgress));
#endif
                yield return waitObj;
                yield return Wait5;
                if (!registered || isQueuedForDeath) yield break;
                Log("Resume ragdoll recover");
            }
            shouldWakeNow = true;

            current = 0f;
            t = 0f;
            while (current < duration)
            {
                if (!registered || isQueuedForDeath) yield break;
                current += Time.deltaTime;
                t = current / duration;
                Singleton<PostProcessingManager>.Instance.SetBlur(Mathf.Lerp(1f, 0f, t));
                Singleton<EyelidOverlay>.Instance.CurrentOpen = t;
                yield return null;
            }

            Singleton<EyelidOverlay>.Instance.CurrentOpen = 1f;
            Singleton<EyelidOverlay>.Instance.AutoUpdate = true;
            Singleton<PostProcessingManager>.Instance.SetBlur(0f);
            PlayerMovement.StaticMoveSpeedMultiplier = moveSpeed;
            PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(true);

            Player.Local.IsUnconscious = false;
            isPassedOut = false;
            yield break;
        }

        public static void OnSleepEnd()
        {
            if (haltExecution) return;

            SetEnergy(1f);
            SetFood(loadedPlayerData.State.Hunger * 0.66f);
            SetWater(loadedPlayerData.State.Thirst * 0.66f);
            SetTemperature(loadedPlayerData.State.Temperature * 0.90f);

            if (currentConfig.DiseasesEnabled && currentConfig.FeverEnabled)
            {
                bool hasFlu = false;
                foreach (Disease activeDisease in allDiseases)
                {
                    if (activeDisease.GetType() == typeof(Fever) && activeDisease.data.Active)
                    {
                        hasFlu = true;
                    }
                }
                if (!hasFlu)
                    loadedPlayerData.State.healthData.daysSinceFlu++;
                else
                    loadedPlayerData.State.healthData.daysSinceFlu = 0;
            }
            
            if (currentConfig.WorldTemperatureChanges)
            {
                dayTempTarget = UnityEngine.Random.Range(16f, maxOutTempCelsius);
                nightTempTarget = UnityEngine.Random.Range(minOutTempCelsius, 6f);
                float morningTemp = Mathf.Lerp(OutsideTemperatureCelsius, nightTempTarget, 0.66f);
                OutsideTemperatureCelsius = GetWeatherAdjustedWorldTemp(morningTemp, nightTempTarget, nightTempTarget + 6f);
            }
        }

        public static void OnMinPass()
        {
            if (isSaving || !registered || isPassedOut || isQueuedForDeath || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress || haltExecution) return;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400) return;
            if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive) return;

            bool shouldParasympatheticSystemBeActive = loadedPlayerData.State.Hunger >= 0.90f;

            if (shouldParasympatheticSystemBeActive)
                SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst - ThirstConsumptionPerMinute * UnityEngine.Random.Range(1.05f, 1.15f)));
            else
                SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst - ThirstConsumptionPerMinute));

            SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger - FoodConsumptionPerMinute));

            if (shouldParasympatheticSystemBeActive)
                SetEnergy(Mathf.Clamp01(loadedPlayerData.State.Energy - EnergyConsumptionPerMinute * UnityEngine.Random.Range(0.55f, 0.7f)));
            else
                SetEnergy(Mathf.Clamp01(loadedPlayerData.State.Energy - EnergyConsumptionPerMinute));

            // Player.Local.Energy.CurrentEnergy = Mathf.Lerp(1f, 100f, loadedPlayerData.State.Energy);

            if (!Mathf.Approximately(PlayerMovement.StaticMoveSpeedMultiplier, loadedPlayerData.State.healthData.MoveSpeedScale))
            {
                PlayerMovement.StaticMoveSpeedMultiplier = Mathf.Lerp(PlayerMovement.StaticMoveSpeedMultiplier, loadedPlayerData.State.healthData.MoveSpeedScale, 0.15f);
            }
            
            if (Player.Local.Health.CurrentHealth > loadedPlayerData.State.healthData.MaxHP)
            {
                Player.Local.Health.CurrentHealth = loadedPlayerData.State.healthData.MaxHP;
                if (Player.Local.Health.onHealthChanged != null)
                    Player.Local.Health.onHealthChanged.Invoke(Player.Local.Health.CurrentHealth);
            }

            if (!Mathf.Approximately(Player.Local.Health.CurrentHealth, loadedPlayerData.State.healthData.CurrentHP))
                loadedPlayerData.State.healthData.CurrentHP = Player.Local.Health.CurrentHealth;

            if (loadedPlayerData.State.Thirst > 0.2f && loadedPlayerData.State.Hunger > 0.1f && loadedPlayerData.State.healthData.CurrentHP != 0f && loadedPlayerData.State.healthData.CurrentHP < loadedPlayerData.State.healthData.MaxHP)
            {
                loadedPlayerData.State.healthData.CurrentHP = Mathf.Clamp(Player.Local.Health.CurrentHealth + HealthRegenPerMinute, 0f, loadedPlayerData.State.healthData.MaxHP);
                Player.Local.Health.CurrentHealth = loadedPlayerData.State.healthData.CurrentHP;
                if (Player.Local.Health.onHealthChanged != null)
                    Player.Local.Health.onHealthChanged.Invoke(Player.Local.Health.CurrentHealth);
            }

            if (loadedPlayerData.State.Hunger >= 0.90f)
                loadedPlayerData.State.healthData.Gluttony = Mathf.Clamp01(loadedPlayerData.State.healthData.Gluttony + 0.000032f);
            else if (loadedPlayerData.State.Hunger >= 0.80f && loadedPlayerData.State.Hunger < 0.90f)
                loadedPlayerData.State.healthData.Gluttony = Mathf.Clamp01(loadedPlayerData.State.healthData.Gluttony + 0.000015f);
            else if (loadedPlayerData.State.Hunger < 0.50f && loadedPlayerData.State.Hunger >= 0.30f)
                loadedPlayerData.State.healthData.Gluttony = Mathf.Clamp01(loadedPlayerData.State.healthData.Gluttony - 0.000015f);
            else if (loadedPlayerData.State.Hunger < 0.30f)
                loadedPlayerData.State.healthData.Gluttony = Mathf.Clamp01(loadedPlayerData.State.healthData.Gluttony - 0.000032f);

            if (!PlayerSingleton<PlayerMovement>.Instance.IsSprinting)
                sprintReserveSeconds = Mathf.Clamp(sprintReserveSeconds + sprintReserveRegenPerMinute, 0f, MaxSprintReserveSeconds);

            if (loadedPlayerData.State.Energy <= 0.2f)
            {
                if (!isSprintExhausted)
                {
                    Log("Set Exhausted Minpass");
                    isSprintExhausted = true;
                    PlayerSingleton<PlayerMovement>.Instance.AddSprintBlocker("Exhaustion");
                    if (PlayerSingleton<PlayerMovement>.Instance.IsSprinting)
                        PlayerSingleton<PlayerMovement>.Instance.IsSprinting = false;
                    if (PlayerSingleton<PlayerMovement>.Instance.ForceSprint)
                        PlayerSingleton<PlayerMovement>.Instance.ForceSprint = false;
                }
                
            }
            else if (loadedPlayerData.State.Energy > 0.2f)
            {
                if (isSprintExhausted && sprintReserveSeconds > 5f)
                {
                    Log("Remove Exhausted Minpass");
                    PlayerSingleton<PlayerMovement>.Instance.RemoveSprintBlocker("Exhaustion");
                    isSprintExhausted = false;
                }
            }

            bool inProperty = Player.Local.CurrentProperty != null;
            bool inBusiness = Player.Local.CurrentBusiness != null;
            bool inVehicle = Player.Local.IsInVehicle;
            bool inSewers = Singleton<SewerCameraPresense>.Instance.IsPointInSewerArea(Player.Local.CenterPointTransform.position);

            if (inProperty)
                minsInsideProperty++;
            else
                minsOutsideProperty++;

            WeatherConditions weather = NetworkSingleton<EnvironmentManager>.Instance._currentWeatherConditions;
            if (weather.Sunny > 0.5f && weather.Cloudy <= 0.49f && weather.Rainy <= 0.25f && weather.Stormy <= 0.25f)
                minsWithSunnyWeather++;

            float localPlayerAmbientTemp = 0f;
            if (inProperty)
                localPlayerAmbientTemp = Player.Local.CurrentProperty.AmbientTemperature;
            else if (inBusiness)
                localPlayerAmbientTemp = Player.Local.CurrentBusiness.AmbientTemperature;
            else if (inVehicle)
                localPlayerAmbientTemp = 20f;
            else if (inSewers)
                localPlayerAmbientTemp = UnityEngine.Random.Range(8f, 14f);
            else if (insideWarmBuilding)
                localPlayerAmbientTemp = UnityEngine.Random.Range(16f, 18.9f);
            else
                localPlayerAmbientTemp = OutsideTemperatureCelsius;

            // For sewers needs ac unit to survive down there basically
            if (inProperty && Player.Local.CurrentProperty.propertyCode == "seweroffice")
            {
                if (propertyHeaterCounts.ContainsKey("seweroffice"))
                {
                    int heaterCount = propertyHeaterCounts["seweroffice"].Count;
                    localPlayerAmbientTemp = Mathf.Lerp(Player.Local.CurrentProperty.AmbientTemperature, 20f, Mathf.Clamp01((float)heaterCount / 3f));
                }
            }

            bool tempSheds = false;
            if (localPlayerAmbientTemp <= 17f)
            {
                tempSheds = true;
                float tempDiff = 37f - localPlayerAmbientTemp;
                float currentHeatShed = tempDiff * TemperatureConsumption;
                currentHeatShed = Mathf.Lerp(currentHeatShed, TemperatureConsumptionPerMinutePerDegreeDiff, playerClothingAmount * 0.90f);
                SetTemperature(Mathf.Clamp01(loadedPlayerData.State.Temperature - currentHeatShed));
            }
            else if (loadedPlayerData.State.Temperature < 0.9f && localPlayerAmbientTemp > 17f)
            {
                SetTemperature(Mathf.Clamp01(loadedPlayerData.State.Temperature + TemperatureIncreasePerMinuteInside * (1f + playerClothingAmount)));
            }

            if (shouldParasympatheticSystemBeActive)
            {
                bool isPlayerMoving = (PlayerSingleton<PlayerMovement>.Instance.Movement.x > 0.01f || PlayerSingleton<PlayerMovement>.Instance.Movement.z > 0.01f);
                // Parasympathetic temperature decrease when full on food 
                if (loadedPlayerData.State.Temperature > 0.25f && !tempSheds && isPlayerMoving)
                    SetTemperature(loadedPlayerData.State.Temperature - ParasympatheticActiveTempDecreasePerMinute * 0.66f);
                else if (loadedPlayerData.State.Temperature > 0.25f && !tempSheds && !isPlayerMoving)
                    SetTemperature(loadedPlayerData.State.Temperature - ParasympatheticActiveTempDecreasePerMinute * 0.88f);
                else if (loadedPlayerData.State.Temperature > 0.25f && tempSheds && isPlayerMoving)
                    SetTemperature(loadedPlayerData.State.Temperature - ParasympatheticActiveTempDecreasePerMinute);
                else if (loadedPlayerData.State.Temperature > 0.25f && tempSheds && !isPlayerMoving)
                    SetTemperature(loadedPlayerData.State.Temperature - ParasympatheticActiveTempDecreasePerMinute * 1.33f);
            }

            EvaluatePlayerStatus();

            stateChanged = true;

            return;
        }

        public static void OnHourPass()
        {
            if (isSaving || !registered || isPassedOut || isQueuedForDeath || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress || haltExecution) return;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400) return;
            if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive) return;

            // Change world temp if config enabled, else 20c flat
            if (currentConfig.WorldTemperatureChanges)
            {
                float currentMin = minOutTempCelsius;
                float currentMax = maxOutTempCelsius;

                bool isDay = false;
                if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1600 || NetworkSingleton<TimeManager>.Instance.CurrentTime <= 700)
                {
                    bool pastNight = NetworkSingleton<TimeManager>.Instance.CurrentTime <= 400;
                    currentMax -= UnityEngine.Random.Range(1, 3);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1700 || pastNight)
                        currentMax -= UnityEngine.Random.Range(2, 4);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1900 || pastNight)
                        currentMax -= UnityEngine.Random.Range(2, 4);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 2100 || pastNight)
                        currentMax -= 5;
                    currentMax = Mathf.Clamp(currentMax, 8, maxOutTempCelsius);
                }
                else if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 700 && NetworkSingleton<TimeManager>.Instance.CurrentTime <= 1600)
                {
                    isDay = true;
                    currentMin += UnityEngine.Random.Range(1, 4);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 900)
                        currentMin += UnityEngine.Random.Range(2, 4);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1100)
                        currentMin += UnityEngine.Random.Range(3, 4);
                    if (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1300)
                        currentMin += 5;
                    currentMin = Mathf.Clamp(currentMin, minOutTempCelsius, 15);
                }

                if (currentMin >= currentMax)
                    currentMin = currentMax - 2;
                else if (currentMax <= currentMin)
                    currentMax = currentMin + 2;


                float lerpSpeed = 0.15f;
                // During sunrise, sunset lerp faster speed
                if ((NetworkSingleton<TimeManager>.Instance.CurrentTime >= 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime < 1200) || (NetworkSingleton<TimeManager>.Instance.CurrentTime >= 1700 && NetworkSingleton<TimeManager>.Instance.CurrentTime < 2100))
                    lerpSpeed = 0.26f;

                float hourTempTarget = Mathf.Lerp(OutsideTemperatureCelsius, UnityEngine.Random.Range(currentMin, currentMax), lerpSpeed / 3f);
                float weatherAdjusted = GetWeatherAdjustedWorldTemp(hourTempTarget, currentMin, currentMax);
                float dayTimeAdjusted = Mathf.Lerp(weatherAdjusted, isDay ? dayTempTarget : nightTempTarget, lerpSpeed / 1.66f);
                float old = OutsideTemperatureCelsius;
                OutsideTemperatureCelsius = Mathf.MoveTowards(old, dayTimeAdjusted, 2.5f);
            }
            else
            {
                OutsideTemperatureCelsius = 20f;
            }

            if (WorldTemperature != null)
            {
                string sign = currentConfig.FahrenheitTemp ? fahrenheitSign : celsiusSign;
                int temp = currentConfig.FahrenheitTemp ?
                    Mathf.RoundToInt(CelsiusToFahrenheit((float)OutsideTemperatureCelsius))
                    : Mathf.RoundToInt(OutsideTemperatureCelsius);
                WorldTemperature.text = $"{temp}{sign}";
            }

            playerClothingAmount = EvaluatePlayerClothing();
        }

        public static void EvaluatePlayerStatus()
        {
            if (loadedPlayerData.State.Thirst <= 0.01f)
            {
                causeOfDeath = "Dehydration";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
            }
            else if (loadedPlayerData.State.Hunger <= 0.01f)
            {
                causeOfDeath = "Starving";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
            }
            else if (loadedPlayerData.State.Temperature <= 0.01f)
            {
                causeOfDeath = "Hypothermia";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
            }
            else if (loadedPlayerData.State.Energy <= 0.01f && !isPassedOut && !isQueuedForDeath)
            {
                coros.Add(MelonCoroutines.Start(PrePlayerPassOut()));
            }
        }

        public static IEnumerator UpdateSliderState()
        {
            for (; ; )
            {
                yield return Wait05;
                if (!registered || (isQueuedForDeath && currentConfig.PermanentDeath))
                    yield break;

                if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive) continue;
                if (!stateChanged) continue;
                if (isSaving || Singleton<SaveManager>.Instance.IsSaving) continue;
                if (haltExecution) continue;

                stateChanged = false;
                if (ThirstSlider != null)
                    ThirstSlider.value = loadedPlayerData.State.Thirst;
                if (HungerSlider != null)
                    HungerSlider.value = loadedPlayerData.State.Hunger;
                if (EnergySlider != null)
                    EnergySlider.value = loadedPlayerData.State.Energy;
                if (TemperatureSlider != null)
                    TemperatureSlider.value = loadedPlayerData.State.Temperature;
            }
        }

        public static IEnumerator WhileSprinting()
        {
            for (; ; )
            {
                yield return Wait05;
                if (!registered || (isQueuedForDeath && currentConfig.PermanentDeath) || haltExecution)
                    yield break;

                if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive) continue;
                if (isSaving || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress) continue;

                if (PlayerSingleton<PlayerMovement>.Instance.IsSprinting)
                {
                    SetEnergy(Mathf.Clamp01(loadedPlayerData.State.Energy - extraEnergyConsumptionWhileRunning));
                    if (loadedPlayerData.State.Temperature < 0.8f)
                        SetTemperature(Mathf.Clamp01(loadedPlayerData.State.Temperature + extraTemperatureIncreaseWhileRunning));

                    sprintReserveSeconds = Mathf.Clamp(sprintReserveSeconds-0.5f, 0f, MaxSprintReserveSeconds);
                    if (!isSprintExhausted && sprintReserveSeconds < 0.5f)
                    {
                        Log("Set sprint exhausted");
                        isSprintExhausted = true;
                        PlayerSingleton<PlayerMovement>.Instance.IsSprinting = false;
                        PlayerSingleton<PlayerMovement>.Instance.AddSprintBlocker("Exhaustion");
                    }

                    if (PlayerSingleton<PlayerMovement>.Instance.IsGrounded && loadedPlayerData.State.healthData.IsLegBoneBroken && UnityEngine.Random.Range(0, 30) == 0)
                    {
                        if (loadedPlayerData.State.healthData.CurrentHP > 0f)
                        {
                            float damage = UnityEngine.Random.Range(5f, 10f);
                            if (Player.Local.Health.CurrentHealth - damage <= 0f)
                                causeOfDeath = $"Leg bone fracture";
                            Player.Local.Health.TakeDamage(damage, flinch: true, playBloodMist: false);
                            AppendDamageSource($"Leg bone damage (-{Mathf.RoundToInt(damage)}HP)");
                            coros.Add(MelonCoroutines.Start(FPRagdoll()));
                        }
                    }
                }
            }
        }

        public static IEnumerator FPRagdoll(bool waitSleepEnd = false, Func<bool> waitState = null, int forceDown = -1)
        {
            if (isFPRagdollActive) yield break;
            isFPRagdollActive = true;

            float originalNearClip = PlayerSingleton<PlayerCamera>.Instance.Camera.nearClipPlane;
            Vector3 origin1stPos = PlayerSingleton<PlayerCamera>.Instance.transform.localPosition;
            Quaternion origin1stRot = PlayerSingleton<PlayerCamera>.Instance.transform.localRotation;
            Transform parent = PlayerSingleton<PlayerCamera>.Instance.transform.parent;

            PlayerSingleton<PlayerMovement>.Instance.IsSprinting = false;
            PlayerSingleton<PlayerMovement>.Instance.AddSprintBlocker("LegDamage");

            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(false);
            PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement("FirstPersonRagdoll");
            PlayerSingleton<PlayerMovement>.Instance.CanMove = false;

            PlayerSingleton<PlayerCamera>.Instance.enabled = false;
            Player.Local.CapCol.enabled = false;
            
            float smoothInDur = 0.3f;
            float currentDur = 0f;
            Vector3 originPos = PlayerSingleton<PlayerCamera>.Instance.transform.localPosition;
            Quaternion originRot = PlayerSingleton<PlayerCamera>.Instance.transform.localRotation;

            bool bindToHeadEnabled = false;
            GameObject visPos = new("CamTrackPos");
            visPos.SetActive(false);
            GameObject visColl = new("CamBlockCollider");
            visColl.SetActive(false);

            BoxCollider bc = visColl.AddComponent<BoxCollider>();
            Rigidbody rb = visColl.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            visPos.transform.parent = Player.Local.Avatar.HeadBone;
            visPos.transform.localScale = Vector3.one;
            visPos.transform.localRotation = Quaternion.identity;
            visPos.transform.localPosition = new Vector3(0f, 0.001f, 0.002f);

            visColl.transform.parent = Player.Local.Avatar.HeadBone;
            visColl.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            visColl.transform.localRotation = Quaternion.identity;
            visColl.transform.localPosition = new Vector3(0f, 0.0001f, 0.0018f); 

            bc.providesContacts = true;
            bc.size = new Vector3(0.06f, 0.06f, 0.7f);
            bc.center = new Vector3(0f, 0.01f, 0f);
            visPos.layer = LayerMask.NameToLayer("Invisible");
            visPos.SetActive(true);
            visColl.layer = LayerMask.NameToLayer("Invisible");
            visColl.SetActive(true);

            IEnumerator WhileRagdolledBindCamToHead()
            {
                while (bindToHeadEnabled)
                {
                    yield return WaitFrame;
                    if (!registered || visPos == null) yield break;
                    PlayerSingleton<PlayerCamera>.Instance.transform.position = visPos.transform.position;
                    PlayerSingleton<PlayerCamera>.Instance.transform.rotation = visPos.transform.rotation;
                    yield return null;
                }
                yield break;
            }
            
            PlayerSingleton<PlayerCamera>.Instance.Camera.nearClipPlane = 0.001f;
            Vector3 worldStart = PlayerSingleton<PlayerCamera>.Instance.transform.position;
            Quaternion worldRot = PlayerSingleton<PlayerCamera>.Instance.transform.rotation;
            while (currentDur < smoothInDur)
            {
                if (!registered) yield break;
                currentDur += Time.deltaTime;
                float t = currentDur / smoothInDur;
                PlayerSingleton<PlayerCamera>.Instance.transform.position = Vector3.Lerp(worldStart, visPos.transform.position, t);
                PlayerSingleton<PlayerCamera>.Instance.transform.rotation = Quaternion.Slerp(worldRot, visPos.transform.rotation, t);
                yield return null;
            }
            bindToHeadEnabled = true;
            MelonCoroutines.Start(WhileRagdolledBindCamToHead());
            Player.Local.SetRagdolled(true);
            PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(false);
            Vector3 force = Vector3.zero;
            if (forceDown == 0)
                force = Vector3.down;
            else
                force = Player.Local.transform.root.transform.forward;

            Player.Local.Avatar.MiddleSpineRB.AddForce(force * 1.4f, ForceMode.VelocityChange);

            yield return Wait5;
            if (!registered) yield break;

            if (isQueuedForDeath && Player.PlayerList.Count == 1)
            {
                bindToHeadEnabled = false;
                GameObject.Destroy(visPos);
                GameObject.Destroy(visColl);
                yield break;
            }

            if (Player.PlayerList.Count > 1)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || Player.Local.Health.IsAlive);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || Player.Local.Health.IsAlive));
#endif
                yield return waitObj;
            }

            if (waitSleepEnd)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || !NetworkSingleton<TimeManager>.Instance.IsSleepInProgress);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || !NetworkSingleton<TimeManager>.Instance.IsSleepInProgress));
#endif
                yield return waitObj;
                yield return Wait5;
                yield return Wait1; // wait eyes open anim first
                if (!registered) yield break;
            }

            if (waitState != null)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !registered || waitState());
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || waitState()));
#endif
                yield return waitObj;
                yield return Wait1; // wait eyes open anim first
                if (!registered) yield break;
            }


            Player.Local.Avatar.Animation.PlayStandUpAnimation();
#if MONO
            WaitUntil stoodUp = new WaitUntil(() => !registered || !Player.Local.Avatar.Animation.StandUpAnimationPlaying);
#else
            WaitUntil stoodUp = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || !Player.Local.Avatar.Animation.StandUpAnimationPlaying));
#endif
            yield return stoodUp;
            if (!registered) yield break;
            bindToHeadEnabled = false;
            GameObject.Destroy(visPos);
            GameObject.Destroy(visColl);
            PlayerSingleton<PlayerCamera>.Instance.transform.localPosition = origin1stPos;
            PlayerSingleton<PlayerCamera>.Instance.transform.localRotation = origin1stRot;
            PlayerSingleton<PlayerCamera>.Instance.Camera.nearClipPlane = originalNearClip;
            PlayerSingleton<PlayerCamera>.Instance.enabled = true;
            PlayerSingleton<PlayerMovement>.Instance.CanMove = true;
            PlayerSingleton<PlayerCamera>.Instance.SetCanLook(true);
            Player.Local.CapCol.enabled = true;
            PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(true);
            PlayerSingleton<PlayerMovement>.Instance.RemoveSprintBlocker("LegDamage");
            Player.Local.SetRagdolled(false);
            PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement("FirstPersonRagdoll");
            isFPRagdollActive = false;
            yield break;
        }

        public static IEnumerator EnsureSystematicHomeostasis()
        {
            for (; ; )
            {
                yield return Wait2;
                if (!registered) yield break;
                if (!Player.Local.Health.IsAlive) continue;
                if (isQueuedForDeath) continue;
                if (isSaving || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress) continue;
                if (haltExecution) continue;

                if (!Mathf.Approximately(ThirstConsumptionPerMinute, DefaultThirstConsumption))
                    ThirstConsumptionPerMinute = Mathf.Lerp(ThirstConsumptionPerMinute, DefaultThirstConsumption, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(FoodConsumptionPerMinute, DefaultFoodConsumption))
                    FoodConsumptionPerMinute = Mathf.Lerp(FoodConsumptionPerMinute, DefaultFoodConsumption, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(EnergyConsumptionPerMinute, DefaultEnergyConsumption))
                    EnergyConsumptionPerMinute = Mathf.Lerp(EnergyConsumptionPerMinute, DefaultEnergyConsumption, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(TemperatureConsumption, TemperatureConsumptionPerMinutePerDegreeDiff))
                    TemperatureConsumption = Mathf.Lerp(TemperatureConsumption, TemperatureConsumptionPerMinutePerDegreeDiff, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(HealthRegenPerMinute, DefaultHealthRegeneration))
                    HealthRegenPerMinute = Mathf.Lerp(HealthRegenPerMinute, DefaultHealthRegeneration, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(loadedPlayerData.State.healthData.MaxHP, 100f))
                    loadedPlayerData.State.healthData.MaxHP = Mathf.Lerp(loadedPlayerData.State.healthData.MaxHP, 100f, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(loadedPlayerData.State.healthData.MoveSpeedScale, 1f))
                    loadedPlayerData.State.healthData.MoveSpeedScale = Mathf.Lerp(loadedPlayerData.State.healthData.MoveSpeedScale, 1f, SystematicHomeostasisSpeed);

                if (!Mathf.Approximately(sprintReserveRegenPerMinute, DefaultSprintReserveRegenPerMinute))
                    sprintReserveRegenPerMinute = Mathf.Lerp(sprintReserveRegenPerMinute, DefaultSprintReserveRegenPerMinute, SystematicHomeostasisSpeed);

            }
        }

        public static float EvaluatePlayerClothing()
        {
            int foundBody = 0;
            int foundAcc = 0;
            foreach (var setting in Player.Local.Avatar.CurrentSettings.BodyLayerSettings)
            {
                if (foundBody == maxBodyLayerClothes)
                    break;

                if (warmClothesPaths.Contains(setting.layerPath))
                {
                    foundBody++;
                    continue;
                }
            }

            foreach (var setting in Player.Local.Avatar.CurrentSettings.AccessorySettings)
            {
                if (foundAcc == maxBodyLayerClothes)
                    break;

                if (warmClothesPaths.Contains(setting.path))
                {
                    foundAcc++;
                    continue;
                }
            }

            return (float)(foundAcc + foundBody) / 6f;
        }

        public static void AppendDamageSource(string source)
        {
            lastDamageSources.Insert(0, source);
            if (lastDamageSources.Count >= 10)
            {
                lastDamageSources.RemoveAt(lastDamageSources.Count - 1);
            }
        }

        public static bool IsPointInsideBox(Vector3 playerPos, BoxCollider bc)
        {
            Vector3 posInBox = bc.transform.InverseTransformPoint(playerPos);
            posInBox -= bc.center;
            Vector3 boxExtent = bc.size * 0.5f;
            return Mathf.Abs(posInBox.x) <= boxExtent.x && Mathf.Abs(posInBox.y) <= boxExtent.y && Mathf.Abs(posInBox.z) <= boxExtent.z;
        }
        public static IEnumerator LazyUpdatePlayerWarmBuilding()
        {
            for (; ; )
            {
                yield return Wait10;
                if (!registered) yield break;
                if (isQueuedForDeath && currentConfig.PermanentDeath) yield break;

                if (!currentConfig.TemperatureRequired) continue;
                if (isPassedOut || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress || haltExecution) continue;

                if (Player.Local.CurrentProperty != null || Player.Local.CurrentBusiness != null) continue;

                bool inWeatherEclosure = false;
                bool foundBuilding = false;

                if (NetworkSingleton<EnvironmentManager>.Instance._weatherEnclosures != null)
                {
#if MONO
                    using (List<WeatherEnclosure>.Enumerator enumerator = NetworkSingleton<EnvironmentManager>.Instance._weatherEnclosures.GetEnumerator())
                    {
                        while(enumerator.MoveNext())
                        {
                            if (enumerator.Current.WithinEnclosure(Player.Local.CenterPointTransform.position, Vector3.zero, out float _, out float _))
                            {
                                inWeatherEclosure = true;
                                break;
                            }
                        }
                    }
#else
                    Il2CppSystem.Collections.Generic.List<WeatherEnclosure>.Enumerator enumerator = NetworkSingleton<EnvironmentManager>.Instance._weatherEnclosures.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.WithinEnclosure(Player.Local.CenterPointTransform.position, Vector3.zero, out float _, out float _))
                            {
                                inWeatherEclosure = true;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        enumerator.Dispose();
                    }
#endif
                }

                if (!inWeatherEclosure)
                {
                    // Check the 2 docks buildings under bridge that are not detectable by the WithinEnclosure function
                    float distNearest = 100f;

                    foreach (var kvp in warmBuildingsInRegion)
                    {
                        if (foundBuilding) break;
                        foreach (BoxCollider bc in kvp.Value)
                        {
                            float distToP = Vector3.Distance(Player.Local.CenterPointTransform.position, bc.transform.position);
                            if (distToP < distNearest)
                                distNearest = distToP;
                            if (distToP < 5f || IsPointInsideBox(Player.Local.CenterPointTransform.position, bc))
                            {
                                insideWarmBuilding = true;
                                string presence = distToP < 5f ? "near" : "";
                                presence += IsPointInsideBox(Player.Local.CenterPointTransform.position, bc) ? "+in box" : "";
                                Log($"Detected player presence in a warm building " + presence);
                                foundBuilding = true;
                                break;
                            }
                        }
                    }
                }

                if (!inWeatherEclosure && !foundBuilding)
                {
                    Log($"Player not inside a warm building in region ");
                    insideWarmBuilding = false;
                }
                else
                {
                    Log($"Detected player presence in a weather enclosure or warm building");
                    insideWarmBuilding = true;
                }

            }
        }

        public static void OnAudioSettingsChanged()
        {
            float sfxScaledVolume = Singleton<AudioManager>.Instance.GetVolume(EAudioType.FX, true);
            Log("Changing volume " + sfxScaledVolume);
            if (FlatlinePlayer.flatlinePlayerAudio != null)
                FlatlinePlayer.flatlinePlayerAudio.volume = sfxScaledVolume;

            if (FlatlineIngestibles.ingestorAudio != null)
                FlatlineIngestibles.ingestorAudio.volume = sfxScaledVolume;

            if (PlayerConsumeDamage.heartBeatSource != null)
                PlayerConsumeDamage.heartBeatSource.volume = sfxScaledVolume;
        }

        public static float GetWeatherAdjustedWorldTemp(float temp, float currentMin, float currentMax)
        {
            WeatherConditions weather = NetworkSingleton<EnvironmentManager>.Instance._currentWeatherConditions;
            float total = weather.Sunny + weather.Cloudy + weather.Windy + weather.Foggy + weather.Rainy + weather.Stormy + weather.Sleet + weather.Hail + weather.Snowy;
            float time = (float)NetworkSingleton<TimeManager>.Instance.CurrentTime;
            float sunPower = (time > 600 && time < 1800) ? 1f : 0f;
            float sunnyWeight = (weather.Sunny * sunPower) / total;
            float moistureWeight = (weather.Foggy + weather.Rainy + weather.Stormy) / total;
            float windCloudWeight = (weather.Cloudy + weather.Windy) / total;
            float icyWeight = (weather.Snowy + weather.Sleet + weather.Hail) / total;
            float weatherTemp = temp;
            weatherTemp = Mathf.Lerp(weatherTemp, currentMax, sunnyWeight * (weather.Sunny * sunPower));
            weatherTemp = Mathf.Lerp(weatherTemp, currentMin, moistureWeight * 0.25f);
            weatherTemp = Mathf.Lerp(weatherTemp, currentMin, windCloudWeight * 0.33f);
            weatherTemp = Mathf.Lerp(weatherTemp, currentMin, icyWeight * 0.53f);
            return weatherTemp;
        }

        public static void SetWater(float val)
        {
            if (!currentConfig.WaterRequired)
                return;
            
            loadedPlayerData.State.Thirst = val;
            return;
        }
        public static void SetFood(float val)
        {
            if (!currentConfig.FoodRequired)
                return;

            loadedPlayerData.State.Hunger = val;

            return;
        }
        public static void SetEnergy(float val)
        {
            if (!currentConfig.EnergyRequired)
                return;

            loadedPlayerData.State.Energy = val;
            return;
        }
        public static void SetTemperature(float val)
        {
            if (!currentConfig.TemperatureRequired)
                return;

            loadedPlayerData.State.Temperature = val;
            return;
        }
    }


    // Disable minpass recover health
    [HarmonyPatch(typeof(PlayerHealth), "MinPass")]
    public static class PlayerHealth_MinPass_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerHealth __instance)
        {
            return false;
        }
    }

    // Harmony patch player impacts to apply disease
    // Also fixes a bug where during bedrot player can deal damage to their own collider
    [HarmonyPatch(typeof(Player), "ReceiveImpact")]
    public static class Player_ReceiveImpact_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Impact impact)
        {
            if (haltExecution) return true;

            if (__instance.NetworkObject == impact.ImpactSource)
            {
                return false;
            }

            FlatlinePlayer.AppendDamageSource($"{impact.ImpactType} impact damage (-{Mathf.RoundToInt(impact.ImpactDamage)}HP)");
            if (Player.Local.Health.CurrentHealth - impact.ImpactDamage <= 0f)
            {
                causeOfDeath = $"{impact.ImpactType} impact";
            }
            else
            {
                try
                {
                    CalculateImpact(impact);
                }
                catch (Exception ex)
                {
                    Log("Failed to process impact: " + ex, "Player_ReceiveImpact_Patch");
                }
            }
            return true;
        }
    }

    // Harmony patch consume product to apply disease and handle extreme drug effects
    [HarmonyPatch(typeof(Player), "ConsumeProduct")]
    public static class Player_ConsumeProduct_Patch
    {
        private static string cukePseudoProductID = string.Empty;
        private static string energyDrinkPseudoProductID = string.Empty;

        public static void AssignPseudoProductIDs()
        {
            if (cukePseudoProductID == string.Empty || energyDrinkPseudoProductID == string.Empty)
            {
                Func<string, ItemDefinition> GetItem;
#if MONO
                GetItem = ScheduleOne.Registry.GetItem;
#else
                GetItem = Il2CppScheduleOne.Registry.GetItem;
#endif
                ItemDefinition energyDrinkDef = GetItem("energydrink");
                ItemDefinition cukeItemDef = GetItem("cuke");

                ItemInstance energyItemInstance = energyDrinkDef.GetDefaultInstance(1);
                ItemInstance cukeItemInstance = cukeItemDef.GetDefaultInstance(1);

                Equippable_Cuke energyEquippable = null;
                Equippable_Cuke cukeEquippable = null;
#if MONO
                energyEquippable = energyItemInstance.Equippable as Equippable_Cuke;
                cukeEquippable = cukeItemDef.Equippable as Equippable_Cuke;
#else
                energyEquippable = energyItemInstance.Equippable.TryCast<Equippable_Cuke>();
                cukeEquippable = cukeItemDef.Equippable.TryCast<Equippable_Cuke>();
#endif
                if (energyEquippable != null)
                    energyDrinkPseudoProductID = energyEquippable.PseudoProduct.ID;
                else
                    Log("Failed to cast energy drink equippable", "Player_ConsumeProduct_Patch");

                if (cukeEquippable != null)
                    cukePseudoProductID = cukeEquippable.PseudoProduct.ID;
                else
                    Log("Failed to cast cuke drink equippable", "Player_ConsumeProduct_Patch");

            }
            return;
        }

        [HarmonyPrefix]
        public static bool Prefix(Player __instance, ProductItemInstance product)
        {
            if (haltExecution) return true;
            if (!ingestibleModuleInitiated) return true;
            if (!setupCompleted) return true;

            if (Player.PlayerList.Count > 1 && !__instance.IsLocalPlayer) return true;

            if (product.ID == cukePseudoProductID)
            {
                ProcessIngestible("cuke");
                return true;
            }
            else if (product.ID == energyDrinkPseudoProductID)
            {
                ProcessIngestible("energydrink");
                return true;
            }

#if MONO
            ProductDefinition defBase = product.Definition as ProductDefinition;
#else
            ProductDefinition defBase = product.Definition.TryCast<ProductDefinition>();
#endif
            if (defBase == null)
            {
                Log("Failed to cast product definition", "Player_ConsumeProduct_Patch");
                return true;
            }
            int drugTypeInt = (int)defBase.DrugType; // because il2 stripping sometimes fails enum strings
            Log("Process drug type int: " + drugTypeInt);
            switch (drugTypeInt)
            {
                case 0:
                    ProcessIngestible("weed", product.Quality, true);
                    break;

                case 1:
                    ProcessIngestible("meth", product.Quality, true);
                    break;

                case 2:
                    ProcessIngestible("cocaine", product.Quality, true);
                    break;

                case 3:
                    Log("MDMA Drug Type Not Implemented", "Player_ConsumeProduct_Patch");
                    break;

                case 4:
                    ProcessIngestible("shroom", product.Quality, true);
                    break;

                case 5:
                    Log("Heroin Drug Type Not Implemented", "Player_ConsumeProduct_Patch");
                    break;

                default:
                    break;
            }

            return true;
        }
    }

    // Harmony patch to delay player death for effects
    [HarmonyPatch(typeof(PlayerHealth), "TakeDamage")]
    public static class PlayerHealth_TakeDamage_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerHealth __instance, float damage, bool flinch = true, bool playBloodMist = true)
        {
            if (haltExecution) return true;
            if (!setupCompleted) return true;
            if (Player.PlayerList.Count > 1 && !__instance.Player.IsLocalPlayer) return true;

            if (FlatlinePlayer.isQueuedForDeath || FlatlinePlayer.isPassedOut) return false;
            if (__instance.CurrentHealth - damage <= 0f)
            {
                coros.Add(MelonCoroutines.Start(FlatlinePlayer.PrePlayerDied()));
                return false;
            }
            return true;
        }
    }

    // Harmony patch to reduce energy for each punch
    [HarmonyPatch(typeof(PunchController), "Punch")]
    public static class PunchController_Punch_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PunchController __instance, float power)
        {
            if (!registered) return true;
            if (haltExecution) return true;
            if (!setupCompleted) return true;
            if (Player.PlayerList.Count > 1 && !__instance.player.IsLocalPlayer) return true;

            if (loadedPlayerData.State.Energy > 0.02f)
            {
                float energyReduction = Mathf.Lerp(FlatlinePlayer.PunchEnergyConsumption * 0.7f, FlatlinePlayer.PunchEnergyConsumption * 4f, Mathf.Clamp01(power));
                loadedPlayerData.State.Energy -= energyReduction;
            }
            return true;
        }
    }

    // Harmony patch to reduce energy for each jump
    [HarmonyPatch(typeof(PlayerMovement), "Jump")]
    public static class PlayerMovement_Jump_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerMovement __instance)
        {
            if (!registered) return true;
            if (haltExecution) return true;
            if (!setupCompleted) return true;

            if (Player.PlayerList.Count > 1 && !__instance.Player.IsLocalPlayer) return true;

            if (loadedPlayerData.State.Energy > 0.02f)
            {
                loadedPlayerData.State.Energy -= FlatlinePlayer.JumpEnergyConsumption;
            }
            return true;
        }
    }
    
}