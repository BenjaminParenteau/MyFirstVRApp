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

        /// <summary>Chips won or lost this session: 40 -> "+$40", -15 -> "-$15", 0 -> "$0".</summary>
        public static string FormatNet(int net)
        {
            return net > 0 ? "+" + FormatChips(net) : FormatChips(net);
        }

        /// <summary>
        /// Moves the hologram's open amount (0 = folded into the watch, 1 = fully projected) one frame toward open or
        /// closed, at a constant rate: <paramref name="openSeconds"/> to open fully, <paramref name="closeSeconds"/> to close.
        /// </summary>
        public static float StepOpen(float amount, bool open, float deltaTime, float openSeconds, float closeSeconds)
        {
            float step = deltaTime / (open ? openSeconds : closeSeconds);
            float next = open ? amount + step : amount - step;
            return next < 0f ? 0f : next > 1f ? 1f : next;
        }

        /// <summary>Ease-out (fast start, gentle stop) for the projected size, so the panel snaps open and settles.</summary>
        public static float EaseOut(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
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
