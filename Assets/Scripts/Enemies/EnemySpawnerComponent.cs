using UnityEngine;
using UnityEngine.Pool;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    public class EnemySpawnerComponent : MonoBehaviour
    {
        [SerializeField] private WaveDefinition wave;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform target;
        [SerializeField] private float spawnRadius = 9f;

        private SpawnScheduler scheduler;
        private ObjectPool<GameObject> pool;
        private float elapsedTime;

        private void Awake()
        {
            scheduler = new SpawnScheduler(wave.InitialSpawnInterval, wave.MinSpawnInterval, wave.SpawnIntervalDecreasePerMinute, wave.InitialEnemiesPerSpawn, wave.EnemiesPerSpawnIncreasePerMinute);
            pool = new ObjectPool<GameObject>(CreateEnemy, OnGetEnemy, OnReleaseEnemy, OnDestroyEnemy);
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            if (!scheduler.TryConsumeSpawnTick(Time.time, elapsedTime))
            {
                return;
            }

            int count = scheduler.ComputeEnemiesPerSpawn(elapsedTime);
            for (int i = 0; i < count; i++)
            {
                if (pool.CountActive >= wave.MaxActiveEnemies)
                {
                    break;
                }

                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            GameObject enemy = pool.Get();

            float angle = Random.value * Mathf.PI * 2f;
            Vector2 spawnPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            enemy.transform.position = spawnPosition;

            enemy.GetComponent<EnemyChaseComponent>().SetTarget(target);
            enemy.GetComponent<HealthComponent>().ResetHealth();
        }

        private GameObject CreateEnemy()
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.GetComponent<PooledObject>().Initialize(go => pool.Release(go));
            return enemy;
        }

        private void OnGetEnemy(GameObject enemy)
        {
            enemy.SetActive(true);
        }

        private void OnReleaseEnemy(GameObject enemy)
        {
            enemy.SetActive(false);
        }

        private void OnDestroyEnemy(GameObject enemy)
        {
            Destroy(enemy);
        }
    }
}
