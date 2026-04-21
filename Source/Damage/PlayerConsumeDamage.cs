

using MelonLoader;
using System.Collections;
using UnityEngine;
using HarmonyLib;

using static Flatline.DebugModule;
using static Flatline.Flatline;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.FlatlinePlayer;
using static Flatline.FlatlineUIModule;
using static Flatline.ConfigLoader;

#if MONO
using ScheduleOne;
using ScheduleOne.Core;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using ScheduleOne.UI;
using ScheduleOne.GameTime;
#else
using MelonLoader.Support;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Core;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.FX;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.GameTime;
#endif
namespace Flatline
{
    public static class PlayerConsumeDamage
    {
        public static readonly List<string> trackedKeys = new()
        {
            "cocaine", "meth", "shroom", "weed", "paracetamol", "addy", "chili"
        };

        #region Drugs
        public static readonly int CocaineLifeTimeHrs = 8;
        public static readonly float cocaineSystemicAmountPerDose = 0.08f;
        public static readonly float cocaineConsumeLungDamage = 0.004f;

        public static readonly int MethLifeTimeHrs = 12;
        public static readonly float methSystemicAmountPerDose = 0.11f;
        public static readonly float methConsumeLungDamage = 0.0005f;

        public static readonly int ShroomsLifeTimeHrs = 6;
        public static readonly float shroomSystemicAmountPerDose = 0.06f;

        public static readonly float weedConsumeLungDamage = 0.009f;
        #endregion

        #region Ingestibles
        public static readonly float paracetamolLiverDamage = 0.005f;

        public static readonly int ParacetamolLifeTimeHrs = 18;

        public static readonly int AddyLifeTimeHrs = 14;

        public static readonly int ChiliLifeTimeHrs = 50;
        #endregion

        public static readonly Dictionary<string, int> lifeTimeHours = new()
        {
            { "cocaine", CocaineLifeTimeHrs },
            { "meth", MethLifeTimeHrs },
            { "shroom", ShroomsLifeTimeHrs },
            { "paracetamol", ParacetamolLifeTimeHrs },
            { "addy", AddyLifeTimeHrs },
            { "chili", ChiliLifeTimeHrs }
        };

        #region Additional effect states
        public static bool ProductEffectRunning = false;
        public static bool IsWeedEffectRunning = false;
        public static bool IsShroomEffectRunning = false;
        public static bool IsMethEffectRunning = false;
        public static bool IsCocaineEffectRunning = false;

        #endregion

        public static AudioSource heartBeatSource = null;
        public static bool heartPumpOverrideActive = false;

        public enum EConsumeType
        {
            None, Weed, Meth, Cocaine, Shroom, Paracetamol, Addy, Chili
        }

        public static void InitConsumeDamageModule()
        {
            GameObject audioObject = new GameObject("FlatlineHeartAudio");
            audioObject.transform.parent = PlayerSingleton<PlayerCamera>.Instance.transform;
            audioObject.transform.localPosition = Vector3.zero;
            heartBeatSource = audioObject.AddComponent<AudioSource>();
            heartBeatSource.volume = 0.7f;
            heartBeatSource.loop = false;
            audioObject.SetActive(true);
            heartBeatSource.enabled = true;
        }

        public static void ResetConsumeDamageModule()
        {
            ProductEffectRunning = false;
            IsWeedEffectRunning = false;
            IsShroomEffectRunning = false;
            IsMethEffectRunning = false;
            IsCocaineEffectRunning = false;
            heartBeatSource = null;
            heartPumpOverrideActive = false;
        }

        public static EConsumeType TypeFromID(string id)
        {
            switch (id)
            {
                case "weed": return EConsumeType.Weed;
                case "meth": return EConsumeType.Meth;
                case "cocaine": return EConsumeType.Cocaine;
                case "shroom": return EConsumeType.Shroom;
                case "paracetamol": return EConsumeType.Paracetamol;
                case "addy": return EConsumeType.Addy;
                case "chili": return EConsumeType.Chili;
                default: return EConsumeType.None;
            }
        }

        public static void OnProductConsumed(EConsumeType type, EQuality quality = EQuality.Trash, bool useQuality = false)
        {
            if (!currentConfig.DrugSideEffects) return;

            if (type == EConsumeType.None)
            {
                return;
            }

            float systemicMult = 1f;
            float damageMult = 1f;
            if (useQuality)
            {
                int totalQualities = Enum.GetNames(typeof(EQuality)).Length - 1;
                systemicMult = Mathf.Lerp(1f, 2f, (float)quality / totalQualities);
                damageMult = Mathf.Lerp(2f, 1f, (float)quality / totalQualities);
                coros.Add(MelonCoroutines.Start(WaitRandomAfterConsume(type, quality)));
            }

            string key = type.ToString().ToLower();
            if (!loadedPlayerData.State.consumptionDatas.ContainsKey(key))
                loadedPlayerData.State.consumptionDatas.Add(key, new ConsumptionData());

            switch (type)
            {
                case EConsumeType.Weed:
                    loadedPlayerData.State.consumptionDatas[key].overtimeLungDamage += weedConsumeLungDamage * damageMult;
                    loadedPlayerData.State.healthData.TimesSmoked++;
                    break;

                case EConsumeType.Meth:
                    loadedPlayerData.State.consumptionDatas[key].overtimeLungDamage += methConsumeLungDamage * damageMult;
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += methSystemicAmountPerDose * systemicMult;
                    loadedPlayerData.State.healthData.TimesSmoked++;
                    coros.Add(MelonCoroutines.Start(CurrentProductMonitor()));
                    coros.Add(MelonCoroutines.Start(StimulantEffectChanger()));
                    coros.Add(MelonCoroutines.Start(HeartPumpOverride("meth")));
                    break;

                case EConsumeType.Cocaine:
                    loadedPlayerData.State.consumptionDatas[key].overtimeLungDamage += cocaineConsumeLungDamage * damageMult;
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += cocaineSystemicAmountPerDose * systemicMult;
                    coros.Add(MelonCoroutines.Start(CurrentProductMonitor()));
                    coros.Add(MelonCoroutines.Start(StimulantEffectChanger()));
                    coros.Add(MelonCoroutines.Start(HeartPumpOverride("cocaine")));
                    break;

                case EConsumeType.Shroom:
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += shroomSystemicAmountPerDose * systemicMult;
                    coros.Add(MelonCoroutines.Start(CurrentProductMonitor()));
                    coros.Add(MelonCoroutines.Start(ShroomEffectChanger()));
                    break;

                case EConsumeType.Paracetamol:
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += IngestibleParacetamol.SystemicAmountPerDose;
                    loadedPlayerData.State.consumptionDatas[key].overtimeLiverDamage += paracetamolLiverDamage;
                    float maxHp = loadedPlayerData.State.healthData.MaxHP;
                    // Paracetamol can heal the permanent maxhp cap damage but consuming too many can kill
                    if (!Mathf.Approximately(maxHp, 100f))
                        loadedPlayerData.State.healthData.MaxHP = Mathf.Clamp(maxHp + IngestibleParacetamol.MaxHPRegen, 1f, 100f);
                    break;

                case EConsumeType.Addy:
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += IngestibleAddy.SystemicAmountPerDose;
                    break;

                case EConsumeType.Chili:
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem += IngestibleChili.SystemicAmountPerDose;
                    break;
            }
            Log("Changed consumption values succesfully");

            return;
        }

        public static IEnumerator WaitRandomAfterConsume(EConsumeType type, EQuality quality)
        {
            int randomWaitMins = UnityEngine.Random.Range(3, 6);
            for (int i = 0; i < randomWaitMins ; i++)
            {
                yield return Wait60;
                if (!registered || isQueuedForDeath || haltExecution) yield break;
            }

            if (isSaving)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !isSaving);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !isSaving));
#endif
                yield return waitObj;
            }
            if (!registered || isQueuedForDeath || haltExecution) yield break;

            Log("Evaluate after effects for drug");
            if ((type == EConsumeType.Meth || type == EConsumeType.Cocaine) && quality < EQuality.Heavenly && UnityEngine.Random.Range(0f, 1f) > 0.80f)
            {
                Player.Local.Avatar.Effects.TriggerSick(false);
                if (loadedPlayerData.State.Hunger > 0.1f)
                    SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger * 0.95f));

                if (loadedPlayerData.State.Thirst > 0.1f)
                    SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst * 0.92f));
            }
            else if (type == EConsumeType.Weed && quality >= EQuality.Standard && UnityEngine.Random.Range(0f, 1f) > 0.20f)
            {
                if (loadedPlayerData.State.Hunger > 0.1f)
                    SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger * 0.75f));

                if (loadedPlayerData.State.Thirst > 0.1f)
                    SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst * 0.75f));
            }

            CalculateDepressionAfterConsumeProbability(type, quality);
            yield break;
        }

        public static void OnHourPass()
        {
            if (!registered || isQueuedForDeath || haltExecution) return;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400) return;
            coros.Add(MelonCoroutines.Start(SystemicDecay()));
            return;
        }

        public static IEnumerator SystemicDecay()
        {
            if (isSaving)
            {
#if MONO
                WaitUntil waitObj = new WaitUntil(() => !isSaving);
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !isSaving));
#endif
                yield return waitObj;
            }

            List<string> keys = new(loadedPlayerData.State.consumptionDatas.Keys);
            foreach (string key in keys)
            {
                float current = loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem;
                if (current > 0)
                {
                    int itemLifeTime = lifeTimeHours[key];
                    float result = DecayHalfLife(current, itemLifeTime);
                    if (result < 0.0001f)
                        result = 0f;
                    loadedPlayerData.State.consumptionDatas[key].currentAmountInSystem = result;
                }
            }

            if (loadedPlayerData.State.consumptionDatas.Any(x => x.Value.currentAmountInSystem >= 1f))
            {
                KeyValuePair<string, ConsumptionData> data = loadedPlayerData.State.consumptionDatas.First(x => x.Value.currentAmountInSystem >= 1f);
                Log("Player consumed lethal dose of: " + data.Key);
                causeOfDeath = $"Lethal dose of {data.Key}";
                Player.Local.Avatar.Effects.TriggerSick(false);
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                if (Player.PlayerList.Count > 1)
                    loadedPlayerData.State.consumptionDatas[data.Key].currentAmountInSystem = 0f;
                yield break;
            }

            if (loadedPlayerData.State.consumptionDatas.Any(x => x.Value.overtimeLiverDamage >= 1f))
            {
                KeyValuePair<string, ConsumptionData> data = loadedPlayerData.State.consumptionDatas.First(x => x.Value.overtimeLiverDamage >= 1f);
                Log("Players liver failed due to: " + data.Key);
                causeOfDeath = $"Liver failure from consumption of {data.Key}";
                Player.Local.Avatar.Effects.TriggerSick(false);
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                if (Player.PlayerList.Count > 1)
                    loadedPlayerData.State.consumptionDatas[data.Key].overtimeLiverDamage = 0f;
                yield break;
            }

            if (loadedPlayerData.State.consumptionDatas.Any(x => x.Value.overtimeLungDamage >= 1f))
            {
                KeyValuePair<string, ConsumptionData> data = loadedPlayerData.State.consumptionDatas.First(x => x.Value.overtimeLungDamage >= 1f);
                Log("Players lungs failed due to: " + data.Key);
                causeOfDeath = $"Pulmonary embolism from smoking {data.Key}";
                Player.Local.Avatar.Effects.TriggerSick(false);
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                if (Player.PlayerList.Count > 1)
                    loadedPlayerData.State.consumptionDatas[data.Key].overtimeLungDamage = 0f;
                yield break;
            }

        }

        public static float DecayHalfLife(float current, int lifeTimeHrs)
        {
            float decayed = current * Mathf.Pow(0.5f, 1f / (lifeTimeHrs));
            return Mathf.Clamp(decayed, 0f, 2f);
        }

        public static IEnumerator CurrentProductMonitor()
        {
            if (ProductEffectRunning) yield break;
            ProductEffectRunning = true;
            yield return Wait025;
            ProductItemInstance current = Player.Local.ConsumedProduct;
            if (current != null)
            {
                bool CanContinue()
                {
                    return !registered || isQueuedForDeath || Player.Local.ConsumedProduct == null || Player.Local.ConsumedProduct != current || haltExecution;
                }
#if MONO
                WaitUntil waitObj = new WaitUntil(() => CanContinue());
#else
                WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => CanContinue()));
#endif
                yield return waitObj;
            }

            Log("Current product has ended, disable");
            ProductEffectRunning = false;
            IsWeedEffectRunning = false;
            IsShroomEffectRunning = false;
            IsMethEffectRunning = false;
            IsCocaineEffectRunning = false;
        }

        public static IEnumerator ShroomEffectChanger()
        {
            if (IsShroomEffectRunning) yield break;
            IsShroomEffectRunning = true;

            float amplitudeMax = 0.5f;
            float amplitudeMin = 0.19f;

            float blendMax = 0.4f;
            float blendMin = 0.016f;

            float noiseScaleMax = 50f;
            float noiseScaleMin = 15f;

            float startAmount = 0f;
            if (loadedPlayerData.State.consumptionDatas.TryGetValue("shroom", out ConsumptionData startData))
            {
                startAmount = Mathf.Clamp01(startData.currentAmountInSystem);
            }
            float targetAmp = Mathf.Lerp(amplitudeMin, amplitudeMax, startAmount) * UnityEngine.Random.Range(0.95f, 1.05f);
            float targetBld = Mathf.Lerp(blendMin, blendMax, startAmount) * UnityEngine.Random.Range(0.95f, 1.05f);
            float targetNoi = Mathf.Lerp(noiseScaleMin, noiseScaleMax, startAmount) * UnityEngine.Random.Range(0.95f, 1.05f);

            PsychedelicFullScreenFeature.MaterialProperties activeProperties = Singleton<PostProcessingManager>.Instance.GetActivePsychedelicEffectProperties();

            PsychedelicFullScreenFeature.MaterialProperties source = activeProperties.Clone();

            PsychedelicFullScreenData targetProperties = Singleton<PostProcessingManager>.Instance.GetPsychedelicEffectDataPreset("Active");

            PsychedelicFullScreenFeature.MaterialProperties targetMaterialProperties = targetProperties.ConvertToMaterialProperties();
            targetMaterialProperties.Blend = targetBld;
            targetMaterialProperties.NoiseScale = targetNoi;
            targetMaterialProperties.Amplitude = targetAmp;

            Singleton<EnvironmentFX>.Instance.SetEnvironmentScrollingActive(true);
            Singleton<PostProcessingManager>.Instance.SetPsychedelicEffectActive(true);

            float easeInTime = 2f;
            float currentEase = 0f;
            while (currentEase < easeInTime)
            {
                if (!registered) yield break;
                currentEase += Time.deltaTime;
                float tEase = currentEase / easeInTime;
                activeProperties.Blend = Mathf.Lerp(source.Blend, targetMaterialProperties.Blend, tEase);
                Singleton<EnvironmentFX>.Instance.SetEnvironmentScrollingSpeedByPercentage(Mathf.Lerp(0f, 1f, tEase));
                yield return null;
            }
            Singleton<PostProcessingManager>.Instance.SetPsychedelicEffectProperties(targetMaterialProperties);


            float sleepTime = 5f;
            float sleepCurrent = 0f;

            bool shouldBreak = false;
            for (; ; )
            {
                if (shouldBreak) break;

                while (sleepCurrent < sleepTime)
                {
                    sleepCurrent += Time.deltaTime;
                    if (!registered) yield break;
                    if (!ProductEffectRunning || !IsShroomEffectRunning)
                    {
                        shouldBreak = true;
                        break;
                    }
                    yield return null;
                }
                sleepCurrent = 0f;
                if (shouldBreak) break;

                if (isSaving) continue;

                if (loadedPlayerData.State.consumptionDatas.TryGetValue("shroom", out ConsumptionData shroomData))
                {
                    float currentSystematicAmount = Mathf.Clamp01(shroomData.currentAmountInSystem);
                    if (currentSystematicAmount > 0.75f && UnityEngine.Random.Range(0f, 1f) > 0.98f)
                    {
                        Player.Local.Avatar.Effects.TriggerSick(false);
                        SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger - 0.05f));
                        SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst - 0.02f));
                    }

                    targetAmp = Mathf.Lerp(amplitudeMin, amplitudeMax, currentSystematicAmount);
                    targetBld = Mathf.Lerp(blendMin, blendMax, currentSystematicAmount);
                    targetNoi = Mathf.Lerp(noiseScaleMin, noiseScaleMax, currentSystematicAmount);

                    PsychedelicFullScreenFeature.MaterialProperties temp = Singleton<PostProcessingManager>.Instance.GetActivePsychedelicEffectProperties().Clone();

                    targetAmp = Mathf.Clamp(targetAmp, temp.Amplitude - amplitudeMax / 10f, temp.Amplitude + amplitudeMax / 10f);
                    targetBld = Mathf.Clamp(targetBld, temp.Blend - blendMax / 10f, temp.Blend + blendMax / 10f);
                    targetNoi = Mathf.Clamp(targetNoi, temp.NoiseScale - noiseScaleMax / 10f, temp.NoiseScale + noiseScaleMax / 10f);

                    float random1 = UnityEngine.Random.Range(0f, 1f);
                    float random2 = UnityEngine.Random.Range(0f, 1f);
                    float random3 = UnityEngine.Random.Range(0f, 1f);
                    bool useBlend = false;
                    bool useNoise = false;
                    bool useAmplitude = false;
                    List<int> opts = new List<int>() { 0, 1, 2 };
#if MONO
                    opts.Shuffle();
#else
                    int n = opts.Count;
                    while (n > 1)
                    {
                        n--;
                        int k = UnityEngine.Random.Range(0, n + 1);
                        int value = opts[k];
                        opts[k] = opts[n];
                        opts[n] = value;
                    }
#endif
                    for (int i = 0; i < 2; i++)
                    {
                        if (opts[i] == 0 && random1 > 0.33)
                            useBlend = true;
                        if (opts[i] == 1 && random2 > 0.33)
                            useNoise = true;
                        if (opts[i] == 2 && random3 > 0.33)
                            useAmplitude = true;
                    }

                    if (useBlend || useNoise || useAmplitude)
                    {
                        currentEase = 0f;
                        while (currentEase < easeInTime)
                        {
                            if (!registered) yield break;
                            if (!ProductEffectRunning || !IsShroomEffectRunning)
                            {
                                shouldBreak = true;
                                break;
                            }
                            currentEase += Time.deltaTime;
                            float tEase = currentEase / easeInTime;
                            float smoothT = Mathf.SmoothStep(0f, 1f, tEase);
                            if (useBlend)
                                activeProperties.Blend = Mathf.Lerp(temp.Blend, targetBld, smoothT);

                            if (useNoise)
                                activeProperties.NoiseScale = Mathf.Lerp(temp.NoiseScale, targetNoi, smoothT);

                            if (useAmplitude)
                                activeProperties.Amplitude = Mathf.Lerp(temp.Amplitude, targetAmp, smoothT);

                            yield return null;
                        }
                        if (shouldBreak) break;
                        targetMaterialProperties = activeProperties.Clone();
                        Singleton<PostProcessingManager>.Instance.SetPsychedelicEffectProperties(targetMaterialProperties);
                        sleepTime = UnityEngine.Random.Range(5f, 10f);
                        easeInTime = UnityEngine.Random.Range(2f, 6f);
                    }

                }
            }

            Log("Shroom effect changer ended");
            IsShroomEffectRunning = false;
            yield break;
        }

        public static IEnumerator StimulantEffectChanger()
        {
            if (IsCocaineEffectRunning || IsMethEffectRunning) yield break;
            IsCocaineEffectRunning = true;
            IsMethEffectRunning = true;

            yield return Wait2;
            if (!registered) yield break;

            float sleepTime = 2f;
            float sleepCurrent = 0f;

            bool shouldBreak = false;

            for (; ; )
            {
                if (shouldBreak) break;
                while (sleepCurrent < sleepTime)
                {
                    sleepCurrent += Time.deltaTime;
                    if (!registered) yield break;
                    if (!ProductEffectRunning || !IsMethEffectRunning || !IsCocaineEffectRunning)
                    {
                        shouldBreak = true;
                        break;
                    }
                    yield return null;
                }
                sleepCurrent = 0f;
                if (shouldBreak) break;

                float stimulantAmount = 0f;
                if (loadedPlayerData.State.consumptionDatas.TryGetValue("meth", out ConsumptionData methData))
                {
                    if (methData.currentAmountInSystem > 0.001f)
                        stimulantAmount = methData.currentAmountInSystem;
                }
                if (loadedPlayerData.State.consumptionDatas.TryGetValue("cocaine", out ConsumptionData cocaineData))
                {
                    if (cocaineData.currentAmountInSystem > 0.001f && cocaineData.currentAmountInSystem > stimulantAmount)
                        stimulantAmount = cocaineData.currentAmountInSystem;
                }

                
                stimulantAmount = Mathf.Clamp01(stimulantAmount);

                if (stimulantAmount > 0.75f && UnityEngine.Random.Range(0f, 1f) > 0.98f)
                {
                    Player.Local.Avatar.Effects.TriggerSick(false);
                    SetFood(Mathf.Clamp01(loadedPlayerData.State.Hunger - 0.05f));
                    SetWater(Mathf.Clamp01(loadedPlayerData.State.Thirst - 0.02f));
                }


                float threshold = Mathf.Lerp(0.90f, 0.75f, stimulantAmount);
                if (UnityEngine.Random.Range(0f, 1f) < threshold) continue;

                float upperRotBoundary = Mathf.Lerp(1.15f, 2f, stimulantAmount);
                float rotAmount = UnityEngine.Random.Range(2f, 6f) * upperRotBoundary;
                rotAmount = UnityEngine.Random.Range(0, 2) == 0 ? -rotAmount : rotAmount;

                for (int i = 0; i < UnityEngine.Random.Range(4, 8); i++)
                {
                    yield return Wait05;
                    if (!registered) yield break;
                    if (!ProductEffectRunning || !IsMethEffectRunning || !IsCocaineEffectRunning)
                    {
                        shouldBreak = true;
                        break;
                    }
                    if (UnityEngine.Random.Range(0f, 1f) > 0.55f)
                    {
                        float lerpTime = 0.5f;
                        float currentTime = 0f;
                        Quaternion orig = Player.Local.transform.root.rotation;
                        while (currentTime < lerpTime)
                        {
                            if (!registered) yield break;
                            if (!ProductEffectRunning || !IsMethEffectRunning || !IsCocaineEffectRunning)
                            {
                                shouldBreak = true;
                                break;
                            }
                            currentTime += Time.deltaTime;
                            float t = currentTime / lerpTime;
                            Player.Local.transform.root.rotation = Quaternion.Lerp(orig, Quaternion.Euler(0f, orig.y + rotAmount, 0f), t);
                        }
                        if (shouldBreak) break;
                    }

                    if (i % 2 == 0)
                    {
                        PlayerInventory inv = PlayerSingleton<PlayerInventory>.Instance;
                        if (!GameInput.IsTyping && !Singleton<PauseMenu>.Instance.IsPaused && inv.HotbarEnabled)
                        {
                            int randomIndex = UnityEngine.Random.Range(0, 8);
                            if (randomIndex != inv.EquippedSlotIndex)
                            {
                                if (inv.EquippedSlotIndex != -1)
                                {
                                    inv.IndexAllSlots(inv.EquippedSlotIndex).Unequip();
                                }
                                inv.PreviousEquippedSlotIndex = inv.EquippedSlotIndex;
                                inv.EquippedSlotIndex = randomIndex;
                                inv.Equip(inv.IndexAllSlots(randomIndex));
                                PlayerSingleton<ViewmodelSway>.Instance.RefreshViewmodel();
                            }
                        }
                    }
                }

                if (shouldBreak) break;
            }



            IsCocaineEffectRunning = false;
            IsMethEffectRunning = false;
            Log("Stimulant effect changer ended");
            yield break;
        }

        public static IEnumerator HeartPumpOverride(string consumedID)
        {
            if (heartPumpOverrideActive) yield break;
            heartPumpOverrideActive = true;
            Log("Starting heart pump override");

            bool CanContinue(ConsumptionData data)
            {
                return PlayerSingleton<PlayerCamera>.Instance.HeartbeatSoundController._sound._audioSource.volume == 0f
                    && Mathf.Clamp01(data.currentAmountInSystem) > 0.55f
                    && !(isQueuedForDeath || isPassedOut) 
                    && ProductEffectRunning;
            }

            if (!loadedPlayerData.State.consumptionDatas.TryGetValue(consumedID, out ConsumptionData data))
            {
                Log("No match for id in consumption data");
                heartPumpOverrideActive = false;
                yield break;
            }

            if (!CanContinue(data))
            {
                heartPumpOverrideActive = false;
                yield break;
            }

            float maxTotalElapsed = 120f;
            float totalElapsed = 0f;

            int maxBeatsPerMinute = 200;
            int minBeatsPerMinute = 70;
            int beatsPerMinute = Mathf.RoundToInt(Mathf.Lerp(minBeatsPerMinute, maxBeatsPerMinute, Mathf.Clamp01(data.currentAmountInSystem)));
            Log("Setting bpm to " + beatsPerMinute);

            float elapsedSinceLast = 0f;
            float beatsPerSecond = (float)beatsPerMinute / 60f;
            float secondsForBeat = 1f / beatsPerSecond;
            for (; ; )
            {
                if (!registered) yield break;
                if (!CanContinue(data)) break;
                if (totalElapsed >= maxTotalElapsed) break;
                
                yield return Wait01;
                totalElapsed += 0.1f;
                elapsedSinceLast += 0.1f;
                if (!(elapsedSinceLast >= secondsForBeat))
                    continue;

                if (heartBeatSource != null && !heartBeatSource.isPlaying)
                    heartBeatSource.PlayOneShot(loadedAudios["singleheartbeat"]);

                if (Mathf.RoundToInt(totalElapsed) % 5 == 0) // Update bpm every 5 sec
                {
                    beatsPerMinute = Mathf.RoundToInt(Mathf.Lerp(minBeatsPerMinute, maxBeatsPerMinute, Mathf.Clamp01(data.currentAmountInSystem)));
                    beatsPerSecond = (float)beatsPerMinute / 60f;
                    secondsForBeat = 1f / beatsPerSecond;
                }
            }
            Log("Finished heart pump override");

            heartPumpOverrideActive = false;
            yield break;
        }

    }

    // Take over the shrooms visual
    [HarmonyPatch(typeof(ShroomInstance), "DoPsychedlicEffectBlend")]
    public static class ShroomInstance_DoPsychedlicEffectBlend_Patch
    {
        [HarmonyPrefix]
#if MONO
        public static bool Prefix(ShroomInstance __instance, ref IEnumerator __result, PsychedelicFullScreenFeature.MaterialProperties targetMaterialProperties, float targetValuePercentage, float duration)
#else
        public static bool Prefix(ShroomInstance __instance, ref Il2CppSystem.Collections.IEnumerator __result, PsychedelicFullScreenFeature.MaterialProperties targetMaterialProperties, float targetValuePercentage, float duration)
#endif
        {
            if (!currentConfig.DrugSideEffects) return true;

            if (targetValuePercentage > 0f)
            {
#if MONO
                __result = EmptyCoroutine();
#else
                // From the melon loader dependencies / support modules / il2cpp dll contains castable class MonoEnumeratorWrapper
                MonoEnumeratorWrapper il2Coro = new MonoEnumeratorWrapper(EmptyCoroutine());
                __result = il2Coro.Cast<Il2CppSystem.Collections.IEnumerator>();
#endif
                return false;
            }
            else
                return true;
        }

        // Because the function above must return and assign a coroutine on return false
        public static IEnumerator EmptyCoroutine()
        {
            yield break;
        }

    }

}