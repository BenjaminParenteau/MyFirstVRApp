using UnityEngine;

namespace HighStakes.Core
{
    /// <summary>
    /// A collectable item (keycard, passcode fragment, intel note). Slot B creates the item assets in
    /// Assets/Content/Progression/; the type lives in Core because the notebook (C) reads it. Append fields only.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Item", menuName = "High Stakes/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Stable id used in code and saves. Never rename once in use.")]
        public string id;

        public string displayName;

        [TextArea] public string description;

        public Sprite icon;
    }
}
