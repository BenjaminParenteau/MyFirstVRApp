using System;
using System.Linq;
using HighStakes.TableGames.Mines;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class MinesGameTests
    {
        const int Edge = 100;

        /// <summary>
        /// The payout is exactly the wager times (1 - edge) over the chance of surviving the picks, before rounding
        /// down. Worked by hand from C(25, k) / C(25 - m, k).
        /// </summary>
        [TestCase(3, 1, 10000L, 11250L)]   // 0.99 * 25/22
        [TestCase(3, 2, 10000L, 12857L)]   // 0.99 * 300/231
        [TestCase(1, 24, 100L, 2475L)]     // 0.99 * C(25,24)/C(24,24) = 0.99 * 25
        [TestCase(24, 1, 100L, 2475L)]     // one safe tile among 25: 0.99 * 25
        [TestCase(12, 13, 1L, 5148297L)]   // 0.99 * C(25,13)/C(13,13) = 0.99 * 5200300
        public void PayoutIsTheWagerOverTheChanceOfSurviving(int mines, int safe, long wager, long expected)
        {
            Assert.AreEqual(expected, MinesGame.PayoutFor(wager, mines, safe, Edge));
        }

        [Test]
        public void BeforeAnyRevealTheMultiplierIsJustTheEdge()
        {
            Assert.AreEqual(99, MinesGame.MultiplierFor(3, 0, Edge));
        }

        [Test]
        public void EverySafeTileRaisesThePayout()
        {
            for (var mines = MinesGame.MinMines; mines <= MinesGame.MaxMines; mines++)
            for (var safe = 1; safe <= MinesGame.Tiles - mines; safe++)
                Assert.Greater(MinesGame.PayoutFor(1000, mines, safe, Edge), MinesGame.PayoutFor(1000, mines, safe - 1, Edge),
                    $"{mines} mines, safe {safe}");
        }

        /// <summary>Any first reveal pays back at least the stake, so a cash out never credits zero.</summary>
        [Test]
        public void TheFirstSafeTileAlreadyReturnsTheStake()
        {
            for (var mines = MinesGame.MinMines; mines <= MinesGame.MaxMines; mines++)
                Assert.GreaterOrEqual(MinesGame.PayoutFor(1, mines, 1, Edge), 1, $"{mines} mines");
        }

        [Test]
        public void StartingTakesTheWagerThenHidesTheMines()
        {
            var wallet = new TestWallet(100);
            var rng = new FakeRng(7, 8, 9);
            Assert.IsTrue(new MinesGame(rng, wallet, Edge).TryStart(10, 3, out var round));

            Assert.AreEqual(90, wallet.Balance);
            Assert.AreEqual("Mines wager", wallet.Ledger.Last().Reason);
            Assert.AreEqual(3, rng.Calls, "one draw per mine");
            CollectionAssert.AreEqual(new[] { (0, 25), (1, 25), (2, 25) }, rng.Ranges);
            Assert.IsTrue(round.IsOpen);
        }

        [Test]
        public void EveryMineCountHidesExactlyThatManyMines()
        {
            for (var mines = MinesGame.MinMines; mines <= MinesGame.MaxMines; mines++)
            {
                var game = new MinesGame(new SeededRng(mines), new TestWallet(100), Edge);
                Assert.IsTrue(game.TryStart(1, mines, out var round));
                round.Abandon();
                Assert.AreEqual(mines, Enumerable.Range(0, MinesGame.Tiles).Count(round.IsMine), $"{mines} mines");
            }
        }

        [Test]
        public void AnUnaffordableRoundIsRefusedWithoutADraw()
        {
            var wallet = new TestWallet(5);
            var rng = new FakeRng(0);
            Assert.IsFalse(new MinesGame(rng, wallet, Edge).TryStart(10, 3, out var round));
            Assert.IsNull(round);
            Assert.AreEqual(5, wallet.Balance);
            Assert.AreEqual(1, wallet.Ledger.Count, "only the opening balance: nothing was debited");
            Assert.AreEqual(0, rng.Calls);
        }

        [Test]
        public void ABetTooLargeToPayIsRefusedBeforeAnyChipMoves()
        {
            var wallet = new TestWallet(long.MaxValue);
            Assert.Throws<OverflowException>(() => new MinesGame(new FakeRng(0), wallet, Edge).TryStart(long.MaxValue / 1000, 12, out _));
            Assert.AreEqual(long.MaxValue, wallet.Balance);
        }

        [Test]
        public void InvalidInputsThrow()
        {
            var game = new MinesGame(new FakeRng(0), new TestWallet(100), Edge);
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryStart(0, 3, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryStart(10, 0, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryStart(10, MinesGame.Tiles, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => MinesGame.PayoutFor(10, 3, 23, Edge), "more safe tiles than exist");
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesGame(new FakeRng(0), new TestWallet(1), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinesGame(new FakeRng(0), new TestWallet(1), 10000));
            Assert.Throws<ArgumentNullException>(() => new MinesGame(null, new TestWallet(1), Edge));
            Assert.Throws<ArgumentNullException>(() => new MinesGame(new FakeRng(0), null, Edge));
        }
    }
}
