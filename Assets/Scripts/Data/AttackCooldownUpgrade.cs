using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "AttackCooldownUpgrade", menuName = "Arena Survivor/Upgrades/Attack Cooldown")]
    public class AttackCooldownUpgrade : Upgrade
    {
        [SerializeField] private float cooldownReduction = 0.1f;

        public override void Apply(GameObject player)
        {
            AutoAttackComponent autoAttack = player.GetComponent<AutoAttackComponent>();
            if (autoAttack == null)
            {
                Debug.LogError("AttackCooldownUpgrade: player has no AutoAttackComponent");
                return;
            }

            autoAttack.ReduceAttackInterval(cooldownReduction);
        }
    }
}
