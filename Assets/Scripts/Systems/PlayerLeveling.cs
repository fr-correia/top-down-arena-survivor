using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PlayerLeveling
    {
        private const float BaseXp = 10f;
        private const float Exponent = 1.2f;

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int XpToNextLevel { get; private set; }

        public event Action<int> OnLevelUp;

        public PlayerLeveling()
        {
            XpToNextLevel = ComputeXpToNextLevel(Level);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXp += amount;

            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
                XpToNextLevel = ComputeXpToNextLevel(Level);
                OnLevelUp?.Invoke(Level);
            }
        }

        private static int ComputeXpToNextLevel(int level)
        {
            return Mathf.CeilToInt(BaseXp * Mathf.Pow(level, Exponent));
        }
    }
}
