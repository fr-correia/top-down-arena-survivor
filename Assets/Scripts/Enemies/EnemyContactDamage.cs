using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float damageInterval = 1f;

        private float lastHitTime = float.NegativeInfinity;

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (Time.time - lastHitTime < damageInterval)
            {
                return;
            }

            HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.ApplyDamage(damageAmount);
            lastHitTime = Time.time;
        }
    }
}
