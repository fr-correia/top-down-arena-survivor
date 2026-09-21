using UnityEngine;
using UnityEngine.UI;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class XpBarComponent : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text levelText;

        private PlayerLevelingComponent playerLeveling;

        private void Awake()
        {
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
        }

        private void Update()
        {
            if (playerLeveling == null)
            {
                return;
            }

            fillImage.fillAmount = (float)playerLeveling.CurrentXp / playerLeveling.XpToNextLevel;
            levelText.text = "Lv. " + playerLeveling.Level;
        }
    }
}
