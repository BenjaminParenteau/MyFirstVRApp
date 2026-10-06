using System;
using System.Linq;
using NUnit.Framework;
using HighStakes.TableGames.Tests;

namespace HighStakes.TableGames.Blackjack.Tests
{
    public class BlackjackGameTests
    {
        static BlackjackGame Game(FakeRng rng, ITableWallet wallet) => new BlackjackGame(rng, wallet, BlackjackRuleSet.Standard);

        [Test]
        public void StartingTakesTheWagerThenDealsFourCardsFromTheWholeDeck()
        {
            var wallet = new TestWallet(100);
            var rng = new FakeRng(new Card(10, Suit.Clubs).Index, new Card(9, Suit.Hearts).Index);
            Assert.IsTrue(Game(rng, wallet).TryStart(10, out _));

            Assert.AreEqual(90, wallet.Balance);
            Assert.AreEqual("Blackjack wager", wallet.Ledger.Last().Reason);
            Assert.AreEqual(4, rng.Calls);
            Assert.IsTrue(rng.Ranges.All(range => range == (0, Card.DeckSize)));
        }

        [Test]
        public void AnUnaffordableRoundIsRefusedWithoutADeal()
        {
            var rng = new FakeRng(0);
            Assert.IsFalse(Game(rng, new TestWallet(5)).TryStart(10, out var round));
            Assert.IsNull(round);
            Assert.AreEqual(0, rng.Calls);
        }

        [Test]
        public void ABetTooLargeToPayIsRefusedBeforeAnyCreditMoves()
        {
            var wallet = new TestWallet(long.MaxValue);
            var rng = new FakeRng(0);
            Assert.Throws<OverflowException>(() => Game(rng, wallet).TryStart(long.MaxValue / 4, out _));
            Assert.AreEqual(long.MaxValue, wallet.Balance);
            Assert.AreEqual(0, rng.Calls);
        }

        [Test]
        public void InvalidInputsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Game(new FakeRng(0), new TestWallet(100)).TryStart(0, out _));
            Assert.Throws<ArgumentNullException>(() => new BlackjackGame(null, new TestWallet(1), BlackjackRuleSet.Standard));
            Assert.Throws<ArgumentNullException>(() => new BlackjackGame(new FakeRng(0), null, BlackjackRuleSet.Standard));
            Assert.Throws<ArgumentNullException>(() => new BlackjackGame(new FakeRng(0), new TestWallet(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlackjackRuleSet(99, false, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlackjackRuleSet(201, false, true));
        }
    }
}
