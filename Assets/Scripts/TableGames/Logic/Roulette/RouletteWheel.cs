using System;

namespace HighStakes.TableGames.Roulette
{
    /// <summary>A European single-zero wheel: pockets 0 to 36, 18 red, 18 black, zero green.</summary>
    public static class RouletteWheel
    {
        public const int Pockets = 37;

        static readonly bool[] red = BuildRed();

        /// <summary>The pockets in order around the wheel, clockwise from zero, as on a real European wheel.</summary>
        public static readonly int[] Order =
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
            5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26,
        };

        static bool[] BuildRed()
        {
            var r = new bool[Pockets];
            foreach (var n in new[] { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 }) r[n] = true;
            return r;
        }

        public static bool IsRed(int pocket)
        {
            if (pocket < 0 || pocket >= Pockets) throw new ArgumentOutOfRangeException(nameof(pocket));
            return red[pocket];
        }

        public static bool IsGreen(int pocket) => pocket == 0;

        /// <summary>"RED", "BLACK" or "GREEN".</summary>
        public static string ColourName(int pocket) => IsGreen(pocket) ? "GREEN" : IsRed(pocket) ? "RED" : "BLACK";

        /// <summary>Where a pocket sits around the wheel, 0 to 36 clockwise from zero.</summary>
        public static int SlotOf(int pocket)
        {
            if (pocket < 0 || pocket >= Pockets) throw new ArgumentOutOfRangeException(nameof(pocket));
            return Array.IndexOf(Order, pocket);
        }
    }
}
