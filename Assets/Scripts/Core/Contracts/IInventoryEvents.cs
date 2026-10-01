using System;

namespace HighStakes.Core
{
    /// <summary>
    /// Inventory notifications. Implemented by Slot B; C's notebook subscribes.
    /// </summary>
    public interface IInventoryEvents
    {
        /// <summary>Raised when the player picks up an item (keycard, clue note, ...).</summary>
        event Action<ItemData> ItemAdded;
    }
}
