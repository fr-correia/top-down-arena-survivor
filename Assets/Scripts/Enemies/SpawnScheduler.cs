using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    public class SpawnScheduler
    {
        private readonly float initialSpawnInterval;
        private readonly float minSpawnInterval;
        private readonly float spawnIntervalDecreasePerMinute;
        private readonly int initialEnemiesPerSpawn;
        private readonly float enemiesPerSpawnIncreasePerMinute;

        private float lastSpawnTime = float.NegativeInfinity;

        public SpawnScheduler(float initialSpawnInterval, float minSpawnInterval, float spawnIntervalDecreasePerMinute, int initialEnemiesPerSpawn, float enemiesPerSpawnIncreasePerMinute)
        {
            this.initialSpawnInterval = initialSpawnInterval;
            this.minSpawnInterval = minSpawnInterval;
            this.spawnIntervalDecreasePerMinute = spawnIntervalDecreasePerMinute;
            this.initialEnemiesPerSpawn = initialEnemiesPerSpawn;
            this.enemiesPerSpawnIncreasePerMinute = enemiesPerSpawnIncreasePerMinute;
        }

        public float ComputeSpawnInterval(float elapsedTime)
        {
            float minutesElapsed = elapsedTime / 60f;
            float interval = initialSpawnInterval - spawnIntervalDecreasePerMinute * minutesElapsed;
            return Mathf.Max(minSpawnInterval, interval);
        }

        public int ComputeEnemiesPerSpawn(float elapsedTime)
        {
            float minutesElapsed = elapsedTime / 60f;
            int count = initialEnemiesPerSpawn + Mathf.FloorToInt(enemiesPerSpawnIncreasePerMinute * minutesElapsed);
            return Mathf.Max(1, count);
        }

        public bool TryConsumeSpawnTick(float currentTime, float elapsedTime)
        {
            float interval = ComputeSpawnInterval(elapsedTime);
            if (currentTime - lastSpawnTime < interval)
            {
                return false;
            }

            lastSpawnTime = currentTime;
            return true;
        }
    }
}
