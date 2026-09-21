using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Tests.EditMode
{
    public class AutoAttackTests
    {
        [Test]
        public void TryGetShotDirection_EmptyTargetList_ReturnsFalse()
        {
            var autoAttack = new AutoAttack();
            var targets = new List<Vector2>();

            bool result = autoAttack.TryGetShotDirection(Vector2.zero, targets, 10f, out Vector2 direction);

            Assert.IsFalse(result);
            Assert.AreEqual(Vector2.zero, direction);
        }

        [Test]
        public void TryGetShotDirection_TargetWithinRange_ReturnsTrueWithNormalizedDirection()
        {
            var autoAttack = new AutoAttack();
            var targets = new List<Vector2> { new Vector2(5f, 0f) };

            bool result = autoAttack.TryGetShotDirection(Vector2.zero, targets, 10f, out Vector2 direction);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(1f, 0f), direction);
        }

        [Test]
        public void TryGetShotDirection_TargetBeyondRange_ReturnsFalse()
        {
            var autoAttack = new AutoAttack();
            var targets = new List<Vector2> { new Vector2(20f, 0f) };

            bool result = autoAttack.TryGetShotDirection(Vector2.zero, targets, 10f, out Vector2 direction);

            Assert.IsFalse(result);
            Assert.AreEqual(Vector2.zero, direction);
        }

        [Test]
        public void TryGetShotDirection_MultipleTargets_PicksDirectionTowardNearest()
        {
            var autoAttack = new AutoAttack();
            var targets = new List<Vector2>
            {
                new Vector2(9f, 0f),
                new Vector2(0f, 2f),
                new Vector2(-8f, 0f),
            };

            bool result = autoAttack.TryGetShotDirection(Vector2.zero, targets, 10f, out Vector2 direction);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(0f, 1f), direction);
        }
    }
}
