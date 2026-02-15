using System.Collections;
using UnityEngine;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.PlayerDiseaseDamage;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
#endif

namespace Flatline
{
    public static class PlayerFallDamage
    {
        public static float lastHighestFrameY = 0f;
        
        public static IEnumerator EvaluatePlayerJumpMovement()
        {
            float minimumYFrame = -8.5f;
            for (; ; )
            {
                yield return Wait01;
                if (!registered)
                {
                    Log("Exiting registered");
                    yield break;
                }
                if (haltExecution) continue;
                if (PlayerSingleton<PlayerMovement>.Instance.IsGrounded) continue;
                while (registered && !PlayerSingleton<PlayerMovement>.Instance.IsGrounded)
                {
                    yield return Wait01;
                    if (!registered) yield break;
                    if (PlayerSingleton<PlayerMovement>.Instance.lastFrameMovement.y < minimumYFrame)
                    {
                        float currentY = -(PlayerSingleton<PlayerMovement>.Instance.lastFrameMovement.y);
                        if (currentY > lastHighestFrameY)
                        {
                            lastHighestFrameY = currentY;
                        }
                    }
                }
                float maxFallDamage = 119f;
                float maxFallFrameY = 62f;

                float result = Mathf.Lerp(0f, maxFallDamage, Mathf.Clamp01(lastHighestFrameY / maxFallFrameY));
                CalculateFallBoneBreakProbability(result);
                yield return Wait05;
                lastHighestFrameY = 0f;
            }
        }
    }


}