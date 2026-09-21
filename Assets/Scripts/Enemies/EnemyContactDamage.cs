using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float damageInterval = 1f;

        private Cooldown cooldown;

        private void Awake()
        {
            cooldown = new Cooldown(damageInterval);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<EnemyChaseComponent>() != null)
            {
                return;
            }

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
