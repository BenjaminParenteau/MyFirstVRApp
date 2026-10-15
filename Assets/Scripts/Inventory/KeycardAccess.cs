using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighStakes.Inventory
{
    /// <summary>
    /// Which doors the player's keycards have opened this session. Lives outside any scene, so an area that unloads
    /// and loads again (the casino after a trip to the VIP lounge) still knows its door was opened.
    /// </summary>
    // ponytail: session only. Save it with the wallet when saves exist.
    public static class KeycardAccess
    {
        static readonly HashSet<string> granted = new HashSet<string>();

        /// <summary>Raised once per access id, the first time a card opens it.</summary>
        public static event Action<string> Granted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            granted.Clear();
            Granted = null;
        }

        public static bool Has(string accessId) => granted.Contains(accessId);

        public static void Grant(string accessId)
        {
            if (granted.Add(accessId)) Granted?.Invoke(accessId);
        }
    }
}
