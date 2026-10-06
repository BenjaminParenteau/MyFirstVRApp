using System;
using HighStakes.Core;
using UnityEngine;

namespace HighStakes.UI.Mocks
{
    /// <summary>
    /// Stand-in ICharacterTier for testing UI before Slot B's real one exists. Picks the highest tier whose
    /// chip threshold the wallet balance has reached. Keep it under _SoloTest so it switches off inside MVP_Main.
    /// </summary>
    public class MockCharacterTier : MonoBehaviour, ICharacterTier
    {
        [Tooltip("Any component implementing IChipWallet (usually the MockChipWallet next to this).")]
        [SerializeField] private MonoBehaviour walletSource;
        [SerializeField] private TierDefinition[] tiers = Array.Empty<TierDefinition>();

        private IChipWallet wallet;

        public TierDefinition Current { get; private set; }

        public event Action<TierDefinition> TierChanged;

        private void OnEnable()
        {
            wallet = walletSource as IChipWallet;
            if (wallet != null)
                wallet.BalanceChanged += Evaluate;
        }

        private void Start()
        {
            // The wallet sets its starting balance in its own Awake, which may run after our OnEnable
            if (wallet != null)
                Evaluate(wallet.Balance);
        }

        private void OnDisable()
        {
            if (wallet != null)
                wallet.BalanceChanged -= Evaluate;
        }

        private void Evaluate(int balance)
        {
            TierDefinition best = null;
            foreach (var tier in tiers)
            {
                if (tier != null && balance >= tier.chipThreshold && (best == null || tier.chipThreshold > best.chipThreshold))
                    best = tier;
            }

            if (best == Current)
                return;

            Current = best;
            TierChanged?.Invoke(Current);
        }
    }
}
