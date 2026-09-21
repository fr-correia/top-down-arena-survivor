using System.Collections.Generic;

namespace ArenaSurvivor.Enemies
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyChaseComponent> activeEnemies = new List<EnemyChaseComponent>();

        public static IReadOnlyList<EnemyChaseComponent> ActiveEnemies => activeEnemies;

        public static void Register(EnemyChaseComponent enemy)
        {
            activeEnemies.Add(enemy);
        }

        public static void Unregister(EnemyChaseComponent enemy)
        {
            activeEnemies.Remove(enemy);
        }
    }
}
