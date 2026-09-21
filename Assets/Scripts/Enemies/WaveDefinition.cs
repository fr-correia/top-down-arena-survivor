using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    [CreateAssetMenu(fileName = "WaveDefinition", menuName = "Arena Survivor/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        [SerializeField] private float initialSpawnInterval = 2f;
        [SerializeField] private float minSpawnInterval = 0.3f;
        [SerializeField] private float spawnIntervalDecreasePerMinute = 0.2f;
        [SerializeField] private int initialEnemiesPerSpawn = 1;
        [SerializeField] private float enemiesPerSpawnIncreasePerMinute = 0.5f;
        [SerializeField] private int maxActiveEnemies = 100;

        public float InitialSpawnInterval => initialSpawnInterval;
        public float MinSpawnInterval => minSpawnInterval;
        public float SpawnIntervalDecreasePerMinute => spawnIntervalDecreasePerMinute;
        public int InitialEnemiesPerSpawn => initialEnemiesPerSpawn;
        public float EnemiesPerSpawnIncreasePerMinute => enemiesPerSpawnIncreasePerMinute;
        public int MaxActiveEnemies => maxActiveEnemies;
    }
}
