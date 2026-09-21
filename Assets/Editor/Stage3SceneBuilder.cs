using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Weapons;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage3SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

        [MenuItem("Arena Survivor/Build Stage 3 Attack")]
        public static void AddAttackAndEnemyDeath()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddDeathReactionToEnemyPrefab();
            GameObject projectilePrefab = CreateProjectilePrefab();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage3SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            if (player.GetComponent<AutoAttackComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: Player already has an AutoAttackComponent — Stage 3 wiring already applied, skipping.");
                return;
            }

            AutoAttackComponent autoAttack = player.AddComponent<AutoAttackComponent>();
            var serialized = new SerializedObject(autoAttack);
            serialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 3 attack wired into " + ScenePath);
        }

        private static void AddDeathReactionToEnemyPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<EnemyDeathReaction>() == null)
            {
                prefabRoot.AddComponent<EnemyDeathReaction>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static GameObject CreateProjectilePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var projectile = new GameObject("Projectile");
            var renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.95f, 0.2f);
            renderer.sortingOrder = 8;
            projectile.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            BoxCollider2D collider = projectile.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            projectile.AddComponent<ProjectileComponent>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
            Object.DestroyImmediate(projectile);

            return prefab;
        }
    }
}
