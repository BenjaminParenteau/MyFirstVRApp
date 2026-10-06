using System;
using HighStakes.Core;
using UnityEngine;

namespace HighStakes.Economy
{
    /// <summary>
    /// The game's one chip balance (IChipWallet). Tables, the cashier cage and purchases spend and add through it;
    /// the wrist display and tier progression subscribe to <see cref="BalanceChanged"/> and find it by the interface.
    ///
    /// Put exactly one in the game (in MVP_Main, outside any _SoloTest). Slice scenes test with their own mock under
    /// _SoloTest, which switches off when MVP_Main loads them.
    /// </summary>
    public class ChipWallet : MonoBehaviour, IChipWallet
    {
        [Tooltip("Chips the player has before visiting the cashier cage.")]
        [SerializeField, Min(0)] private int startingBalance;

        private ChipLedger ledger;

        // Lazy so a consumer whose OnEnable runs before our Awake can still subscribe; serialized fields are already set.
        // ponytail: balance resets every session; save it (PlayerPrefs or a save file) when the week-6 cash-out lands.
        private ChipLedger Ledger => ledger ??= new ChipLedger(startingBalance);

        public int Balance => Ledger.Balance;

        public event Action<int> BalanceChanged
        {
            add => Ledger.BalanceChanged += value;
            remove => Ledger.BalanceChanged -= value;
        }

        public bool TrySpend(int amount) => Ledger.TrySpend(amount);

        public void Add(int amount) => Ledger.Add(amount);

        private void OnEnable()
        {
            foreach (var other in FindObjectsByType<ChipWallet>(FindObjectsSortMode.None))
                if (other != this)
                    Debug.LogError($"[ChipWallet] More than one wallet is active ({other.name} in {other.gameObject.scene.name}). There must be exactly one.", this);
        }

        // Play-mode test helpers (component's ⋮ menu in the Inspector).
        [ContextMenu("Add 1,000 chips")]
        private void DebugAdd() => Add(1000);

        [ContextMenu("Spend 500 chips")]
        private void DebugSpend() => Debug.Log(TrySpend(500) ? $"[ChipWallet] Spent 500, balance {Balance}." : $"[ChipWallet] Can't spend 500, balance {Balance}.");
    }
}
