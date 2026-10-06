using System;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class WagerAmountTests
    {
        [Test]
        public void HalvesAndDoublesWithinTheLimits()
        {
            var wager = new WagerAmount(10, 10, 100);
            wager.Double();
            wager.Double();
            Assert.AreEqual(40, wager.Value);
            wager.Double();
            wager.Double();
            Assert.AreEqual(100, wager.Value, "doubling stops at the table maximum");
            wager.Halve();
            Assert.AreEqual(50, wager.Value);
            wager.Halve();
            wager.Halve();
            wager.Halve();
            Assert.AreEqual(10, wager.Value, "halving stops at the table minimum");
        }

        [TestCase(5, 10)]
        [TestCase(500, 100)]
        [TestCase(37, 37)]
        public void AnOpeningValueOutsideTheLimitsIsClamped(long opening, long expected)
        {
            Assert.AreEqual(expected, new WagerAmount(opening, 10, 100).Value);
        }

        [Test]
        public void OneLegalWagerLeavesNothingToAdjust()
        {
            var wager = new WagerAmount(25, 25, 25);
            wager.Halve();
            Assert.AreEqual(25, wager.Value);
            wager.Double();
            Assert.AreEqual(25, wager.Value);
        }

        [Test]
        public void DoublingNearTheLargestLongDoesNotOverflow()
        {
            var wager = new WagerAmount(long.MaxValue / 2 + 1, 1, long.MaxValue);
            wager.Double();
            Assert.AreEqual(long.MaxValue, wager.Value);
        }

        [Test]
        public void ImpossibleLimitsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WagerAmount(10, 0, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WagerAmount(10, -1, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WagerAmount(10, 50, 49));
        }
    }
}
