namespace HighStakes.Security
{
    /// <summary>
    /// Pure rules for the security wire panel: four sockets, each needing the wire of its own colour. No Unity types,
    /// so it lives in its own assembly and is covered by EditMode tests.
    /// </summary>
    public static class WirePanelLogic
    {
        /// <summary>
        /// Whether every socket holds the wire colour it needs. <paramref name="plugged"/>[i] is the colour index
        /// currently seated in socket i, or -1 if empty; an empty socket never matches.
        /// </summary>
        public static bool IsSolved(int[] plugged, int[] required)
        {
            if (plugged == null || required == null || plugged.Length != required.Length || plugged.Length == 0)
                return false;
            for (int i = 0; i < required.Length; i++)
                if (plugged[i] != required[i])
                    return false;
            return true;
        }
    }
}
