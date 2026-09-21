using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class UpgradeSelectorTests
    {
        [Test]
        public void PoolSmallerThanCount_ReturnsWholePool()
        {
            var selector = new UpgradeSelector(new Random(1));
            var pool = new List<string> { "A", "B" };

            List<string> result = selector.SelectRandomUnique(pool, 5);

            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEquivalent(pool, result);
        }

        [Test]
        public void RequestingFewerThanPoolSize_ReturnsExactCountAllUniqueFromPool()
        {
            var selector = new UpgradeSelector(new Random(2));
            var pool = new List<string> { "A", "B", "C", "D", "E" };

            List<string> result = selector.SelectRandomUnique(pool, 3);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result.Distinct().Count());
            foreach (string item in result)
            {
                Assert.Contains(item, pool);
            }
        }

        [Test]
        public void ManySeeds_NeverProducesDuplicateInOneCall()
        {
            var pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

            for (int seed = 0; seed < 100; seed++)
            {
                var selector = new UpgradeSelector(new Random(seed));
                List<int> result = selector.SelectRandomUnique(pool, 4);

                Assert.AreEqual(4, result.Distinct().Count(), "Seed " + seed + " produced a duplicate");
            }
        }
    }
}
