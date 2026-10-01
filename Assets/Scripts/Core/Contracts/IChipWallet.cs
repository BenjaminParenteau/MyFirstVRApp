using System;

namespace HighStakes.Core
{
    /// <summary>
    /// The single chip balance. Implemented by Slot A (Economy); tier progression (B) and the wrist display (C) subscribe.
    /// </summary>
    public interface IChipWallet
    {
        int Balance { get; }

        /// <summary>Removes <paramref name="amount"/> if the balance covers it. Returns false and changes nothing otherwise.</summary>
        bool TrySpend(int amount);

        void Add(int amount);

        /// <summary>Raised after every change, with the new balance.</summary>
        event Action<int> BalanceChanged;
    }
}
