using System;
using System.Numerics;

namespace HighStakes.TableGames.Mines
{
    /// <summary>
    /// Mines hidden under a 5 by 5 grid. The player reveals tiles one at a time; every safe tile raises the payout, a
    /// mine ends the round. After k safe tiles with m mines, cashing out pays the wager times (1 - edge) over the chance
    /// of surviving k picks, so every strategy returns 1 - edge.
    /// </summary>
    public sealed class MinesGame
    {
        public const int Tiles = 25;
        public const int MinMines = 1;
        public const int MaxMines = Tiles - 1;

        readonly IRng rng;
        readonly ITableWallet wallet;
        readonly int houseEdgeBasisPoints;

        public MinesGame(IRng rng, ITableWallet wallet, int houseEdgeBasisPoints)
        {
            if (houseEdgeBasisPoints < 0 || houseEdgeBasisPoints >= 10000)
                throw new ArgumentOutOfRangeException(nameof(houseEdgeBasisPoints));
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.houseEdgeBasisPoints = houseEdgeBasisPoints;
        }

        /// <summary>
        /// Total returned for cashing out after safe tiles, stake included, rounded down: the wager times (1 - edge)
        /// divided by the chance of surviving that many picks, C(25 - mines, safe) / C(25, safe). Throws
        /// OverflowException rather than wrapping.
        /// </summary>
        public static long PayoutFor(long wager, int mines, int safe, int houseEdgeBasisPoints)
        {
            if (mines < MinMines || mines > MaxMines) throw new ArgumentOutOfRangeException(nameof(mines));
            if (safe < 0 || safe > Tiles - mines) throw new ArgumentOutOfRangeException(nameof(safe));
            var numerator = new BigInteger(wager) * (10000 - houseEdgeBasisPoints) * Choose(Tiles, safe);
            var denominator = 10000 * Choose(Tiles - mines, safe);
            return (long)(numerator / denominator);
        }

        /// <summary>The payout multiplier after safe tiles, in hundredths, rounded down. 0.99x before any.</summary>
        public static long MultiplierFor(int mines, int safe, int houseEdgeBasisPoints) =>
            PayoutFor(100, mines, safe, houseEdgeBasisPoints);

        static BigInteger Choose(int n, int k)
        {
            BigInteger result = 1;
            for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
            return result;
        }

        /// <summary>
        /// Takes the wager and hides the mines. False, with nothing debited and nothing drawn, when the wallet cannot
        /// cover the wager.
        /// </summary>
        public bool TryStart(long wager, int mines, out MinesRound round)
        {
            if (wager <= 0) throw new ArgumentOutOfRangeException(nameof(wager));
            if (mines < MinMines || mines > MaxMines) throw new ArgumentOutOfRangeException(nameof(mines));

            // Priced at a cleared board before the debit, so a bet too large to pay is refused with the wallet untouched.
            _ = PayoutFor(wager, mines, Tiles - mines, houseEdgeBasisPoints);
            round = null;
            if (!wallet.TryDebit(wager, "Mines wager")) return false;

            // A partial Fisher-Yates shuffle: the first `mines` slots of a shuffled grid hold the mines.
            var order = new int[Tiles];
            for (var i = 0; i < Tiles; i++) order[i] = i;
            var isMine = new bool[Tiles];
            for (var i = 0; i < mines; i++)
            {
                var j = rng.Next(i, Tiles);
                (order[i], order[j]) = (order[j], order[i]);
                isMine[order[i]] = true;
            }

            round = new MinesRound(wallet, wager, mines, isMine, houseEdgeBasisPoints);
            return true;
        }
    }
}
