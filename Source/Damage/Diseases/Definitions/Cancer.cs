using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;
using static Flatline.ConfigLoader;

#if MONO
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne.PlayerScripts;
#endif

namespace Flatline
{

    public class Cancer : Disease
    {
        public static readonly float MaximumMaxHPReduction = 100f;

        public Cancer(DiseaseData data)
        {
            this.data = data;
            this.data.DiseaseID = "cancer";
            base.minsRequiredForProgression = 60 * 24 * 7;
        }

        public override void DiseaseEffect()
        {
            float severityAdjustedMins = Mathf.Round((float)this.data.MinsSinceDiseaseStart * (1f + this.data.Severity));
            float sevMinsDay = (float)base.minsRequiredForProgression * (1f + this.data.Severity);

            if (severityAdjustedMins / (float)base.minsRequiredForProgression >= this.data.Progression)
                this.data.Progression++;

            if (this.data.Progression >= 5)
            {
                Log("Player died of cancer");
                causeOfDeath = "Cancer";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                this.data.Active = false;
                return;
            }

            if (this.data.Progression >= 3)
            {
                // max hp cap / minsPerDay * severity adjusted days max / speed so that overtime player loses all hp towards progression 5
                float maxProgDays = (sevMinsDay * 5f) / 1440f;
                float progDaysUntil3 = (sevMinsDay * 3f) / 1440f;
                float progDaysAfter3 = maxProgDays - progDaysUntil3;
                float maxHPReduction = MaximumMaxHPReduction / 1440f * progDaysAfter3 / 10f;
                // Now = max hp reduction * max prog days * 144 * max prog level (5) = ~MaximumMaxHPReduction

                Log("Cancer Max HP reduction: " + maxHPReduction);
                if ((loadedPlayerData.State.healthData.MaxHP - maxHPReduction) < loadedPlayerData.State.healthData.CurrentHP)
                {
                    float damage = loadedPlayerData.State.healthData.CurrentHP - (loadedPlayerData.State.healthData.MaxHP - maxHPReduction);
                    if (Player.Local.Health.CurrentHealth - damage <= 0f)
                        causeOfDeath = $"Cancer";
                    Player.Local.Health.TakeDamage(damage, flinch: true, playBloodMist: false);
                    AppendDamageSource($"Cancer damage (-{damage}HP)");
                }

                loadedPlayerData.State.healthData.MaxHP = Mathf.Clamp(loadedPlayerData.State.healthData.MaxHP - maxHPReduction, 0f, 100f);
            }

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
                if (this.data.Progression == 2 && UnityEngine.Random.Range(0f, 1f) > 0.99f)
                    flatlinePlayerAudio.PlayOneShot(clip);
                else if (this.data.Progression >= 3 && UnityEngine.Random.Range(0f, 1f) > 0.95f)
                    flatlinePlayerAudio.PlayOneShot(clip);
            }
            return;
        }

        public override void DiseaseHealed()
        {
            Log("Healed cancer succesfully");
            return;
        }

        public override void UpdateDiseaseData()
        {
            return;
        }
    }

}