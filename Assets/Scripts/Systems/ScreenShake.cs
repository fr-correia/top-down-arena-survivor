using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ScreenShake
    {
        public Vector2 ComputeOffset(float elapsedShakeTime, float duration, float magnitude, System.Random random)
        {
            if (elapsedShakeTime >= duration || duration <= 0f)
            {
                return Vector2.zero;
            }

            float remainingFraction = 1f - (elapsedShakeTime / duration);
            float x = ((float)random.NextDouble() * 2f - 1f) * magnitude * remainingFraction;
            float y = ((float)random.NextDouble() * 2f - 1f) * magnitude * remainingFraction;
            return new Vector2(x, y);
        }
    }
}
