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
    }
}
