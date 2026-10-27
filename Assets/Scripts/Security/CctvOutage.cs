using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>
    /// CCTV feeds the player has knocked out this session, by the area scene they watch (the breaker panel cuts
    /// MVP_Vault's camera). <see cref="SecurityMonitorWall"/> blacks those feeds out; later, the suspicion meter can
    /// check <see cref="IsDown"/> before reacting to the player in a watched area.
    /// </summary>
    public static class CctvOutage
    {
        static readonly HashSet<string> down = new HashSet<string>();

        public static event Action<string> WentDown;

        public static bool IsDown(string sceneName) => !string.IsNullOrEmpty(sceneName) && down.Contains(sceneName);

        public static void Cut(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName) && down.Add(sceneName)) WentDown?.Invoke(sceneName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            down.Clear();
            WentDown = null;
        }
    }
}
