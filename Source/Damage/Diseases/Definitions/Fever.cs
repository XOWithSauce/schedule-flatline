using System.Collections;
using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;
using static Flatline.ConfigLoader;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.FX;
#endif


namespace Flatline
{
    public class Fever : Disease
    {
        public static readonly float baseEnergyConsumptionIncrease = 0.00007f;
        public static readonly float baseTemperatureConsumptionIncrease = 0.00025f;
        public static readonly float baseThirstConsumptionIncrease = 0.00012f;
        public static readonly float passiveDiseaseHealingMax = 0.0055f;
        public static readonly float passiveDiseaseHealingMin = 0.0003f;

        public static readonly float MaximumMaxHPReduction = 33f;
        private int minsSinceLastFeverShiver = 0;
        private int minsUntilNextFeverShiver = 120;

        public Fever(DiseaseData data)
        {
            this.data = data;
            this.data.DiseaseID = "fever";
            base.minsRequiredForProgression = 60 * 18;
            base.onDiseaseStarted = FeverStarted;
        }

        private void FeverStarted()
        {
            Player.Local.Avatar.Effects.SetSicklySkinColor(true);
            Player.Local.Avatar.EmotionManager.AddEmotionOverride("Concerned", "Sickly", 0f, 5);
        }

        public override void DiseaseEffect()
        {
            minsSinceLastFeverShiver += 10;
            Log("Mins until next fever jitter " + (minsUntilNextFeverShiver - minsSinceLastFeverShiver));
            float severityAdjustedMins = Mathf.Round((float)this.data.MinsSinceDiseaseStart * (1f + this.data.Severity));
            float sevMinsDay = (float)base.minsRequiredForProgression * (1f + this.data.Severity);

            if (severityAdjustedMins / (float)base.minsRequiredForProgression >= this.data.Progression)
                this.data.Progression++;

            if (this.data.Progression >= 5)
            {
                Log("Player died of fever");
                causeOfDeath = "Fever";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                this.data.Active = false;
                return;
            }

            this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax);
            if (BedRotSimulator.isBedrotting)
                this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin * 5f, passiveDiseaseHealingMax * 5f);

            if (PlayerSingleton<PlayerMovement>.Instance.IsSprinting)
                this.data.HealState = Mathf.Clamp01(this.data.HealState - UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax * 2f));

            if (!flatlinePlayerAudio.isPlaying)
            {
                AudioClip clip = null;
                if (Player.Local.Avatar.CurrentSettings.Gender > 0.5f)
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                        clip = loadedAudios["femalecough"];
                    else
                        clip = loadedAudios["femalesneeze"];
                }
                else
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                        clip = loadedAudios["malecough"];
                    else
                        clip = loadedAudios["malesneeze"];
                }
                if (this.data.Progression == 1 && UnityEngine.Random.Range(0f, 1f) > 0.96f)
                    flatlinePlayerAudio.PlayOneShot(clip);
                else if (this.data.Progression == 2 && UnityEngine.Random.Range(0f, 1f) > 0.94f)
                    flatlinePlayerAudio.PlayOneShot(clip);
                else if (this.data.Progression == 3 && UnityEngine.Random.Range(0f, 1f) > 0.92f)
                    flatlinePlayerAudio.PlayOneShot(clip);
                else if (this.data.Progression >= 4 && UnityEngine.Random.Range(0f, 1f) > 0.87f)
                    flatlinePlayerAudio.PlayOneShot(clip);
            }

            if (this.data.Progression >= 1)
            {
                float energyTarget = Mathf.Clamp(EnergyConsumptionPerMinute + Mathf.Clamp(baseEnergyConsumptionIncrease * this.data.Progression * (1f + this.data.Severity), 0f, 0.0009f), 0f, DefaultEnergyConsumption * 3f);
                EnergyConsumptionPerMinute = Mathf.Lerp(EnergyConsumptionPerMinute, energyTarget, MaximumSystematicPropertyChangePerTick * 2f);

                float tempTarget = Mathf.Clamp(TemperatureConsumption + Mathf.Clamp(baseTemperatureConsumptionIncrease * this.data.Progression * (1f + this.data.Severity), 0f, 0.0015f), 0f, TemperatureConsumptionPerMinutePerDegreeDiff * 3f);
                TemperatureConsumption = Mathf.Lerp(TemperatureConsumption, tempTarget, MaximumSystematicPropertyChangePerTick * 3f);

                float thirstTarget = Mathf.Clamp(ThirstConsumptionPerMinute + Mathf.Clamp(baseThirstConsumptionIncrease * this.data.Progression * (1f + this.data.Severity), 0f, 0.0015f), 0f, DefaultThirstConsumption * 3f);
                ThirstConsumptionPerMinute = Mathf.Lerp(ThirstConsumptionPerMinute, thirstTarget, MaximumSystematicPropertyChangePerTick * 3f);
            }

            if (this.data.Progression >= 2 && this.data.HealState < 0.8f && loadedPlayerData.State.healthData.MaxHP > (100f - MaximumMaxHPReduction))
            {
                // max hp cap / minsPerDay * severity adjusted days max / speed so that overtime player loses max 33 hp towards prog 5
                float maxProgDays = (sevMinsDay * 5f) / 1440f;
                float progDaysUntil2 = (sevMinsDay * 2f) / 1440f;
                float progDaysAfter2 = maxProgDays - progDaysUntil2;
                float maxHPReduction = MaximumMaxHPReduction / 1440f * progDaysAfter2 / 10f;
                // Now = max hp reduction * max prog days * 144 * max prog level (5) = ~MaximumMaxHPReduction
                // Then lerp towards 0 based on heal state
                maxHPReduction = Mathf.Clamp01(Mathf.Lerp(maxHPReduction, 0f, this.data.HealState));

                Log("Fever Max HP reduction: " + maxHPReduction);
                if ((loadedPlayerData.State.healthData.MaxHP - maxHPReduction) < loadedPlayerData.State.healthData.CurrentHP)
                {
                    float damage = loadedPlayerData.State.healthData.CurrentHP - (loadedPlayerData.State.healthData.MaxHP - maxHPReduction);
                    if (Player.Local.Health.CurrentHealth - damage <= 0f)
                        causeOfDeath = $"Fever";
                    Player.Local.Health.TakeDamage(damage, flinch: false, playBloodMist: false);
                    AppendDamageSource($"Fever damage (-{damage}HP)");
                }

                loadedPlayerData.State.healthData.MaxHP = Mathf.Clamp(loadedPlayerData.State.healthData.MaxHP - maxHPReduction, 10f, 100f);
            }

            if (minsSinceLastFeverShiver >= minsUntilNextFeverShiver)
            {
                coros.Add(MelonCoroutines.Start(BlurShort()));
                minsSinceLastFeverShiver = 0;
                switch (this.data.Progression)
                {
                    case 1:
                        minsUntilNextFeverShiver = UnityEngine.Random.Range(100, 200);
                        break;

                    case 2:
                        minsUntilNextFeverShiver = UnityEngine.Random.Range(80, 180);
                        break;

                    case 3:
                        minsUntilNextFeverShiver = UnityEngine.Random.Range(70, 120);
                        break;

                    case 4:
                        minsUntilNextFeverShiver = UnityEngine.Random.Range(50, 100);
                        break;

                    default:
                        minsUntilNextFeverShiver = UnityEngine.Random.Range(80, 360);
                        break;
                }
            }
        }

        public IEnumerator BlurShort()
        {
            if (!registered || isPassedOut || isQueuedForDeath) yield break;
            Log("Start blur");
            float dur = Mathf.Lerp(2f, 5f, (float)this.data.Progression / 5f);
            float current = 0f;
            PlayerSingleton<PlayerCamera>.Instance.FoVChangeSmoother.AddOverride(-8f, 5, "fever");
            PlayerSingleton<PlayerCamera>.Instance.SmoothLookSmoother.AddOverride(0.8f, 5, "fever");
            if (this.data.Progression > 3)
                Player.Local.Disoriented = true;
            Player.Local.Seizure = true;
            Singleton<PostProcessingManager>.Instance.SetBlur(1f);

            yield return Wait5;
            if (!registered) yield break;

            Player.Local.Seizure = false;
            if (this.data.Progression > 3 && Player.Local.Disoriented)
                Player.Local.Disoriented = false;
            Singleton<PostProcessingManager>.Instance.SetBlur(0f);
            PlayerSingleton<PlayerCamera>.Instance.FoVChangeSmoother.RemoveOverride("fever");
            PlayerSingleton<PlayerCamera>.Instance.SmoothLookSmoother.RemoveOverride("fever");
            Log("End blur");
            yield break;
        }

        public override void DiseaseHealed()
        {
            if (EnergyConsumptionPerMinute > DefaultEnergyConsumption)
                EnergyConsumptionPerMinute = Mathf.Lerp(EnergyConsumptionPerMinute, DefaultFoodConsumption, 0.5f);

            if (ThirstConsumptionPerMinute > DefaultThirstConsumption)
                ThirstConsumptionPerMinute = Mathf.Lerp(ThirstConsumptionPerMinute, DefaultThirstConsumption, 0.5f);

            if (TemperatureConsumption > TemperatureConsumptionPerMinutePerDegreeDiff)
                TemperatureConsumption = Mathf.Lerp(TemperatureConsumption, TemperatureConsumptionPerMinutePerDegreeDiff, 0.5f);

            Player.Local.Avatar.Effects.SetSicklySkinColor(false);
            Player.Local.Avatar.EmotionManager.RemoveEmotionOverride("Sickly");

            Log("Healed fever succesfully");
            return;
        }
        public override void UpdateDiseaseData()
        {
            return;
        }
    }

}