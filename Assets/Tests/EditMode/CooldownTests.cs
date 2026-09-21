using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class CooldownTests
    {
        [Test]
        public void TryConsume_FirstCall_AlwaysSucceeds()
        {
            var cooldown = new Cooldown(1f);

            bool result = cooldown.TryConsume(0f);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryConsume_BeforeIntervalElapsed_ReturnsFalse_ButBoundaryIsInclusive()
        {
            var cooldown = new Cooldown(1f);
            cooldown.TryConsume(0f);

            bool tooSoon = cooldown.TryConsume(0.5f);

            Assert.IsFalse(tooSoon);
            // The failed call above must not have reset the internal timer:
            // a consume exactly one interval after the original trigger (1.0)
            // should still succeed (boundary is inclusive).
            Assert.IsTrue(cooldown.TryConsume(1.0f));
        }

        [Test]
        public void TryConsume_RequiresFullIntervalAfterEachSuccessfulConsume()
        {
            var cooldown = new Cooldown(1f);

            Assert.IsTrue(cooldown.TryConsume(0f));
            Assert.IsFalse(cooldown.TryConsume(0.5f));
            Assert.IsTrue(cooldown.TryConsume(1.0f));
            Assert.IsFalse(cooldown.TryConsume(1.5f));
            Assert.IsTrue(cooldown.TryConsume(2.0f));
        }

        [Test]
        public void IsReady_BeforeAnyConsume_MatchesTryConsumeAnswer()
        {
            var cooldown = new Cooldown(1f);

            Assert.IsTrue(cooldown.IsReady(0f));
        }

        [Test]
        public void IsReady_RepeatedCallsWhenNotReady_NeverBecomesTrueOnItsOwn()
        {
            var cooldown = new Cooldown(1f);
            cooldown.TryConsume(0f);

            Assert.IsFalse(cooldown.IsReady(0.5f));
            Assert.IsFalse(cooldown.IsReady(0.5f));
            Assert.IsFalse(cooldown.IsReady(0.5f));
        }

        [Test]
        public void IsReady_DoesNotAdvanceInternalTimer_TryConsumeStillNeededToArm()
        {
            var cooldown = new Cooldown(1f);
            cooldown.TryConsume(0f);

            // Peeking repeatedly at t=1.0 must not itself consume the cooldown.
            Assert.IsTrue(cooldown.IsReady(1.0f));
            Assert.IsTrue(cooldown.IsReady(1.0f));
            Assert.IsTrue(cooldown.IsReady(1.0f));

            // Only an actual TryConsume should arm the next interval.
            Assert.IsTrue(cooldown.TryConsume(1.0f));
            Assert.IsFalse(cooldown.IsReady(1.5f));
        }

        [Test]
        public void IsReady_MatchesTryConsumeAnswer_WithoutSideEffect()
        {
            var cooldown = new Cooldown(1f);
            cooldown.TryConsume(0f);

            // IsReady says false before the interval elapses.
            Assert.IsFalse(cooldown.IsReady(0.9f));

            // IsReady says true once the interval has elapsed, matching what TryConsume would return.
            Assert.IsTrue(cooldown.IsReady(1.0f));
            Assert.IsTrue(cooldown.TryConsume(1.0f));
        }

        [Test]
        public void SetInterval_ChangesFutureTiming()
        {
            var cooldown = new Cooldown(1f);
            cooldown.TryConsume(0f);

            cooldown.SetInterval(0.5f);

            Assert.IsFalse(cooldown.IsReady(0.3f));
            Assert.IsTrue(cooldown.IsReady(0.5f));
        }

        [Test]
        public void SetInterval_ClampsToFloor_NeverZeroOrNegative()
        {
            var cooldown = new Cooldown(1f);

            cooldown.SetInterval(-5f);

            Assert.AreEqual(0.1f, cooldown.Interval, 0.0001f);
        }
    }
}
