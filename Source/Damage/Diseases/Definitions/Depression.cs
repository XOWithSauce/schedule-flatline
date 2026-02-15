using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;
using static Flatline.DepressionSimulator;

namespace Flatline
{

    public class Depression : Disease
    {
        public static readonly float passiveDiseaseHealingMax = 0.0025f;
        public static readonly float passiveDiseaseHealingMin = 0.0001f;
        public static readonly float baseEnergyConsumptionIncrease = 0.002f;
        public static readonly float baseFoodConsumptionIncrease = 0.0008f;
        public static readonly int maxTemporaryCureMins = 120;
        public static readonly float maxBlandEffectDuration = 180f;
        public static readonly float minBlandEffectDuration = 80f;
        public static readonly float maxDisablerEffectDuration = 120f;
        public static readonly float minDisablerEffectDuration = 50f;

        private static int minsSpentBedrotting = 0;
        public bool isTemporaryCurePresent = false;
        private int minsSinceTemporaryCureStarted = 0;

        public static readonly List<string> temporaryDepressionCures = new()
        {
            "weed", "shroom", "cocaine", "meth"
        };

        public Depression(DiseaseData data)
        {
            this.data = data;
            this.data.DiseaseID = "depression";
            base.minsRequiredForProgression = 60 * 24 * 3;
            base.onDiseaseStarted = RunDepressionCoros;
        }

        private void RunDepressionCoros()
        {
            coros.Add(MelonCoroutines.Start(DepressionBlandWorld(this)));
            coros.Add(MelonCoroutines.Start(DepressionDoorDisabler(this)));
            coros.Add(MelonCoroutines.Start(DepressionMessagerDisabler(this)));
        }

        public override void DiseaseEffect()
        {
            float severityAdjustedMins = Mathf.Round((float)this.data.MinsSinceDiseaseStart * (1f + this.data.Severity));

            if (severityAdjustedMins / (float)base.minsRequiredForProgression >= this.data.Progression)
                this.data.Progression++;

            if (this.data.Progression >= 5)
            {
                Log("Player died of depression");
                causeOfDeath = "Depression";
                coros.Add(MelonCoroutines.Start(PrePlayerDied()));
                this.data.Active = false;
                return;
            }

            if (BedRotSimulator.isBedrotting)
            {
                // so here its 1440 max mins of bedrotting to == 1 so 1 full ingame day of bedrot in total (24min irl
                // and again because its iterative it must always be lerp
                // and the result must always be higher than existing heal state
                // if its not higher than the existing heal state add result / 10 then it rewards most bedrotting while
                // depressed?
                minsSpentBedrotting += 10;
                float result = Mathf.Lerp(0f, 1f, Mathf.Clamp01((float)minsSpentBedrotting / 1440f));
                if (result > this.data.HealState)
                    this.data.HealState = result;
                else
                    this.data.HealState += result / 10f;


            }

            this.data.HealState += UnityEngine.Random.Range(passiveDiseaseHealingMin, passiveDiseaseHealingMax);

            if (this.isTemporaryCurePresent)
            {
                minsSinceTemporaryCureStarted += 10;
                if (minsSinceTemporaryCureStarted >= maxTemporaryCureMins)
                    isTemporaryCurePresent = false;
            }

            if (loadedPlayerData.State.healthData.TimesSmoked > 0 && loadedPlayerData.State.consumptionDatas.Count > 0)
            {
                Dictionary<string, ConsumptionData> currentData = new(loadedPlayerData.State.consumptionDatas);
                float amountOfDrugsInSystem = 0f;
                int dataPointCount = 0;
                foreach (var kvp in currentData)
                {
                    if (!temporaryDepressionCures.Contains(kvp.Key)) continue;
                    amountOfDrugsInSystem += kvp.Value.currentAmountInSystem;
                    if (amountOfDrugsInSystem > 0.15f && !isTemporaryCurePresent)
                    {
                        isTemporaryCurePresent = true;
                        minsSinceTemporaryCureStarted = 0;
                    }
                    dataPointCount++;
                }
                if (dataPointCount > 0)
                {
                    amountOfDrugsInSystem = Mathf.Clamp01(amountOfDrugsInSystem / dataPointCount);
                    float maxFix = passiveDiseaseHealingMax * (1f + amountOfDrugsInSystem);
                    float minFix = passiveDiseaseHealingMin * (1f + amountOfDrugsInSystem);
                    float drugFix = UnityEngine.Random.Range(minFix, maxFix);
                    drugFix = Mathf.Lerp(drugFix, maxFix, amountOfDrugsInSystem);
                    this.data.HealState += Mathf.Clamp(drugFix, 0f, 0.01f);
                }
            }

            // dont run below if bedrotting
            if (BedRotSimulator.isBedrotting)
                return;

            float multiplier = Mathf.Lerp(3f, 5f, ((float)this.data.Progression / 5f) - (1f / 5f));
            float energyConsumptionIncrease = Mathf.Clamp(baseEnergyConsumptionIncrease * this.data.Progression * (1f + this.data.Severity), 0f, 0.01f);
            float foodConsumptionIncrease = Mathf.Clamp(baseFoodConsumptionIncrease * this.data.Progression * (1f + this.data.Severity), 0f, 0.0052f);

            EnergyConsumptionPerMinute = Mathf.Lerp(EnergyConsumptionPerMinute, EnergyConsumptionPerMinute + energyConsumptionIncrease, MaximumSystematicPropertyChangePerTick * multiplier);

            FoodConsumptionPerMinute = Mathf.Lerp(FoodConsumptionPerMinute, FoodConsumptionPerMinute + foodConsumptionIncrease, MaximumSystematicPropertyChangePerTick * multiplier);
        }

        public override void DiseaseHealed()
        {
            // reset the increased changes if applied
            if (FoodConsumptionPerMinute > DefaultFoodConsumption)
                FoodConsumptionPerMinute = Mathf.Lerp(FoodConsumptionPerMinute, DefaultFoodConsumption, 0.5f);

            if (EnergyConsumptionPerMinute > DefaultEnergyConsumption)
                EnergyConsumptionPerMinute = Mathf.Lerp(EnergyConsumptionPerMinute, DefaultEnergyConsumption, 0.5f);

            Log("Healed depression succesfully");
            return;
        }


        public override void UpdateDiseaseData()
        {
            return;
        }
    }

}