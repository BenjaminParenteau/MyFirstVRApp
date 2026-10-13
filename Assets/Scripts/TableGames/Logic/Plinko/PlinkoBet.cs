using System;
using System.Linq;

namespace HighStakes.TableGames.Plinko
{
    /// <summary>The player's wager, row count and risk, kept to what the table offers.</summary>
    public sealed class PlinkoBet
    {
        readonly WagerAmount wager;
        readonly PlinkoPaytable paytable;

        public long Wager => wager.Value;
        public int Rows { get; private set; }

        /// <summary>Every risk comes with every row count, so any of them is a board the table offers.</summary>
        public PlinkoRisk Risk { get; set; }

        public PlinkoBet(long wager, long minWager, long maxWager, int rows, PlinkoRisk risk, PlinkoPaytable paytable)
        {
            this.wager = new WagerAmount(wager, minWager, maxWager);
            this.paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));
            // An opening row count the table does not offer falls to the nearest one it does.
            Rows = paytable.Rows.OrderBy(r => Math.Abs(r - rows)).ThenBy(r => r).First();
            Risk = risk;
        }

        /// <summary>The bucket multipliers of the board this bet plays, in hundredths.</summary>
        public long[] Multipliers => paytable.For(Rows, Risk);

        public bool CanAddRows => Rows < paytable.Rows[paytable.Rows.Count - 1];
        public bool CanRemoveRows => Rows > paytable.Rows[0];

        public void HalveWager() => wager.Halve();

        public void DoubleWager() => wager.Double();

        /// <summary>The next row count on offer up (+1) or down (-1), stopping at the ends.</summary>
        public void StepRows(int direction)
        {
            var offered = paytable.Rows;
            var index = 0;
            for (var i = 0; i < offered.Count; i++)
                if (offered[i] == Rows) index = i;
            Rows = offered[Math.Max(0, Math.Min(offered.Count - 1, index + Math.Sign(direction)))];
        }
    }
}
