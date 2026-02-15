using UnityEngine;

using static Flatline.DebugModule;
using static Flatline.Flatline;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.FlatlinePlayer;

namespace Flatline
{
    public static class ConsoleModule
    {
        public static bool isLoggingEnabled = false;
        public static readonly HashSet<string> ConsoleMethodNames = new()
        {
            "Help", "Set", "List", "Execute", "Add"
        };

        [Flags]
        public enum CommandSupport
        {
            None = 0,
            Set = 1 << 0,
            List = 1 << 1,
            Add = 1 << 2
        }

        public class MemberTypeException : System.Exception
        {
            public MemberTypeException() { }
            public MemberTypeException(string message) : base(message) { }
        }

        public abstract class ConsoleCommandBase
        {
            public virtual string Name { get; }
            public virtual CommandSupport SupportedMethods { get; }
            public virtual void Set(string id, string str, float fVal = -1f, bool bVal = false, string sVal = "") => Log("Not implemented");
            public virtual void Execute(List<string> args)
            {
                Log("Not implemented");
            }
            public virtual void List() => Log("Not implemented");
            public virtual void Add(List<string> args) => Log("Not implemented");

            public void ArgTypeParse(string id, string str, string val)
            {
                if (val == string.Empty) return;

                bool bVal = false;
                float fVal = -1f;
                try
                {
                    if (float.TryParse(val, out fVal))
                        Set(id, str, fVal, false, "");
                    else if (bool.TryParse(val, out bVal))
                        Set(id, str, -1f, bVal, val);
                    else
                        Set(id, str, -1f, false, val);
                }
                catch (MemberTypeException ex)
                {
                    Log("Failed to run command with exception: " + ex.Message);
                }
                
            }

            public bool AssertNotDefault(float fVal, bool bVal, string sVal, out Type type)
            {
                if (bool.TryParse(sVal, out _))
                {
                    type = typeof(bool);
                    return true;
                }
                if (fVal != -1f)
                {
                    type = typeof(float);
                    return true;
                }
                if (sVal != "")
                {
                    type = typeof(string);
                    return true;
                }

                type = null;
                return false;
            }

        }
        public class PlayerTarget : ConsoleCommandBase
        {
            public override string Name => "Player";
            public override CommandSupport SupportedMethods => CommandSupport.Set | CommandSupport.List;
            private readonly List<string> supportedMembers = new()
            {
                "thirst", "hunger", "energy", "temperature", "maxhp", "currenthp",
                "movespeedscale", "predisposition", "gluttony", "timessmoked", "islegbonebroken"
            };

            public override void Execute(List<string> args)
            {
                if (args.Count < 5)
                {
                    Log("Usage: flatline set player (member) (value)");
                    return;
                }
                if (!supportedMembers.Any(m => m.Equals(args[3], StringComparison.OrdinalIgnoreCase)))
                {
                    Log($"Player member: '{args[3]}' does not exist!\n    Try: 'flatline list player' for supported values");
                    return;
                }
                ArgTypeParse("", args[3], args[4]);

            }
            public override void Set(string id, string str, float fVal = -1f, bool bVal = false, string sVal = "")
            {
                if (!AssertNotDefault(fVal, bVal, sVal, out Type type))
                    return;

                switch (str.ToLower())
                {
                    case "thirst":
                        if (type != typeof(float)) 
                            throw new MemberTypeException("Invalid member value type for thirst");
                        loadedPlayerData.State.Thirst = Mathf.Clamp01(fVal);
                        break;
                    case "hunger":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for hunger");
                        loadedPlayerData.State.Hunger =  Mathf.Clamp01(fVal);
                        break;
                    case "energy":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for energy");
                        loadedPlayerData.State.Energy = Mathf.Clamp01(fVal);
                        break;
                    case "temperature":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for temperature");
                        loadedPlayerData.State.Temperature = Mathf.Clamp01(fVal);
                        break;
                    case "maxhp":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for max hp");
                        loadedPlayerData.State.healthData.MaxHP = Mathf.Clamp(fVal, 0f, 100f);
                        break;
                    case "currenthp":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for current hp");
                        loadedPlayerData.State.healthData.CurrentHP = Mathf.Clamp(fVal, 0f, 100f);
                        break;
                    case "movespeedscale":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for move speed scale");
                        loadedPlayerData.State.healthData.MoveSpeedScale = Mathf.Clamp(fVal, 0.1f, 3f);
                        break;
                    case "predisposition":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for predisposition");
                        loadedPlayerData.State.healthData.Predisposition = Mathf.Clamp01(fVal);
                        break;
                    case "gluttony":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for gluttony");
                        loadedPlayerData.State.healthData.Gluttony = Mathf.Clamp01(fVal);
                        break;
                    case "timessmoked":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for times smoked");
                        loadedPlayerData.State.healthData.TimesSmoked = Mathf.Clamp((int)fVal, 0, int.MaxValue);
                        break;
                    case "islegbonebroken":
                        if (type != typeof(bool))
                            throw new MemberTypeException("Invalid member value type for is leg bone broken");
                        loadedPlayerData.State.healthData.IsLegBoneBroken = bVal;
                        break;
                }
                Log("Player state set");
                return;
            }

            public override void List()
            {
                string listmessage = "";
                listmessage += $"\nMember name: Member value";
                listmessage += $"\n thirst: {loadedPlayerData.State.Thirst}";
                listmessage += $"\n energy: {loadedPlayerData.State.Energy}";
                listmessage += $"\n temperature: {loadedPlayerData.State.Temperature}";
                listmessage += $"\n maxhp: {loadedPlayerData.State.healthData.MaxHP}";
                listmessage += $"\n currenthp: {loadedPlayerData.State.healthData.CurrentHP}";
                listmessage += $"\n movespeedscale: {loadedPlayerData.State.healthData.MoveSpeedScale}";
                listmessage += $"\n predisposition: {loadedPlayerData.State.healthData.Predisposition}";
                listmessage += $"\n gluttony: {loadedPlayerData.State.healthData.Gluttony}";
                listmessage += $"\n timesSmoked: {loadedPlayerData.State.healthData.TimesSmoked}";
                listmessage += $"\n isLegBoneBroken: {loadedPlayerData.State.healthData.IsLegBoneBroken}";
                listmessage += $"\n\nMoving values per minute:";
                listmessage += $"\n Thirst consumption: {ThirstConsumptionPerMinute} (delta: {ThirstConsumptionPerMinute - DefaultThirstConsumption})";
                listmessage += $"\n Food consumption: {FoodConsumptionPerMinute} (delta: {FoodConsumptionPerMinute - DefaultFoodConsumption})";
                listmessage += $"\n Energy consumption: {EnergyConsumptionPerMinute} (delta: {EnergyConsumptionPerMinute - DefaultEnergyConsumption})";
                listmessage += $"\n Temperature consumption: {TemperatureConsumption} (delta: {TemperatureConsumption - TemperatureConsumptionPerMinutePerDegreeDiff})";
                listmessage += $"\n HP Regen: {HealthRegenPerMinute} (delta: {HealthRegenPerMinute - DefaultHealthRegeneration})";
                listmessage += $"\n Sprint Regen: {sprintReserveRegenPerMinute} (delta: {sprintReserveRegenPerMinute - DefaultSprintReserveRegenPerMinute})";
                Log(listmessage);
            }
        }

        public class DiseaseTarget : ConsoleCommandBase
        {
            public override string Name => "Disease";
            public override CommandSupport SupportedMethods => CommandSupport.Set | CommandSupport.List | CommandSupport.Add;
            private readonly List<string> supportedMembers = new()
            {
                "active", "progression", "healstate", "minssincediseasestart", "severity"
            };

            public override void Execute(List<string> args)
            {
                if (args.Count < 6)
                {
                    Log("Usage: flatline set disease (disease ID) (member) (value)");
                    return;
                }
                if (!diseaseNames.Contains(args[3].ToLower()))
                {
                    Log($"Disease with ID: '{args[3]}' does not exist!\n    Try: 'flatline list disease' for supported values");
                    return;
                }
                if (!supportedMembers.Any(m => m.Equals(args[4], StringComparison.OrdinalIgnoreCase)))
                {
                    Log($"Disease member: '{args[4]}' does not exist!\n    Try: 'flatline list disease' for supported values");
                    return;
                }
                ArgTypeParse(args[3], args[4], args[5]);
            }
            public override void Set(string id, string str, float fVal = -1f, bool bVal = false, string sVal = "")
            {
                if (!AssertNotDefault(fVal, bVal, sVal, out Type type))
                    return;

                Disease disease = allDiseases.Find(x => x.data.DiseaseID.ToLower() == id.ToLower());
                if (disease == null)
                {
                    Log($"Failed to find a disease with ID: {id}");
                    return;
                }

                switch (str.ToLower())
                {
                    case "active":
                        if (type != typeof(bool))
                            throw new MemberTypeException("Invalid member value type for active");
                        disease.data.Active = bVal;
                        break;

                    case "progression":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for progression");
                        disease.data.Progression = Mathf.Clamp((int)fVal, 1, 5);
                        break;

                    case "healstate":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for heal state");
                        disease.data.HealState = Mathf.Clamp01(fVal);
                        break;

                    case "minssincediseasestart":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for mins since disease start");
                        disease.data.MinsSinceDiseaseStart = Mathf.Clamp((int)fVal, 0, int.MaxValue);
                        break;

                    case "severity":
                        if (type != typeof(float))
                            throw new MemberTypeException("Invalid member value type for severity");
                        disease.data.Severity = Mathf.Clamp(fVal, 0f, 0.3f);
                        break;

                }
                Log("Disease state set");
                return;
            }
            public override void List()
            {
                string listmessage = "";
                listmessage += $"\n# SUPPORTED DISEASE ID LIST";
                foreach (string diseaseName in diseaseNames)
                {
                    listmessage += $"\n  {diseaseName}";
                }
                listmessage += $"\n# SUPPORTED MEMBER NAMES";
                foreach (string member in supportedMembers)
                {
                    listmessage += $"\n  {member}";
                }
                listmessage += $"\n# ACTIVE DISEASES";
                foreach (Disease disease in allDiseases)
                {
                    listmessage += $"\nDiseaseID: {disease.data.DiseaseID}";
                    listmessage += $"\n  active: {disease.data.Active}";
                    listmessage += $"\n  progression: {disease.data.Progression}";
                    listmessage += $"\n  healstate: {disease.data.HealState}";
                    listmessage += $"\n  minssincediseasestart: {disease.data.MinsSinceDiseaseStart}";
                    listmessage += $"\n  severity: {disease.data.Severity}";
                }
                Log(listmessage);
            }

            public override void Add(List<string> args)
            {
                if (args.Count != 4)
                {
                    Log("Usage: flatline add disease (disease ID)");
                    return;
                }
                if (!diseaseNames.Contains(args[3].ToLower()))
                {
                    Log($"Disease with ID: '{args[3]}' does not exist!\n    Try: 'flatline list disease' for supported values");
                    return;
                }
                switch (args[3])
                {
                    case "cancer":
                        AddNewDisease<Cancer>(0.01f);
                        break;

                    case "fever":
                        AddNewDisease<Fever>(0.01f);
                        break;

                    case "bonebreak":
                        AddNewDisease<BoneBreak>(0.01f);
                        break;

                    case "bleed":
                        AddNewDisease<Bleeding>(0.3f);
                        break;

                    case "depression":
                        AddNewDisease<Depression>(0.01f);
                        break;
                }

                return;
            }

        }

        public class ConsumptionTarget : ConsoleCommandBase
        {
            public override string Name => "Consumption";
            public override CommandSupport SupportedMethods => CommandSupport.List;

            public override void Execute(List<string> args)
            {
                Log("Not implemented");
            }
            public override void Set(string id, string str, float fVal = -1f, bool bVal = false, string sVal = "")
            {
                Log("Not implemented");
            }
            public override void List()
            {
                string listmessage = "";
                listmessage += $"\n# CONSUMED ITEMS";
                foreach (var kvp in loadedPlayerData.State.consumptionDatas)
                {
                    listmessage += $"\n{kvp.Key}:";
                    listmessage += $"\n  Currently in system: {kvp.Value.currentAmountInSystem}";
                    listmessage += $"\n  Lung Damage: {kvp.Value.overtimeLungDamage}";
                    listmessage += $"\n  Liver Damage: {kvp.Value.overtimeLiverDamage}";
                }
                Log(listmessage);
            }

        }

    }
}