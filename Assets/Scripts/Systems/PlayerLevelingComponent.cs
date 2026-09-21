using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PlayerLevelingComponent : MonoBehaviour
    {
        private PlayerLeveling leveling;

        public event Action<int> OnLevelUp;

        public int Level => leveling.Level;
        public int CurrentXp => leveling.CurrentXp;
        public int XpToNextLevel => leveling.XpToNextLevel;

        private void Awake()
        {
            leveling = new PlayerLeveling();
            leveling.OnLevelUp += HandleLevelUp;
        }

        public void AddExperience(int amount)
        {
            leveling.AddExperience(amount);
        }

        private void HandleLevelUp(int newLevel)
        {
            OnLevelUp?.Invoke(newLevel);
        }
    }
}
