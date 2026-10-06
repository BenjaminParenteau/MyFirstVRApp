using System;
using System.Linq;
using NUnit.Framework;

namespace HighStakes.TableGames.Blackjack.Tests
{
    public class BlackjackHandTests
    {
        static BlackjackHand Hand(params int[] ranks) => Hand(false, ranks);

        static BlackjackHand Hand(bool fromSplit, params int[] ranks)
        {
            var hand = new BlackjackHand(10, fromSplit);
            foreach (var rank in ranks) hand.Add(new Card(rank, Suit.Hearts));
            return hand;
        }

        [TestCase(new[] { 10, 7 }, 17, false)]
        [TestCase(new[] { 1, 6 }, 17, true)]
        [TestCase(new[] { 1, 6, 10 }, 17, false)]
        [TestCase(new[] { 1, 1 }, 12, true)]
        [TestCase(new[] { 1, 1, 9 }, 21, true)]
        [TestCase(new[] { 1, 1, 1, 1, 7 }, 21, true)]
        [TestCase(new[] { 12, 13 }, 20, false)]
        [TestCase(new[] { 10, 6, 9 }, 25, false)]
        public void TotalsCountOneAceAsElevenWhenThatDoesNotBust(int[] ranks, int total, bool soft)
        {
            var hand = Hand(ranks);
            Assert.AreEqual(total, hand.Total);
            Assert.AreEqual(soft, hand.IsSoft);
            Assert.AreEqual(total > 21, hand.IsBust);
        }

        [Test]
        public void ABlackjackIsTwentyOneOnTwoCardsOfAHandThatWasNotSplit()
        {
            Assert.IsTrue(Hand(1, 13).IsBlackjack);
            Assert.IsFalse(Hand(true, 1, 13).IsBlackjack, "after a split it is just 21");
            Assert.IsFalse(Hand(7, 7, 7).IsBlackjack, "three cards");
        }

        [Test]
        public void TheCardsCannotBeChangedFromOutside()
        {
            var hand = Hand(10, 7);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<Card>)hand.Cards).Clear());
            Assert.AreEqual(2, hand.Cards.Count);
        }

        [Test]
        public void EveryIndexIsADifferentCardAndMapsBack()
        {
            var deck = Enumerable.Range(0, Card.DeckSize).Select(Card.FromIndex).ToList();
            Assert.AreEqual(Card.DeckSize, deck.Distinct().Count());
            for (var i = 0; i < Card.DeckSize; i++) Assert.AreEqual(i, deck[i].Index);
            Assert.AreEqual(16, deck.Count(card => card.Points == 10), "ten, jack, queen and king of four suits");
            Assert.Throws<ArgumentOutOfRangeException>(() => Card.FromIndex(Card.DeckSize));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Card(0, Suit.Spades));
        }
    }
}
