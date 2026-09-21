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

        [Test]
        public void IncreaseMaxHealth_RaisesMaxAndCurrentByAmount()
        {
            var health = new Health(50);
            health.TakeDamage(20);

            health.IncreaseMaxHealth(10);

            Assert.AreEqual(60, health.MaxHealth);
            Assert.AreEqual(40, health.CurrentHealth);
        }

        [Test]
        public void IncreaseMaxHealth_NonPositiveAmount_IsNoOp()
        {
            var health = new Health(50);

            health.IncreaseMaxHealth(0);
            health.IncreaseMaxHealth(-5);

            Assert.AreEqual(50, health.MaxHealth);
            Assert.AreEqual(50, health.CurrentHealth);
        }

        [Test]
        public void ResetHealth_RestoresCurrentHealthAndClearsIsDead()
        {
            var health = new Health(50);
            health.TakeDamage(50);

            health.ResetHealth();

            Assert.AreEqual(50, health.CurrentHealth);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void ResetHealth_AllowsTakeDamageToWorkAgain()
        {
            var health = new Health(50);
            health.TakeDamage(50);
            health.ResetHealth();

            health.TakeDamage(20);

            Assert.AreEqual(30, health.CurrentHealth);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void OnDamaged_FiresWithCorrectAmount_OnSuccessfulHit()
        {
            var health = new Health(100);
            int receivedAmount = 0;
            health.OnDamaged += amount => receivedAmount = amount;

            health.TakeDamage(15);

            Assert.AreEqual(15, receivedAmount);
        }

        [Test]
        public void OnDamaged_DoesNotFire_ForNoOpTakeDamage()
        {
            var health = new Health(100);
            int fireCount = 0;
            health.OnDamaged += _ => fireCount++;

            health.TakeDamage(0);
            health.TakeDamage(-5);

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void LethalHit_FiresBothOnDamagedAndOnDeath()
        {
            var health = new Health(10);
            bool damagedFired = false;
            bool deathFired = false;
            health.OnDamaged += _ => damagedFired = true;
            health.OnDeath += () => deathFired = true;

            health.TakeDamage(10);

            Assert.IsTrue(damagedFired);
            Assert.IsTrue(deathFired);
        }
    }
}
