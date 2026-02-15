

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.PropertyTemperatureController;

#if MONO
using ScheduleOne.Building.Doors;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Messages;
using ScheduleOne.GameTime;
using TMPro;
#else
using Il2CppScheduleOne.Building.Doors;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.FX;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.GameTime;
using Il2CppTMPro;
#endif

namespace Flatline
{

    public static class DepressionSimulator
    {
        public static GameObject disabledOverlay;

        public static void InitiateDepressionSimulatorModule()
        {
            disabledOverlay = new("DisableOverlay");

            disabledOverlay.transform.SetParent(PlayerSingleton<MessagesApp>.Instance.transform, false);
            RectTransform disableRt = disabledOverlay.AddComponent<RectTransform>();
            disableRt.sizeDelta = new Vector2(800f, 1200f);
            disableRt.localScale = Vector3.one;
            disableRt.localPosition = Vector3.zero;
            disableRt.localRotation = Quaternion.identity;

            Image image = disabledOverlay.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.93f);

            GameObject disableText = new("Text");
            disableText.transform.SetParent(disabledOverlay.transform);
            disableText.transform.localScale = Vector3.one;
            disableText.transform.localPosition = Vector3.zero;
            disableText.transform.localRotation = Quaternion.identity;
            RectTransform textRt = disableText.AddComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(600f, 1200f);
            TextMeshProUGUI textComp = disableText.AddComponent<TextMeshProUGUI>();
            textComp.text = "You are too depressed right now";
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.fontSize = 52f;
            disabledOverlay.SetActive(false);
            Log("Initiated depression simulator");
        }

        public static void ResetDepressionSimulatorModule()
        {
            disabledOverlay = null;
        }

        public static bool ShouldSkip()
        {
            if (Player.PlayerList.Count > 1 && !Player.Local.Health.IsAlive)
                return true;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400)
                return true;
            if (isSaving)
                return true;
            if (haltExecution)
                return true;
            else
                return false;
        }

        public static IEnumerator DepressionBlandWorld(Depression depression)
        {
            for (; ; )
            {
                yield return Wait10;
                if (!registered || !depression.data.Active || (FlatlinePlayer.isQueuedForDeath && currentConfig.PermanentDeath)) yield break;
                if (ShouldSkip()) continue;
                if (depression.isTemporaryCurePresent) continue;

                float chance = Mathf.Lerp(0.95f, 0.75f, depression.data.Severity / 0.3f);

                if (UnityEngine.Random.Range(0f, 1f) < chance) continue;
                float duration = Mathf.Lerp(Depression.minBlandEffectDuration, Depression.maxBlandEffectDuration, depression.data.Severity / 0.3f);
                float current = 0f;

                Singleton<PostProcessingManager>.Instance.SaturationController.AddOverride(-56f, 25, "Depression");
                for (; ; )
                {
                    yield return Wait1;
                    if (!registered) yield break;

                    current += 1f;
                    if (current >= duration) break;
                    if (!depression.data.Active) break;
                    if (depression.isTemporaryCurePresent) break;
                }
                Singleton<PostProcessingManager>.Instance.SaturationController.RemoveOverride("Depression");
            }
        }

        public static IEnumerator DepressionMessagerDisabler(Depression depression)
        {
            for (; ; )
            {
                yield return Wait10;
                if (!registered || !depression.data.Active || (FlatlinePlayer.isQueuedForDeath && currentConfig.PermanentDeath)) yield break;
                if (ShouldSkip()) continue;
                if (depression.isTemporaryCurePresent) continue;

                float chance = Mathf.Lerp(0.95f, 0.75f, depression.data.Severity / 0.3f);

                if (UnityEngine.Random.Range(0f, 1f) < chance) continue;

                float duration = Mathf.Lerp(Depression.minDisablerEffectDuration, Depression.maxDisablerEffectDuration, depression.data.Severity / 0.3f);
                float current = 0f;

                disabledOverlay.SetActive(true);
                for (; ; )
                {
                    yield return Wait1;
                    if (!registered) yield break;

                    current += 1f;
                    if (current >= duration) break;
                    if (!depression.data.Active) break;
                    if (depression.isTemporaryCurePresent) break;
                }
                disabledOverlay.SetActive(false);
            }
        }

        public static IEnumerator DepressionDoorDisabler(Depression depression)
        {
            for (; ; )
            {
                yield return Wait10;
                if (!registered || !depression.data.Active || (FlatlinePlayer.isQueuedForDeath && currentConfig.PermanentDeath)) yield break;
                if (ShouldSkip()) continue;
                if (Player.Local.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None) continue;
                if (depression.isTemporaryCurePresent) continue;

                float progressionAdjustedSeverity = Mathf.Clamp01((((float)depression.data.Progression / 5f) + depression.data.Severity / 0.3f) / 2f);

                float chance = Mathf.Lerp(0.95f, 0.75f, progressionAdjustedSeverity);

                if (UnityEngine.Random.Range(0f, 1f) < chance) continue;

                if (Player.Local.CurrentProperty != null && Player.Local.CurrentProperty.IsOwned)
                {
                    if (propertyDoors.TryGetValue(Player.Local.CurrentProperty.propertyCode, out PropertyDoorController[] doors))
                    {
                        foreach (PropertyDoorController controller in doors)
                        {
                            if (!controller.IsOpen)
                            {
#if MONO
                                controller.PlayerAccess = ScheduleOne.Doors.EDoorAccess.Locked;
#else
                                controller.PlayerAccess = Il2CppScheduleOne.Doors.EDoorAccess.Locked;
#endif
                                controller.noAccessErrorMessage = "You are too depressed right now";
                            }
                        }

                        float duration = Mathf.Lerp(Depression.minDisablerEffectDuration, Depression.maxDisablerEffectDuration, progressionAdjustedSeverity);
                        float current = 0f;

                        for (; ; )
                        {
                            yield return Wait1;
                            if (!registered) yield break;

                            current += 1f;
                            if (current >= duration) break;
                            if (!depression.data.Active) break;
                            if (depression.isTemporaryCurePresent) break;
                        }

                        foreach (PropertyDoorController controller in doors)
                        {
#if MONO
                            if (controller.PlayerAccess == ScheduleOne.Doors.EDoorAccess.Locked && controller.noAccessErrorMessage == "You are too depressed right now")
                            {
                                controller.PlayerAccess = ScheduleOne.Doors.EDoorAccess.Open;
                                controller.noAccessErrorMessage = "";
                            }
#else
                            if (controller.PlayerAccess == Il2CppScheduleOne.Doors.EDoorAccess.Locked && controller.noAccessErrorMessage == "You are too depressed right now")
                            {
                                controller.PlayerAccess = Il2CppScheduleOne.Doors.EDoorAccess.Open;
                                controller.noAccessErrorMessage = "";
                            }
#endif
                        }
                    }
                }
            }
        }
    }
}