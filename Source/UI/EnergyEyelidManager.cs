using MelonLoader;
using UnityEngine;
using System.Collections;

using static Flatline.Flatline;
using static Flatline.FlatlinePlayer;
using static Flatline.DebugModule;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.DevUtilities;
using ScheduleOne.Tools;
using ScheduleOne.UI;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
#endif

namespace Flatline
{

    public static class EnergyEyelidManager
    {
        public static FloatSmoother eyelid;
        public static float currentEnergyEyelidValue = 1f;
        public static void CreateEyelidOverride()
        {
            eyelid = Singleton<EyelidOverlay>.Instance.OpenMultiplier;

            if (loadedPlayerData.State.Energy < 0.15f)
                currentEnergyEyelidValue = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(loadedPlayerData.State.Energy / 0.15f));

            eyelid.AddOverride(currentEnergyEyelidValue, 1, "flatline_energy");
        }

        public static IEnumerator UpdateEyelidEnergyState()
        {
            for (; ; )
            {
                yield return Wait5;
                if (!registered || eyelid == null || eyelid.activeOverride == null || currentConfig == null || loadedPlayerData == null)
                {
                    yield break;
                }

                if (!setupCompleted || !currentConfig.EnergyRequired || loadedPlayerData.State.Energy <= 0f || NetworkSingleton<SleepController>.Instance.IsSleepInProgress || eyelid.activeOverride.Label != "flatline_energy")
                    continue;

                // pad the energy a bit so that its below 15% left when it starts to do it
                if (loadedPlayerData.State.Energy > 0.15f && currentEnergyEyelidValue != 1f)
                {
                    currentEnergyEyelidValue = 1f;
                    eyelid.activeOverride.Value = 1f;
                }
                else if (loadedPlayerData.State.Energy <= 0.15f)
                {
                    currentEnergyEyelidValue = eyelid.activeOverride.Value;
                    eyelid.activeOverride.Value = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(loadedPlayerData.State.Energy / 0.15f));
                }
            }

            yield break;
        }

        public static void ResetEyelidOverride()
        {
            eyelid = null;
            currentEnergyEyelidValue = 1f;
        }


    }
}
