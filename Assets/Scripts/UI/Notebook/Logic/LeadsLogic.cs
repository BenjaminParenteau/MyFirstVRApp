using System;
using System.Collections.Generic;

namespace HighStakes.UI
{
    /// <summary>
    /// Pure rules for the notebook's leads page: the Guide's leads in the order they were given, the newest being the
    /// one to follow now and the older ones ticked off. No Unity types, so it is covered by EditMode tests.
    /// </summary>
    public static class LeadsLogic
    {
        public const string NoLeads = "NO LEADS YET";

        /// <summary>Rows on the leads page. The oldest leads drop off the page first.</summary>
        public const int Rows = 5;

        /// <summary>Adds a lead. Returns false for a blank lead or one already written down (case-insensitive).</summary>
        public static bool Add(IList<string> leads, string lead)
        {
            if (string.IsNullOrWhiteSpace(lead))
                return false;
            lead = lead.Trim();
            foreach (var existing in leads)
            {
                if (string.Equals(existing, lead, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            leads.Add(lead);
            return true;
        }

        /// <summary>
        /// The leads shown on the page's <paramref name="rows"/> rows, as indexes into the list: the newest (the one to
        /// follow now) first, then older ones until the rows run out.
        /// </summary>
        public static int[] NewestFirst(int count, int rows)
        {
            int shown = Math.Max(0, Math.Min(count, rows));
            var order = new int[shown];
            for (int i = 0; i < shown; i++)
                order[i] = count - 1 - i;
            return order;
        }
    }
}
