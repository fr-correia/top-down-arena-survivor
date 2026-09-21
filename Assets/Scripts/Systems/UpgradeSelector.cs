using System;
using System.Collections.Generic;

namespace ArenaSurvivor.Systems
{
    public class UpgradeSelector
    {
        private readonly Random random;

        public UpgradeSelector(Random random)
        {
            this.random = random;
        }

        public List<T> SelectRandomUnique<T>(IReadOnlyList<T> pool, int count)
        {
            var poolCopy = new List<T>(pool);
            var result = new List<T>();
            int selectCount = Math.Min(count, poolCopy.Count);

            for (int i = 0; i < selectCount; i++)
            {
                int index = random.Next(poolCopy.Count);
                result.Add(poolCopy[index]);
                poolCopy.RemoveAt(index);
            }

            return result;
        }
    }
}
