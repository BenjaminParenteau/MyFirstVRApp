using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HighStakes.Core;
using HighStakes.Inventory;
using HighStakes.UI;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>
    /// What the back-room puzzles share across area visits: a chip reward paid once per wallet (the claim follows the
    /// wallet, like Inventory/BarPuzzle), and a reward note that comes back after a door trip until it is actually in
    /// the notebook, so solving a puzzle and walking off without the note never locks the player out of the vault.
    /// </summary>
    public static class PuzzleSession
    {
        static ConditionalWeakTable<IChipWallet, HashSet<string>> paid = new ConditionalWeakTable<IChipWallet, HashSet<string>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => paid = new ConditionalWeakTable<IChipWallet, HashSet<string>>();

        /// <summary>Adds <paramref name="amount"/> chips the first time <paramref name="puzzleId"/> pays this wallet.</summary>
        public static bool PayOnce(string puzzleId, int amount)
        {
            IChipWallet wallet = null;
            foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component is IChipWallet candidate) { wallet = candidate; break; }
            if (wallet == null) return false;
            var claims = paid.GetValue(wallet, _ => new HashSet<string>());
            if (claims.Contains(puzzleId) || wallet.Balance > int.MaxValue - amount) return false;
            claims.Add(puzzleId);
            wallet.Add(amount);
            return true;
        }

        /// <summary>Puts the note at <paramref name="seat"/> unless its item is already in the notebook.</summary>
        public static void SpawnUncollected(GameObject notePrefab, Transform seat, Transform parent)
        {
            if (notePrefab == null || seat == null) return;
            var pickup = notePrefab.GetComponent<PickupItem>();
            if (pickup != null && ItemLedger.Has(pickup.Item)) return;
            Object.Instantiate(notePrefab, seat.position, seat.rotation, parent);
        }
    }
}
