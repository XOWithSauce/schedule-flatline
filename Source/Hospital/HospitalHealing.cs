
using MelonLoader;
using UnityEngine;
using System.Collections;

using static Flatline.DebugModule;
using static Flatline.Flatline;
using static Flatline.PlayerDiseaseDamage;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.VoiceOver;
using ScheduleOne.Map;

#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.CharacterClasses;
using Il2CppScheduleOne.VoiceOver;
using Il2CppScheduleOne.Map;

#endif

namespace Flatline
{
    public static class HospitalHealing
    {
        public static Lisa lisaNPC;
        public static Irene ireneNPC;

        // diseaseID, created choice obj
        public static Dictionary<string, DialogueController.DialogueChoice> lisaCureChoices = new();
        public static Dictionary<string, DialogueController.DialogueChoice> ireneCureChoices = new();

        public static bool hospitalChoiceInitialized = false;

        public static Vector3 hospitalDoorPoint;

        public static readonly Dictionary<string, float> cureCosts = new()
        {
            { "cancer", 80000f },
            { "bonebreak", 18000f },
            { "bleed", 30000f },
            { "fever", 8000f },
            { "depression", 4000f },
        };

        public static void InitHospitalHealing()
        {
            lisaNPC = UnityEngine.Object.FindObjectOfType<Lisa>(true);
            ireneNPC = UnityEngine.Object.FindObjectOfType<Irene>(true);
            if (lisaNPC == null || ireneNPC == null)
            {
                Log("Failed to init hospital healing module");
                return;
            }

            hospitalDoorPoint = Singleton<Map>.Instance.MedicalCentre.RespawnPoint.position;

            foreach (var kvp in cureCosts)
            {
                DialogueController.DialogueChoice newChoiceLisa = AddDiseaseCureChoice(lisaNPC, kvp.Key);
                lisaCureChoices.Add(kvp.Key, newChoiceLisa);
                DialogueController.DialogueChoice newChoiceIrene = AddDiseaseCureChoice(ireneNPC, kvp.Key);
                ireneCureChoices.Add(kvp.Key, newChoiceIrene);
            }
            hospitalChoiceInitialized = true;
            return;
        }

        public static void ResetHospitalHealing()
        {
            hospitalChoiceInitialized = false;
            lisaNPC = null;
            ireneNPC = null;
            lisaCureChoices.Clear();
            ireneCureChoices.Clear();
        }

        public static DialogueController.DialogueChoice AddDiseaseCureChoice(NPC npc, string diseaseID)
        {
            DialogueController controller = npc.DialogueHandler.gameObject.GetComponent<DialogueController>();
            DialogueController.DialogueChoice choice = new();
            choice.ChoiceText = $"Cure {diseaseID} <color=#FF3008>-${cureCosts[diseaseID]}</color>";
            choice.Enabled = false;

            void OnCureChoosen()
            {
                coros.Add(MelonCoroutines.Start(OnCureChoiceChoosen(controller, choice, diseaseID)));
            }
            choice.onChoosen.AddListener((UnityEngine.Events.UnityAction)OnCureChoosen);

            controller.AddDialogueChoice(choice);
            return choice;
        }

        public static IEnumerator OnCureChoiceChoosen(DialogueController controller, DialogueController.DialogueChoice choice, string diseaseID)
        {
            controller.handler.ContinueSubmitted();
            yield return Wait05;
            if (!registered) yield break;

            if (Vector3.Distance(controller.npc.CenterPoint, hospitalDoorPoint) > 7f)
            {
                controller.handler.WorldspaceRend.ShowText($"I'm off duty. Ask for the charge nurse at the hospital.", 10f);
                controller.npc.PlayVO(EVOLineType.Annoyed, false);
                yield break;
            }

            Disease targetDisease = null;
            foreach (Disease disease in allDiseases)
            {
                if (string.Equals(disease.data.DiseaseID, diseaseID) && disease.data.Active)
                {
                    targetDisease = disease;
                    break;
                }
            }
            if (targetDisease == null)
            {
                Log("Failed to find the target disease");
            }

            bool canAfford = true;

            float paid = cureCosts[diseaseID];

            float cash = NetworkSingleton<MoneyManager>.Instance.cashBalance;
            float bank = NetworkSingleton<MoneyManager>.Instance.onlineBalance;
            if (cash + bank < paid)
                canAfford = false;

            if (!canAfford)
            {
                controller.handler.WorldspaceRend.ShowText($"Sorry, but you can't afford the treatment.", 10f);
                controller.npc.PlayVO(EVOLineType.No, false);
                yield break;
            }

            if (targetDisease.data.DiseaseID == "cancer" && targetDisease.data.Progression >= 4)
            {
                controller.handler.WorldspaceRend.ShowText($"Sorry, but your cancer is terminal...", 10f);
                controller.npc.PlayVO(EVOLineType.Concerned, false);
                yield break;
            }

            bool useCash = false;
            bool useBank = false;
            float bankRemainder = 0f;

            if (cash >= paid)
                useCash = true;
            else
            {
                useCash = true;
                useBank = true;
                bankRemainder = paid - cash;
            }

            if (useCash && useBank)
            {
                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-cash, true, false);
                NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction("Removed online balance", -bankRemainder, 1f, "Flatline mod medical fee");
            }
            else
                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-paid, true, false);

            targetDisease.data.HealState = 1f;
            Log("Disease cured: " + diseaseID);

            controller.handler.WorldspaceRend.ShowText($"You're all patched up!", 5f);
            controller.npc.PlayVO(EVOLineType.Thanks, false);
            choice.Enabled = false;

            yield break;
        }

      
    }
}
