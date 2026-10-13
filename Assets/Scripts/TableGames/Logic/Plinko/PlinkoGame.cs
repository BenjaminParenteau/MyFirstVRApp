using System;
using System.Collections.Generic;

namespace HighStakes.TableGames.Plinko
{
    /// <summary>
    /// One ball: the wager is taken and the path drawn when it is dropped, but the return is only credited by
    /// <see cref="Settle"/>, when the ball lands, so the chip count can't give the bucket away during the fall.
    /// </summary>
    public sealed class PlinkoDrop
    {
        readonly ITableWallet wallet;

        public long Wager { get; }

        /// <summary>Which way the ball went at each row, top first: true is right.</summary>
        public IReadOnlyList<bool> Path { get; }

        /// <summary>The bucket the ball lands in, 0 at the far left. Equal to the number of rights.</summary>
        public int Bucket { get; }

        /// <summary>The bucket's multiplier in hundredths.</summary>
        public long Multiplier { get; }

        /// <summary>Total returned, stake included. Below the wager in the low buckets, zero only on a zero multiplier.</summary>
        public long Payout { get; }

        public long Net => Payout - Wager;

        public bool Settled { get; private set; }

        internal PlinkoDrop(ITableWallet wallet, long wager, bool[] path, int bucket, long multiplier, long payout)
        {
            this.wallet = wallet;
            Wager = wager;
            Path = path;
            Bucket = bucket;
            Multiplier = multiplier;
            Payout = payout;
        }

        /// <summary>Credits the return, once. Also how a ball the player walked away from still pays.</summary>
        public void Settle()
        {
            if (Settled) return;
            Settled = true;
            if (Payout > 0) wallet.Credit(Payout, "Plinko return");
        }
    }

    /// <summary>
    /// Drop a ball through rows of pegs into a bucket that pays its multiplier. One coin flip per row, Next(0, 2), top
    /// row first: 1 is right, and the bucket is the number of rights, so bucket k of n rows comes up C(n, k) / 2^n of
    /// the time. The payout, stake included, is wager * multiplier / 100 rounded down.
    /// </summary>
    public sealed class PlinkoGame
    {
        readonly IRng rng;
        readonly ITableWallet wallet;
        readonly PlinkoPaytable paytable;

        public PlinkoGame(IRng rng, ITableWallet wallet, PlinkoPaytable paytable)
        {
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));
        }

        /// <summary>Total returned for a bucket's multiplier, rounded down. Throws OverflowException rather than wrapping.</summary>
        public static long PayoutFor(long wager, long multiplier) => checked(wager * multiplier) / 100;

        /// <summary>
        /// Takes the wager and draws the path; call <see cref="PlinkoDrop.Settle"/> when the ball lands to pay it.
        /// False, with nothing debited and no flips, when the wallet cannot cover the wager.
        /// </summary>
        public bool TryDrop(long wager, int rows, PlinkoRisk risk, out PlinkoDrop drop)
        {
            if (wager <= 0) throw new ArgumentOutOfRangeException(nameof(wager));
            var multipliers = paytable.For(rows, risk);

            // Priced at the best bucket before the debit, so a bet too large to pay is refused with the wallet untouched.
            _ = PayoutFor(wager, paytable.Largest(rows, risk));
            drop = null;
            if (!wallet.TryDebit(wager, "Plinko wager")) return false;

            var path = new bool[rows];
            var bucket = 0;
            for (var row = 0; row < rows; row++)
            {
                path[row] = rng.Next(0, 2) == 1;
                if (path[row]) bucket++;
            }

            var multiplier = multipliers[bucket];
            drop = new PlinkoDrop(wallet, wager, path, bucket, multiplier, PayoutFor(wager, multiplier));
            return true;
        }
    }
}
