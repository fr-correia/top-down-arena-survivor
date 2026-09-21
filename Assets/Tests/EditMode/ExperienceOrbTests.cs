using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ExperienceOrbTests
    {
        [Test]
        public void OutsideMagnetRadius_DoesNotMove()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(10f, 0f), Vector2.zero, 8f, 1f);

            Assert.AreEqual(new Vector2(10f, 0f), result);
        }

        [Test]
        public void InsideMagnetRadius_MovesTowardPlayerWithoutOvershooting()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(2f, 0f), Vector2.zero, 100f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ExactlyAtMagnetRadius_IsTreatedAsInRange()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(3f, 0f), Vector2.zero, 1f, 1f);

            Assert.AreEqual(new Vector2(2f, 0f), result);
        }
    }
}
