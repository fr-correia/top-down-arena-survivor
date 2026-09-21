using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyDeathReaction : MonoBehaviour
    {
        private HealthComponent healthComponent;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            healthComponent.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            PooledObject pooled = GetComponent<PooledObject>();
            if (pooled != null)
            {
                pooled.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
