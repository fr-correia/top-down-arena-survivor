using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public static class PauseState
    {
        private static readonly HashSet<string> activeReasons = new HashSet<string>();

        public static bool IsPaused => activeReasons.Count > 0;

        public static void Pause(string reason)
        {
            activeReasons.Add(reason);
            Time.timeScale = 0f;
        }

        public static void Resume(string reason)
        {
            activeReasons.Remove(reason);
            if (activeReasons.Count == 0)
            {
                Time.timeScale = 1f;
            }
        }

        public static void ClearAll()
        {
            activeReasons.Clear();
            Time.timeScale = 1f;
        }
    }
}
