
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
        public static TextMeshProUGUI deathTitleText;
        private static bool deathCleanupStarted;

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
            deathTitleText = null;
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

#if IL2CPP
            // The 0.4.7 hierarchy no longer guarantees the old child name.
            Button respawnButton = Singleton<DeathScreen>.Instance.respawnButton;
            if (respawnButton != null)
                newButton = UnityEngine.Object.Instantiate(respawnButton.gameObject, Singleton<DeathScreen>.Instance.Container);
#else
            for (int i = 0; i < Singleton<DeathScreen>.Instance.Container.childCount; i++)
            {
                Transform tr = Singleton<DeathScreen>.Instance.Container.GetChild(i);
                if (tr.name.ToLower().Contains("respawn"))
                {
                    newButton = UnityEngine.Object.Instantiate(tr.gameObject, Singleton<DeathScreen>.Instance.Container);
                    break;
                }
            }
#endif

            if (newButton != null)
            {
                newButton.transform.position = new Vector3(960f, 510f, 0f);
                newButton.name = "Return to Menu";
                RectTransform rt = newButton.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(180f, 70f);
                CustomButton = newButton.GetComponent<Button>();
                if (CustomButton != null)
                {
                    // Cloning a prefab can retain persistent respawn listeners.
                    CustomButton.onClick = new Button.ButtonClickedEvent();
                    CustomButton.onClick.AddListener((UnityEngine.Events.UnityAction)OnReturnToMenuClicked);
                }

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textComp != null)
                {
                    textComp.text = Player.PlayerList.Count == 1 ? "Delete Save and Return to Menu" : "Return to Menu (co-op save kept)";
                    textComp.color = Color.white;
                }
                newButton.SetActive(true);
            }
            else
                MelonLogger.Warning("Flatline: no respawn button to clone; automatic solo permadeath cleanup will still run.");

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

            Transform titleTr = Singleton<DeathScreen>.Instance.Container.transform.Find("Title");
            if (titleTr != null)
            {
                deathTitleText = titleTr.gameObject.GetComponent<TextMeshProUGUI>();
            }
            if (deathTitleText == null)
                MelonLogger.Warning("Flatline: death screen title not found; skipping its animation.");
            Log("DeathScreen elements done");
            return;
        }
        
        public static IEnumerator GenerateDeathScreenScore()
        {
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

            if (scoreBoard == null)
            {
                MelonLogger.Warning("Flatline: death scoreboard unavailable; skipping the score display.");
                yield break;
            }

            Dictionary<string, string> scoreDisplays = new()
            {
                { "Days survived: ", NetworkSingleton<TimeManager>.Instance != null ? NetworkSingleton<TimeManager>.Instance.ElapsedDays.ToString() : "?" },
                { "Networth: ", NetworkSingleton<MoneyManager>.Instance != null ? Mathf.RoundToInt(NetworkSingleton<MoneyManager>.Instance.LastCalculatedNetworth).ToString() : "?" },
                { "Cause of Death: ", causeOfDeath },
                { "Last damage source: ", lastDamage },
            };

            List<TextMeshProUGUI> textElements = new();

            // in multiplayer if no perma death monitor until respawn button is pressed
            string originalTitle = deathTitleText != null ? deathTitleText.text : string.Empty;
            if (Player.PlayerList.Count > 1 && !currentConfig.PermanentDeath)
                coros.Add(MelonCoroutines.Start(WaitUntilRespawn(originalTitle)));

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

            // animate title characters change
            if (deathTitleText != null)
                coros.Add(MelonCoroutines.Start(AnimateRandomLettersInTitle()));

            Log("Generated death screen");
            yield break;
        }

        public static IEnumerator AnimateRandomLettersInTitle()
        {
            if (deathTitleText == null) yield break;
            string flatlined = "FLATLINED";
            if (deathTitleText != null)
            {
                for (int k = 0; k < deathTitleText.text.Length; k++)
                {
                    yield return Wait025;
                    if (!registered || Player.Local.Health.IsAlive) yield break;
                    char newChar = ' ';
                    char[] chars = deathTitleText.text.ToCharArray();
                    chars[k] = newChar;
                    deathTitleText.text = new string(chars);
                }
                deathTitleText.text = new string(' ', flatlined.Length);

                float baseDelay = 0.025f;
                float maxDelay = 0.1f;

                for (int k = 0; k < flatlined.Length; k++)
                {
                    float progress = (float)k / (flatlined.Length - 1);
                    float currentWait = Mathf.Lerp(baseDelay, maxDelay, Mathf.Pow(progress, 2));
                    int scrambleSteps = 3;
                    for (int j = 0; j < scrambleSteps; j++)
                    {
                        if (!registered || Player.Local.Health.IsAlive) yield break;

                        char[] charsCurr = deathTitleText.text.ToCharArray();
                        charsCurr[k] = flatlined[UnityEngine.Random.Range(0, flatlined.Length)];
                        deathTitleText.text = new string(charsCurr);
                        yield return new WaitForSeconds(currentWait / 3f);
                    }
                    char[] finalChars = deathTitleText.text.ToCharArray();
                    finalChars[k] = flatlined[k];
                    deathTitleText.text = new string(finalChars);

                    yield return new WaitForSeconds(currentWait);
                }
            }
            else
            {
                Log("Death title text is null");
            }
            yield break;
        }

        public static IEnumerator WaitUntilRespawn(string originalText)
        {
            Log("Detected multiplayer instance where permanent death is disabled");
#if MONO
            WaitUntil waitObj = new WaitUntil(() => !registered || Player.Local.Health.IsAlive);
#else
            WaitUntil waitObj = new WaitUntil((Il2CppSystem.Func<bool>)(() => !registered || Player.Local.Health.IsAlive));
#endif
            yield return waitObj;

            // And then wait here because the animate must end if button was pressed before it ends
            yield return Wait025;
            if (!registered) yield break;

            Log("Player respawned resetting death screen text");
            if (deathTitleText != null)
                deathTitleText.text = originalText;
            yield break;
        }

        public static void OnReturnToMenuClicked()
        {
            Log("Returning to menu...");
            BeginPermanentDeathCleanup();
        }

        public static IEnumerator FinishPermanentDeathAfterDelay()
        {
            // This must not depend on the death-screen button or scaled game time.
            float deadline = Time.realtimeSinceStartup + 30f;
            while (registered && Time.realtimeSinceStartup < deadline &&
                   (Singleton<DeathScreen>.Instance == null || !Singleton<DeathScreen>.Instance.isOpen))
                yield return null;
            if (Singleton<DeathScreen>.Instance == null || !Singleton<DeathScreen>.Instance.isOpen)
            {
                MelonLogger.Error("Flatline: death screen did not open; automatic save deletion cancelled.");
                yield break;
            }
            yield return new WaitForSecondsRealtime(5f);
            if (registered && isQueuedForDeath && currentConfig != null && currentConfig.PermanentDeath &&
                Player.Local != null && Player.Local.Health != null && !Player.Local.Health.IsAlive &&
                Singleton<DeathScreen>.Instance != null && Singleton<DeathScreen>.Instance.isOpen)
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
                manager.ExitToMenu(null, null, false);
                return;
            }

            SaveInfo currentInfo = manager.ActiveSaveInfo;
            if (currentInfo == null || string.IsNullOrWhiteSpace(currentInfo.SavePath))
            {
                MelonLogger.Error("Flatline: active save path unavailable; returning to menu without deleting anything.");
                manager.ExitToMenu(null, null, false);
                return;
            }

            MelonCoroutines.Start(ReturnAndDeleteSave(currentInfo));
        }

        public static IEnumerator ReturnAndDeleteSave(SaveInfo info)
        {
            if (!TryGetActiveSaveFolder(info.SavePath, info.SaveSlotNumber, out string path))
            {
                MelonLogger.Error("Flatline: refusing to delete an unrecognized save path.");
                Singleton<LoadManager>.Instance.ExitToMenu(null, null, false);
                yield break;
            }

            string orgName = info.OrganisationName;
            int slotNum = info.SaveSlotNumber;

            Singleton<LoadManager>.Instance.ExitToMenu(null, null, false);
#if IL2CPP
            string menuSceneName = SaveManager.MENU_SCENE_NAME;
#else
            string menuSceneName = "Menu";
#endif
            float deadline = Time.realtimeSinceStartup + 30f;
            bool menuReady = false;
            yield return null; // Let the game begin the transition before checking the scene.
            while (Time.realtimeSinceStartup < deadline)
            {
                LoadManager manager = Singleton<LoadManager>.Instance;
                if (SceneManager.GetActiveScene().name == menuSceneName && (manager == null || !manager.IsLoading))
                {
                    menuReady = true;
                    break;
                }
                yield return null;
            }
            if (!menuReady)
            {
                MelonLogger.Error("Flatline: menu did not finish loading; save deletion cancelled.");
                yield break;
            }

            // Give any save-on-exit callback time to finish writing before deletion.
            yield return new WaitForSecondsRealtime(1f);
            LoadManager loadManager = Singleton<LoadManager>.Instance;
            if (SceneManager.GetActiveScene().name != menuSceneName || (loadManager != null && loadManager.IsLoading))
            {
                MelonLogger.Error("Flatline: menu state changed; save deletion cancelled.");
                yield break;
            }

            Log("Deleting save data...");
            try
            {
                if (TryGetActiveSaveFolder(path, slotNum, out string checkedPath))
                {
                    Directory.Delete(checkedPath, true);
                    Log("Save data deleted succesfully");
                    RemoveSaveAssociatedData(orgName, slotNum);
                    if (Singleton<LoadManager>.Instance != null)
                        Singleton<LoadManager>.Instance.RefreshSaveInfo();
                }
                else
                {
                    MelonLogger.Error("Flatline: save path disappeared or is no longer safe; deletion cancelled.");
                }
            } catch (Exception ex)
            {
                MelonLogger.Error("Flatline: failed to delete save data: " + ex);
            }

            yield break;
        }

        // SaveInfo.SavePath must be exactly Saves/<profile>/SaveGame_<slot>.
        // Never delete a parent folder, another game folder, or a reparse point.
        private static bool TryGetActiveSaveFolder(string savePath, int slotNumber, out string fullPath)
        {
            string root = Path.Combine(Application.persistentDataPath, "Saves");
            return SaveFolderGuard.TryValidate(root, savePath, slotNumber, out fullPath);
        }

        #endregion


    }
}