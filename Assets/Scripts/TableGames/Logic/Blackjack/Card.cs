using System;

namespace HighStakes.TableGames.Blackjack
{
    public enum Suit
    {
        Spades,
        Hearts,
        Clubs,
        Diamonds,
    }

    /// <summary>A playing card. Rank 1 is the ace, 11 to 13 are the jack, queen and king.</summary>
    public readonly struct Card : IEquatable<Card>
    {
        public const int DeckSize = 52;

        public readonly int Rank;
        public readonly Suit Suit;

        public Card(int rank, Suit suit)
        {
            if (rank < 1 || rank > 13) throw new ArgumentOutOfRangeException(nameof(rank));
            if (suit < Suit.Spades || suit > Suit.Diamonds) throw new ArgumentOutOfRangeException(nameof(suit));
            Rank = rank;
            Suit = suit;
        }

        /// <summary>The card at a position 0 to 51 of a deck in suit order, ace to king within each suit.</summary>
        public static Card FromIndex(int index)
        {
            if (index < 0 || index >= DeckSize) throw new ArgumentOutOfRangeException(nameof(index));
            return new Card(index % 13 + 1, (Suit)(index / 13));
        }

        public int Index => (int)Suit * 13 + Rank - 1;

        public bool IsAce => Rank == 1;

        /// <summary>Blackjack points with the ace counted as 1; the hand decides whether it counts 11.</summary>
        public int Points => Math.Min(Rank, 10);

        public bool Equals(Card other) => Rank == other.Rank && Suit == other.Suit;
        public override bool Equals(object obj) => obj is Card other && Equals(other);
        public override int GetHashCode() => Index;

        public override string ToString()
        {
            var rank = Rank == 1 ? "A" : Rank == 11 ? "J" : Rank == 12 ? "Q" : Rank == 13 ? "K" : Rank.ToString();
            return rank + "SHCD"[(int)Suit];
        }
    }
}
