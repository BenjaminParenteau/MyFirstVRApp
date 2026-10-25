using System;
using System.Collections.Generic;
using System.Text;

namespace HighStakes.UI
{
    /// <summary>One line item in the notebook: a keycard, passcode clue or intel note.</summary>
    public readonly struct NotebookEntry
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Body;

        public NotebookEntry(string id, string title, string body)
        {
            Id = id;
            Title = title;
            Body = body;
        }
    }

    /// <summary>
    /// Pure rules for the notebook: which pickups become entries, which one is selected, and how the page reads.
    /// No Unity types, so it lives in its own assembly and is covered by EditMode tests.
    /// </summary>
    public static class NotebookLogic
    {
        /// <summary>Entries listed on the page at once. The list scrolls so the selected one is always shown.</summary>
        public const int MaxVisible = 5;

        public const string EmptyPage = "NO NOTES YET";

        public const string EmptyScreen = "NO ITEM";

        /// <summary>Entries whose key contains this are keycards: one sits in the notebook's card slot.</summary>
        public const string CardKeyword = "keycard";

        /// <summary>The key that decides whether two pickups are the same item: the id, or the title when there is no id.</summary>
        public static string KeyFor(string id, string title)
        {
            return !string.IsNullOrWhiteSpace(id) ? id.Trim() : title?.Trim() ?? "";
        }

        /// <summary>Whether two item keys name the same item (case-insensitive).</summary>
        public static bool SameKey(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a pickup unless the notebook already has it (see <see cref="KeyFor"/>). Returns false for a duplicate
        /// or an item with neither id nor title.
        /// </summary>
        public static bool TryAdd(IList<NotebookEntry> entries, string id, string title, string body)
        {
            string key = KeyFor(id, title);
            if (key.Length == 0)
                return false;

            foreach (var existing in entries)
            {
                if (SameKey(existing.Id, key))
                    return false;
            }

            entries.Add(new NotebookEntry(key, string.IsNullOrWhiteSpace(title) ? key : title.Trim(), body?.Trim() ?? ""));
            return true;
        }

        /// <summary>The next entry to select, wrapping to the first. -1 when there are none.</summary>
        public static int NextIndex(int count, int current)
        {
            if (count <= 0)
                return -1;
            return current < 0 || current >= count - 1 ? 0 : current + 1;
        }

        /// <summary>
        /// The page text: entries in the order they were picked up, the selected one marked with "&gt;" (and its note
        /// underneath if <paramref name="showSelectedNote"/>), the held one marked "(IN HAND)". Shows at most
        /// <paramref name="maxVisible"/> rows, scrolled so the selection is on the page. No entries gives
        /// <see cref="EmptyPage"/>.
        /// </summary>
        public static string FormatPage(IReadOnlyList<NotebookEntry> entries, int selected, string heldKey,
                                        int maxVisible = MaxVisible, bool showSelectedNote = true)
        {
            if (entries.Count == 0 || maxVisible <= 0)
                return EmptyPage;

            int first = selected >= maxVisible ? selected - maxVisible + 1 : 0;
            int last = Math.Min(entries.Count, first + maxVisible);

            var page = new StringBuilder();
            for (int i = first; i < last; i++)
            {
                if (i > first)
                    page.Append('\n');
                var entry = entries[i];
                page.Append(i == selected ? "> " : "   ").Append(entry.Title.ToUpperInvariant());
                if (SameKey(entry.Id, heldKey))
                    page.Append("  (IN HAND)");
                if (showSelectedNote && i == selected && entry.Body.Length > 0)
                    page.Append("\n   ").Append(entry.Body);
            }
            return page.ToString();
        }

        /// <summary>
        /// The blue screen on the left page: the selected item's name on its own line, its note on the next.
        /// Nothing selected (or no entries) gives <see cref="EmptyScreen"/>.
        /// </summary>
        public static string FormatScreen(IReadOnlyList<NotebookEntry> entries, int selected)
        {
            if (selected < 0 || selected >= entries.Count)
                return EmptyScreen;
            var entry = entries[selected];
            string title = entry.Title.ToUpperInvariant();
            return entry.Body.Length > 0 ? title + "\n" + entry.Body : title;
        }

        /// <summary>Whether a keycard sits in the card slot: the notebook has one that isn't the one in the hand.</summary>
        public static bool ShowsCard(IReadOnlyList<NotebookEntry> entries, string heldKey, string keyword = CardKeyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;
            foreach (var entry in entries)
            {
                if (entry.Id.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 && !SameKey(entry.Id, heldKey))
                    return true;
            }
            return false;
        }
    }
}
