using System.Text;

namespace HighStakes.UI
{
    /// <summary>
    /// Pure rules for a digit lockbox: typing digits up to the code's length, and how the entry is shown. No Unity
    /// types, so it lives in its own assembly and is covered by EditMode tests.
    /// </summary>
    public static class LockboxLogic
    {
        /// <summary>Adds a digit (0-9) unless the entry is already full. Anything else leaves the entry as it was.</summary>
        public static string Press(string entry, int digit, int length)
        {
            entry ??= "";
            if (digit < 0 || digit > 9 || entry.Length >= length)
                return entry;
            return entry + (char)('0' + digit);
        }

        /// <summary>Whether a full entry opens the box. A short or wrong entry doesn't.</summary>
        public static bool Opens(string entry, string code)
        {
            return !string.IsNullOrEmpty(code) && entry == code;
        }

        /// <summary>The entry as the display shows it: digits typed so far, underscores for the rest. "47", 3 → "4 7 _".</summary>
        public static string Display(string entry, int length)
        {
            entry ??= "";
            var text = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (i > 0)
                    text.Append(' ');
                text.Append(i < entry.Length ? entry[i] : '_');
            }
            return text.ToString();
        }
    }
}
