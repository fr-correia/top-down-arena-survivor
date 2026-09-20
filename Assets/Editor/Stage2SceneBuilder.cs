using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage2SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private static readonly Vector3 EnemySpawnPosition = new Vector3(5f, 5f, 0f);

        [MenuItem("Arena Survivor/Build Stage 2 Enemy")]
        public static void AddEnemyAndPlayerHealth()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage2SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            if (player.GetComponent<HealthComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: Player already has a HealthComponent — Stage 2 wiring already applied, skipping. Delete it manually first if you want to re-run with new settings.");
                return;
            }

            HealthComponent playerHealth = player.AddComponent<HealthComponent>();
            SetMaxHealth(playerHealth, 100);
            player.AddComponent<PlayerDeathReaction>();

            GameObject enemyPrefab = CreateEnemyPrefab();

            var enemyInstance = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemyInstance.transform.position = EnemySpawnPosition;
            EnemyChaseComponent chase = enemyInstance.GetComponent<EnemyChaseComponent>();
            chase.SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 2 enemy added to " + ScenePath);
        }

        private static GameObject CreateEnemyPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var enemy = new GameObject("Enemy");
            var renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.2f, 0.2f);
            renderer.sortingOrder = 5;
            enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            enemy.AddComponent<BoxCollider2D>();
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            SetMaxHealth(health, 30);

            enemy.AddComponent<EnemyChaseComponent>();
            enemy.AddComponent<EnemyContactDamage>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);

            return prefab;
        }

        private static void SetMaxHealth(HealthComponent healthComponent, int maxHealth)
        {
            var serialized = new SerializedObject(healthComponent);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.ApplyModifiedProperties();
        }
    }
}
