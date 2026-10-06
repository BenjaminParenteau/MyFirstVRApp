using System;

namespace HighStakes.TableGames.Blackjack
{
    /// <summary>Infinite-deck Blackjack against the house: every card is an independent draw from a full deck.</summary>
    public sealed class BlackjackGame
    {
        /// <summary>The most a round can take: a split into two hands, both doubled.</summary>
        public const int MostWagersStaked = 4;

        /// <summary>The most a round can pay: both of those hands winning even money.</summary>
        public const int MostWagersReturned = 8;

        readonly IRng rng;
        readonly ITableWallet wallet;
        readonly BlackjackRuleSet rules;

        public BlackjackGame(IRng rng, ITableWallet wallet, BlackjackRuleSet rules)
        {
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Takes the wager and deals. False, with nothing debited and nothing dealt,
        /// when the wallet cannot cover the wager. The round may already be settled
        /// when a blackjack is dealt.
        /// </summary>
        public bool TryStart(long wager, out BlackjackRound round)
        {
            if (wager <= 0) throw new ArgumentOutOfRangeException(nameof(wager));

            // Priced at the largest possible return before the debit, so a bet too large to pay is refused with the wallet untouched.
            _ = checked(wager * MostWagersReturned);
            round = null;
            if (!wallet.TryDebit(wager, "Blackjack wager")) return false;

            round = new BlackjackRound(rng, wallet, rules, wager);
            return true;
        }
    }
}
