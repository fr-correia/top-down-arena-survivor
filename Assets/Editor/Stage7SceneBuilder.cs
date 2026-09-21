using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Player;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage7SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";

        [MenuItem("Arena Survivor/Build Stage 7 Polish")]
        public static void AddPolish()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddHitFlashToEnemyPrefab();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<GameFlowComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: GameFlowComponent already present — Stage 7 wiring already applied, skipping.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder could not find the Main Camera in " + ScenePath);
                return;
            }

            HealthComponent playerHealth = player.GetComponent<HealthComponent>();

            AddPlayerAndCameraComponents(player, mainCamera.gameObject);
            EnsureEventSystem();

            GameFlowComponent gameFlow = BuildFlowUI(playerHealth);

            if (!VerifyWiring(gameFlow))
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder wiring verification failed — scene NOT saved.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 7 polish wired into " + ScenePath);
        }

        private static void AddHitFlashToEnemyPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<HitFlashComponent>() == null)
            {
                prefabRoot.AddComponent<HitFlashComponent>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void AddPlayerAndCameraComponents(GameObject player, GameObject cameraObject)
        {
            if (player.GetComponent<HitFlashComponent>() == null)
            {
                player.AddComponent<HitFlashComponent>();
            }

            if (player.GetComponent<PlayerFeedbackComponent>() == null)
            {
                player.AddComponent<PlayerFeedbackComponent>();
            }

            if (cameraObject.GetComponent<ScreenShakeComponent>() == null)
            {
                cameraObject.AddComponent<ScreenShakeComponent>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static GameFlowComponent BuildFlowUI(HealthComponent playerHealth)
        {
            var canvasObject = new GameObject("FlowCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject mainMenuPanel = BuildMainMenuPanel(canvasObject.transform, out Button startButton);
            GameObject gameOverPanel = BuildGameOverPanel(canvasObject.transform, out Button restartButton, out Text finalTimeText, out Text bestTimeText);
            Text runTimerText = CreateText(canvasObject.transform, "RunTimerText", "00:00", 24, new Vector2(0.9f, 0.95f), new Vector2(200f, 50f));

            RunTimerComponent runTimer = canvasObject.AddComponent<RunTimerComponent>();
            var runTimerSerialized = new SerializedObject(runTimer);
            runTimerSerialized.FindProperty("timerText").objectReferenceValue = runTimerText;
            runTimerSerialized.ApplyModifiedProperties();

            HighScoreComponent highScoreComponent = canvasObject.AddComponent<HighScoreComponent>();

            GameFlowComponent gameFlow = canvasObject.AddComponent<GameFlowComponent>();
            var flowSerialized = new SerializedObject(gameFlow);
            flowSerialized.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            flowSerialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            flowSerialized.FindProperty("finalTimeText").objectReferenceValue = finalTimeText;
            flowSerialized.FindProperty("bestTimeText").objectReferenceValue = bestTimeText;
            flowSerialized.FindProperty("runTimer").objectReferenceValue = runTimer;
            flowSerialized.FindProperty("highScoreComponent").objectReferenceValue = highScoreComponent;
            flowSerialized.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            flowSerialized.FindProperty("startButton").objectReferenceValue = startButton;
            flowSerialized.FindProperty("restartButton").objectReferenceValue = restartButton;
            flowSerialized.ApplyModifiedProperties();

            return gameFlow;
        }

        private static GameObject BuildMainMenuPanel(Transform parent, out Button startButton)
        {
            GameObject panel = CreateFullScreenPanel(parent, "MainMenuPanel");

            CreateText(panel.transform, "Title", "Arena Survivor", 48, new Vector2(0.5f, 0.65f), new Vector2(600f, 80f));
            startButton = CreateButton(panel.transform, "StartButton", "Start", new Vector2(0.5f, 0.4f));

            return panel;
        }

        private static GameObject BuildGameOverPanel(Transform parent, out Button restartButton, out Text finalTimeText, out Text bestTimeText)
        {
            GameObject panel = CreateFullScreenPanel(parent, "GameOverPanel");

            CreateText(panel.transform, "Title", "Game Over", 48, new Vector2(0.5f, 0.7f), new Vector2(600f, 80f));
            finalTimeText = CreateText(panel.transform, "FinalTimeText", "00:00", 28, new Vector2(0.5f, 0.55f), new Vector2(400f, 50f));
            bestTimeText = CreateText(panel.transform, "BestTimeText", "Best: 00:00", 22, new Vector2(0.5f, 0.48f), new Vector2(400f, 50f));
            restartButton = CreateButton(panel.transform, "RestartButton", "Play Again", new Vector2(0.5f, 0.3f));

            panel.SetActive(false);

            return panel;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.85f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchorPosition, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f);
            Button button = buttonObject.AddComponent<Button>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(240f, 60f);

            CreateText(buttonObject.transform, "Label", label, 24, new Vector2(0.5f, 0.5f), new Vector2(220f, 50f));

            return button;
        }

        private static bool VerifyWiring(GameFlowComponent gameFlow)
        {
            var serialized = new SerializedObject(gameFlow);
            bool ok = true;

            ok &= VerifyField(serialized, "mainMenuPanel");
            ok &= VerifyField(serialized, "gameOverPanel");
            ok &= VerifyField(serialized, "finalTimeText");
            ok &= VerifyField(serialized, "bestTimeText");
            ok &= VerifyField(serialized, "runTimer");
            ok &= VerifyField(serialized, "highScoreComponent");
            ok &= VerifyField(serialized, "playerHealth");
            ok &= VerifyField(serialized, "startButton");
            ok &= VerifyField(serialized, "restartButton");

            return ok;
        }

        private static bool VerifyField(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder verification failed — '" + propertyName + "' is null.");
                return false;
            }

            return true;
        }
    }
}
