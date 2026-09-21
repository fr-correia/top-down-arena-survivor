using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class GameFlowComponent : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text finalTimeText;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private RunTimerComponent runTimer;
        [SerializeField] private HighScoreComponent highScoreComponent;
        [SerializeField] private HealthComponent playerHealth;

        private GameFlow flow;

        private void Awake()
        {
            flow = new GameFlow();
            flow.OnStateChanged += HandleStateChanged;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += HandlePlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath -= HandlePlayerDeath;
            }
        }

        private void Start()
        {
            HandleStateChanged(flow.State);
        }

        public void StartGame()
        {
            PauseState.Resume("menu");
            flow.StartGame();
        }

        public void RestartGame()
        {
            PauseState.ClearAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void HandlePlayerDeath()
        {
            flow.EndGame();
        }

        private void HandleStateChanged(GameFlowState state)
        {
            mainMenuPanel.SetActive(state == GameFlowState.MainMenu);
            gameOverPanel.SetActive(state == GameFlowState.GameOver);

            switch (state)
            {
                case GameFlowState.MainMenu:
                    PauseState.Pause("menu");
                    break;
                case GameFlowState.Playing:
                    runTimer.StartTimer();
                    break;
                case GameFlowState.GameOver:
                    runTimer.StopTimer();
                    bool isNewBest = highScoreComponent.TrySubmit(runTimer.ElapsedSeconds);
                    finalTimeText.text = FormatTime(runTimer.ElapsedSeconds);
                    bestTimeText.text = "Best: " + FormatTime(highScoreComponent.Best) + (isNewBest ? " (New!)" : "");
                    PauseState.Pause("gameover");
                    break;
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, secs);
        }
    }
}
