using System;
using System.Collections.Generic;

namespace HighStakes.TableGames.Roulette
{
    /// <summary>
    /// One spin: the pocket is drawn and the bets are taken when it starts, but the winnings are only credited by
    /// <see cref="Settle"/>, when the ball lands, so the chip count can't give the result away during the spin.
    /// </summary>
    public sealed class RouletteSpin
    {
        readonly ITableWallet wallet;

        public int Pocket { get; }

        /// <summary>Every chip on the layout for this spin.</summary>
        public long Staked { get; }

        /// <summary>Everything credited back, winning stakes included. Zero when nothing won.</summary>
        public long Payout { get; }

        public long Net => Payout - Staked;

        /// <summary>The bets that won, each with what it paid back.</summary>
        public IReadOnlyList<(RouletteBet Bet, long Paid)> Winners { get; }

        public bool Settled { get; private set; }

        internal RouletteSpin(ITableWallet wallet, int pocket, long staked, long payout, IReadOnlyList<(RouletteBet, long)> winners)
        {
            this.wallet = wallet;
            Pocket = pocket;
            Staked = staked;
            Payout = payout;
            Winners = winners;
        }

        /// <summary>Credits the winnings, once. Also how a spin the player walked away from still pays.</summary>
        public void Settle()
        {
            if (Settled) return;
            Settled = true;
            if (Payout > 0) wallet.Credit(Payout, "Roulette win");
        }
    }

    /// <summary>
    /// European roulette against the house. A spin takes every chip on the layout at once, then the RNG picks a pocket
    /// from 0 to 36 and each winning bet pays its stake times (ratio + 1) when the spin settles. Every bet returns 36/37
    /// of what it stakes.
    /// </summary>
    public sealed class RouletteGame
    {
        readonly IRng rng;
        readonly ITableWallet wallet;

        public RouletteGame(IRng rng, ITableWallet wallet)
        {
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <summary>
        /// Takes all the bets and draws the pocket; call <see cref="RouletteSpin.Settle"/> to pay the winners.
        /// False, with nothing debited and no spin, when the wallet cannot cover them.
        /// Stakes must be positive; the same spot may appear more than once (its chips add up).
        /// </summary>
        public bool TrySpin(IReadOnlyCollection<(RouletteBet Bet, long Stake)> bets, out RouletteSpin spin)
        {
            if (bets == null) throw new ArgumentNullException(nameof(bets));
            if (bets.Count == 0) throw new ArgumentException("A spin needs at least one bet.", nameof(bets));

            long staked = 0, mostReturned = 0;
            foreach (var (bet, stake) in bets)
            {
                if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(bets), "Every stake must be positive.");
                staked = checked(staked + stake);
                // Priced before the debit, so a layout too large to pay throws with the wallet untouched.
                mostReturned = checked(mostReturned + stake * (bet.PayoutRatio + 1));
            }

            spin = null;
            if (!wallet.TryDebit(staked, "Roulette bets")) return false;

            int pocket = rng.Next(0, RouletteWheel.Pockets);
            long payout = 0;
            var winners = new List<(RouletteBet, long)>();
            foreach (var (bet, stake) in bets)
            {
                if (!bet.Wins(pocket)) continue;
                long paid = stake * (bet.PayoutRatio + 1);
                payout += paid;
                winners.Add((bet, paid));
            }
            spin = new RouletteSpin(wallet, pocket, staked, payout, winners);
            return true;
        }
    }
}
