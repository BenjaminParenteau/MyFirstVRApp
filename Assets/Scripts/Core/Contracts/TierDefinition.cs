using UnityEngine;

namespace HighStakes.Core
{
    /// <summary>
    /// One status tier (e.g. Ordinary Night, Known High Roller, Valued Associate). Slot B creates the tier assets
    /// in Assets/Content/Progression/; the type lives in Core because every slot reads it. Append fields only.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Tier", menuName = "High Stakes/Tier Definition")]
    public class TierDefinition : ScriptableObject
    {
        [Tooltip("Name shown on the wrist display.")]
        public string displayName;

        [Tooltip("0 = starting tier. Higher index = higher status; door readers compare this.")]
        public int index;

        [Tooltip("Chip balance needed to reach this tier.")]
        public int chipThreshold;
    }
}
