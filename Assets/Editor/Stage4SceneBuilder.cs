using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage4SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string XpOrbPrefabPath = "Assets/Prefabs/XpOrb.prefab";

        [MenuItem("Arena Survivor/Build Stage 4 XP And Leveling")]
        public static void AddXpAndLeveling()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            GameObject xpOrbPrefab = CreateXpOrbPrefab();
            AddXpDropToEnemyPrefab(xpOrbPrefab);

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage4SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            if (player.GetComponent<PlayerLevelingComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: Player already has a PlayerLevelingComponent — Stage 4 wiring already applied, skipping.");
                return;
            }

            player.AddComponent<PlayerLevelingComponent>();

            BuildXpBarUI();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 4 XP and leveling wired into " + ScenePath);
        }

        private static void AddXpDropToEnemyPrefab(GameObject xpOrbPrefab)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<EnemyXpDrop>() == null)
            {
                EnemyXpDrop drop = prefabRoot.AddComponent<EnemyXpDrop>();
                var serialized = new SerializedObject(drop);
                serialized.FindProperty("xpOrbPrefab").objectReferenceValue = xpOrbPrefab;
                serialized.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static GameObject CreateXpOrbPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var orb = new GameObject("XpOrb");
            var renderer = orb.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.2f, 0.9f, 0.9f);
            renderer.sortingOrder = 3;
            orb.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            BoxCollider2D collider = orb.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            Rigidbody2D rb = orb.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            orb.AddComponent<ExperienceOrbComponent>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(orb, XpOrbPrefabPath);
            Object.DestroyImmediate(orb);

            return prefab;
        }

        private static void BuildXpBarUI()
        {
            var canvasObject = new GameObject("XpBarCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var backgroundObject = new GameObject("XpBarBackground");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 1f);
            backgroundRect.anchorMax = new Vector2(0.5f, 1f);
            backgroundRect.pivot = new Vector2(0.5f, 1f);
            backgroundRect.anchoredPosition = new Vector2(0f, -20f);
            backgroundRect.sizeDelta = new Vector2(300f, 20f);

            var fillObject = new GameObject("XpBarFill");
            fillObject.transform.SetParent(backgroundObject.transform, false);
            Image fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.2f, 0.9f, 0.3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("XpBarLevelText");
            textObject.transform.SetParent(canvasObject.transform, false);
            Text levelText = textObject.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 18;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = Color.white;
            levelText.text = "Lv. 1";
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -45f);
            textRect.sizeDelta = new Vector2(300f, 25f);

            XpBarComponent xpBar = canvasObject.AddComponent<XpBarComponent>();
            var serialized = new SerializedObject(xpBar);
            serialized.FindProperty("fillImage").objectReferenceValue = fill;
            serialized.FindProperty("levelText").objectReferenceValue = levelText;
            serialized.ApplyModifiedProperties();
        }
    }
}
