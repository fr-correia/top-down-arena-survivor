using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    public class EnemyChase
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float speed, float deltaTime)
        {
            return Vector2.MoveTowards(currentPosition, targetPosition, speed * deltaTime);
        }
    }
}
