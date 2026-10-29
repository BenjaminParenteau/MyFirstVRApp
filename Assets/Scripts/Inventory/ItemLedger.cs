using System;
using System.Collections.Generic;
using HighStakes.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Inventory
{
    /// <summary>
    /// Every item the player has collected this session, by <see cref="ItemData.id"/>. Listens to whatever implements
    /// <see cref="IInventoryEvents"/> (the notebook in MVP_Main), so an area scene that loads and unloads can still ask
    /// what the player carries: the hub's vault clearance panel, or a puzzle deciding whether to put its note back.
    /// Lives outside any scene, like <see cref="KeycardAccess"/>.
    /// </summary>
    // ponytail: session only. Save it with the wallet when saves exist.
    public static class ItemLedger
    {
        static readonly HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly List<IInventoryEvents> sources = new List<IInventoryEvents>();

        /// <summary>Raised once per item id, the first time it is collected.</summary>
        public static event Action<ItemData> Added;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ids.Clear();
            sources.Clear();
            Added = null;
            SceneManager.sceneLoaded -= Watch;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded += Watch;
            for (int i = 0; i < SceneManager.sceneCount; i++) Watch(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        public static bool Has(string id) => !string.IsNullOrEmpty(id) && ids.Contains(id);

        public static bool Has(ItemData item) => item != null && Has(item.id);

        public static void Record(ItemData item)
        {
            if (item != null && !string.IsNullOrEmpty(item.id) && ids.Add(item.id)) Added?.Invoke(item);
        }

        static void Watch(Scene scene, LoadSceneMode _)
        {
            if (!scene.isLoaded) return;
            sources.RemoveAll(s => (s as UnityEngine.Object) == null); // destroyed with an unloaded scene
            foreach (var root in scene.GetRootGameObjects())
                foreach (var source in root.GetComponentsInChildren<IInventoryEvents>(true))
                {
                    if (sources.Contains(source)) continue;
                    sources.Add(source);
                    source.ItemAdded += Record;
                }
        }
    }
}
