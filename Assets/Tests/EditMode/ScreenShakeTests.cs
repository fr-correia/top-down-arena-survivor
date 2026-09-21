using System;
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ScreenShakeTests
    {
        [Test]
        public void ElapsedAtOrPastDuration_ReturnsZero()
        {
            var shake = new ScreenShake();
            var random = new System.Random(1);

            Vector2 result = shake.ComputeOffset(1f, 1f, 0.5f, random);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void OffsetMagnitude_NeverExceedsMagnitude()
        {
            var shake = new ScreenShake();
            var random = new System.Random(2);

            for (int i = 0; i < 50; i++)
            {
                float elapsed = i * 0.01f;
                Vector2 offset = shake.ComputeOffset(elapsed, 0.5f, 0.3f, random);

                Assert.LessOrEqual(Mathf.Abs(offset.x), 0.3f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), 0.3f);
            }
        }

        [Test]
        public void OffsetMagnitude_ShrinksAsElapsedApproachesDuration()
        {
            var shake = new ScreenShake();
            var earlyRandom = new System.Random(42);
            var lateRandom = new System.Random(42);

            Vector2 earlyOffset = shake.ComputeOffset(0f, 1f, 1f, earlyRandom);
            Vector2 lateOffset = shake.ComputeOffset(0.95f, 1f, 1f, lateRandom);

            Assert.Greater(earlyOffset.magnitude, lateOffset.magnitude);
        }

        [Test]
        public void ZeroOrNegativeDuration_ReturnsZero()
        {
            var shake = new ScreenShake();
            var random = new System.Random(5);

            Vector2 result = shake.ComputeOffset(0f, 0f, 0.5f, random);

            Assert.AreEqual(Vector2.zero, result);
        }
    }
}
