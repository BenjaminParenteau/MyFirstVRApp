using System;
using System.Linq;
using HighStakes.TableGames.Plinko;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class PlinkoGameTests
    {
        /// <summary>An 8 row board whose buckets are easy to tell apart: 0.00x in the middle, 0.50x beside it, 10.00x at the edges.</summary>
        static readonly long[] Buckets = { 1000, 300, 150, 50, 0, 50, 150, 300, 1000 };

        static PlinkoPaytable Paytable(long[] buckets = null)
        {
            buckets = buckets ?? Buckets;
            var risks = (PlinkoRisk[])Enum.GetValues(typeof(PlinkoRisk));
            return new PlinkoPaytable(risks.Select(risk => (8, risk, buckets)));
        }

        static PlinkoGame Game(IRng rng, ITableWallet wallet, long[] buckets = null) =>
            new PlinkoGame(rng, wallet, Paytable(buckets));

        /// <summary>Drops on the 8 row low board and lets the ball land.</summary>
        static PlinkoDrop DropAndSettle(PlinkoGame game, long wager, int rows = 8, PlinkoRisk risk = PlinkoRisk.Low)
        {
            Assert.IsTrue(game.TryDrop(wager, rows, risk, out var drop));
            drop.Settle();
            return drop;
        }

        [Test]
        public void TheBucketIsTheNumberOfRightTurns()
        {
            var drop = DropAndSettle(Game(new FakeRng(1, 0, 1, 1, 0, 0, 1, 0), new TestWallet(100)), 10);
            Assert.AreEqual(4, drop.Bucket);
            CollectionAssert.AreEqual(new[] { true, false, true, true, false, false, true, false }, drop.Path);
        }

        [Test]
        public void AllLeftAndAllRightLandInTheEdgeBuckets()
        {
            Assert.AreEqual(0, DropAndSettle(Game(new FakeRng(0), new TestWallet(100)), 10).Bucket);
            Assert.AreEqual(8, DropAndSettle(Game(new FakeRng(1), new TestWallet(100)), 10).Bucket);
        }

        [Test]
        public void ADropAsksForOneCoinFlipPerRowTopFirst()
        {
            var rng = new FakeRng(0, 1);
            DropAndSettle(Game(rng, new TestWallet(100)), 10);
            Assert.AreEqual(8, rng.Calls);
            Assert.IsTrue(rng.Ranges.All(range => range == (0, 2)));
        }

        [Test]
        public void AnEdgeBucketPaysItsMultiplier()
        {
            var wallet = new TestWallet(100);
            var drop = DropAndSettle(Game(new FakeRng(1), wallet), 10);

            Assert.AreEqual(1000, drop.Multiplier);
            Assert.AreEqual(100, drop.Payout);
            Assert.AreEqual(90, drop.Net);
            Assert.AreEqual(190, wallet.Balance);
            Assert.AreEqual("Plinko wager", wallet.Ledger[1].Reason);
            Assert.AreEqual("Plinko return", wallet.Ledger[2].Reason);
        }

        /// <summary>A bucket under 1.00x hands back part of the stake, as a credit of its own.</summary>
        [Test]
        public void ALowBucketReturnsPartOfTheStake()
        {
            var wallet = new TestWallet(100);
            // Three rights: bucket 3, which pays 0.50x.
            var drop = DropAndSettle(Game(new FakeRng(1, 1, 1, 0, 0, 0, 0, 0), wallet), 10);
            Assert.AreEqual(3, drop.Bucket);
            Assert.AreEqual(5, drop.Payout);
            Assert.AreEqual(95, wallet.Balance);
            Assert.AreEqual("Plinko return", wallet.Ledger[2].Reason);
        }

        [Test]
        public void AZeroBucketCreditsNothingAndDoesNotThrow()
        {
            var wallet = new TestWallet(100);
            var drop = DropAndSettle(Game(new FakeRng(1, 1, 1, 1, 0, 0, 0, 0), wallet), 10);

            Assert.AreEqual(4, drop.Bucket);
            Assert.AreEqual(0, drop.Payout);
            Assert.AreEqual(90, wallet.Balance);
            Assert.AreEqual(2, wallet.Ledger.Count, "a zero bucket writes the opening balance and the wager, nothing else");
        }

        [TestCase(10L, 233L, 23L)]
        [TestCase(3L, 69L, 2L)]
        [TestCase(1L, 19L, 0L)]
        public void PayoutIsWagerTimesMultiplierRoundedDown(long wager, long multiplier, long expected)
        {
            Assert.AreEqual(expected, PlinkoGame.PayoutFor(wager, multiplier));
        }

        [Test]
        public void TheWagerLeavesTheWalletBeforeAnyFlip()
        {
            var wallet = new TestWallet(100);
            Assert.Throws<ThrowingRng.Rolled>(() => Game(new ThrowingRng(), wallet).TryDrop(10, 8, PlinkoRisk.Low, out _));
            Assert.AreEqual(90, wallet.Balance, "the wager left the wallet before any flip");
        }

        [Test]
        public void TheReturnArrivesOnlyWhenTheBallLandsAndOnlyOnce()
        {
            var wallet = new TestWallet(100);
            Assert.IsTrue(Game(new FakeRng(1), wallet).TryDrop(10, 8, PlinkoRisk.Low, out var drop));
            Assert.AreEqual(90, wallet.Balance, "while the ball falls only the wager has moved");
            Assert.IsFalse(drop.Settled);
            drop.Settle();
            drop.Settle();
            Assert.IsTrue(drop.Settled);
            Assert.AreEqual(190, wallet.Balance, "paid once");
            Assert.AreEqual(3, wallet.Ledger.Count);
        }

        [Test]
        public void AWagerTheWalletCannotCoverIsRefusedWithoutAFlip()
        {
            var wallet = new TestWallet(5);
            var rng = new FakeRng(0);
            Assert.IsFalse(Game(rng, wallet).TryDrop(10, 8, PlinkoRisk.Low, out var drop));
            Assert.IsNull(drop);
            Assert.AreEqual(0, rng.Calls);
            Assert.AreEqual(5, wallet.Balance);
            Assert.AreEqual(1, wallet.Ledger.Count, "only the opening balance");
        }

        [Test]
        public void ABetTooLargeToPayIsRefusedBeforeAnyChipMoves()
        {
            var wallet = new TestWallet(long.MaxValue);
            var rng = new FakeRng(0);
            Assert.Throws<OverflowException>(() => Game(rng, wallet).TryDrop(long.MaxValue / 10, 8, PlinkoRisk.Low, out _));
            Assert.AreEqual(long.MaxValue, wallet.Balance);
            Assert.AreEqual(0, rng.Calls);
        }

        [Test]
        public void ABoardTheTableDoesNotOfferIsRefusedBeforeAnyChipMoves()
        {
            var wallet = new TestWallet(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => Game(new FakeRng(0), wallet).TryDrop(10, 12, PlinkoRisk.Low, out _));
            Assert.AreEqual(100, wallet.Balance);
        }

        [Test]
        public void InvalidInputsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Game(new FakeRng(0), new TestWallet(100)).TryDrop(0, 8, PlinkoRisk.Low, out _));
            Assert.Throws<ArgumentNullException>(() => new PlinkoGame(null, new TestWallet(1), Paytable()));
            Assert.Throws<ArgumentNullException>(() => new PlinkoGame(new FakeRng(0), null, Paytable()));
            Assert.Throws<ArgumentNullException>(() => new PlinkoGame(new FakeRng(0), new TestWallet(1), null));
        }

        /// <summary>
        /// Every shipped board, played for a long seeded session, gives back what its paytable declares: proof the coin
        /// flips land in the buckets as often as the binomial says. A 100 chip wager pays each multiplier exactly, so
        /// rounding plays no part. The tolerance is four standard errors of the session, worked out from each board's
        /// own spread: about 0.003 on the 8 row low board and 0.034 on the 16 row high one, whose top bucket comes up
        /// once in 32,768 drops.
        /// </summary>
        [Test]
        public void EveryShippedBoardReturnsWhatItDeclaresOverALongSession()
        {
            const int Drops = 200_000;
            const long Wager = 100;
            var paytable = PlinkoPaytable.Standard();
            foreach (var rows in paytable.Rows)
            foreach (PlinkoRisk risk in Enum.GetValues(typeof(PlinkoRisk)))
            {
                var wallet = new TestWallet(Drops * Wager);
                var game = new PlinkoGame(new SeededRng(20241013), wallet, paytable);
                long returned = 0;
                for (var i = 0; i < Drops; i++) returned += DropAndSettle(game, Wager, rows, risk).Payout;

                var declared = paytable.ExpectedReturn(rows, risk);
                var tolerance = 4 * StandardDeviation(paytable, rows, risk) / Math.Sqrt(Drops);
                Assert.AreEqual(declared, (double)returned / (Drops * Wager), tolerance, $"{rows} rows {risk}");
            }
        }

        /// <summary>The spread of one drop's return per chip staked, from the binomial chance of each bucket.</summary>
        static double StandardDeviation(PlinkoPaytable paytable, int rows, PlinkoRisk risk)
        {
            var multipliers = paytable.For(rows, risk);
            double mean = 0, square = 0, ways = 1;
            for (var k = 0; k <= rows; k++)
            {
                var chance = ways / Math.Pow(2, rows);
                var x = multipliers[k] / 100.0;
                mean += chance * x;
                square += chance * x * x;
                ways = ways * (rows - k) / (k + 1);
            }
            return Math.Sqrt(square - mean * mean);
        }
    }
}
