using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ProjectileTests
    {
        [Test]
        public void StraightLineMovement_MatchesVelocityTimesDeltaTime()
        {
            var projectile = new Projectile();

            Vector2 result = projectile.ComputeNextPosition(Vector2.zero, new Vector2(10f, 0f), 0.5f);

            Assert.AreEqual(new Vector2(5f, 0f), result);
        }

        [Test]
        public void ZeroVelocity_ProducesNoMovement()
        {
            var projectile = new Projectile();

            Vector2 result = projectile.ComputeNextPosition(new Vector2(3f, 3f), Vector2.zero, 1f);

            Assert.AreEqual(new Vector2(3f, 3f), result);
        }
    }
}
