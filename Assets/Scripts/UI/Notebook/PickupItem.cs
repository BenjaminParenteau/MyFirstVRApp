using HighStakes.Core;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// Marks a grabbable object as a collectable. When the player grabs it, <see cref="NotebookPickups"/> raises
    /// ItemAdded with this item, so it is written in the notebook. Prototype until Slot B's inventory takes over.
    /// </summary>
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private ItemData item;

        public ItemData Item => item;

        public UnityEngine.Events.UnityEvent Collected = new UnityEngine.Events.UnityEvent();
    }
}
