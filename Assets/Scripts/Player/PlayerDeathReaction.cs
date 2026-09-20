using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerDeathReaction : MonoBehaviour
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
            Debug.Log("Player died");
            gameObject.SetActive(false);
        }
    }
}
