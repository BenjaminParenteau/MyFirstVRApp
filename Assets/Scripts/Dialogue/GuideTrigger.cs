using System.Collections.Generic;
using HighStakes.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Dialogue
{
    /// <summary>
    /// Asks the Guide for one line when something happens in the game: the first chip win or loss, reaching a tier,
    /// picking up an item, or entering an area. One component per line; the Guide plays each line once per session,
    /// so triggers don't need to remember anything. Sources are found in the loaded scenes (another slot's scene may
    /// load later, so it keeps looking), the same way the wrist display finds the wallet.
    /// </summary>
    public class GuideTrigger : MonoBehaviour
    {
        public enum When
        {
            FirstChipChange,
            TierReached,
            ItemAdded,
            AreaLoaded,
        }

        [SerializeField] private string lineId;
        [SerializeField] private When when;
        [Tooltip("TierReached: the tier index to reach (0 = starting tier).")]
        [SerializeField, Min(0)] private int tierIndex = 1;
        [Tooltip("ItemAdded: the ItemData id, e.g. vip_keycard.")]
        [SerializeField] private string itemId;
        [Tooltip("AreaLoaded: the scene name, e.g. MVP_SecurityHall.")]
        [SerializeField] private string sceneName;
        [Tooltip("Optional. Empty = the first IGuideVoice in the loaded scenes.")]
        [SerializeField] private MonoBehaviour voiceSource;

        // Events from sources that are still starting up (the wallet's opening balance, the starting tier) aren't
        // the player doing anything, so they only set the baseline.
        private const float SettleSeconds = 1f;
        private const float SearchInterval = 1f;

        private IGuideVoice voice;
        private IChipWallet wallet;
        private ICharacterTier tier;
        private readonly List<IInventoryEvents> inventories = new List<IInventoryEvents>();
        private float settledAt;
        private float nextSearchTime;
        private int highestTier = -1;

        private void OnEnable()
        {
            if (when == When.AreaLoaded || when == When.ItemAdded)
                SceneManager.sceneLoaded += OnSceneLoaded;
            Bind();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (tier != null) tier.TierChanged -= OnTierChanged;
            foreach (var source in inventories) source.ItemAdded -= OnItemAdded;
            wallet = null;
            tier = null;
            inventories.Clear();
        }

        private void Update()
        {
            if (Bound() || Time.unscaledTime < nextSearchTime)
                return;
            nextSearchTime = Time.unscaledTime + SearchInterval;
            Bind();
        }

        private bool Bound()
        {
            switch (when)
            {
                case When.FirstChipChange: return wallet != null;
                case When.TierReached: return tier != null;
                case When.ItemAdded: return inventories.Count > 0; // later scenes re-bind on load
                default: return true;
            }
        }

        private void Bind()
        {
            switch (when)
            {
                case When.FirstChipChange when wallet == null:
                    wallet = Resolve<IChipWallet>(null);
                    if (wallet != null)
                    {
                        settledAt = Time.unscaledTime + SettleSeconds;
                        wallet.BalanceChanged += OnBalanceChanged;
                    }
                    break;
                case When.TierReached when tier == null:
                    tier = Resolve<ICharacterTier>(null);
                    if (tier != null)
                    {
                        settledAt = Time.unscaledTime + SettleSeconds;
                        highestTier = tier.Current != null ? tier.Current.index : -1;
                        tier.TierChanged += OnTierChanged;
                    }
                    break;
                case When.ItemAdded:
                    // Every source counts (the pickup prototype, a test mock, later Slot B's inventory); drop the
                    // ones whose scene was unloaded, as the notebook does
                    inventories.RemoveAll(source => source is Object unityObject && unityObject == null);
                    foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    {
                        if (candidate is IInventoryEvents source && !inventories.Contains(source))
                        {
                            inventories.Add(source);
                            source.ItemAdded += OnItemAdded;
                        }
                    }
                    break;
            }
        }

        private void OnBalanceChanged(int balance)
        {
            if (Time.unscaledTime >= settledAt)
                Fire();
        }

        private void OnTierChanged(TierDefinition newTier)
        {
            int index = newTier != null ? newTier.index : -1;
            bool rose = index > highestTier;
            highestTier = Mathf.Max(highestTier, index);
            if (rose && index >= tierIndex && Time.unscaledTime >= settledAt)
                Fire();
        }

        private void OnItemAdded(ItemData item)
        {
            if (item != null && item.id == itemId)
                Fire();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (when == When.ItemAdded)
                Bind();
            else if (scene.name == sceneName)
                Fire();
        }

        private void Fire()
        {
            voice ??= Resolve<IGuideVoice>(voiceSource);
            if (voice != null)
                voice.Play(lineId);
            else
                Debug.LogWarning($"[Guide] No IGuideVoice loaded for line '{lineId}'.", this);
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
    }
}
