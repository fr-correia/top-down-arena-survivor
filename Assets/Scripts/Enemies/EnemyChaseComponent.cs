using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyChaseComponent : MonoBehaviour
    {
        [SerializeField] private float chaseSpeed = 3f;
        [SerializeField] private Transform target;

        private Rigidbody2D rb;
        private EnemyChase enemyChase;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemyChase = new EnemyChase();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 nextPosition = enemyChase.ComputeNextPosition(rb.position, target.position, chaseSpeed, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }
    }
}
