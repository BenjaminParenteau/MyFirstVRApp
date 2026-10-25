using System;
using HighStakes.Core;
using UnityEngine;

namespace HighStakes.UI.Mocks
{
    /// <summary>
    /// Stand-in IInventoryEvents for testing the notebook before Slot B's real inventory exists. NOT the source of
    /// truth. Keep it under _SoloTest so it switches off inside MVP_Main. Inspector buttons "pick up" sample items.
    /// </summary>
    public class MockInventoryEvents : MonoBehaviour, IInventoryEvents
    {
        [SerializeField] private ItemData[] sampleItems;

        public event Action<ItemData> ItemAdded;

        public ItemData[] SampleItems => sampleItems;

        public void PickUp(ItemData item)
        {
            if (item != null)
                ItemAdded?.Invoke(item);
        }
    }
}
