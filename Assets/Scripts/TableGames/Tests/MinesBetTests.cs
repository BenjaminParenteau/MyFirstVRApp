using System;
using HighStakes.TableGames.Mines;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class MinesBetTests
    {
        [Test]
        public void MineCountStepsWithinTheTableLimits()
        {
            var bet = new MinesBet(10, 2, 10, 100, 1, 3);
            bet.FewerMines();
            bet.FewerMines();
            Assert.AreEqual(1, bet.Mines);
            bet.MoreMines();
            bet.MoreMines();
            bet.MoreMines();
            Assert.AreEqual(3, bet.Mines);
        }

        [Test]
        public void AnOpeningMineCountOutsideTheLimitsIsClamped()
        {
            Assert.AreEqual(5, new MinesBet(10, 99, 10, 100, 1, 5).Mines);
            Assert.AreEqual(2, new MinesBet(10, 0, 10, 100, 2, 5).Mines);
        }

        [Test]
        public void WagerIsHandedToWagerAmount()
        {
            var bet = new MinesBet(10, 3, 10, 100, 1, 24);
            bet.DoubleWager();
            Assert.AreEqual(20, bet.Wager);
            bet.HalveWager();
            bet.HalveWager();
            Assert.AreEqual(10, bet.Wager);
        }

        [Test]
        public void MineLimitsOutsideTheGameOrOutOfOrderThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesBet(10, 3, 10, 100, 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesBet(10, 3, 10, 100, 1, MinesGame.Tiles));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesBet(10, 3, 10, 100, 6, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesBet(10, 3, 0, 100, 1, 5));
        }
    }
}
