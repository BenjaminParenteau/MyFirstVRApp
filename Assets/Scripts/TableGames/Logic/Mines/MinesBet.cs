using System;

namespace HighStakes.TableGames.Mines
{
    /// <summary>The player's wager and mine count, kept inside the table limits.</summary>
    public sealed class MinesBet
    {
        readonly WagerAmount wager;

        public long Wager => wager.Value;
        public int Mines { get; private set; }
        public int MinMines { get; }
        public int MaxMines { get; }

        public MinesBet(long wager, int mines, long minWager, long maxWager, int minMines, int maxMines)
        {
            if (minMines < MinesGame.MinMines || maxMines > MinesGame.MaxMines || maxMines < minMines)
                throw new ArgumentOutOfRangeException(nameof(maxMines),
                    $"Mine limits must be ordered and inside [{MinesGame.MinMines}, {MinesGame.MaxMines}].");
            this.wager = new WagerAmount(wager, minWager, maxWager);
            MinMines = minMines;
            MaxMines = maxMines;
            Mines = Math.Clamp(mines, minMines, maxMines);
        }

        public void HalveWager() => wager.Halve();

        public void DoubleWager() => wager.Double();

        public void FewerMines() => Mines = Math.Max(MinMines, Mines - 1);

        public void MoreMines() => Mines = Math.Min(MaxMines, Mines + 1);
    }
}
