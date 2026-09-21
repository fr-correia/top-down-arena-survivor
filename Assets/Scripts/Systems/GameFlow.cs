using System;

namespace ArenaSurvivor.Systems
{
    public enum GameFlowState
    {
        MainMenu,
        Playing,
        GameOver
    }

    public class GameFlow
    {
        public GameFlowState State { get; private set; } = GameFlowState.MainMenu;

        public event Action<GameFlowState> OnStateChanged;

        public void StartGame()
        {
            if (State != GameFlowState.MainMenu)
            {
                return;
            }

            SetState(GameFlowState.Playing);
        }

        public void EndGame()
        {
            if (State != GameFlowState.Playing)
            {
                return;
            }

            SetState(GameFlowState.GameOver);
        }

        public void ReturnToMenu()
        {
            SetState(GameFlowState.MainMenu);
        }

        private void SetState(GameFlowState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
