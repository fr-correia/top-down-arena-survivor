using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float damageInterval = 1f;

        private DamageCooldown cooldown;

        private void Awake()
        {
            cooldown = new DamageCooldown(damageInterval);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!cooldown.TryConsume(Time.time))
            {
                return;
            }

            HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.ApplyDamage(damageAmount);
        }
    }
}
