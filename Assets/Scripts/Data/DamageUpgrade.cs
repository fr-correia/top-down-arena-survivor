using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "DamageUpgrade", menuName = "Arena Survivor/Upgrades/Damage")]
    public class DamageUpgrade : Upgrade
    {
        [SerializeField] private int damageIncrease = 5;

        public override void Apply(GameObject player)
        {
            AutoAttackComponent autoAttack = player.GetComponent<AutoAttackComponent>();
            if (autoAttack == null)
            {
                Debug.LogError("DamageUpgrade: player has no AutoAttackComponent");
                return;
            }

            autoAttack.IncreaseDamage(damageIncrease);
        }
    }
}
