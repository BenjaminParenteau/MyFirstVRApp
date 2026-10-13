using System;
using System.Collections.Generic;
using System.Linq;

namespace HighStakes.TableGames.Plinko
{
    public enum PlinkoRisk
    {
        Low,
        Medium,
        High,
    }

    /// <summary>
    /// The multipliers for every board a table offers, one row of buckets per (rows, risk). Validated once, on
    /// construction: a board has rows + 1 buckets, is mirror symmetric, and pays nothing negative, and every row count
    /// comes in every risk, so the rows and risk controls cannot reach a board that does not exist.
    /// </summary>
    public sealed class PlinkoPaytable
    {
        public const int MinRows = 8;
        public const int MaxRows = 16;

        static readonly PlinkoRisk[] Risks = (PlinkoRisk[])Enum.GetValues(typeof(PlinkoRisk));

        readonly Dictionary<(int, PlinkoRisk), long[]> boards = new Dictionary<(int, PlinkoRisk), long[]>();

        /// <summary>The row counts on offer, smallest first.</summary>
        public IReadOnlyList<int> Rows { get; }

        public PlinkoPaytable(IEnumerable<(int Rows, PlinkoRisk Risk, long[] Multipliers)> boards)
        {
            foreach (var (rows, risk, multipliers) in boards)
            {
                if (rows < MinRows || rows > MaxRows)
                    throw new ArgumentException($"A board has {MinRows} to {MaxRows} rows, not {rows}.");
                if (multipliers == null || multipliers.Length != rows + 1)
                    throw new ArgumentException($"A {rows} row board has {rows + 1} buckets.");
                for (var k = 0; k <= rows; k++)
                {
                    if (multipliers[k] < 0) throw new ArgumentException($"{rows} rows {risk}: bucket {k} is negative.");
                    if (multipliers[k] != multipliers[rows - k])
                        throw new ArgumentException($"{rows} rows {risk}: buckets {k} and {rows - k} must match.");
                }
                if (this.boards.ContainsKey((rows, risk)))
                    throw new ArgumentException($"{rows} rows {risk} is defined twice.");
                this.boards.Add((rows, risk), (long[])multipliers.Clone());
            }
            if (this.boards.Count == 0) throw new ArgumentException("A paytable needs at least one board.");

            Rows = this.boards.Keys.Select(key => key.Item1).Distinct().OrderBy(count => count).ToArray();
            foreach (var rows in Rows)
            foreach (var risk in Risks)
                if (!this.boards.ContainsKey((rows, risk)))
                    throw new ArgumentException($"{rows} rows has no {risk} board. Every row count needs every risk.");
        }

        /// <summary>
        /// The boards a new rules asset starts with. Kept here, and read by the asset's field initialiser, so the
        /// Unity-free tests can hold every shipped board to its return.
        /// </summary>
        public static PlinkoPaytable Standard() => new PlinkoPaytable(new (int, PlinkoRisk, long[])[]
        {
            // Each board returns 0.99 per credit before rounding: shaped by risk, floored to hundredths, then topped
            // back up from the centre out without passing 0.99.
            (8, PlinkoRisk.Low, new long[] { 233, 168, 126, 92, 69, 92, 126, 168, 233 }),
            (8, PlinkoRisk.Medium, new long[] { 534, 280, 147, 77, 42, 77, 147, 280, 534 }),
            (8, PlinkoRisk.High, new long[] { 1287, 444, 152, 52, 19, 52, 152, 444, 1287 }),
            (12, PlinkoRisk.Low, new long[] { 381, 279, 209, 153, 113, 84, 63, 84, 113, 153, 209, 279, 381 }),
            (12, PlinkoRisk.Medium, new long[] { 1479, 776, 409, 216, 113, 59, 32, 59, 113, 216, 409, 776, 1479 }),
            (12, PlinkoRisk.High, new long[] { 6029, 2078, 717, 247, 85, 29, 11, 29, 85, 247, 717, 2078, 6029 }),
            (16, PlinkoRisk.Low, new long[] { 631, 474, 348, 256, 190, 140, 104, 77, 59, 77, 104, 140, 190, 256, 348, 474, 631 }),
            (16, PlinkoRisk.Medium, new long[] { 4191, 2198, 1159, 609, 322, 168, 88, 46, 27, 46, 88, 168, 322, 609, 1159, 2198, 4191 }),
            (16, PlinkoRisk.High, new long[] { 28932, 9978, 3439, 1185, 409, 141, 48, 16, 8, 16, 48, 141, 409, 1185, 3439, 9978, 28932 }),
        });

        public bool Offers(int rows, PlinkoRisk risk) => boards.ContainsKey((rows, risk));

        /// <summary>The bucket multipliers in hundredths, left to right. A copy: the table cannot be changed through it.</summary>
        public long[] For(int rows, PlinkoRisk risk)
        {
            if (!boards.TryGetValue((rows, risk), out var multipliers))
                throw new ArgumentOutOfRangeException(nameof(rows), $"This table has no {rows} row {risk} board.");
            return (long[])multipliers.Clone();
        }

        public long Largest(int rows, PlinkoRisk risk)
        {
            var largest = 0L;
            foreach (var m in For(rows, risk)) largest = Math.Max(largest, m);
            return largest;
        }

        /// <summary>
        /// Credits returned per credit staked on a board, before rounding a payout down: each bucket's multiplier
        /// weighted by its chance, C(rows, k) / 2^rows.
        /// </summary>
        public double ExpectedReturn(int rows, PlinkoRisk risk)
        {
            var multipliers = For(rows, risk);
            double total = 0, ways = 1;
            for (var k = 0; k <= rows; k++)
            {
                total += ways * multipliers[k];
                ways = ways * (rows - k) / (k + 1);
            }
            return total / Math.Pow(2, rows) / 100.0;
        }
    }
}
