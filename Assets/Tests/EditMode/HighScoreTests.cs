using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class HighScoreTests
    {
        [Test]
        public void TrySubmit_SavesAndReturnsTrue_WhenNewScoreExceedsBest()
        {
            float stored = 10f;
            var highScore = new HighScore(() => stored, v => stored = v);

            bool result = highScore.TrySubmit(20f);

            Assert.IsTrue(result);
            Assert.AreEqual(20f, stored);
        }

        [Test]
        public void TrySubmit_DoesNotSave_WhenNewScoreDoesNotExceedBest()
        {
            float stored = 10f;
            var highScore = new HighScore(() => stored, v => stored = v);

            bool result = highScore.TrySubmit(5f);

            Assert.IsFalse(result);
            Assert.AreEqual(10f, stored);
        }

        [Test]
        public void Best_ReflectsInjectedLoadFunction()
        {
            var highScore = new HighScore(() => 42f, v => { });

            Assert.AreEqual(42f, highScore.Best);
        }
    }
}
