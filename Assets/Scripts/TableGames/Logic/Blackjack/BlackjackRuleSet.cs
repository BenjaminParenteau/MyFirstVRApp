using System;

namespace HighStakes.TableGames.Blackjack
{
    /// <summary>The table rules a round is played under. Built from the rules asset.</summary>
    public sealed class BlackjackRuleSet
    {
        /// <summary>Profit on a natural blackjack as a percentage of the stake: 150 is 3:2, 120 is 6:5.</summary>
        public int BlackjackPaysPercent { get; }

        /// <summary>The dealer draws on soft 17 instead of standing on every 17.</summary>
        public bool DealerHitsSoft17 { get; }

        public bool DoubleAfterSplit { get; }

        public BlackjackRuleSet(int blackjackPaysPercent, bool dealerHitsSoft17, bool doubleAfterSplit)
        {
            if (blackjackPaysPercent < 100 || blackjackPaysPercent > 200)
                throw new ArgumentOutOfRangeException(nameof(blackjackPaysPercent), "A natural pays between 1:1 and 2:1.");
            BlackjackPaysPercent = blackjackPaysPercent;
            DealerHitsSoft17 = dealerHitsSoft17;
            DoubleAfterSplit = doubleAfterSplit;
        }

        /// <summary>3:2, dealer stands on all 17s, double after split.</summary>
        public static BlackjackRuleSet Standard => new BlackjackRuleSet(150, false, true);
    }
}
