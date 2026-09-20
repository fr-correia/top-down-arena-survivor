using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class PlayerMovement
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 moveInput, float speed, float deltaTime)
        {
            Vector2 direction = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            return currentPosition + direction * speed * deltaTime;
        }
    }
}
