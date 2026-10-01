using System;
using HighStakes.Core;
using UnityEngine;

namespace HighStakes.UI.Mocks
{
    /// <summary>
    /// Stand-in IChipWallet for testing UI before Slot A's real wallet exists. NOT the economy source of truth.
    /// Keep it under _SoloTest so it switches off inside MVP_Main. Inspector buttons add/spend chips in Play mode.
    /// </summary>
    public class MockChipWallet : MonoBehaviour, IChipWallet
    {
        [SerializeField] private int startingBalance = 500;

        private int balance;

        public int Balance => balance;

        public event Action<int> BalanceChanged;

        private void Awake()
        {
            balance = startingBalance;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > balance)
                return false;

            balance -= amount;
            BalanceChanged?.Invoke(balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            balance += amount;
            BalanceChanged?.Invoke(balance);
        }
    }
}
