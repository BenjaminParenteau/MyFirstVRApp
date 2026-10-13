using System;

namespace HighStakes.TableGames.Dice
{
    /// <summary>The player's current wager and target, kept inside the table limits.</summary>
    public sealed class DiceBet
    {
        readonly WagerAmount wager;

        public long Wager => wager.Value;
        public int Target { get; private set; }

        public DiceBet(long wager, int target, long minWager, long maxWager)
        {
            this.wager = new WagerAmount(wager, minWager, maxWager);
            SetTarget(target);
        }

        public void HalveWager() => wager.Halve();

        public void DoubleWager() => wager.Double();

        public void SetTarget(int target) => Target = Math.Clamp(target, DiceGame.MinTarget, DiceGame.MaxTarget);
    }
}
