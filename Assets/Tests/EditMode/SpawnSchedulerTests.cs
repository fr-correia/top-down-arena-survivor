using NUnit.Framework;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.Tests.EditMode
{
    public class SpawnSchedulerTests
    {
        [Test]
        public void ComputeSpawnInterval_DecreasesWithElapsedTime()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            float atStart = scheduler.ComputeSpawnInterval(0f);
            float afterOneMinute = scheduler.ComputeSpawnInterval(60f);

            Assert.AreEqual(2f, atStart, 0.0001f);
            Assert.AreEqual(1.8f, afterOneMinute, 0.0001f);
        }

        [Test]
        public void ComputeSpawnInterval_ClampsAtFloor()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            float afterTenMinutes = scheduler.ComputeSpawnInterval(600f);

            Assert.AreEqual(0.3f, afterTenMinutes, 0.0001f);
        }

        [Test]
        public void ComputeEnemiesPerSpawn_IncreasesWithElapsedTime_NeverBelowOne()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            int atStart = scheduler.ComputeEnemiesPerSpawn(0f);
            int afterTwoMinutes = scheduler.ComputeEnemiesPerSpawn(120f);

            Assert.AreEqual(1, atStart);
            Assert.AreEqual(2, afterTwoMinutes);
        }

        [Test]
        public void TryConsumeSpawnTick_GatesOnCurrentRampedInterval()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            bool first = scheduler.TryConsumeSpawnTick(0f, 0f);
            bool tooSoon = scheduler.TryConsumeSpawnTick(1f, 1f);
            bool readyAtInitialInterval = scheduler.TryConsumeSpawnTick(2f, 2f);

            Assert.IsTrue(first);
            Assert.IsFalse(tooSoon);
            Assert.IsTrue(readyAtInitialInterval);
        }
    }
}
