using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerFeedbackComponent : MonoBehaviour
    {
        [SerializeField] private float hitShakeDuration = 0.15f;
        [SerializeField] private float hitShakeMagnitude = 0.15f;
        [SerializeField] private float deathShakeDuration = 0.4f;
        [SerializeField] private float deathShakeMagnitude = 0.35f;

        private HealthComponent healthComponent;
        private ScreenShakeComponent screenShake;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            screenShake = Object.FindFirstObjectByType<ScreenShakeComponent>();
        }

        private void OnEnable()
        {
            healthComponent.OnDamaged += HandleDamaged;
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            healthComponent.OnDamaged -= HandleDamaged;
            healthComponent.OnDeath -= HandleDeath;
        }

        private void HandleDamaged(int amount)
        {
            if (screenShake != null)
            {
                screenShake.Shake(hitShakeDuration, hitShakeMagnitude);
            }
        }

        private void HandleDeath()
        {
            if (screenShake != null)
            {
                screenShake.Shake(deathShakeDuration, deathShakeMagnitude);
            }
        }
    }
}
