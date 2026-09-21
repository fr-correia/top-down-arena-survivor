using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "MoveSpeedUpgrade", menuName = "Arena Survivor/Upgrades/Move Speed")]
    public class MoveSpeedUpgrade : Upgrade
    {
        [SerializeField] private float speedIncrease = 1f;

        public override void Apply(GameObject player)
        {
            PlayerMovementComponent movement = player.GetComponent<PlayerMovementComponent>();
            if (movement == null)
            {
                Debug.LogError("MoveSpeedUpgrade: player has no PlayerMovementComponent");
                return;
            }

            movement.IncreaseSpeed(speedIncrease);
        }
    }
}
