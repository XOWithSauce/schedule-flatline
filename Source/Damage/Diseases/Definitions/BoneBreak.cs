using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
#endif

namespace Flatline
{

    public class BoneBreak : Disease
    {
        public static readonly float passiveDiseaseHealingMax = 0.001f;
        public static readonly float passiveDiseaseHealingMin = 0.0001f;
        public static readonly float healingReductionWhileSprinting = 0.008f;

        public static readonly float baseMoveSpeedReduction = 0.03f;
        public static readonly float minMoveSpeedScaleValue = 0.65f;
        private bool hasDisabledJump = false;

        public BoneBreak(DiseaseData data)
        {
            this.data = data;
            this.data.DiseaseID = "bonebreak";
            base.minsRequiredForProgression = 60 * 24 * 3;
        }

        public override void DiseaseEffect()
        {
            float severityAdjustedMins = Mathf.Round((float)this.data.MinsSinceDiseaseStart * (1f + this.data.Severity));

            if (severityAdjustedMins / (float)base.minsRequiredForProgression >= this.data.Progression)
                this.data.Progression++;

            if (this.data.Progression >= 5)
            {
                Log("Player died of a broken bone");
                causeOfDeath = "Broken bone";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                this.data.Active = false;
                return;
            }

            this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax);

            if (PlayerSingleton<PlayerMovement>.Instance.IsSprinting)
                this.data.HealState = Mathf.Clamp01(this.data.HealState - healingReductionWhileSprinting * this.data.Progression);

            if (BedRotSimulator.isBedrotting)
                this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax);

            if (this.data.Progression >= 1)
            {
                float minMoveSpeedTarget = Mathf.Lerp(minMoveSpeedScaleValue, 1f, this.data.HealState);
                if (loadedPlayerData.State.healthData.MoveSpeedScale > minMoveSpeedTarget)
                {
                    float moveSpeedReduction = Mathf.Lerp((baseMoveSpeedReduction * this.data.Progression * (1f + this.data.Severity)), 0f, this.data.HealState);
                    float newSpeed = Mathf.Clamp(
                        loadedPlayerData.State.healthData.MoveSpeedScale - moveSpeedReduction,
                        minMoveSpeedScaleValue,
                        1f
                    );
                    loadedPlayerData.State.healthData.MoveSpeedScale = newSpeed;
                }
            }

            if (this.data.Progression >= 3 && this.data.HealState < 0.5f && loadedPlayerData.State.healthData.IsLegBoneBroken)
            {
                if (PlayerSingleton<PlayerMovement>.Instance.CanJump && !hasDisabledJump)
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                    {
                        PlayerSingleton<PlayerMovement>.Instance.CanJump = false;
                        hasDisabledJump = true;
                    }
                }
                else if (!PlayerSingleton<PlayerMovement>.Instance.CanJump && hasDisabledJump)
                {
                    PlayerSingleton<PlayerMovement>.Instance.CanJump = true;
                    hasDisabledJump = false;
                }
            }
        }

        public override void DiseaseHealed()
        {
            loadedPlayerData.State.healthData.IsLegBoneBroken = false;
            Log("Healed bone break succesfully");
            return;
        }

        public override void UpdateDiseaseData()
        {
            return;
        }
    }

}