using UnityEngine;

namespace ArenaSurvivor.Weapons
{
    public class Projectile
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 velocity, float deltaTime)
        {
            return currentPosition + velocity * deltaTime;
        }
    }
}
