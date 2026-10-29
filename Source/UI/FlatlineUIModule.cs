
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MelonLoader;
using System.Collections;

using static Flatline.ConfigLoader;
using static Flatline.DebugModule;
using static Flatline.Flatline;
using static Flatline.PlayerDiseaseDamage;
using static Flatline.FlatlinePlayer;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.Persistence;
using ScheduleOne.UI;
using ScheduleOne.UI.Phone;
using ScheduleOne.PlayerScripts;
using TMPro;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppTMPro;
#endif

namespace Flatline
{
    public static class FlatlineUIModule
    {
        public static Slider ThirstSlider;
        public static Slider HungerSlider;
        public static Slider EnergySlider;
        public static Slider TemperatureSlider;

        public static Text WorldTemperature;
        public static Button CustomButton;
        public static readonly string celsiusSign = "°C";
        public static readonly string fahrenheitSign = "°F";

        public static RectTransform scoreBoard;
        public static string causeOfDeath = string.Empty;
        public static bool deathCleanupStarted = false;

        public static Dictionary<string, GameObject> diseaseIcons = new();
        public static GameObject diseaseContainerObj;
        public static void ResetFlatlineUIModule()
        {
            ThirstSlider = null;
            HungerSlider = null;
            EnergySlider = null;
            TemperatureSlider = null;
            CustomButton = null;
            WorldTemperature = null;
            scoreBoard = null;
            causeOfDeath = string.Empty;
            diseaseIcons.Clear();
            diseaseContainerObj = null;
        }

        #region Survival states
        public static void InitiateSurvivalSliders()
        {
            // Create the transform base in HUD 
            GameObject trRoot = new("SurvivalStates");
            trRoot.transform.parent = Singleton<HUD>.Instance.transform;
            RectTransform rt = trRoot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700f, 180f);
            rt.anchoredPosition = new Vector2(560f, -435f);
            VerticalLayoutGroup group = trRoot.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = false;
            group.childControlWidth = false;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            group.spacing = 38f;
            group.childAlignment = TextAnchor.MiddleCenter;


            List<string> sliderNames = new() { "water", "meat", "energy", "temperature" };
            // Create the sliders for each
            foreach (string name in sliderNames)
            {
                GameObject sliderObj = new($"Slider_{name}");
                sliderObj.transform.parent = trRoot.transform;
                RectTransform baseRt = sliderObj.AddComponent<RectTransform>();
                baseRt.sizeDelta = new Vector2(200f, 12f);

                Slider sliderComp = null;
                switch (name)
                {
                    case "water": 
                        ThirstSlider = sliderObj.AddComponent<Slider>();
                        sliderComp = ThirstSlider;
                        break;

                    case "meat":
                        HungerSlider = sliderObj.AddComponent<Slider>();
                        sliderComp = HungerSlider;
                        break;

                    case "energy":
                        EnergySlider = sliderObj.AddComponent<Slider>();
                        sliderComp = EnergySlider;
                        break;

                    case "temperature":
                        TemperatureSlider = sliderObj.AddComponent<Slider>();
                        sliderComp = TemperatureSlider;
                        break;
                }

                GameObject fillRectObj = new("FillRect");
                fillRectObj.transform.parent = sliderObj.transform;
                RectTransform fillRect = fillRectObj.AddComponent<RectTransform>();
                sliderComp.fillRect = fillRect;
                fillRect.anchoredPosition = new Vector2(160f, 0f);
                fillRect.sizeDelta = new Vector2(1f, 1f);
                fillRectObj.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
                // Add logo foreach
                GameObject logo = new("Logo");
                logo.transform.parent = sliderObj.transform;
                RectTransform logoRt = logo.AddComponent<RectTransform>();
                logoRt.anchoredPosition = new Vector2(25f, 0f);
                logoRt.sizeDelta = new Vector2(46f, 46f);
                Image image = logo.AddComponent<Image>();
                image.sprite = loadedSprites[name];
            }
            return;
        }

        public static void InitiateDiseasesHolder()
        {
            diseaseContainerObj = new("DiseaseIconContainer");
            diseaseContainerObj.transform.parent = Singleton<HUD>.Instance.transform;
            RectTransform containerRt = diseaseContainerObj.AddComponent<RectTransform>();
            containerRt.sizeDelta = new Vector2(1000f, 54f);
            containerRt.localPosition = new Vector3(0f, -370f, 0f);
            HorizontalLayoutGroup group = diseaseContainerObj.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 30f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlHeight = false;
            group.childControlWidth = false;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;

            foreach (string name in diseaseNames)
            {
                if (!imageResources.Contains(name))
                {
                    Log($"Disease with name {name} does not have a logo in Resources");
                    continue;
                }
                // Add logo foreach
                GameObject logo = new($"{name}");
                logo.SetActive(false);
                logo.transform.parent = diseaseContainerObj.transform;
                RectTransform logoRt = logo.AddComponent<RectTransform>();
                logoRt.localPosition = Vector3.zero;
                logoRt.sizeDelta = new Vector2(54f, 54f);
                Image image = logo.AddComponent<Image>();
                image.sprite = loadedSprites[name];
                diseaseIcons.Add(name, logo);
            }
        }

        public static IEnumerator UpdateDiseasesHolder()
        {
            for (; ; )
            {
                yield return Wait01;
                if (!registered) yield break;
                if (diseaseContainerObj == null) continue;
                // set invisible diseases icons if they are visible during ui usage
                if (PlayerSingleton<PlayerCamera>.Instance.ActiveUIElementCount > 0 && diseaseContainerObj.activeSelf)
                    diseaseContainerObj.SetActive(false);
                else if (PlayerSingleton<PlayerCamera>.Instance.ActiveUIElementCount == 0 && !diseaseContainerObj.activeSelf)
                    diseaseContainerObj.SetActive(true);
            }
            yield break;
        }

        #endregion

        #region Temperature
        public static IEnumerator InitiateWorldTemperatureText()
        {
            yield return Wait5;
            GameObject newElement = null;
            for (int i = 0; i < PlayerSingleton<HomeScreen>.Instance.transform.childCount; i++)
            {
                if (PlayerSingleton<HomeScreen>.Instance.transform.GetChild(i).name.ToLower().Contains("infobar"))
                {
                    Transform infobar = PlayerSingleton<HomeScreen>.Instance.transform.GetChild(i);
                    for (int j = 0; j < infobar.childCount; j++)
                    {
                        if (infobar.GetChild(j).name.ToLower() == "data")
                        {
                            newElement = UnityEngine.Object.Instantiate(infobar.GetChild(j).gameObject, infobar);
                            break;
                        }
                    }
                    break;
                }
            }

            if (newElement == null)
                yield break;

            newElement.name = "Temperature";
            RectTransform rt = newElement.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150f, 35f);
            newElement.transform.localRotation = Quaternion.identity;
            newElement.transform.localScale = Vector3.one;
            newElement.transform.localPosition = new Vector3(150.5101f, 0f, 0f);
            Text textComp = newElement.GetComponent<Text>();
            if (textComp == null)
            {
                textComp = newElement.GetComponentInChildren<Text>();
            }

            if (textComp == null)
                yield break;

            WorldTemperature = textComp;
            string sign = currentConfig.FahrenheitTemp ? fahrenheitSign : celsiusSign;
            int temp = UnityEngine.Random.Range(16, 21);
            temp = currentConfig.FahrenheitTemp ? (int)CelsiusToFahrenheit((float)temp) : temp;
            WorldTemperature.text = $"{temp}{sign}";

            Log("World temperature text succesfully created");
            yield break;
        }
        public static float FahrenheitToCelsius(float fahreneit)
        {
            return (fahreneit - 32f) * (5f / 9f);
        }
        public static float CelsiusToFahrenheit(float celsius)
        {
            return (celsius * (9f / 5f)) + 32f;
        }
        #endregion

        #region Death Screen
        public static void InitiateDeathScreen()
        {
            deathCleanupStarted = false;
            GameObject newButton = null;

            // Instantiate new button based on the respawn button in to the buttons container
            Transform buttonContainer = Singleton<DeathScreen>.Instance.Container.Find("Buttons");
            Button respawnButton = Singleton<DeathScreen>.Instance.respawnButton;
            if (respawnButton != null)
                newButton = UnityEngine.Object.Instantiate(respawnButton.gameObject, buttonContainer ? buttonContainer : Singleton<DeathScreen>.Instance.Container);

            if (newButton == null)
            {
                Log("Failed to instantiate a button in death screen");
                return;
            }

            newButton.transform.position = new Vector3(960f, 510f, 0f);
            newButton.name = "Return to Menu";
            RectTransform rt = newButton.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 70f);
            CustomButton = newButton.GetComponent<Button>();
            CustomButton.onClick = new();
            CustomButton.onClick.AddListener((UnityEngine.Events.UnityAction)OnReturnToMenuClicked);

            if (newButton.transform.childCount > 0)
            {
                TextMeshProUGUI textComp = newButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                textComp.text = Player.PlayerList.Count == 1 ? "Delete Save and Return to Menu" : "Return to Menu (co-op save kept)";
                    textComp.color = Color.white;
            }
            newButton.SetActive(true);

            GameObject scoreBoardObj = new GameObject("Scoreboard");
            scoreBoardObj.transform.parent = Singleton<DeathScreen>.Instance.Container;
            scoreBoardObj.transform.localScale = Vector3.one;
            scoreBoard = scoreBoardObj.AddComponent<RectTransform>();
            scoreBoard.sizeDelta = new Vector2(500f, 400f);
            scoreBoard.localPosition = Vector3.zero;
            scoreBoard.anchoredPosition = new Vector2(0f, 300f);
            VerticalLayoutGroup group = scoreBoardObj.AddComponent<VerticalLayoutGroup>();
            group.childAlignment = TextAnchor.UpperLeft; 
            group.spacing = 4f;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            group.childControlHeight = false;
            group.childControlWidth = false;

            Log("DeathScreen elements done");
            return;
        }
        
        public static IEnumerator GenerateDeathScreenScore()
        {
            if (Player.PlayerList.Count > 1 && !currentConfig.PermanentDeath)
            {
                yield break;
            }
#if MONO
            WaitUntil waitObj = new WaitUntil(() => Singleton<DeathScreen>.Instance.isOpen);
#else
            WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => Singleton<DeathScreen>.Instance.isOpen));
#endif
            yield return waitObj;

            if (!registered) yield break;
            if (Player.Local.Health.IsAlive)
                Player.Local.Health.IsAlive = false;

            yield return Wait05;
            if (!registered) yield break;

            string lastDamage = "";
            if (lastDamageSources.Count > 0)
                lastDamage = lastDamageSources[0];

            Dictionary<string, string> scoreDisplays = new()
            {
                { "Days survived: ", NetworkSingleton<TimeManager>.Instance != null ? NetworkSingleton<TimeManager>.Instance.ElapsedDays.ToString() : "?" },
                { "Networth: ", NetworkSingleton<MoneyManager>.Instance != null ? Mathf.RoundToInt(NetworkSingleton<MoneyManager>.Instance.LastCalculatedNetworth).ToString() : "?" },
                { "Cause of Death: ", causeOfDeath },
                { "Last damage source: ", lastDamage },
            };

            List<TextMeshProUGUI> textElements = new();

            // create score
            int i = 0;
            foreach (var kvp in scoreDisplays)
            {
                GameObject newText = new($"ScoreKVP{i}");
                newText.SetActive(false);

                RectTransform textRt = newText.AddComponent<RectTransform>();
                textRt.sizeDelta = new Vector2(500f, 35f);
                TextMeshProUGUI textComp = newText.AddComponent<TextMeshProUGUI>();
                textComp.fontSize = 16f;
                textComp.text = $"{kvp.Key}{kvp.Value}";
                newText.transform.parent = scoreBoard;
                newText.transform.localScale = Vector3.one;
                textComp.color = new Color(1f, 1f, 1f, 0f);
                textElements.Insert(0, textComp);
                i++;
            }
            // fade them in
            foreach(TextMeshProUGUI textComp in textElements)
            {
                yield return Wait025;
                if (!registered) yield break;
                textComp.gameObject.SetActive(true);
                float fadeDur = 1f;
                float current = 0f;
                float t = 0f;
                while (registered && current < fadeDur)
                {
                    current += Time.deltaTime;
                    t = current / fadeDur;
                    float alpha = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                    textComp.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
                textComp.color = new Color(1f, 1f, 1f, 1f);
            }

            Log("Generated death screen");
            yield break;
        }


        public static void OnReturnToMenuClicked()
        {
            Log("Returning to menu...");
            BeginPermanentDeathCleanup();
        }

        public static void BeginPermanentDeathCleanup()
        {
            if (deathCleanupStarted) return;
            deathCleanupStarted = true;

            LoadManager manager = Singleton<LoadManager>.Instance;
            if (manager == null) return;

            if (currentConfig == null || !currentConfig.PermanentDeath || !isQueuedForDeath ||
                Player.Local == null || Player.Local.Health == null || Player.Local.Health.IsAlive ||
                Player.PlayerList.Count != 1)
            {
                Log("Pass delete due to failure exiting save");
                manager.ExitToMenu(null, null, false);
                return;
            }

            SaveInfo currentInfo = manager.ActiveSaveInfo;
            if (currentInfo == null)
            {
                MelonLogger.Error("Flatline: active save info unavailable; returning to menu without deleting anything.");
                manager.ExitToMenu(null, null, false);
                return;
            }

            string savePath = currentInfo.SavePath;
            string orgName = currentInfo.OrganisationName;
            int slotNum = currentInfo.SaveSlotNumber;
            MelonCoroutines.Start(ReturnAndDeleteSave(savePath, orgName, slotNum));
        }

        public static IEnumerator ReturnAndDeleteSave(string path, string orgName, int slotNum)
        {
            Singleton<LoadManager>.Instance.ExitToMenu(null, null, false);
            yield return null; // Let the game begin the transition before checking the scene.
            for (; ; )
            {
                if (SceneManager.GetActiveScene().name == "Menu" && !Singleton<LoadManager>.Instance.IsLoading)
                {
                    break;
                }
                yield return null;
            }

            Log("Deleting save data...");
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Log("Save data deleted succesfully");
                    Singleton<LoadManager>.Instance.RefreshSaveInfo();
                }
                else
                {
                    Log("Save data not found at path: " + path);
                }
            } catch (Exception ex)
            {
                Log("Failed to delete save data: " + ex);
            }

            Log("Deleting flatline persist data");
            RemoveSaveAssociatedData(orgName, slotNum);
            yield break;
        }

        #endregion


    }
}