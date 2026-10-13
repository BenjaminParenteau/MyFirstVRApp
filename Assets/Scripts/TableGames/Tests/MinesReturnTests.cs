using System.Linq;
using System.Numerics;
using HighStakes.TableGames.Mines;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    /// <summary>
    /// The house rules every table is held to: the wager leaves the wallet before anything is drawn, a refused wager
    /// draws nothing, and a fixed strategy returns 1 - edge. Any fixed strategy does, because the payout is exactly
    /// (1 - edge) over the chance of surviving it.
    /// </summary>
    public class MinesReturnTests
    {
        const int Edge = 100;

        [Test]
        public void TheWagerIsTakenBeforeTheMinesAreDrawn()
        {
            var wallet = new TestWallet(100);
            Assert.Throws<ThrowingRng.Rolled>(() => new MinesGame(new ThrowingRng(), wallet, Edge).TryStart(10, 3, out _));
            Assert.AreEqual(90, wallet.Balance, "the stake left the wallet before any draw");
            Assert.AreEqual("Mines wager", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void AnUnaffordableWagerDrawsNothingAndLeavesTheWalletAlone()
        {
            var wallet = new TestWallet(9);
            Assert.IsFalse(new MinesGame(new ThrowingRng(), wallet, Edge).TryStart(10, 3, out var round), "no draw, so no throw");
            Assert.IsNull(round);
            Assert.AreEqual(9, wallet.Balance);
            Assert.AreEqual(1, wallet.Ledger.Count, "only the opening balance");
        }

        /// <summary>
        /// Exact, with no RNG: for every mine count and number of picks, the payout times the chance of surviving,
        /// C(25 - m, k) / C(25, k), is the wager times (1 - edge), short by less than the one chip lost to rounding down.
        /// Cross-multiplied so it stays in whole numbers.
        /// </summary>
        [Test]
        public void EveryFixedStrategyIsPricedAtOneMinusTheEdge()
        {
            const long wager = 1_000_000;
            for (var mines = MinesGame.MinMines; mines <= MinesGame.MaxMines; mines++)
            for (var safe = 1; safe <= MinesGame.Tiles - mines; safe++)
            {
                var payout = MinesGame.PayoutFor(wager, mines, safe, Edge);
                var survive = Choose(MinesGame.Tiles - mines, safe) * 10000;
                var fair = new BigInteger(wager) * (10000 - Edge) * Choose(MinesGame.Tiles, safe);
                Assert.IsTrue(payout * survive <= fair && fair < (payout + 1) * survive, $"{mines} mines, {safe} picks");
            }
        }

        /// <summary>
        /// A long seeded session: hide the mines, reveal the first <paramref name="picks"/> tiles in order and cash out if
        /// they survive. 3 mines and 2 picks survives 77% of the time paying 1.2857x (spread about 0.54 per round);
        /// 10 mines and 3 picks is a 1 in 5 long shot paying about 5x (spread about 2.0), where a wrong shuffle or
        /// payout would show fastest. 50000 rounds put each tolerance about 4 standard deviations out. A 10000-chip
        /// wager keeps rounding down to about 0.0001 of the return.
        /// </summary>
        [TestCase(3, 2, 0.01)]
        [TestCase(10, 3, 0.04)]
        public void ALongSessionReturnsOneMinusTheEdge(int mines, int picks, double tolerance)
        {
            const long wager = 10000;
            const int rounds = 50000;
            var wallet = new TestWallet(wager * rounds);
            var game = new MinesGame(new SeededRng(20241013), wallet, Edge);
            long staked = 0, returned = 0;
            for (var i = 0; i < rounds; i++)
            {
                Assert.IsTrue(game.TryStart(wager, mines, out var round));
                for (var tile = 0; tile < picks && round.IsOpen; tile++) round.Reveal(tile);
                if (round.IsOpen) round.CashOut();
                staked += round.Wager;
                returned += round.Payout;
            }
            Assert.AreEqual(1 - Edge / 10000.0, (double)returned / staked, tolerance);
        }

        static BigInteger Choose(int n, int k)
        {
            BigInteger result = 1;
            for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
            return result;
        }
    }
}
