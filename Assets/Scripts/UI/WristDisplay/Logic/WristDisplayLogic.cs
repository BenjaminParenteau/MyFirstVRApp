using System.Globalization;

namespace HighStakes.UI
{
    /// <summary>
    /// Pure rules for the wrist display: text formatting and when the face should show. No Unity types,
    /// so it lives in its own assembly and is covered by EditMode tests.
    /// </summary>
    public static class WristDisplayLogic
    {
        /// <summary>Shown when a value isn't available yet (no wallet or tier found).</summary>
        public const string NoValue = "--";

        /// <summary>25000 → "$25,000", -1500 → "-$1,500".</summary>
        public static string FormatChips(int chips)
        {
            string digits = ((long)chips < 0 ? -(long)chips : chips).ToString("N0", CultureInfo.InvariantCulture);
            return chips < 0 ? "-$" + digits : "$" + digits;
        }

        /// <summary>"Known High Roller" → "KNOWN HIGH ROLLER"; empty or null → <see cref="NoValue"/>.</summary>
        public static string FormatTier(string tierName)
        {
            return string.IsNullOrWhiteSpace(tierName) ? NoValue : tierName.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Whether an accessory shown for tiers <paramref name="minTier"/>..<paramref name="maxTier"/> is visible at
        /// <paramref name="tier"/>. A negative maxTier means "and every tier above". A negative tier means no tier is
        /// known yet, which counts as the starting tier (0).
        /// </summary>
        public static bool IsVisibleAtTier(int tier, int minTier, int maxTier)
        {
            if (tier < 0)
                tier = 0;
            return tier >= minTier && (maxTier < 0 || tier <= maxTier);
        }

        /// <summary>
        /// Whether the watch face should be lit. <paramref name="facingDot"/> is the dot product of the face normal and
        /// the direction to the player's eyes (1 = looking straight at it). Uses two thresholds so the face doesn't
        /// flicker at the edge: it turns on above <paramref name="showDot"/> and only turns off below
        /// <paramref name="hideDot"/> (hideDot must be lower than showDot).
        /// </summary>
        public static bool ShouldShowFace(bool currentlyShown, float facingDot, float distance,
                                          float showDot, float hideDot, float maxDistance)
        {
            if (distance > maxDistance)
                return false;

            return currentlyShown ? facingDot >= hideDot : facingDot >= showDot;
        }
    }
}
