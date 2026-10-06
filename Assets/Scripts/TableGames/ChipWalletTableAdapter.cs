using System;
using HighStakes.Core;

namespace HighStakes.TableGames
{
    /// <summary>
    /// Lets the tables' long amounts run against the game's one int IChipWallet. A debit above int.MaxValue can never
    /// be covered, so it is refused rather than truncated; a credit that large saturates in the wallet anyway.
    /// IChipWallet keeps no reasons, so they are dropped here.
    /// </summary>
    public sealed class ChipWalletTableAdapter : ITableWallet
    {
        readonly IChipWallet wallet;

        public ChipWalletTableAdapter(IChipWallet wallet)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public long Balance => wallet.Balance;

        public bool TryDebit(long amount, string reason)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "A debit must be at least one chip.");
            return amount <= int.MaxValue && wallet.TrySpend((int)amount);
        }

        public void Credit(long amount, string reason)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "A credit must be at least one chip.");
            wallet.Add((int)Math.Min(amount, int.MaxValue));
        }
    }
}
