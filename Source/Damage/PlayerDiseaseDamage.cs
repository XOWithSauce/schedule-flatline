
using UnityEngine;
using MelonLoader;

using static Flatline.Flatline;
using static Flatline.DebugModule;
using static Flatline.FlatlineUIModule;
using static Flatline.FlatlinePlayer;
using static Flatline.PlayerConsumeDamage;
using static Flatline.ConfigLoader;

#if MONO
using ScheduleOne.Money;
using ScheduleOne.ItemFramework;
using ScheduleOne.Combat;
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Map;
using ScheduleOne.Interaction;
#else
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Interaction;
#endif

namespace Flatline
{
    public static class PlayerDiseaseDamage
    {
        public static List<Disease> allDiseases = new();
        public static readonly List<string> diseaseNames = new()
        {
            "cancer", "fever", "bonebreak", "bleed", "depression"
        };

        public static GameObject bleedPressureIntObj;
        public static bool isBleedingStemmed = false;

        public static void InitPlayerDiseaseDamage()
        {
            bleedPressureIntObj = new("ApplyPressureInt");
            bleedPressureIntObj.layer = LayerMask.NameToLayer("Task"); // otherwise player can hit themselves through this objects collider
            bleedPressureIntObj.transform.parent = Player.Local.transform.root;
            bleedPressureIntObj.transform.localScale = Vector3.one;
            bleedPressureIntObj.transform.localRotation = Quaternion.identity;
            bleedPressureIntObj.transform.localPosition = new Vector3(0f, 0.1f, 0.1f);

            BoxCollider bc = bleedPressureIntObj.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.2f, 0.2f, 0.2f);

            InteractableObject intObj = bleedPressureIntObj.AddComponent<InteractableObject>();
            intObj.message = "Stem bleeding";
            intObj.MaxInteractionRange = 2f;
            intObj.interactionType = InteractableObject.EInteractionType.Key_Press;

            void SetIsBleedingStemmedFalse() 
            {
                Log("Disable bleeding stemmed");
                isBleedingStemmed = false; 
            
            }
            void SetIsBleedingStemmedTrue() 
            {
                Log("Set bleeding stemmed");
                isBleedingStemmed = true; 
            }
            intObj.onInteractStart.AddListener((UnityEngine.Events.UnityAction)SetIsBleedingStemmedTrue);
            intObj.onInteractEnd.AddListener((UnityEngine.Events.UnityAction)SetIsBleedingStemmedFalse);

            bleedPressureIntObj.SetActive(false);
        }

        public static void ResetPlayerDiseaseDamage()
        {
            bleedPressureIntObj = null;
            isBleedingStemmed = false;
        }

        public static void ApplyLoadedDiseases()
        {
            foreach(DiseaseData data in loadedPlayerData.DiseaseData)
            {
                switch (data.DiseaseID)
                {
                    case "cancer":
                        Cancer cancer = new(data);
                        allDiseases.Add(cancer);
                        break;

                    case "fever":
                        Fever fever = new(data);
                        allDiseases.Add(fever);
                        break;

                    case "bonebreak":
                        BoneBreak bonebreak = new(data);
                        allDiseases.Add(bonebreak);
                        break;

                    case "bleed":
                        Bleeding bleeding = new(data);
                        allDiseases.Add(bleeding);
                        break;

                    case "depression":
                        Depression depression = new(data);
                        allDiseases.Add(depression);
                        break;

                    default:
                        Log("Unknown disease id: " + data.DiseaseID);
                        break;
                }
            }

            bool hasBoneBreak = false;
            foreach (Disease disease in allDiseases)
            {
                if (disease.data.Active && disease.data.HealState < 1f)
                {
                    disease.diseaseCoroutine = MelonCoroutines.Start(disease.DiseaseEvaluator());
                }
                if (disease.data.DiseaseID == "bonebreak")
                    hasBoneBreak = true;
            }

            // defensive prog for invalid leg bone state
            if (loadedPlayerData.State.healthData.IsLegBoneBroken && !hasBoneBreak)
            {
                Log("Player leg bone cant be broken since there is no active disease, setting false");
                loadedPlayerData.State.healthData.IsLegBoneBroken = false;
            }
        }

        public static void UpdateDiseaseData()
        {
            loadedPlayerData.DiseaseData.Clear();
            foreach (Disease disease in allDiseases)
            {
                if (disease.data.Active)
                {
                    disease.UpdateDiseaseData();
                    loadedPlayerData.DiseaseData.Add(disease.data);
                }
            }
        }

        public static T AddNewDisease<T>(float severity) where T : Disease
        {
            float newSeverity = Mathf.Clamp(severity, 0f, 0.3f);
            foreach (Disease activeDisease in allDiseases)
            {
                if (activeDisease.GetType() == typeof(T) && activeDisease.data.Active)
                {
                    Log($"Disease of type {typeof(T).Name} is already active, can't add duplicate");

                    // For when bleeding reapplies worse bleeding -> upgrade existing and reduce its healstate by 25%
                    if (activeDisease.GetType() == typeof(Bleeding) && activeDisease.data.Severity < newSeverity)
                    {
                        Log("Upgrading bleeding to worse severity on reapply");
                        activeDisease.data.Severity = newSeverity;
                        activeDisease.data.HealState *= 0.75f;
                        (activeDisease as Bleeding).MaximumMaxHPReduction = Mathf.Lerp(40f, 150f, newSeverity / 0.3f);
                    }

                    return null;
                }
            }
            DiseaseData newData = new();
            newData.Active = true;
            newData.Severity = newSeverity;
            T disease = (T)System.Activator.CreateInstance(typeof(T), new object[] { newData });
            allDiseases.Add(disease);
            disease.diseaseCoroutine = MelonCoroutines.Start(disease.DiseaseEvaluator());
            Log($"Succesfully added new disease: {typeof(T).Name}");
            return disease;
        }

        public static void OnHourPass()
        {
            if (haltExecution) return;
            if (NetworkSingleton<TimeManager>.Instance.IsSleepInProgress) return;
            if (NetworkSingleton<TimeManager>.Instance.CurrentTime < 659 && NetworkSingleton<TimeManager>.Instance.CurrentTime > 400) return;
            CalculateFeverProbability();
        }

        public static void OnSleepEnd()
        {
            if (haltExecution) return;
            CalculateCancerProbability();
            CalculateDepressionPassiveProbability();
        }

        public static void CalculateDepressionPassiveProbability()
        {
            float cumulativePlayTime = Mathf.Clamp01((float)NetworkSingleton<TimeManager>.Instance.ElapsedDays / 100f);

            int currentMinsInside = minsInsideProperty != 0 ? minsInsideProperty : 1;
            int currentMinsOutside = minsOutsideProperty != 0 ? minsOutsideProperty : 1;

            float cabinLunacy = 0f;
            if (currentMinsInside >= currentMinsOutside)
            {
                int diff = currentMinsInside - currentMinsOutside;
                if (diff > (1440f / 3f))
                {
                    cabinLunacy += 0.09f;
                }
                else if (diff > (1440f / 4f))
                {
                    cabinLunacy += 0.06f;
                }
                else if (diff > (1440f / 6f))
                {
                    cabinLunacy += 0.03f;
                }
            }

            int drugTypesConsumed = 0;
            float currentlyInSystem = 0f;
            if (loadedPlayerData.State.consumptionDatas.Count > 0)
            {
                foreach (var kvp in loadedPlayerData.State.consumptionDatas)
                {
                    if (!Depression.temporaryDepressionCures.Contains(kvp.Key)) continue;
                    currentlyInSystem += kvp.Value.currentAmountInSystem;
                    drugTypesConsumed++;
                }
            }
            float dependence = 0f;
            if (drugTypesConsumed > 0)
            {
                currentlyInSystem = currentlyInSystem / (float)drugTypesConsumed;
                dependence = Mathf.Lerp(0f, 0.03f, currentlyInSystem);
            }
            dependence += Mathf.Clamp((float)loadedPlayerData.State.healthData.TimesSmoked / 100f, 0f, 0.33f);
            dependence = Mathf.Clamp01(dependence);

            float stateTotal = 0f;
            if (loadedPlayerData.State.Thirst <= 0.65f)
                stateTotal += 0.002f;
            if (loadedPlayerData.State.Hunger <= 0.65f)
                stateTotal += 0.002f;
            if (loadedPlayerData.State.Temperature <= 0.65f)
                stateTotal += 0.002f;
            if (NetworkSingleton<MoneyManager>.Instance.cashBalance < 5000f)
                stateTotal += 0.002f;

            float gender = Player.Local.Avatar.CurrentSettings.Gender;
            float genderAdjustedProbability = 0f;
            if (gender > 0.5f)
                genderAdjustedProbability = UnityEngine.Random.Range(0.005f, 0.007f);
            else
                genderAdjustedProbability = UnityEngine.Random.Range(0.003f, 0.004f);

            float subtotal = (cumulativePlayTime + cabinLunacy + currentlyInSystem + stateTotal + genderAdjustedProbability) / 5f;

            Log("Depression chance subtotal: " + subtotal);
            if (UnityEngine.Random.Range(0f, 1f) < subtotal)
            {
                Log("Depression consumption chance hits");
                AddNewDisease<Depression>(UnityEngine.Random.Range(0.001f, 0.3f));
            }

            minsOutsideProperty = 0;
            minsInsideProperty = 0;
            return;
        }

        public static void CalculateDepressionAfterConsumeProbability(EConsumeType type, EQuality quality)
        {
            if (!(type == EConsumeType.Weed || type == EConsumeType.Meth || type == EConsumeType.Cocaine || type == EConsumeType.Shroom)) return;

            float typeWeightedProbability = 0f;
            switch (type)
            {
                case EConsumeType.Weed:
                    if (loadedPlayerData.State.consumptionDatas.TryGetValue("weed", out ConsumptionData weedData))
                        if (weedData.currentAmountInSystem < 0.30f)
                            typeWeightedProbability = UnityEngine.Random.Range(0.002f, 0.019f);
                    break;

                case EConsumeType.Shroom:
                    if (loadedPlayerData.State.consumptionDatas.TryGetValue("shroom", out ConsumptionData shroomData))
                        if (shroomData.currentAmountInSystem > 0.08f)
                            typeWeightedProbability = UnityEngine.Random.Range(0.002f, 0.025f);
                    break;

                case EConsumeType.Meth:
                    if (loadedPlayerData.State.consumptionDatas.TryGetValue("meth", out ConsumptionData methData))
                        if (methData.currentAmountInSystem < 0.35f)
                            typeWeightedProbability = UnityEngine.Random.Range(0.030f, 0.095f);
                    break;

                case EConsumeType.Cocaine:
                    if (loadedPlayerData.State.consumptionDatas.TryGetValue("cocaine", out ConsumptionData cocaineData))
                        if (cocaineData.currentAmountInSystem < 0.35f)
                            typeWeightedProbability = UnityEngine.Random.Range(0.030f, 0.125f);
                    break;
            }

            if (typeWeightedProbability == 0f)
                typeWeightedProbability = 0.05f;

            float annualOccurance = typeWeightedProbability;
            float predisposition = (1f + loadedPlayerData.State.healthData.Predisposition);
            int totalQualities = Enum.GetNames(typeof(EQuality)).Length - 1;
            float qualityMult = Mathf.Lerp(0.95f, 1.08f, ((float)(int)quality / (float)totalQualities));
            float subtotal = Mathf.Clamp01(annualOccurance * predisposition * qualityMult);
            Log("Depression subtotal consumption: " + subtotal);
            if (UnityEngine.Random.Range(0f, 1f) < subtotal)
            {
                Log("Depression consumption chance hits");
                AddNewDisease<Depression>(UnityEngine.Random.Range(0.001f, 0.3f));
            }
            return;
        }

        public static void CalculateFallBoneBreakProbability(float fallDamage)
        {
            float probability = 0f;
            if (fallDamage >= 20f && fallDamage <= 25f)
                probability += 0.20f;
            else if (fallDamage > 20f && fallDamage <= 28f)
                probability += 0.35f;
            else if (fallDamage > 28f && fallDamage <= 38f)
                probability += 0.65f;
            else if (fallDamage > 38f)
                probability += 0.75f;

            float weight = Player.Local.Avatar.CurrentSettings.Weight;
            if (weight <= 0.25f)
                probability += 0.005f;
            else if (weight > 0.25f && weight <= 0.40f)
                probability += 0.015f;
            else if (weight > 0.40f && weight <= 0.65f)
                probability += 0.03f;
            else if (weight > 0.65f)
                probability += 0.07f;

            bool hasSetRagdolled = false;
            bool isGoingToDie = false;

            if (!(fallDamage <= 0.12f))
            {
                if (Player.Local.Health.CurrentHealth - fallDamage <= 0f)
                {
                    causeOfDeath = $"Fall damage";
                    isGoingToDie = true;
                }
                if (!isGoingToDie && !isFPRagdollActive && !Player.Local.IsRagdolled && fallDamage > 20f)
                {
                    hasSetRagdolled = true;
                    coros.Add(MelonCoroutines.Start(FPRagdoll(false, null, 0)));
                }
                Player.Local.Health.TakeDamage(fallDamage, true, false);
                AppendDamageSource($"Fall damage (-{Mathf.RoundToInt(fallDamage)}HP)");
            }

            if (!(fallDamage <= 0.18f) && UnityEngine.Random.Range(0f, 1f) < probability)
            {
                if (!hasSetRagdolled && !isGoingToDie && !isFPRagdollActive && !Player.Local.IsRagdolled)
                    coros.Add(MelonCoroutines.Start(FPRagdoll(false, null, 0)));

                if (!flatlinePlayerAudio.isPlaying)
                    flatlinePlayerAudio.PlayOneShot(loadedAudios["bonebreak"]);

                if (!isGoingToDie)
                {
                    AddNewDisease<BoneBreak>(UnityEngine.Random.Range(0.01f, 0.3f));
                    loadedPlayerData.State.healthData.IsLegBoneBroken = true;
                }
                Log("Fall damage bone break chance hits");
            }
        }

        enum ImpactLocation
        {
            Legs, Torso, Head
        }
        public static void CalculateImpact(Impact impact)
        {
            Vector3 playerCenter = Player.Local.CenterPointTransform.position;
            Vector3 distanceFromCenter = impact.HitPoint - playerCenter;
            ImpactLocation loc;
            if (distanceFromCenter.y <= -0.1f)
                loc = ImpactLocation.Legs;
            else if (distanceFromCenter.y > -0.1f && distanceFromCenter.y < 0.24f)
                loc = ImpactLocation.Torso;
            else
                loc = ImpactLocation.Head;

            CalculateImpactBleedingProbability(impact, loc);
            CalculateImpactBoneBreakProbability(impact, loc);
        }

        private static void CalculateImpactBleedingProbability(Impact impact, ImpactLocation loc)
        {
            float probability = 0.0001f;
            float bleedSeverity = 0.002f;
            switch (loc)
            {
                case ImpactLocation.Legs:
                    probability += 0.03f;
                    break;

                case ImpactLocation.Torso:
                    probability += 0.07f;
                    bleedSeverity += 0.05f;
                    break;

                case ImpactLocation.Head:
                    probability += 0.05f;
                    bleedSeverity += 0.08f;
                    break;
            }

            switch (impact.ImpactType)
            {
                case EImpactType.Punch:
                    probability *= 0.1f;
                    break;

                case EImpactType.BluntMetal:
                    probability += 0.005f;
                    break;

                case EImpactType.SharpMetal:
                    probability += 0.23f;
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f && loc == ImpactLocation.Head)
                        bleedSeverity += 0.20f;
                    else
                        bleedSeverity += 0.12f;
                    break;

                case EImpactType.Bullet:
                    probability += 0.33f;
                    bleedSeverity += 0.20f;
                    break;
            }

            probability += loadedPlayerData.State.healthData.Predisposition * 0.2f;
            Log($"Impact Bleeding probability: {probability} - {loc}, {impact.ImpactType}");
            if (UnityEngine.Random.Range(0f, 1f) < probability)
            {
                Log("Impact bleeding chance hits");
                AddNewDisease<Bleeding>(bleedSeverity);
            }
        }

        private static void CalculateImpactBoneBreakProbability(Impact impact, ImpactLocation loc)
        {
            float probability = 0.0001f;
            switch (loc)
            {
                case ImpactLocation.Legs:
                    probability += 0.005f;
                    break;

                case ImpactLocation.Torso:
                    probability += 0.012f;
                    break;

                case ImpactLocation.Head:
                    probability += 0.009f;
                    break;
            }

            switch (impact.ImpactType)
            {
                case EImpactType.Punch:
                    probability += 0.0002f;
                    break;

                case EImpactType.BluntMetal:
                    probability += 0.22f;
                    break;

                case EImpactType.SharpMetal:
                    probability += 0.05f;
                    break;

                case EImpactType.Bullet:
                    probability += 0.12f;
                    break;

                case EImpactType.PhysicsProp:
                    probability += 0.08f;
                    break;
            }

            probability += loadedPlayerData.State.healthData.Predisposition * 0.2f;

            Log($"Impact Bone break probability: {probability} - {loc}, {impact.ImpactType}");
            if (UnityEngine.Random.Range(0f, 1f) < probability)
            {
                if (!flatlinePlayerAudio.isPlaying)
                    flatlinePlayerAudio.PlayOneShot(loadedAudios["bonebreak"]);

                AddNewDisease<BoneBreak>(UnityEngine.Random.Range(0.001f, 0.15f));
                if (loc == ImpactLocation.Legs)
                    loadedPlayerData.State.healthData.IsLegBoneBroken = true;
                Log("Impact Bone break chance hits");
            }

        }

        public static void CalculateFeverProbability()
        {
            float annualOccurance = UnityEngine.Random.Range(5000f, 20000f) / 100000f;
            float hourlyChance = annualOccurance / 365f / 24f;

            hourlyChance = Mathf.Clamp01(hourlyChance + Mathf.Lerp(0.55f, 0f, loadedPlayerData.State.Temperature));

            hourlyChance = Mathf.Clamp01(hourlyChance + loadedPlayerData.State.healthData.Predisposition);

            for (int i = 0; i < allDiseases.Count; i++)
            {
                if (allDiseases[i].data.DiseaseID == "cancer" && allDiseases[i].data.Active && allDiseases[i].data.HealState < 1f)
                    hourlyChance = Mathf.Clamp01(hourlyChance * UnityEngine.Random.Range(1.02f, 1.1f));
            }

            if (Singleton<SewerCameraPresense>.Instance.IsPointInSewerArea(Player.Local.CenterPointTransform.position))
                hourlyChance = Mathf.Clamp01(hourlyChance + UnityEngine.Random.Range(0.001f, 0.08f));

            hourlyChance = hourlyChance / 5f;
            Log("Fever eval chance: " + hourlyChance);
            if (UnityEngine.Random.Range(0f, 1f) < hourlyChance)
            {
                AddNewDisease<Fever>(UnityEngine.Random.Range(0.001f, 0.3f));
                Log("Fever chance hits");
            }
        }

        public static void CalculateCancerProbability()
        {
            float gender = Player.Local.Avatar.CurrentSettings.Gender;
            float daysElapsed = (float)NetworkSingleton<TimeManager>.Instance.ElapsedDays;

            if (daysElapsed < 4) return;

            float minDaysForMaleCancers = 25f;
            float maxDaysForMaleCancers = 47f;
            float maxDaysForFemaleCancers = 36f;
            float minDaysForFemaleCancers = 18f;

            float genderTimeAdjustedProbability = 0f;
            if (gender > 0.5f)
            {
                if (daysElapsed < minDaysForFemaleCancers)
                    genderTimeAdjustedProbability = Mathf.Lerp(0.0001f, 0.08f, daysElapsed / minDaysForFemaleCancers);

                else if (daysElapsed > minDaysForFemaleCancers && daysElapsed < maxDaysForFemaleCancers)
                    genderTimeAdjustedProbability = Mathf.Lerp(0.08f, 0.01f, daysElapsed / maxDaysForFemaleCancers);

                else if (daysElapsed > maxDaysForFemaleCancers)
                {
                    float clamp100d = Mathf.Clamp01((daysElapsed - maxDaysForFemaleCancers) / 100f);
                    genderTimeAdjustedProbability = Mathf.Lerp(0.01f, 0.1f, clamp100d);
                }
            }
            else
            {
                if (daysElapsed < minDaysForMaleCancers)
                    genderTimeAdjustedProbability = Mathf.Lerp(0.0001f, 0.03f, daysElapsed / maxDaysForMaleCancers);

                else if (daysElapsed > minDaysForMaleCancers && daysElapsed < maxDaysForMaleCancers)
                    genderTimeAdjustedProbability = Mathf.Lerp(0.03f, 0.07f, daysElapsed / maxDaysForMaleCancers);

                else if (daysElapsed > maxDaysForMaleCancers)
                {
                    float clamp100d = Mathf.Clamp01((daysElapsed - maxDaysForMaleCancers) / 100f);
                    genderTimeAdjustedProbability = Mathf.Lerp(0.07f, 0.1f, clamp100d);
                }
            }


            float annualOccurance = UnityEngine.Random.Range(300f, 900f) / 100000f;
            float dailyOccurance = annualOccurance / 365f;

            float maxHpLostNorm = Mathf.Lerp(1f, 0f, Mathf.Clamp01(loadedPlayerData.State.healthData.MaxHP / 100f));
            float predisposition = loadedPlayerData.State.healthData.Predisposition;
            float gluttony = loadedPlayerData.State.healthData.Gluttony;
            float weight = Player.Local.Avatar.CurrentSettings.Weight;

            int maxSmokingYears = UnityEngine.Random.Range(5, 15);
            int packsPerYearMax = 30;
            int ciggiesPerPack = 20;
            int maxTimesSmoked = maxSmokingYears * packsPerYearMax * ciggiesPerPack;
            int timesSmoked = loadedPlayerData.State.healthData.TimesSmoked;
            float timesSmokeNorm = Mathf.Clamp01((float)timesSmoked / (float)maxTimesSmoked);

            float totalSystematicDamage = 0f;
            foreach (ConsumptionData data in loadedPlayerData.State.consumptionDatas.Values)
            {
                totalSystematicDamage = Mathf.Clamp01(data.overtimeLungDamage + data.overtimeLiverDamage);
            }

            // For when player sleeps in the sewers apartment (or passes out due to energy and sleeps in sewer)
            float sewerToxinPresence = 0f; 
            if (Singleton<SewerCameraPresense>.Instance.IsPointInSewerArea(Player.Local.CenterPointTransform.position))
                sewerToxinPresence = UnityEngine.Random.Range(0.01f, 0.30f);

            float subtotal = Mathf.Clamp01(((maxHpLostNorm + predisposition + gluttony + weight + timesSmokeNorm + totalSystematicDamage) / 10f) + (dailyOccurance + sewerToxinPresence + genderTimeAdjustedProbability) / 5f);

            Log("Cancer eval subtotal: " + subtotal);
            if (UnityEngine.Random.Range(0f, 1f) < subtotal)
            {
                Log("Cancer chance hits");
                AddNewDisease<Cancer>(UnityEngine.Random.Range(0.001f, 0.29f));
            }
        }
    }
    
}