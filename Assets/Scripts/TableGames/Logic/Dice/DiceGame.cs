using System;

namespace HighStakes.TableGames.Dice
{
    /// <summary>
    /// One roll: the wager is taken and the number drawn when it starts, but a win is only credited by
    /// <see cref="Settle"/>, when the marker lands, so the chip count can't give the result away first.
    /// </summary>
    public sealed class DiceRound
    {
        readonly ITableWallet wallet;

        public long Wager { get; }
        public int Target { get; }

        /// <summary>1 to 100.</summary>
        public int Roll { get; }

        public bool Won => Roll > Target;

        /// <summary>Everything credited back on a win, stake included. Zero on a loss.</summary>
        public long Payout { get; }

        public long Net => Payout - Wager;

        public bool Settled { get; private set; }

        internal DiceRound(ITableWallet wallet, long wager, int target, int roll, long payout)
        {
            this.wallet = wallet;
            Wager = wager;
            Target = target;
            Roll = roll;
            Payout = payout;
        }

        /// <summary>Credits the win, once. Also how a roll the player walked away from still pays.</summary>
        public void Settle()
        {
            if (Settled) return;
            Settled = true;
            // A win at an extreme edge can round down to nothing, and the wallet refuses a zero credit.
            if (Payout > 0) wallet.Credit(Payout, "Dice win");
        }
    }

    /// <summary>
    /// Roll-over dice. The house rolls 1 to 100 and the player wins on a roll strictly above a target from 1 to 99.
    /// A win returns wager * (10000 - edge) / (100 * (100 - target)), stake included, rounded down; so whatever the
    /// target, the player gets back (1 - edge) of what they stake. The wager is debited before the roll.
    /// </summary>
    public sealed class DiceGame
    {
        public const int MinRoll = 1;
        public const int MaxRoll = 100;
        public const int MinTarget = 1;
        public const int MaxTarget = 99;

        readonly IRng rng;
        readonly ITableWallet wallet;
        readonly int houseEdgeBasisPoints;

        public DiceGame(IRng rng, ITableWallet wallet, int houseEdgeBasisPoints)
        {
            if (houseEdgeBasisPoints < 0 || houseEdgeBasisPoints >= 10000)
                throw new ArgumentOutOfRangeException(nameof(houseEdgeBasisPoints));
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.houseEdgeBasisPoints = houseEdgeBasisPoints;
        }

        // ponytail: unchecked, as in vrstake. wager * (10000 - edge) overflows near 9.3e14 chips at a 1% edge, far above
        // any table limit and the int chip wallet; price it checked before the debit (as roulette does) if limits grow.
        /// <summary>Total returned on a win, stake included. Rounds down in favour of the house.</summary>
        public static long PayoutFor(long wager, int target, int houseEdgeBasisPoints)
        {
            return wager * (10000 - houseEdgeBasisPoints) / (100L * (MaxRoll - target));
        }

        /// <summary>
        /// Takes the wager and rolls; call <see cref="DiceRound.Settle"/> to pay a win.
        /// False, with nothing debited and no roll, when the wallet cannot cover the wager.
        /// </summary>
        public bool TryRoll(long wager, int target, out DiceRound round)
        {
            if (wager <= 0) throw new ArgumentOutOfRangeException(nameof(wager));
            if (target < MinTarget || target > MaxTarget) throw new ArgumentOutOfRangeException(nameof(target));

            round = null;
            if (!wallet.TryDebit(wager, "Dice wager")) return false;

            var roll = rng.Next(MinRoll, MaxRoll + 1);
            var payout = roll > target ? PayoutFor(wager, target, houseEdgeBasisPoints) : 0;
            round = new DiceRound(wallet, wager, target, roll, payout);
            return true;
        }
    }
}
