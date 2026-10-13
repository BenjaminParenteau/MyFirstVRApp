using System;
using HighStakes.TableGames.Dice;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class DiceBetTests
    {
        static DiceBet Bet() => new DiceBet(10, 50, 10, 100);

        /// <summary>The limit arithmetic is WagerAmount's; this proves the bet hands its wager to it.</summary>
        [Test]
        public void WagerHalvesAndDoublesWithinLimits()
        {
            var bet = Bet();
            bet.DoubleWager();
            bet.DoubleWager();
            Assert.AreEqual(40, bet.Wager);
            bet.DoubleWager();
            bet.DoubleWager();
            Assert.AreEqual(100, bet.Wager);
            bet.HalveWager();
            Assert.AreEqual(50, bet.Wager);
            bet.HalveWager();
            bet.HalveWager();
            bet.HalveWager();
            Assert.AreEqual(10, bet.Wager);
        }

        [Test]
        public void SetTargetClampsToGameRange()
        {
            var bet = Bet();
            bet.SetTarget(0);
            Assert.AreEqual(DiceGame.MinTarget, bet.Target);
            bet.SetTarget(100);
            Assert.AreEqual(DiceGame.MaxTarget, bet.Target);
            bet.SetTarget(73);
            Assert.AreEqual(73, bet.Target);
        }

        [Test]
        public void DefaultsOutsideLimitsAreClamped()
        {
            var bet = new DiceBet(5, 150, 10, 100);
            Assert.AreEqual(10, bet.Wager);
            Assert.AreEqual(DiceGame.MaxTarget, bet.Target);
        }

        [Test]
        public void ImpossibleLimitsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceBet(10, 50, 0, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceBet(10, 50, -1, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceBet(10, 50, 100, 99));
        }
    }
}
