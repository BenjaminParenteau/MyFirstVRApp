using System;
using System.Linq;
using HighStakes.TableGames.Roulette;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class RouletteTests
    {
        static (RouletteBet, long)[] Bets(params (RouletteBet, long)[] bets) => bets;

        /// <summary>Spins and lets the ball land.</summary>
        static RouletteSpin SpinAndSettle(RouletteGame game, params (RouletteBet, long)[] bets)
        {
            Assert.IsTrue(game.TrySpin(bets, out var spin));
            spin.Settle();
            return spin;
        }

        static int[] Covered(RouletteBet bet) => Enumerable.Range(0, RouletteWheel.Pockets).Where(bet.Wins).ToArray();

        [Test]
        public void TheWheelHasEveryPocketOnceAndEighteenOfEachColour()
        {
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 37), RouletteWheel.Order);
            Assert.AreEqual(18, Enumerable.Range(1, 36).Count(RouletteWheel.IsRed));
            Assert.IsFalse(RouletteWheel.IsRed(0));
            Assert.AreEqual("GREEN", RouletteWheel.ColourName(0));
            Assert.AreEqual(0, RouletteWheel.SlotOf(0));
            Assert.AreEqual(1, RouletteWheel.SlotOf(32), "32 sits next to zero");
        }

        [Test]
        public void EveryBetCoversTheRightPocketsAndNeverZeroUnlessStraight()
        {
            Assert.AreEqual(new[] { 17 }, Covered(RouletteBet.Straight(17)));
            Assert.AreEqual(new[] { 0 }, Covered(RouletteBet.Straight(0)));
            foreach (var kind in new[] { RouletteBetKind.Red, RouletteBetKind.Black, RouletteBetKind.Odd, RouletteBetKind.Even, RouletteBetKind.Low, RouletteBetKind.High })
            {
                var pockets = Covered(RouletteBet.EvenMoney(kind));
                Assert.AreEqual(18, pockets.Length, kind.ToString());
                Assert.IsFalse(pockets.Contains(0), kind + " never wins on zero");
            }
            CollectionAssert.AreEqual(Enumerable.Range(13, 12), Covered(RouletteBet.Dozen(2)));
            CollectionAssert.AreEqual(new[] { 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36 }, Covered(RouletteBet.Column(3)));
            CollectionAssert.AreEqual(Enumerable.Range(1, 18), Covered(RouletteBet.EvenMoney(RouletteBetKind.Low)));
        }

        [Test]
        public void RedAndBlackSplitTheNonZeroPockets()
        {
            var red = Covered(RouletteBet.EvenMoney(RouletteBetKind.Red));
            var black = Covered(RouletteBet.EvenMoney(RouletteBetKind.Black));
            CollectionAssert.AreEquivalent(Enumerable.Range(1, 36), red.Concat(black));
        }

        [Test]
        public void AStraightUpWinPaysThirtyFiveToOne()
        {
            var wallet = new TestWallet(1000);
            var spin = SpinAndSettle(new RouletteGame(new FakeRng(17), wallet), (RouletteBet.Straight(17), 10));
            Assert.AreEqual(17, spin.Pocket);
            Assert.AreEqual(360, spin.Payout, "10 back plus 350");
            Assert.AreEqual(1350, wallet.Balance);
            Assert.AreEqual("Roulette win", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void MixedBetsPayOnlyTheWinners()
        {
            var wallet = new TestWallet(1000);
            // 17 is black, odd, low, 2nd dozen, column 2.
            var bets = Bets(
                (RouletteBet.Straight(17), 5),
                (RouletteBet.EvenMoney(RouletteBetKind.Black), 20),
                (RouletteBet.EvenMoney(RouletteBetKind.Even), 20),
                (RouletteBet.Dozen(2), 10),
                (RouletteBet.Column(1), 10));
            var spin = SpinAndSettle(new RouletteGame(new FakeRng(17), wallet), bets);
            Assert.AreEqual(65, spin.Staked);
            Assert.AreEqual(5 * 36 + 20 * 2 + 10 * 3, spin.Payout);
            CollectionAssert.AreEquivalent(new[] { RouletteBet.Straight(17), RouletteBet.EvenMoney(RouletteBetKind.Black), RouletteBet.Dozen(2) },
                spin.Winners.Select(w => w.Bet));
            Assert.AreEqual(1000 - 65 + 250, wallet.Balance);
        }

        [Test]
        public void ZeroLosesEveryOutsideBet()
        {
            var wallet = new TestWallet(1000);
            var bets = Bets((RouletteBet.EvenMoney(RouletteBetKind.Red), 50), (RouletteBet.EvenMoney(RouletteBetKind.Black), 50), (RouletteBet.Dozen(1), 50));
            var spin = SpinAndSettle(new RouletteGame(new FakeRng(0), wallet), bets);
            Assert.AreEqual(0, spin.Payout);
            Assert.AreEqual(850, wallet.Balance);
        }

        [Test]
        public void TheBetsAreTakenBeforeTheWheelSpinsAndTheWheelHasThirtySevenPockets()
        {
            var wallet = new TestWallet(100);
            Assert.Throws<ThrowingRng.Rolled>(() => new RouletteGame(new ThrowingRng(), wallet).TrySpin(Bets((RouletteBet.Straight(5), 10)), out _));
            Assert.AreEqual(90, wallet.Balance, "the stake left the wallet before any roll");

            var rng = new FakeRng(3);
            new RouletteGame(rng, new TestWallet(100)).TrySpin(Bets((RouletteBet.Straight(5), 10)), out _);
            Assert.AreEqual((0, 37), rng.Ranges.Single());
        }

        [Test]
        public void WinningsArriveOnlyWhenTheSpinSettlesAndOnlyOnce()
        {
            var wallet = new TestWallet(1000);
            Assert.IsTrue(new RouletteGame(new FakeRng(17), wallet).TrySpin(Bets((RouletteBet.Straight(17), 10)), out var spin));
            Assert.AreEqual(990, wallet.Balance, "while the ball is still rolling only the stake has moved");
            Assert.IsFalse(spin.Settled);
            spin.Settle();
            spin.Settle();
            Assert.IsTrue(spin.Settled);
            Assert.AreEqual(1350, wallet.Balance, "paid once");
        }

        [Test]
        public void BetsTheWalletCannotCoverAreRefusedWithoutASpin()
        {
            var wallet = new TestWallet(25);
            var rng = new FakeRng(1);
            Assert.IsFalse(new RouletteGame(rng, wallet).TrySpin(Bets((RouletteBet.Straight(1), 20), (RouletteBet.Straight(2), 10)), out var spin));
            Assert.IsNull(spin);
            Assert.AreEqual(25, wallet.Balance);
            Assert.AreEqual(0, rng.Calls);
        }

        [Test]
        public void ALayoutTooLargeToPayIsRefusedBeforeAnyChipMoves()
        {
            var wallet = new TestWallet(long.MaxValue);
            Assert.Throws<OverflowException>(() => new RouletteGame(new FakeRng(1), wallet).TrySpin(Bets((RouletteBet.Straight(1), long.MaxValue / 10)), out _));
            Assert.AreEqual(long.MaxValue, wallet.Balance);
        }

        [Test]
        public void InvalidInputsThrow()
        {
            var game = new RouletteGame(new FakeRng(1), new TestWallet(100));
            Assert.Throws<ArgumentException>(() => game.TrySpin(Bets(), out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TrySpin(Bets((RouletteBet.Straight(1), 0)), out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => RouletteBet.Straight(37));
            Assert.Throws<ArgumentOutOfRangeException>(() => RouletteBet.Dozen(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => RouletteBet.EvenMoney(RouletteBetKind.Column));
            Assert.Throws<ArgumentNullException>(() => new RouletteGame(null, new TestWallet(1)));
        }

        /// <summary>Every bet type returns 36/37 of its stake over a long seeded session (the single zero's 2.7% edge).</summary>
        [Test]
        public void EveryBetTypeReturnsThirtySixThirtySevenths()
        {
            foreach (var bet in new[] { RouletteBet.Straight(7), RouletteBet.EvenMoney(RouletteBetKind.Red), RouletteBet.Dozen(3), RouletteBet.Column(1) })
            {
                var wallet = new TestWallet(100_000_000);
                var game = new RouletteGame(new SeededRng(20241013), wallet);
                long staked = 0, returned = 0;
                for (int i = 0; i < 200_000; i++)
                {
                    var spin = SpinAndSettle(game, (bet, 100));
                    staked += spin.Staked;
                    returned += spin.Payout;
                }
                // A straight bet's return has a standard deviation near 5.8 per spin; 200k spins put this tolerance ~3 sigma out.
                Assert.AreEqual(36.0 / 37, (double)returned / staked, bet.Kind == RouletteBetKind.Straight ? 0.04 : 0.01, bet.ToString());
            }
        }
    }
}
