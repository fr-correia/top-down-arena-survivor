using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class GameFlowTests
    {
        [Test]
        public void StartGame_FromMainMenu_TransitionsToPlaying_FiresEventOnce()
        {
            var flow = new GameFlow();
            int eventCount = 0;
            GameFlowState lastState = GameFlowState.MainMenu;
            flow.OnStateChanged += state => { eventCount++; lastState = state; };

            flow.StartGame();

            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(GameFlowState.Playing, lastState);
        }

        [Test]
        public void StartGame_FromNonMainMenuState_IsNoOp()
        {
            var flow = new GameFlow();
            flow.StartGame();
            int eventCount = 0;
            flow.OnStateChanged += _ => eventCount++;

            flow.StartGame();

            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void EndGame_FromPlaying_TransitionsToGameOver()
        {
            var flow = new GameFlow();
            flow.StartGame();

            flow.EndGame();

            Assert.AreEqual(GameFlowState.GameOver, flow.State);
        }

        [Test]
        public void EndGame_FromNonPlayingState_IsNoOp()
        {
            var flow = new GameFlow();

            flow.EndGame();

            Assert.AreEqual(GameFlowState.MainMenu, flow.State);
        }

        [Test]
        public void ReturnToMenu_AlwaysLandsOnMainMenu()
        {
            var flow = new GameFlow();
            flow.StartGame();
            flow.EndGame();

            flow.ReturnToMenu();

            Assert.AreEqual(GameFlowState.MainMenu, flow.State);
        }
    }
}
