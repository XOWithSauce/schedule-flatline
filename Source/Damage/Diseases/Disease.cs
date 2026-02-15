using System.Collections;
using UnityEngine;

using static Flatline.Flatline;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;
using static Flatline.HospitalHealing;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.NPCs;
using static ScheduleOne.Dialogue.DialogueController;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.NPCs;
using static Il2CppScheduleOne.Dialogue.DialogueController;
#endif

namespace Flatline
{
    public abstract class Disease
    {
        // about 34 days to full heal through just parasympathetic heal
        public readonly float ParasympatheticHealing = 0.0002f;
        public readonly float MaximumSystematicPropertyChangePerTick = 1f / ((60f * 24f) / 10f);
        public DiseaseData data;
        public object diseaseCoroutine;
        public int minsRequiredForProgression { get; set; }
        public Action onDiseaseStarted;

        public abstract void DiseaseEffect();
        public abstract void DiseaseHealed();
        public abstract void UpdateDiseaseData();

        public IEnumerator DiseaseEvaluator()
        {
            diseaseIcons[this.data.DiseaseID].SetActive(true);

            if (this.onDiseaseStarted != null)
            {
                this.onDiseaseStarted.Invoke();
            }

            while (this.data.Active)
            {
                yield return Wait10;
                if (isQueuedForDeath && !currentConfig.PermanentDeath && Player.PlayerList.Count > 1) continue;
                if (!registered || isQueuedForDeath) yield break;
                if (isSaving || isPassedOut || NetworkSingleton<TimeManager>.Instance.IsSleepInProgress || haltExecution) continue;
                if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive) continue;
                if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400) continue;

                if (this.data.DiseaseID != "cancer" && loadedPlayerData.State.Hunger > 0.9f)
                    this.data.HealState += ParasympatheticHealing;

                if (this.data.HealState >= 1f)
                    break;

                if (hospitalChoiceInitialized)
                {
                    UpdateChoiceState(lisaNPC, lisaCureChoices);
                    UpdateChoiceState(ireneNPC, ireneCureChoices);
                }

                this.data.MinsSinceDiseaseStart += 10;

                DiseaseEffect();
            }

            this.data.Active = false;
            diseaseIcons[this.data.DiseaseID].SetActive(false);
            DiseaseHealed();

            if (hospitalChoiceInitialized)
            {
                UpdateChoiceState(lisaNPC, lisaCureChoices);
                UpdateChoiceState(ireneNPC, ireneCureChoices);
            }

            this.diseaseCoroutine = null;
            yield break;
        }

        private void UpdateChoiceState(NPC npc, Dictionary<string, DialogueChoice> choices)
        {
            if (npc != null && choices.TryGetValue(this.data.DiseaseID, out var choice))
            {
                choice.Enabled = Vector3.Distance(npc.CenterPoint, hospitalDoorPoint) <= 7f && this.data.Active && this.data.HealState < 1f;
            }
        }

    }

}