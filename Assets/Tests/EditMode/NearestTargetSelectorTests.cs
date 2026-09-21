using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Tests.EditMode
{
    public class NearestTargetSelectorTests
    {
        [Test]
        public void EmptyList_ReturnsNegativeOne()
        {
            var candidates = new List<Vector2>();

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void SingleCandidate_ReturnsIndexZero()
        {
            var candidates = new List<Vector2> { new Vector2(5f, 5f) };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void MultipleCandidates_ReturnsClosest()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(10f, 0f),
                new Vector2(2f, 0f),
                new Vector2(5f, 0f)
            };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void TiedDistances_ReturnsFirstOccurrence()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(3f, 0f),
                new Vector2(0f, 3f)
            };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(0, result);
        }
    }
}
