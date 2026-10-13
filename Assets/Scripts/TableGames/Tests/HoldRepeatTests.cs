using System;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class HoldRepeatTests
    {
        static int Repeats(HoldRepeat hold, float seconds, float frame = 1f / 72f)
        {
            var total = 0;
            for (var t = 0f; t < seconds; t += frame) total += hold.Advance(frame);
            return total;
        }

        [Test]
        public void AQuickPressNeverRepeats()
        {
            Assert.AreEqual(0, Repeats(new HoldRepeat(0.4f, 0.15f, 0.04f, 1.5f), 0.35f));
        }

        [Test]
        public void AHoldRepeatsAfterTheDelayAtTheStartingInterval()
        {
            var hold = new HoldRepeat(0.4f, 0.15f, 0.15f, 0f);
            Assert.AreEqual(0, hold.Advance(0.39f));
            Assert.AreEqual(1, hold.Advance(0.02f), "the first repeat lands at the delay");
            Assert.AreEqual(1, hold.Advance(0.15f));
            Assert.AreEqual(2, hold.Advance(0.30f));
        }

        [Test]
        public void ALongHoldSpeedsUpToTheFastestInterval()
        {
            var early = Repeats(new HoldRepeat(0.4f, 0.15f, 0.04f, 1.5f), 1.4f);
            var hold = new HoldRepeat(0.4f, 0.15f, 0.04f, 1.5f);
            Repeats(hold, 2.4f);
            var late = hold.Advance(1f);
            Assert.That(late, Is.InRange(24, 26), "a second at full speed is about 25 repeats");
            Assert.Less(early, late, "the first second of repeats is slower than a later one");
        }

        [Test]
        public void ALongFrameCatchesUpInsteadOfDroppingRepeats()
        {
            var hold = new HoldRepeat(0.4f, 0.1f, 0.1f, 0f);
            Assert.AreEqual(6, hold.Advance(0.95f), "0.4, 0.5, 0.6, 0.7, 0.8, 0.9");
        }

        [Test]
        public void BeginningAgainStartsANewHold()
        {
            var hold = new HoldRepeat(0.4f, 0.1f, 0.1f, 0f);
            hold.Advance(1f);
            hold.Begin();
            Assert.AreEqual(0, hold.Advance(0.3f));
        }

        [Test]
        public void ImpossibleTimingsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HoldRepeat(-1f, 0.1f, 0.1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HoldRepeat(0.4f, 0.1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HoldRepeat(0.4f, 0.05f, 0.1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HoldRepeat(0.4f, 0.1f, 0.05f, -1f));
        }
    }
}
