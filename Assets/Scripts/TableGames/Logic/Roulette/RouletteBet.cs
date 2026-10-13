using System;

namespace HighStakes.TableGames.Roulette
{
    public enum RouletteBetKind
    {
        /// <summary>One number, 0 to 36. Pays 35 to 1.</summary>
        Straight,
        Red,
        Black,
        Odd,
        Even,
        /// <summary>1 to 18.</summary>
        Low,
        /// <summary>19 to 36.</summary>
        High,
        /// <summary>1-12, 13-24 or 25-36 (Number 1, 2 or 3). Pays 2 to 1.</summary>
        Dozen,
        /// <summary>The numbers in one row of the layout: 1, 4, 7... (Number 1), 2, 5, 8... (2) or 3, 6, 9... (3). Pays 2 to 1.</summary>
        Column,
    }

    /// <summary>
    /// A place on the layout chips can go. Zero is green and belongs to no outside bet, which is the whole house edge:
    /// every bet pays as if the wheel had 36 pockets, but it has 37.
    /// </summary>
    public readonly struct RouletteBet : IEquatable<RouletteBet>
    {
        public readonly RouletteBetKind Kind;

        /// <summary>The number for a straight bet; 1 to 3 for a dozen or column; unused otherwise.</summary>
        public readonly int Number;

        RouletteBet(RouletteBetKind kind, int number)
        {
            Kind = kind;
            Number = number;
        }

        public static RouletteBet Straight(int number)
        {
            if (number < 0 || number >= RouletteWheel.Pockets) throw new ArgumentOutOfRangeException(nameof(number));
            return new RouletteBet(RouletteBetKind.Straight, number);
        }

        public static RouletteBet Dozen(int which) => new RouletteBet(RouletteBetKind.Dozen, Third(which));

        public static RouletteBet Column(int which) => new RouletteBet(RouletteBetKind.Column, Third(which));

        /// <summary>Red, black, odd, even, low or high.</summary>
        public static RouletteBet EvenMoney(RouletteBetKind kind)
        {
            if (kind == RouletteBetKind.Straight || kind == RouletteBetKind.Dozen || kind == RouletteBetKind.Column)
                throw new ArgumentOutOfRangeException(nameof(kind), "Not an even-money bet.");
            return new RouletteBet(kind, 0);
        }

        static int Third(int which)
        {
            if (which < 1 || which > 3) throw new ArgumentOutOfRangeException(nameof(which), "Dozens and columns are 1, 2 or 3.");
            return which;
        }

        /// <summary>Winnings per chip on a win, not counting the chip itself: 35, 2 or 1.</summary>
        public int PayoutRatio => Kind == RouletteBetKind.Straight ? 35 : Kind == RouletteBetKind.Dozen || Kind == RouletteBetKind.Column ? 2 : 1;

        public bool Wins(int pocket)
        {
            if (pocket < 0 || pocket >= RouletteWheel.Pockets) throw new ArgumentOutOfRangeException(nameof(pocket));
            if (Kind == RouletteBetKind.Straight) return pocket == Number;
            if (pocket == 0) return false;
            switch (Kind)
            {
                case RouletteBetKind.Red: return RouletteWheel.IsRed(pocket);
                case RouletteBetKind.Black: return !RouletteWheel.IsRed(pocket);
                case RouletteBetKind.Odd: return pocket % 2 == 1;
                case RouletteBetKind.Even: return pocket % 2 == 0;
                case RouletteBetKind.Low: return pocket <= 18;
                case RouletteBetKind.High: return pocket >= 19;
                case RouletteBetKind.Dozen: return (pocket - 1) / 12 + 1 == Number;
                default: return (pocket - 1) % 3 + 1 == Number; // Column
            }
        }

        /// <summary>What the layout says on the spot: "17", "RED", "1st 12", "2 TO 1".</summary>
        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case RouletteBetKind.Straight: return Number.ToString();
                    case RouletteBetKind.Low: return "1-18";
                    case RouletteBetKind.High: return "19-36";
                    case RouletteBetKind.Dozen: return Number == 1 ? "1st 12" : Number == 2 ? "2nd 12" : "3rd 12";
                    case RouletteBetKind.Column: return "2 TO 1";
                    default: return Kind.ToString().ToUpperInvariant();
                }
            }
        }

        public bool Equals(RouletteBet other) => Kind == other.Kind && Number == other.Number;
        public override bool Equals(object obj) => obj is RouletteBet other && Equals(other);
        public override int GetHashCode() => (int)Kind * 64 + Number;
        public override string ToString() => Kind == RouletteBetKind.Straight ? "Straight " + Number : Kind + (Number > 0 ? " " + Number : "");
    }
}
