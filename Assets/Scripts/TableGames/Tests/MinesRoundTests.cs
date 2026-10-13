using System;
using System.Linq;
using HighStakes.TableGames.Mines;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class MinesRoundTests
    {
        const int Edge = 100;

        /// <summary>
        /// A round whose mines sit on the given tiles. Each scripted draw j swaps slot i with slot j, and slots start in
        /// tile order, so drawing a tile number at or after i puts that tile's mine in slot i.
        /// </summary>
        static MinesRound Start(TestWallet wallet, long wager, params int[] mineTiles)
        {
            var rng = new FakeRng(mineTiles);
            Assert.IsTrue(new MinesGame(rng, wallet, Edge).TryStart(wager, mineTiles.Length, out var round));
            return round;
        }

        static int[] SafeTiles(params int[] mines) => Enumerable.Range(0, MinesGame.Tiles).Except(mines).ToArray();

        [Test]
        public void TheMinesAreWhereTheDrawsPutThem()
        {
            var round = Start(new TestWallet(1000), 10, 3, 17, 24);
            round.Reveal(0);
            round.CashOut();
            for (var tile = 0; tile < MinesGame.Tiles; tile++)
                Assert.AreEqual(tile == 3 || tile == 17 || tile == 24, round.IsMine(tile), $"tile {tile}");
        }

        [Test]
        public void ASafeTileRaisesTheCashOutValue()
        {
            var round = Start(new TestWallet(1000), 100, 3, 17, 24);
            Assert.AreEqual(0, round.CashOutValue, "nothing to cash out before a reveal");
            Assert.IsTrue(round.Reveal(0));
            Assert.AreEqual(MinesGame.PayoutFor(100, 3, 1, Edge), round.CashOutValue);
            Assert.IsTrue(round.Reveal(1));
            Assert.AreEqual(MinesGame.PayoutFor(100, 3, 2, Edge), round.CashOutValue);
            Assert.AreEqual(2, round.Safe);
        }

        [Test]
        public void TheNextMultiplierIsTheOneTheNextSafeTileBrings()
        {
            var round = Start(new TestWallet(1000), 10, 24);
            Assert.AreEqual(MinesGame.MultiplierFor(1, 1, Edge), round.NextMultiplier);
            foreach (var tile in SafeTiles(24).Take(23)) round.Reveal(tile);
            Assert.AreEqual(MinesGame.MultiplierFor(1, 24, Edge), round.NextMultiplier);
        }

        [Test]
        public void CashingOutCreditsTheValueAndEndsTheRound()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 100, 3, 17, 24);
            round.Reveal(0);
            round.Reveal(1);
            var value = round.CashOutValue;

            round.CashOut();

            Assert.AreEqual(MinesState.CashedOut, round.State);
            Assert.AreEqual(value, round.Payout);
            Assert.AreEqual(900 + value, wallet.Balance);
            Assert.AreEqual("Mines win", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void AMineBustsTheRoundAndCreditsNothing()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 100, 3, 17, 24);
            round.Reveal(0);
            Assert.IsFalse(round.Reveal(17));

            Assert.AreEqual(MinesState.Busted, round.State);
            Assert.AreEqual(0, round.Payout);
            Assert.AreEqual(900, wallet.Balance);
            Assert.AreEqual(2, wallet.Ledger.Count, "a bust writes the wager and nothing else");
        }

        [Test]
        public void ClearingEverySafeTileCashesOutOnItsOwn()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 10, 24);
            foreach (var tile in SafeTiles(24).Take(23)) round.Reveal(tile);
            Assert.IsTrue(round.IsOpen);
            round.Reveal(SafeTiles(24).Last());

            Assert.AreEqual(MinesState.CashedOut, round.State);
            Assert.AreEqual(MinesGame.PayoutFor(10, 1, 24, Edge), round.Payout);
            Assert.AreEqual(0, round.NextMultiplier, "nothing left to reveal");
        }

        [Test]
        public void TheMinesStayHiddenWhileTheRoundIsOpen()
        {
            var round = Start(new TestWallet(1000), 10, 3);
            Assert.Throws<InvalidOperationException>(() => round.IsMine(3));
        }

        [Test]
        public void AFinishedRoundTakesNoMoreMoves()
        {
            var round = Start(new TestWallet(1000), 10, 3);
            round.Reveal(3);
            Assert.Throws<InvalidOperationException>(() => round.Reveal(4));
            Assert.Throws<InvalidOperationException>(() => round.CashOut());
        }

        [Test]
        public void MovesThatMakeNoSenseThrow()
        {
            var round = Start(new TestWallet(1000), 10, 3);
            Assert.Throws<InvalidOperationException>(() => round.CashOut(), "cash out before a reveal");
            round.Reveal(0);
            Assert.Throws<InvalidOperationException>(() => round.Reveal(0), "the same tile twice");
            Assert.Throws<ArgumentOutOfRangeException>(() => round.Reveal(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => round.Reveal(MinesGame.Tiles));
        }

        [Test]
        public void WalkingAwayBeforeARevealRefundsTheWager()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 100, 3);

            round.Abandon();

            Assert.AreEqual(MinesState.Refunded, round.State);
            Assert.AreEqual(1000, wallet.Balance);
            Assert.AreEqual("Mines refund", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void WalkingAwayAfterARevealCashesOut()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 100, 3);
            round.Reveal(0);
            var value = round.CashOutValue;

            round.Abandon();
            round.Abandon();

            Assert.AreEqual(MinesState.CashedOut, round.State);
            Assert.AreEqual(900 + value, wallet.Balance, "abandoning twice settles once");
        }

        [Test]
        public void WalkingAwayFromAFinishedRoundChangesNothing()
        {
            var wallet = new TestWallet(1000);
            var round = Start(wallet, 100, 3);
            round.Reveal(3);
            round.Abandon();
            Assert.AreEqual(MinesState.Busted, round.State);
            Assert.AreEqual(900, wallet.Balance);
        }
    }
}
