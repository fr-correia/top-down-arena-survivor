using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class PlayerLevelingTests
    {
        [Test]
        public void BelowThreshold_DoesNotLevelUp()
        {
            var leveling = new PlayerLeveling();
            int levelUpCount = 0;
            leveling.OnLevelUp += _ => levelUpCount++;

            leveling.AddExperience(5);

            Assert.AreEqual(1, leveling.Level);
            Assert.AreEqual(5, leveling.CurrentXp);
            Assert.AreEqual(0, levelUpCount);
        }

        [Test]
        public void CrossingThreshold_LevelsUpOnceWithRemainderCarried()
        {
            var leveling = new PlayerLeveling();
            int newLevel = 0;
            leveling.OnLevelUp += level => newLevel = level;

            leveling.AddExperience(13);

            Assert.AreEqual(2, leveling.Level);
            Assert.AreEqual(2, newLevel);
            Assert.AreEqual(3, leveling.CurrentXp);
        }

        [Test]
        public void LargeXpGain_TriggersMultipleLevelUps()
        {
            var leveling = new PlayerLeveling();
            int levelUpCount = 0;
            leveling.OnLevelUp += _ => levelUpCount++;

            leveling.AddExperience(1000);

            Assert.Greater(levelUpCount, 1);
            Assert.AreEqual(leveling.Level, 1 + levelUpCount);
            Assert.Less(leveling.CurrentXp, leveling.XpToNextLevel);
        }

        [Test]
        public void NonPositiveAmounts_AreIgnored()
        {
            var leveling = new PlayerLeveling();

            leveling.AddExperience(0);
            leveling.AddExperience(-5);

            Assert.AreEqual(0, leveling.CurrentXp);
            Assert.AreEqual(1, leveling.Level);
        }

        [Test]
        public void XpToNextLevel_StrictlyIncreasesWithLevel()
        {
            var leveling = new PlayerLeveling();
            int thresholdAtLevel1 = leveling.XpToNextLevel;

            leveling.AddExperience(thresholdAtLevel1);

            Assert.AreEqual(2, leveling.Level);
            Assert.Greater(leveling.XpToNextLevel, thresholdAtLevel1);
        }
    }
}
