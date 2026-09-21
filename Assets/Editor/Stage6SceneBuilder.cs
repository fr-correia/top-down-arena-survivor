using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage6SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
        private const string WaveDefinitionPath = "Assets/ScriptableObjects/Enemies/WaveDefinition.asset";

        [MenuItem("Arena Survivor/Build Stage 6 Spawner And Pooling")]
        public static void AddSpawnerAndPooling()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddPooledObjectToPrefab(EnemyPrefabPath);
            AddPooledObjectToPrefab(ProjectilePrefabPath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<EnemySpawnerComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: EnemySpawnerComponent already present — Stage 6 wiring already applied, skipping.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            RemoveHandPlacedEnemy();

            // Create the WaveDefinition asset AFTER the scene is open, and use it
            // immediately afterward with no further scene load in between. EditorSceneManager.OpenScene
            // triggers Unity's unused-asset unload pass; a freshly created ScriptableObject
            // instance that nothing yet references (as this one does, before it's assigned
            // onto the EnemySpawnerComponent below) can be invalidated by that pass even
            // though the .asset file itself is already written to disk with a valid GUID.
            // Creating it after the scene load — instead of before it — avoids that window
            // entirely (same lesson as Stage5SceneBuilder's CreateUpgradeAssets).
            WaveDefinition wave = CreateWaveDefinitionAsset();

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            EnemySpawnerComponent spawner = BuildSpawner(wave, enemyPrefab, player.transform);

            if (!VerifyWiring(spawner))
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder wiring verification failed — scene NOT saved.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 6 spawner and pooling wired into " + ScenePath);
        }

        private static void AddPooledObjectToPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabRoot.GetComponent<PooledObject>() == null)
            {
                prefabRoot.AddComponent<PooledObject>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static WaveDefinition CreateWaveDefinitionAsset()
        {
            WaveDefinition existing = AssetDatabase.LoadAssetAtPath<WaveDefinition>(WaveDefinitionPath);
            if (existing != null)
            {
                return existing;
            }

            System.IO.Directory.CreateDirectory("Assets/ScriptableObjects/Enemies");

            WaveDefinition wave = ScriptableObject.CreateInstance<WaveDefinition>();
            AssetDatabase.CreateAsset(wave, WaveDefinitionPath);
            return wave;
        }

        private static void RemoveHandPlacedEnemy()
        {
            GameObject existingEnemy = GameObject.Find("Enemy");
            if (existingEnemy != null)
            {
                Object.DestroyImmediate(existingEnemy);
            }
        }

        private static EnemySpawnerComponent BuildSpawner(WaveDefinition wave, GameObject enemyPrefab, Transform target)
        {
            var spawnerObject = new GameObject("EnemySpawner");
            EnemySpawnerComponent spawner = spawnerObject.AddComponent<EnemySpawnerComponent>();

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("wave").objectReferenceValue = wave;
            serialized.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.ApplyModifiedProperties();

            return spawner;
        }

        private static bool VerifyWiring(EnemySpawnerComponent spawner)
        {
            var serialized = new SerializedObject(spawner);
            bool ok = true;

            ok &= VerifyField(serialized, "wave");
            ok &= VerifyField(serialized, "enemyPrefab");
            ok &= VerifyField(serialized, "target");

            return ok;
        }

        private static bool VerifyField(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder verification failed — '" + propertyName + "' is null.");
                return false;
            }

            return true;
        }
    }
}
