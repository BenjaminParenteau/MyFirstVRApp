using HighStakes.Core;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// Shows <see cref="target"/> only within a tier range, so the player's wrist upgrades as their tier rises
    /// (e.g. steel watch at tier 0, gold watch from tier 1). Add one per accessory. Finds the active ICharacterTier
    /// in the loaded scenes, like WristDisplay does.
    /// </summary>
    public class TierVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [Tooltip("First tier index this is shown at (0 = starting tier).")]
        [SerializeField] private int minTier;
        [Tooltip("Last tier index this is shown at. -1 = this tier and every tier above.")]
        [SerializeField] private int maxTier = -1;

        private ICharacterTier tier;
        private float nextSearchTime;

        private void Awake()
        {
            Apply(-1); // until a tier exists, look like the starting tier
        }

        private void OnDisable()
        {
            if (tier != null) tier.TierChanged -= OnTierChanged;
            tier = null;
        }

        private void Update()
        {
            if (tier != null || Time.unscaledTime < nextSearchTime)
                return;

            nextSearchTime = Time.unscaledTime + 1f;
            foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is ICharacterTier found)
                {
                    tier = found;
                    tier.TierChanged += OnTierChanged;
                    OnTierChanged(tier.Current);
                    return;
                }
            }
        }

        private void OnTierChanged(TierDefinition newTier) => Apply(newTier != null ? newTier.index : -1);

        private void Apply(int tierIndex)
        {
            if (target != null)
                target.SetActive(WristDisplayLogic.IsVisibleAtTier(tierIndex, minTier, maxTier));
        }
    }
}
