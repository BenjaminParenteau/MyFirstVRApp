using System;

namespace HighStakes.Economy
{
    /// <summary>
    /// The chip balance rules, with no Unity dependency so they can be unit tested. <c>ChipWallet</c> wraps one
    /// of these as the game's single IChipWallet.
    /// </summary>
    public class ChipLedger
    {
        public int Balance { get; private set; }

        /// <summary>Raised after every change, with the new balance. Not raised for zero amounts.</summary>
        public event Action<int> BalanceChanged;

        public ChipLedger(int startingBalance = 0)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance), "Balance can't start negative.");
            Balance = startingBalance;
        }

        /// <summary>Removes <paramref name="amount"/> if the balance covers it. Returns false and changes nothing otherwise.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Spend a positive amount; use Add to give chips.");
            if (amount > Balance) return false;
            if (amount == 0) return true;
            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Add a positive amount; use TrySpend to take chips.");
            if (amount == 0) return;
            Balance = (int)Math.Min((long)Balance + amount, int.MaxValue); // saturate instead of wrapping negative
            BalanceChanged?.Invoke(Balance);
        }
    }
}
