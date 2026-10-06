namespace HighStakes.TableGames
{
    /// <summary>
    /// What a table game needs from the chip wallet. Kept inside this Unity-free assembly so the games are testable;
    /// in the game it is <c>ChipWalletTableAdapter</c> over the one IChipWallet. Reasons are for logs and tests.
    /// </summary>
    public interface ITableWallet
    {
        long Balance { get; }

        /// <summary>Removes chips. Returns false, changing nothing, if the balance is too low.</summary>
        bool TryDebit(long amount, string reason);

        void Credit(long amount, string reason);
    }
}
