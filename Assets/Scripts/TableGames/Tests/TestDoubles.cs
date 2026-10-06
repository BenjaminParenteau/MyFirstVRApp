using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HighStakes.TableGames.Tests
{
    /// <summary>Returns a scripted sequence of rolls, cycling once it runs out.</summary>
    public sealed class FakeRng : IRng
    {
        readonly int[] values;
        int next;

        public int Calls { get; private set; }

        /// <summary>The range each roll was asked for, in order.</summary>
        public List<(int MinInclusive, int MaxExclusive)> Ranges { get; } =
            new List<(int, int)>();

        public FakeRng(params int[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Needs at least one scripted value.", nameof(values));
            this.values = values;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            Calls++;
            Ranges.Add((minInclusive, maxExclusive));
            var value = values[next++ % values.Length];
            if (value < minInclusive || value >= maxExclusive)
                throw new InvalidOperationException(
                    $"Scripted value {value} is outside the requested range [{minInclusive}, {maxExclusive}).");
            return value;
        }
    }

    /// <summary>
    /// Throws on the first roll, so a test can prove what a game did to the
    /// wallet before it knew the outcome.
    /// </summary>
    public sealed class ThrowingRng : IRng
    {
        public sealed class Rolled : Exception
        {
            public Rolled() : base("ThrowingRng was asked for a roll.") { }
        }

        public int Next(int minInclusive, int maxExclusive) => throw new Rolled();
    }

    public readonly struct LedgerEntry
    {
        public readonly long Delta;
        public readonly long BalanceAfter;
        public readonly string Reason;

        public LedgerEntry(long delta, long balanceAfter, string reason)
        {
            Delta = delta;
            BalanceAfter = balanceAfter;
            Reason = reason;
        }
    }

    /// <summary>
    /// An in-memory wallet that remembers every move and why, so a test can check
    /// the reason strings and replay the ledger to the balance. The real chip wallet
    /// has neither, which is why the games are held to this one.
    /// </summary>
    public sealed class TestWallet : ITableWallet
    {
        readonly List<LedgerEntry> ledger = new List<LedgerEntry>();
        readonly ReadOnlyCollection<LedgerEntry> ledgerView;

        public long Balance { get; private set; }

        public IReadOnlyList<LedgerEntry> Ledger => ledgerView;

        public TestWallet(long openingBalance)
        {
            ledgerView = ledger.AsReadOnly();
            if (openingBalance < 0) throw new ArgumentOutOfRangeException(nameof(openingBalance));
            if (openingBalance > 0) Apply(openingBalance, "Opening balance");
        }

        public bool TryDebit(long amount, string reason)
        {
            RequirePositive(amount);
            if (amount > Balance) return false;
            Apply(-amount, reason);
            return true;
        }

        public void Credit(long amount, string reason)
        {
            RequirePositive(amount);
            Apply(amount, reason);
        }

        void Apply(long delta, string reason)
        {
            Balance += delta;
            ledger.Add(new LedgerEntry(delta, Balance, reason));
        }

        static void RequirePositive(long amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }
    }
}
