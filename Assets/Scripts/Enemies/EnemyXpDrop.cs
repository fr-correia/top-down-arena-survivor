using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyXpDrop : MonoBehaviour
    {
        [SerializeField] private GameObject xpOrbPrefab;

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
            if (xpOrbPrefab != null)
            {
                Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
