using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ArenaSurvivor.Data;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage5SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string UpgradesFolder = "Assets/ScriptableObjects/Upgrades";

        [MenuItem("Arena Survivor/Build Stage 5 Upgrade Screen")]
        public static void AddUpgradeScreen()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            Upgrade[] upgrades = CreateUpgradeAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<UpgradeChoiceComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: UpgradeChoiceComponent already present — Stage 5 wiring already applied, skipping.");
                return;
            }

            EnsureEventSystem();
            BuildUpgradeChoiceUI(upgrades);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 5 upgrade screen wired into " + ScenePath);
        }

        private static Upgrade[] CreateUpgradeAssets()
        {
            System.IO.Directory.CreateDirectory(UpgradesFolder);

            var upgrades = new Upgrade[4];
            upgrades[0] = CreateUpgradeAsset<DamageUpgrade>("DamageUpgrade", "More Firepower", "Increases projectile damage.");
            upgrades[1] = CreateUpgradeAsset<MoveSpeedUpgrade>("MoveSpeedUpgrade", "Quick Feet", "Increases movement speed.");
            upgrades[2] = CreateUpgradeAsset<AttackCooldownUpgrade>("AttackCooldownUpgrade", "Rapid Fire", "Reduces time between shots.");
            upgrades[3] = CreateUpgradeAsset<MaxHealthUpgrade>("MaxHealthUpgrade", "Vitality", "Increases max health and heals.");

            return upgrades;
        }

        private static T CreateUpgradeAsset<T>(string fileName, string displayName, string description) where T : Upgrade
        {
            string path = UpgradesFolder + "/" + fileName + ".asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T upgrade = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(upgrade);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(upgrade, path);
            return upgrade;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildUpgradeChoiceUI(Upgrade[] upgrades)
        {
            var canvasObject = new GameObject("UpgradeChoiceCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("UpgradePanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelBackground = panelObject.AddComponent<Image>();
            panelBackground.color = new Color(0f, 0f, 0f, 0.7f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var buttons = new Button[3];
            var titles = new Text[3];
            var descriptions = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                float xOffset = (i - 1) * 350f;
                CreateOptionButton(panelObject.transform, xOffset, out buttons[i], out titles[i], out descriptions[i]);
            }

            UpgradeChoiceComponent upgradeChoice = canvasObject.AddComponent<UpgradeChoiceComponent>();
            var serialized = new SerializedObject(upgradeChoice);
            SetObjectArray(serialized, "availableUpgrades", upgrades);
            serialized.FindProperty("panelRoot").objectReferenceValue = panelObject;
            SetObjectArray(serialized, "optionButtons", buttons);
            SetObjectArray(serialized, "optionTitles", titles);
            SetObjectArray(serialized, "optionDescriptions", descriptions);
            serialized.ApplyModifiedProperties();

            panelObject.SetActive(false);
        }

        private static void CreateOptionButton(Transform parent, float xOffset, out Button button, out Text title, out Text description)
        {
            var buttonObject = new GameObject("UpgradeOption");
            buttonObject.transform.SetParent(parent, false);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.3f);
            button = buttonObject.AddComponent<Button>();
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(xOffset, 0f);
            buttonRect.sizeDelta = new Vector2(300f, 200f);

            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(buttonObject.transform, false);
            title = titleObject.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 20;
            title.alignment = TextAnchor.UpperCenter;
            title.color = Color.white;
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.6f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var descriptionObject = new GameObject("Description");
            descriptionObject.transform.SetParent(buttonObject.transform, false);
            description = descriptionObject.AddComponent<Text>();
            description.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            description.fontSize = 14;
            description.alignment = TextAnchor.UpperCenter;
            description.color = Color.white;
            RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 0.6f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
