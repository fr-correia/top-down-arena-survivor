using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArenaSurvivor.Data;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class UpgradeChoiceComponent : MonoBehaviour
    {
        [SerializeField] private Upgrade[] availableUpgrades;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Text[] optionTitles;
        [SerializeField] private Text[] optionDescriptions;

        private UpgradeSelector selector;
        private PlayerLevelingComponent playerLeveling;
        private Upgrade[] currentOptions;

        private void Awake()
        {
            selector = new UpgradeSelector(new System.Random());
            playerLeveling = UnityEngine.Object.FindFirstObjectByType<PlayerLevelingComponent>();
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (playerLeveling != null)
            {
                playerLeveling.OnLevelUp += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            if (playerLeveling != null)
            {
                playerLeveling.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            List<Upgrade> options = selector.SelectRandomUnique(availableUpgrades, optionButtons.Length);
            currentOptions = options.ToArray();

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < currentOptions.Length)
                {
                    Upgrade upgrade = currentOptions[i];
                    optionTitles[i].text = upgrade.DisplayName;
                    optionDescriptions[i].text = upgrade.Description;
                    optionButtons[i].gameObject.SetActive(true);

                    int optionIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectUpgrade(optionIndex));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            panelRoot.SetActive(true);
            Time.timeScale = 0f;
        }

        private void SelectUpgrade(int optionIndex)
        {
            currentOptions[optionIndex].Apply(playerLeveling.gameObject);
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
