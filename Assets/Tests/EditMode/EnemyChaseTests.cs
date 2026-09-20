using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.Tests.EditMode
{
    public class EnemyChaseTests
    {
        [Test]
        public void MovesTowardTarget()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(Vector2.zero, new Vector2(10f, 0f), 3f, 1f);

            Assert.AreEqual(new Vector2(3f, 0f), result);
        }

        [Test]
        public void NeverOvershootsTarget()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(Vector2.zero, new Vector2(1f, 0f), 100f, 1f);

            Assert.AreEqual(new Vector2(1f, 0f), result);
        }

        [Test]
        public void ZeroSpeed_ProducesNoMovement()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(new Vector2(5f, 5f), new Vector2(10f, 10f), 0f, 1f);

            Assert.AreEqual(new Vector2(5f, 5f), result);
        }

        [Test]
        public void ReachesTargetExactly_WhenCloseEnough()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(new Vector2(9.5f, 0f), new Vector2(10f, 0f), 3f, 1f);

            Assert.AreEqual(new Vector2(10f, 0f), result);
        }
    }
}
