using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class CameraFollow
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-followSpeed * deltaTime);
            return Vector2.Lerp(currentPosition, targetPosition, t);
        }

        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime, Bounds bounds)
        {
            Vector2 next = ComputeNextPosition(currentPosition, targetPosition, followSpeed, deltaTime);
            next.x = Mathf.Clamp(next.x, bounds.min.x, bounds.max.x);
            next.y = Mathf.Clamp(next.y, bounds.min.y, bounds.max.y);
            return next;
        }
    }
}
