using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ExperienceOrb
    {
        public int XpValue { get; }
        public float MagnetRadius { get; }

        public ExperienceOrb(int xpValue, float magnetRadius)
        {
            XpValue = xpValue;
            MagnetRadius = magnetRadius;
        }

        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 playerPosition, float magnetSpeed, float deltaTime)
        {
            float distance = Vector2.Distance(currentPosition, playerPosition);
            if (distance > MagnetRadius)
            {
                return currentPosition;
            }

            return Vector2.MoveTowards(currentPosition, playerPosition, magnetSpeed * deltaTime);
        }
    }
}
