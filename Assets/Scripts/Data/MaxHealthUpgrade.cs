using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "MaxHealthUpgrade", menuName = "Arena Survivor/Upgrades/Max Health")]
    public class MaxHealthUpgrade : Upgrade
    {
        [SerializeField] private int healthIncrease = 20;

        public override void Apply(GameObject player)
        {
            HealthComponent health = player.GetComponent<HealthComponent>();
            if (health == null)
            {
                Debug.LogError("MaxHealthUpgrade: player has no HealthComponent");
                return;
            }

            health.IncreaseMaxHealth(healthIncrease);
        }
    }
}
