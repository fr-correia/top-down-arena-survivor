using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;

        private Health health;

        public event Action OnDeath;
        public event Action<int> OnDamaged;

        private void Awake()
        {
            health = new Health(maxHealth);
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }

        public void ApplyDamage(int amount)
        {
            health.TakeDamage(amount);
        }

        public void IncreaseMaxHealth(int amount)
        {
            health.IncreaseMaxHealth(amount);
        }

        public void ResetHealth()
        {
            health.ResetHealth();
        }

        private void HandleDeath()
        {
            OnDeath?.Invoke();
        }

        private void HandleDamaged(int amount)
        {
            OnDamaged?.Invoke(amount);
        }
    }
}
