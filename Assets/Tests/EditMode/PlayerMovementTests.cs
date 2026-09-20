using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Tests.EditMode
{
    public class PlayerMovementTests
    {
        [Test]
        public void ZeroInput_ProducesNoDisplacement()
        {
            var movement = new PlayerMovement();

            Vector2 result = movement.ComputeNextPosition(Vector2.zero, Vector2.zero, 5f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void StraightLineInput_MovesBySpeedTimesDeltaTime()
        {
            var movement = new PlayerMovement();

            Vector2 result = movement.ComputeNextPosition(Vector2.zero, Vector2.right, 5f, 0.5f);

            Assert.AreEqual(new Vector2(2.5f, 0f), result);
        }

        [Test]
        public void DiagonalInput_IsNormalized_SoSpeedMatchesAxisAligned()
        {
            var movement = new PlayerMovement();

            Vector2 diagonalResult = movement.ComputeNextPosition(Vector2.zero, new Vector2(1f, 1f), 5f, 1f);
            Vector2 straightResult = movement.ComputeNextPosition(Vector2.zero, Vector2.right, 5f, 1f);

            Assert.AreEqual(straightResult.magnitude, diagonalResult.magnitude, 0.0001f);
        }
    }
}
