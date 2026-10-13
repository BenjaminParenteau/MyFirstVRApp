using System;
using HighStakes.TableGames.Plinko;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class PlinkoBetTests
    {
        static PlinkoBet Bet(int rows, long wager = 10) =>
            new PlinkoBet(wager, 10, 500, rows, PlinkoRisk.Low, PlinkoPaytable.Standard());

        [Test]
        public void AnOpeningRowCountTheTableDoesNotOfferFallsToTheNearestOne()
        {
            Assert.AreEqual(8, Bet(9).Rows);
            Assert.AreEqual(12, Bet(11).Rows);
            Assert.AreEqual(8, Bet(10).Rows, "a tie goes to the smaller board");
            Assert.AreEqual(16, Bet(40).Rows);
        }

        [Test]
        public void RowsStepThroughWhatIsOfferedAndStopAtTheEnds()
        {
            var bet = Bet(8);
            Assert.IsFalse(bet.CanRemoveRows);
            bet.StepRows(-1);
            Assert.AreEqual(8, bet.Rows);
            bet.StepRows(+1);
            Assert.AreEqual(12, bet.Rows);
            bet.StepRows(+1);
            Assert.AreEqual(16, bet.Rows);
            Assert.IsFalse(bet.CanAddRows);
            bet.StepRows(+1);
            Assert.AreEqual(16, bet.Rows);
            bet.StepRows(-1);
            Assert.AreEqual(12, bet.Rows);
        }

        [Test]
        public void TheMultipliersFollowTheChosenBoard()
        {
            var bet = Bet(12);
            bet.Risk = PlinkoRisk.High;
            CollectionAssert.AreEqual(PlinkoPaytable.Standard().For(12, PlinkoRisk.High), bet.Multipliers);
            Assert.AreEqual(13, bet.Multipliers.Length);
        }

        [Test]
        public void TheWagerStaysWithinTheTableLimits()
        {
            var bet = Bet(8, 400);
            bet.DoubleWager();
            Assert.AreEqual(500, bet.Wager);
            for (var i = 0; i < 10; i++) bet.HalveWager();
            Assert.AreEqual(10, bet.Wager);
            Assert.Throws<ArgumentNullException>(() => new PlinkoBet(10, 10, 500, 8, PlinkoRisk.Low, null));
        }
    }
}
