using System.Collections;
using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
#endif

namespace Flatline
{

    public class Bleeding : Disease
    {
        public static readonly float passiveDiseaseHealingMax = 0.003f;
        public static readonly float passiveDiseaseHealingMin = 0.0001f;

        public float MaximumMaxHPReduction = 100f;
        private float maxHPReduced = 0f;

        public Bleeding(DiseaseData data)
        {
            this.data = data;
            this.data.DiseaseID = "bleed";
            base.minsRequiredForProgression = 60 * 4;
            base.onDiseaseStarted = BleedingStarted;
            MaximumMaxHPReduction = Mathf.Lerp(40f, 150f, this.data.Severity / 0.3f);

            if (this.data.DiseaseStates == null)
                this.data.DiseaseStates = new();

            if (this.data.DiseaseStates.ContainsKey("maxHPReduced"))
            {
                // For when it was loaded from save data
                this.maxHPReduced = this.data.DiseaseStates["maxHPReduced"];
            }
            else
            {
                // Initiated new disease add key
                this.data.DiseaseStates.Add("maxHPReduced", 0f);
            }
        }

        private void BleedingStarted()
        {
            PlayerDiseaseDamage.isBleedingStemmed = false;
            if (!PlayerDiseaseDamage.bleedPressureIntObj.activeSelf)
                PlayerDiseaseDamage.bleedPressureIntObj.SetActive(true);
            coros.Add(MelonCoroutines.Start(RunBleedDamage()));
        }

        private IEnumerator RunBleedDamage()
        {
            for (; ; )
            {
                float waitTime = 0f;
                if (this.data.Progression >= 3)
                {
                    yield return Wait05;
                    waitTime = 0.5f;
                }
                else
                {
                    yield return Wait1;
                    waitTime = 1f;
                }

                if (!registered || !this.data.Active || this.data.Progression >= 5)
                    yield break;

                if (haltExecution) continue;

                if (isSaving || FlatlinePlayer.isPassedOut || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress) continue;

                if (FlatlinePlayer.isQueuedForDeath)
                    yield break;

                if (maxHPReduced >= MaximumMaxHPReduction)
                    yield break;

                // So it feels like bleeding has stopped at late stage but might start bleeding
                // again if the player stops pressuring
                if (this.data.HealState > 0.90f)
                    continue;

                if (PlayerDiseaseDamage.isBleedingStemmed)
                    continue;

                float sevMinsDay = (float)base.minsRequiredForProgression * (1f + this.data.Severity);
                float maxProgDays = (sevMinsDay * 5f) / 1440f;
                float progDaysUntil1 = (sevMinsDay * 1f) / 1440f;
                float progDaysAfter1 = maxProgDays - progDaysUntil1;
                float maxHPReduction = MaximumMaxHPReduction / 1440f * progDaysAfter1 / waitTime;

                // so progression increases dmg, healstate decreases dmg
                // Towards prog 5 set the lerp t to be closer to 0 so that max hp reduction is at highest possible
                float healAmnt = Mathf.Approximately(this.data.HealState, 0f) ? 0.015f : this.data.HealState;
                float t = Mathf.Lerp(healAmnt, 0.001f, ((float)this.data.Progression / 5f) - (1f / 5f));
                maxHPReduction = Mathf.Clamp01(Mathf.Lerp(maxHPReduction, 0f, t)) * this.data.Progression;
                Log("Calc bleed reduction: " + maxHPReduction);
                if (UnityEngine.Random.Range(0f, 1f) > 0.33f)
                    Player.Local.Health.PlayBloodMist();

                if ((loadedPlayerData.State.healthData.MaxHP - maxHPReduction) < loadedPlayerData.State.healthData.CurrentHP)
                {
                    float damage = loadedPlayerData.State.healthData.CurrentHP - (loadedPlayerData.State.healthData.MaxHP - maxHPReduction);
                    if (Player.Local.Health.CurrentHealth - damage <= 0f)
                        causeOfDeath = $"Bleeding";
                    Player.Local.Health.TakeDamage(damage, flinch: UnityEngine.Random.Range(0, 10) == 0, playBloodMist: false);
                    AppendDamageSource($"Bleeding damage (-{damage}HP)");
                }
                else
                {
                    Log("Bleed amount not smaller than current hp");
                }

                maxHPReduced += maxHPReduction;
                loadedPlayerData.State.healthData.MaxHP = Mathf.Clamp(loadedPlayerData.State.healthData.MaxHP - maxHPReduction, 0f, 100f);
            }
        }

        public override void DiseaseEffect()
        {
            float minsSinceStart = (float)this.data.MinsSinceDiseaseStart;

            if (!PlayerDiseaseDamage.bleedPressureIntObj.activeSelf)
                PlayerDiseaseDamage.bleedPressureIntObj.SetActive(true);

            // so that whenever the bleeding would normally kill player if they hold it stemmed it might still heal
            if (PlayerDiseaseDamage.isBleedingStemmed)
                minsSinceStart *= 0.33f;

            float bleedAdjustedMins = Mathf.Round(minsSinceStart * (1f + this.data.Severity));

            if (bleedAdjustedMins / (float)base.minsRequiredForProgression >= this.data.Progression)
                this.data.Progression++;

            if (this.data.Progression >= 5 && maxHPReduced >= MaximumMaxHPReduction && !PlayerDiseaseDamage.isBleedingStemmed)
            {
                Log("Player died of bleeding");
                causeOfDeath = "Bleeding";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                this.data.Active = false;
                return;
            }

            if (this.data.Progression >= 4 && loadedPlayerData.State.Thirst > 0.15f)
            {
                loadedPlayerData.State.Thirst -= 0.0015f;
            }

            this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax);

            if (maxHPReduced >= MaximumMaxHPReduction) // get on with it quicker
                this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin * 3f, passiveDiseaseHealingMax * 3f);

            // To make stemming bleeding healing more effective overtime and blood clots faster under pressure
            float multiplier = Mathf.Lerp(12f, 24f, Mathf.Clamp01(this.data.HealState));
            float progMultiplier = Mathf.Lerp(1f, 3f, Mathf.Clamp01((float)this.data.Progression / 5f));
            if (PlayerDiseaseDamage.isBleedingStemmed)
                this.data.HealState += passiveDiseaseHealingMax * multiplier;
            else // Not stemmed reduce healing overtime
                this.data.HealState = Mathf.Clamp01(this.data.HealState - passiveDiseaseHealingMax * progMultiplier);

            Log("Bleeding Max HP reduction total so far: " + maxHPReduced);
        }

        public override void DiseaseHealed()
        {
            if (PlayerDiseaseDamage.bleedPressureIntObj.activeSelf)
                PlayerDiseaseDamage.bleedPressureIntObj.SetActive(false);
            PlayerDiseaseDamage.isBleedingStemmed = false;
            Log("Healed bleeding succesfully");
            return;
        }

        public override void UpdateDiseaseData()
        {
            if (this.data.DiseaseStates == null)
                this.data.DiseaseStates = new();

            if (this.data.DiseaseStates.ContainsKey("maxHPReduced"))
            {
                // Update the data when save is pending
                this.data.DiseaseStates["maxHPReduced"] = this.maxHPReduced;
            }
            else
            {
                // Edge case where key does not exist somehow
                this.data.DiseaseStates.Add("maxHPReduced", this.maxHPReduced);
            }
            return;
        }
    }

}