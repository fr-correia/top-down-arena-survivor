using System;

namespace ArenaSurvivor.Systems
{
    public class Health
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action OnDeath;

        public Health(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - amount);

            if (CurrentHealth == 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }
    }
}
