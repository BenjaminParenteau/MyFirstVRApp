using System;

namespace HighStakes.TableGames
{
    /// <summary>
    /// The chips a player puts on the next round, kept inside a table's limits and adjusted the way every table does
    /// it: halve and double.
    /// </summary>
    public sealed class WagerAmount
    {
        public long Min { get; }
        public long Max { get; }
        public long Value { get; private set; }

        public WagerAmount(long value, long min, long max)
        {
            if (min <= 0) throw new ArgumentOutOfRangeException(nameof(min), "The smallest wager must be at least one chip.");
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "The largest wager must not sit below the smallest.");
            Min = min;
            Max = max;
            Value = Math.Clamp(value, min, max);
        }

        public void Halve() => Value = Math.Max(Min, Value / 2);

        // Compared before multiplying so a huge limit cannot overflow.
        public void Double() => Value = Value > Max / 2 ? Max : Value * 2;
    }
}
