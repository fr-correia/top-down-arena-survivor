using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            var health = new Health(100);

            health.TakeDamage(30);

            Assert.AreEqual(70, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ClampsAtZero_NeverNegative()
        {
            var health = new Health(10);

            health.TakeDamage(999);

            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void OnDeath_FiresExactlyOnce_WhenHealthReachesZero()
        {
            var health = new Health(10);
            int deathCount = 0;
            health.OnDeath += () => deathCount++;

            health.TakeDamage(10);

            Assert.AreEqual(1, deathCount);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void OnDeath_DoesNotFireAgain_AfterDeath()
        {
            var health = new Health(10);
            int deathCount = 0;
            health.OnDeath += () => deathCount++;

            health.TakeDamage(10);
            health.TakeDamage(5);

            Assert.AreEqual(1, deathCount);
        }

        [Test]
        public void TakeDamage_IgnoresNonPositiveAmounts()
        {
            var health = new Health(100);

            health.TakeDamage(0);
            health.TakeDamage(-5);

            Assert.AreEqual(100, health.CurrentHealth);
        }
    }
}
