using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;

        private Health health;

        public event Action OnDeath;

        private void Awake()
        {
            health = new Health(maxHealth);
            health.OnDeath += HandleDeath;
        }

        public void ApplyDamage(int amount)
        {
            health.TakeDamage(amount);
        }

        public void IncreaseMaxHealth(int amount)
        {
            health.IncreaseMaxHealth(amount);
        }

        private void HandleDeath()
        {
            OnDeath?.Invoke();
        }
    }
}
