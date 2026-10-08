using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// Shows the bankroll and status tier on the wrist watch face. Display only: it observes IChipWallet and
    /// ICharacterTier and never changes them. Sources can be assigned in the Inspector; if left empty, the first
    /// active wallet / tier in the loaded scenes is used (that's how it finds A's and B's objects in MVP_Main).
    /// </summary>
    public class WristDisplay : MonoBehaviour
    {
        [Tooltip("Optional. Any component implementing IChipWallet. Empty = find one in the loaded scenes.")]
        [SerializeField] private MonoBehaviour walletSource;
        [Tooltip("Optional. Any component implementing ICharacterTier. Empty = find one in the loaded scenes.")]
        [SerializeField] private MonoBehaviour tierSource;
        [SerializeField] private TMP_Text bankrollText;
        [SerializeField] private TMP_Text tierText;

        private const float SearchInterval = 1f;

        private IChipWallet wallet;
        private ICharacterTier tier;
        private float nextSearchTime;

        /// <summary>The wallet this watch found (null until one is loaded). Other watch parts read through here.</summary>
        public IChipWallet Wallet => wallet;

        /// <summary>The tier source this watch found (null until one is loaded).</summary>
        public ICharacterTier Tier => tier;

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void Start()
        {
            // Sources may have finished their own Awake/Start after our OnEnable
            Refresh();
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (tier != null) tier.TierChanged -= OnTierChanged;
            wallet = null;
            tier = null;
        }

        private void Update()
        {
            // Keep looking until both sources exist (another slot's scene may load later)
            if ((wallet == null || tier == null) && Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + SearchInterval;
                Bind();
                Refresh();
            }
        }

        private void Bind()
        {
            if (wallet == null)
            {
                wallet = Resolve<IChipWallet>(walletSource);
                if (wallet != null) wallet.BalanceChanged += OnBalanceChanged;
            }

            if (tier == null)
            {
                tier = Resolve<ICharacterTier>(tierSource);
                if (tier != null) tier.TierChanged += OnTierChanged;
            }
        }

        private static T Resolve<T>(MonoBehaviour assigned) where T : class
        {
            if (assigned is T fromInspector)
                return fromInspector;

            foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is T found)
                    return found;
            }
            return null;
        }

        private void OnBalanceChanged(int balance) => SetText(bankrollText, WristDisplayLogic.FormatChips(balance));

        private void OnTierChanged(TierDefinition newTier) => SetText(tierText, WristDisplayLogic.FormatTier(newTier != null ? newTier.displayName : null));

        private void Refresh()
        {
            SetText(bankrollText, wallet != null ? WristDisplayLogic.FormatChips(wallet.Balance) : WristDisplayLogic.NoValue);
            OnTierChanged(tier?.Current);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value;
        }
    }
}
