using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class PauseStateTests
    {
        [Test]
        public void Pause_SetsTimeScaleToZero()
        {
            PauseState.ClearAll();

            PauseState.Pause("test");

            Assert.AreEqual(0f, Time.timeScale);
            PauseState.ClearAll();
        }

        [Test]
        public void Resume_LastReason_RestoresTimeScaleToOne()
        {
            PauseState.ClearAll();
            PauseState.Pause("a");

            PauseState.Resume("a");

            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void Resume_WhileOtherReasonStillHeld_KeepsTimeScaleZero()
        {
            PauseState.ClearAll();
            PauseState.Pause("a");
            PauseState.Pause("b");

            PauseState.Resume("a");

            Assert.AreEqual(0f, Time.timeScale);
            PauseState.ClearAll();
        }

        [Test]
        public void DuplicatePause_SameReason_IsIdempotent()
        {
            PauseState.ClearAll();
            PauseState.Pause("a");
            PauseState.Pause("a");

            PauseState.Resume("a");

            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void ClearAll_RestoresTimeScaleRegardlessOfReasonCount()
        {
            PauseState.ClearAll();
            PauseState.Pause("a");
            PauseState.Pause("b");

            PauseState.ClearAll();

            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(PauseState.IsPaused);
        }

        [Test]
        public void Resume_UnheldReason_DoesNotForceUnpauseIfOthersHeld()
        {
            PauseState.ClearAll();
            PauseState.Pause("a");

            PauseState.Resume("never-paused");

            Assert.AreEqual(0f, Time.timeScale);
            PauseState.ClearAll();
        }
    }
}
