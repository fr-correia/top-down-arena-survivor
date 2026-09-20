using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Tests.EditMode
{
    public class CameraFollowTests
    {
        [Test]
        public void MovesTowardTarget()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 0f);

            Vector2 result = follow.ComputeNextPosition(current, target, 5f, 0.1f);

            Assert.Greater(result.x, current.x);
            Assert.LessOrEqual(result.x, target.x);
        }

        [Test]
        public void NeverOvershootsTargetInOneStep()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 0f);

            Vector2 result = follow.ComputeNextPosition(current, target, 50f, 1f);

            Assert.LessOrEqual(Vector2.Distance(result, target), Vector2.Distance(current, target));
        }

        [Test]
        public void ConvergesCloseToTargetAfterManySteps()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 5f);

            for (int i = 0; i < 200; i++)
            {
                current = follow.ComputeNextPosition(current, target, 5f, 0.02f);
            }

            Assert.Less(Vector2.Distance(current, target), 0.01f);
        }

        [Test]
        public void BoundsOverload_ClampsResultInsideBounds()
        {
            var follow = new CameraFollow();
            var bounds = new Bounds(Vector2.zero, new Vector3(4f, 4f, 0f));

            Vector2 result = follow.ComputeNextPosition(Vector2.zero, new Vector2(100f, 100f), 50f, 1f, bounds);

            Assert.LessOrEqual(result.x, bounds.max.x);
            Assert.LessOrEqual(result.y, bounds.max.y);
        }
    }
}
