using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;

using static Flatline.AudioLoader;

#if MONO
using ScheduleOne.Persistence;
#else
using Il2CppScheduleOne.Persistence;
#endif

namespace Flatline
{
    #region Persistent JSON Files and Serialization

    [Serializable]
    public class FlatlinePlayerData
    {
        public FlatlinePlayerData() { }
        public FlatlinePlayerData(FlatlinePlayerData original)
        {
            this.State = new(original.State);
            this.DiseaseData = new();
            foreach (DiseaseData data in original.DiseaseData)
                this.DiseaseData.Add(new DiseaseData(data));
        }
        public FlatlinePlayerState State = new();
        public List<DiseaseData> DiseaseData = new();
    }

    [Serializable]
    public class FlatlinePlayerState
    {
        public FlatlinePlayerState() { }
        public FlatlinePlayerState(FlatlinePlayerState original)
        {
            this.Thirst = original.Thirst;
            this.Hunger = original.Hunger;
            this.Energy = original.Energy;
            this.Temperature = original.Temperature;
            foreach (var kvp in original.consumptionDatas)
            {
                this.consumptionDatas.Add(kvp.Key, new(kvp.Value));
            }
            this.healthData = new(original.healthData);
        }
        public Dictionary<string, ConsumptionData> consumptionDatas = new();
        public HealthData healthData = new();
        public float Thirst = 1f;
        public float Hunger = 1f;
        public float Energy = 1f;
        public float Temperature = 1f;
    }

    [Serializable]
    public class ConsumptionData
    {
        public ConsumptionData() { }
        public ConsumptionData(ConsumptionData original)
        {
            this.overtimeLungDamage = original.overtimeLungDamage;
            this.overtimeLiverDamage = original.overtimeLiverDamage;
            this.currentAmountInSystem = original.currentAmountInSystem;
        }
        public float overtimeLungDamage = 0f;
        public float overtimeLiverDamage = 0f;
        public float currentAmountInSystem = 0f;
    }

    [Serializable]
    public class HealthData
    {
        public HealthData() { }
        public HealthData(HealthData original)
        {
            this.MaxHP = original.MaxHP;
            this.CurrentHP = original.CurrentHP;
            this.MoveSpeedScale = original.MoveSpeedScale;
            this.Predisposition = original.Predisposition;
            this.Gluttony = original.Gluttony;
            this.TimesSmoked = original.TimesSmoked;
            this.IsLegBoneBroken = original.IsLegBoneBroken;
            this.daysSinceFlu = original.daysSinceFlu;
        }
        public float MaxHP = 100f;
        public float CurrentHP = 100f;
        public float MoveSpeedScale = 1f;
        public float Predisposition = 0f; // random roll
        public float Gluttony = 0f; // based on the amount eaten on average, gluttony 0...1
        public int TimesSmoked = 0;
        public bool IsLegBoneBroken = false; // if BoneBreak not active always false, otherwise set to true on specific bone break disease
        public int daysSinceFlu = 0;
    }

    [Serializable]
    public class DiseaseData
    {
        public DiseaseData() { }
        public DiseaseData(DiseaseData original)
        {
            this.DiseaseID = original.DiseaseID;
            this.Active = original.Active;
            this.MinsSinceDiseaseStart = original.MinsSinceDiseaseStart;
            this.Progression = original.Progression;
            this.Severity = original.Severity;
            this.HealState = original.HealState;
            if (original.DiseaseStates != null)
                this.DiseaseStates = new(original.DiseaseStates);
        }

        public string DiseaseID = "";
        public bool Active = false;
        public int MinsSinceDiseaseStart = 0;
        public int Progression = 1;
        public float Severity = 0f; // max 0.3
        public float HealState = 0f;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, float> DiseaseStates;
    }

    public class FlatlineModConfig
    {
        public bool PermanentDeath = true;
        public bool DrugSideEffects = true;
        public bool PropertyTemperatureChanges = true;
        public bool WorldTemperatureChanges = true;
        public bool FahrenheitTemp = false;

        public bool DiseasesEnabled = true;
        public bool BleedingEnabled = true;
        public bool BoneBreakEnabled = true;
        public bool CancerEnabled = true;
        public bool DepressionEnabled = true;
        public bool FeverEnabled = true;

        public bool WaterRequired = true;
        public bool FoodRequired = true;
        public bool EnergyRequired = true;
        public bool TemperatureRequired = true;

        public float WaterConsumption = 0.00087958f;
        public float FoodConsumption = 0.0015f;
        public float EnergyConsumption = 0.0007f;
        public float TemperatureConsumption = 0.00022f;
    }
    #endregion

    #region Mod resources loader
    public static class ConfigLoader
    {
        // Folder where all images and audios folder always reside in
        private readonly static string BASE_USERDATA_NAME = "XO_WithSauce-Flatline";

        // For when user uses thunderstore mod manager it creates the following name for the folder where it puts the folders, depends on backend version
        private readonly static string TS_PACKAGE_NAME = "XO_WithSauce-Flatline_";
#if MONO
        private readonly static string packagePathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, TS_PACKAGE_NAME + "MONO", BASE_USERDATA_NAME);
#else
        private readonly static string packagePathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, TS_PACKAGE_NAME + "IL2CPP", BASE_USERDATA_NAME);
#endif

        // For when user drags and drops the UserData folder from manual download, this is fallback checked path for the mod userdata folder
        private readonly static string manualPathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, BASE_USERDATA_NAME);


        // Thunderstore Mod manager final paths
        private readonly static string pathPlayerData = Path.Combine(packagePathUserData, "PlayerData"); // filename "{save slot number}_{save file organistaion name}"
        private readonly static string pathModImageResources = Path.Combine(packagePathUserData, "ModResources", "Images");
        private readonly static string pathModAudioResources = Path.Combine(packagePathUserData, "ModResources", "Audio");

        // Manual drag and drop final paths
        private readonly static string pathManualPlayerData = Path.Combine(manualPathUserData, "PlayerData"); // filename "{save slot number}_{save file organistaion name}"
        private readonly static string pathManualModImageResources = Path.Combine(manualPathUserData, "ModResources", "Images");
        private readonly static string pathManualModAudioResources = Path.Combine(manualPathUserData, "ModResources", "Audio");


        public readonly static List<string> imageResources = new() //.png
        {
            "bleed",
            "bonebreak",
            "cancer",
            "depression",
            "energy",
            "fever",
            "meat",
            "temperature",
            "water"
        };

        public readonly static List<string> audioResources = new() //.wav
        {
            "malecough",
            "malesneeze",
            "femalecough",
            "femalesneeze",
            "bonebreak",
            "flatline",
            "singleheartbeat"
        };

 
        public static object playerDataLock = new object();

        // Image Resources name -> generated sprite
        public static Dictionary<string, Sprite> loadedSprites = new();

        // Audio Resources name -> generated audio clip
        public static Dictionary<string, AudioClip> loadedAudios = new();

        #region Flatline Player Data
        public static FlatlinePlayerData LoadPlayerData()
        {
            FlatlinePlayerData playerData;
            string orgName = LoadManager.Instance.ActiveSaveInfo.OrganisationName;
            int slotNumber = LoadManager.Instance.ActiveSaveInfo.SaveSlotNumber;
            string fileName = $"{slotNumber}_{SanitizeAndFormatName(orgName)}";

            string playerDataPath = "";
            if (Directory.Exists(pathPlayerData))
            {
                // User has installed mod through mod manager and teamname_packagename folder exists for it
                playerDataPath = pathPlayerData;
            }
            else if (Directory.Exists(pathManualPlayerData))
            {
                // User has drag and dropped the UserData folder
                playerDataPath = pathManualPlayerData;
            }
            else
            {
                // Somehow both dont exist at all
                MelonLogger.Error("Failed to locate Flatline PlayerData from the UserData folder!");
                return null;
            }

            if (File.Exists(Path.Combine(playerDataPath, fileName)))
            {
                try
                {
                    string json = File.ReadAllText(Path.Combine(playerDataPath, fileName));
                    playerData = JsonConvert.DeserializeObject<FlatlinePlayerData>(json);
                }
                catch (Exception ex)
                {
                    playerData = new();
                    MelonLogger.Warning("Failed to read flatline player data: " + ex);
                }
            }
            else
            {
                playerData = new();
                playerData.State.healthData.Predisposition = UnityEngine.Random.Range(0.0005f, 0.02f);
                Save(playerData);
            }
            return playerData;
        }
        public static void Save(FlatlinePlayerData playerData)
        {
            lock (playerDataLock)
            {
                FlatlinePlayerData currentData = new(playerData);

                try
                {
                    string orgName = LoadManager.Instance.ActiveSaveInfo.OrganisationName;
                    int slotNumber = LoadManager.Instance.ActiveSaveInfo.SaveSlotNumber;
                    string fileName = $"{slotNumber}_{SanitizeAndFormatName(orgName)}";

                    string playerDataPath = "";
                    if (Directory.Exists(pathPlayerData))
                    {
                        // User has installed mod through mod manager and teamname_packagename folder exists for it
                        playerDataPath = pathPlayerData;
                    }
                    else if (Directory.Exists(pathManualPlayerData))
                    {
                        // User has drag and dropped the UserData folder
                        playerDataPath = pathManualPlayerData;
                    }
                    else
                    {
                        // Somehow both dont exist at all saving will prefer the first option that uses the TS package auto formatting
                        playerDataPath = pathPlayerData;
                    }

                    string saveDestination = Path.Combine(playerDataPath, fileName);
                    string json = JsonConvert.SerializeObject(currentData, Formatting.Indented);
                    Directory.CreateDirectory(Path.GetDirectoryName(saveDestination));
                    File.WriteAllText(saveDestination, json);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning("Failed to save Flatline player data: " + ex);
                }
            }

            return;
        }

        public static void RemoveSaveAssociatedData(string orgName, int slotNum)
        {
            string fileName = $"{slotNum}_{SanitizeAndFormatName(orgName)}";

            string playerDataPath = "";

            if (Directory.Exists(pathPlayerData))
            {
                // User has installed mod through mod manager and teamname_packagename folder exists for it
                playerDataPath = pathPlayerData;
            }
            else if (Directory.Exists(pathManualPlayerData))
            {
                // User has drag and dropped the UserData folder
                playerDataPath = pathManualPlayerData;
            }
            else
            {
                // Somehow both dont exist at all
                MelonLogger.Error("Failed to locate Flatline PlayerData from the UserData folder!");
                return;
            }

            if (File.Exists(Path.Combine(playerDataPath, fileName)))
            {
                try
                {
                    File.Delete(Path.Combine(playerDataPath, fileName));
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning("Failed to remove save associated flatline player data: " + ex);
                }
            }
            return;
        }
        #endregion

        #region Image loader, Audio loader
        public static void LoadModResources()
        {
            string imageResourcesPath = "";
            string audioResourcesPath = "";
            if (Directory.Exists(packagePathUserData))
            {
                // User has installed mod through mod manager and teamname_packagename folder exists for it
                imageResourcesPath = pathModImageResources;
                audioResourcesPath = pathModAudioResources;
            }
            else if (Directory.Exists(manualPathUserData))
            {
                // User has drag and dropped the UserData folder
                imageResourcesPath = pathManualModImageResources;
                audioResourcesPath = pathManualModAudioResources;
            }
            else
            {
                // Somehow both dont exist at all
                MelonLogger.Error("Failed to locate Flatline mod resources from UserData!");
                return;
            }

            if (Directory.Exists(imageResourcesPath))
            {
                foreach (string imageName in imageResources)
                {
                    string imagePath = Path.Combine(imageResourcesPath, imageName + ".png");
                    // if image file exists load into bytearray convert to texture and make a sprite
                    if (File.Exists(imagePath))
                    {
                        byte[] imageData = File.ReadAllBytes(imagePath);
                        Texture2D iconTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
#if IL2CPP
                        iconTex.hideFlags |= HideFlags.DontSave; // because otherwise il2cpp collects garbage
#endif
                        ImageConversion.LoadImage(iconTex, imageData);
                        iconTex.name = imageName;
                        iconTex.anisoLevel = 1;
                        iconTex.Apply();
                        Sprite newIcon = Sprite.Create(iconTex, new Rect(0, 0, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
#if IL2CPP
                        newIcon.hideFlags |= HideFlags.DontSave; // because otherwise il2cpp collects garbage
#endif
                        loadedSprites.Add(imageName, newIcon);
                    }
                    else
                    {
                        MelonLogger.Error($"Flatline mod expected to find file '{imageName}' but it's missing!");
                    }
                }
            }
            else
            {
                MelonLogger.Error($"Flatline mod expected to find directory '{imageResourcesPath}' but it's missing!");
            }

            if (Directory.Exists(audioResourcesPath))
            {
                foreach (string audioName in audioResources)
                {
                    string audioPath = Path.Combine(audioResourcesPath, audioName + ".wav");
                    // if image file exists load into bytearray convert to audio clip
                    if (File.Exists(audioPath))
                    {
                        byte[] audioData = File.ReadAllBytes(audioPath);
                        AudioClip clip = ToAudioClip(audioData);
#if IL2CPP
                        clip.hideFlags |= HideFlags.DontSave; // because otherwise il2cpp collects garbage
#endif
                        loadedAudios.Add(audioName, clip);
                    }
                    else
                    {
                        MelonLogger.Error($"Flatline mod expected to find file '{audioName}' but it's missing!");
                    }
                }
            }
            else
            {
                MelonLogger.Error($"Flatline mod expected to find directory '{audioResourcesPath}' but it's missing!");
            }

            return;
        }
#endregion

        #region Helper function
        public static string SanitizeAndFormatName(string orgName)
        {
            string saveFileName = orgName;

            if (saveFileName != null)
            {
                saveFileName = saveFileName.Replace(" ", "_").ToLower();
                saveFileName = saveFileName.Replace(",", "");
                saveFileName = saveFileName.Replace(".", "");
                saveFileName = saveFileName.Replace("<", "");
                saveFileName = saveFileName.Replace(">", "");
                saveFileName = saveFileName.Replace(":", "");
                saveFileName = saveFileName.Replace("\"", "");
                saveFileName = saveFileName.Replace("/", "");
                saveFileName = saveFileName.Replace("\\", "");
                saveFileName = saveFileName.Replace("|", "");
                saveFileName = saveFileName.Replace("?", "");
                saveFileName = saveFileName.Replace("*", "");
            }
            saveFileName = saveFileName + ".json";
            return saveFileName;
        }
        #endregion

    }
#endregion
}
