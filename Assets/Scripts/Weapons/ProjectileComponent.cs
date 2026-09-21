using UnityEngine;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileComponent : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private int damage = 10;

        private Rigidbody2D rb;
        private Projectile projectile;
        private Vector2 velocity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projectile = new Projectile();
        }

        public void Launch(Vector2 direction)
        {
            velocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            Vector2 nextPosition = projectile.ComputeNextPosition(rb.position, velocity, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnemyChaseComponent>() == null)
            {
                return;
            }

            HealthComponent healthComponent = other.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.ApplyDamage(damage);
            Destroy(gameObject);
        }
    }
}
