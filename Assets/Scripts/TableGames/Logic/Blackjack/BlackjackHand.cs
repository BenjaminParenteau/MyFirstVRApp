using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HighStakes.TableGames.Blackjack
{
    public enum HandResult
    {
        /// <summary>Still being played, or waiting for the dealer.</summary>
        Open,
        Blackjack,
        Win,
        Push,
        Lose,
        Bust,
    }

    /// <summary>A player's or the dealer's cards, and for a player what is riding on them.</summary>
    public sealed class BlackjackHand
    {
        readonly List<Card> cards = new List<Card>();
        readonly ReadOnlyCollection<Card> view;

        public IReadOnlyList<Card> Cards => view;

        /// <summary>Credits riding on this hand. Doubles when the hand is doubled.</summary>
        public long Stake { get; internal set; }

        /// <summary>Made by a split: 21 on two cards here is 21, not blackjack.</summary>
        public bool FromSplit { get; }

        public bool Doubled { get; internal set; }

        /// <summary>No more cards for this hand: stood, doubled, bust, or reached 21.</summary>
        public bool Finished { get; internal set; }

        public HandResult Result { get; internal set; }

        /// <summary>What the hand paid when the round settled, stake included.</summary>
        public long Payout { get; internal set; }

        internal BlackjackHand(long stake, bool fromSplit)
        {
            view = cards.AsReadOnly();
            Stake = stake;
            FromSplit = fromSplit;
        }

        internal void Add(Card card) => cards.Add(card);

        internal Card TakeSecond()
        {
            var card = cards[1];
            cards.RemoveAt(1);
            return card;
        }

        int Hard
        {
            get
            {
                var total = 0;
                foreach (var card in cards) total += card.Points;
                return total;
            }
        }

        bool HasAce
        {
            get
            {
                foreach (var card in cards)
                    if (card.IsAce) return true;
                return false;
            }
        }

        /// <summary>The best total: one ace counts 11 when that does not bust the hand.</summary>
        public int Total => IsSoft ? Hard + 10 : Hard;

        /// <summary>An ace is counting 11.</summary>
        public bool IsSoft => HasAce && Hard + 10 <= 21;

        public bool IsBust => Total > 21;

        /// <summary>21 on the first two cards of a hand that was not split.</summary>
        public bool IsBlackjack => cards.Count == 2 && Total == 21 && !FromSplit;

        public bool CanBeSplit => cards.Count == 2 && cards[0].Rank == cards[1].Rank && !FromSplit;
    }
}
