using System.Collections.Generic;
using NUnit.Framework;

namespace HighStakes.UI.Tests
{
    public class NotebookLogicTests
    {
        static List<NotebookEntry> Entries(params string[] titles)
        {
            var entries = new List<NotebookEntry>();
            foreach (var title in titles)
                NotebookLogic.TryAdd(entries, title.ToLowerInvariant(), title, title + " note");
            return entries;
        }

        [Test]
        public void TryAdd_AddsANewItem()
        {
            var entries = new List<NotebookEntry>();

            Assert.IsTrue(NotebookLogic.TryAdd(entries, "vip_keycard", "VIP Keycard", "Opens the VIP door."));

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("VIP Keycard", entries[0].Title);
            Assert.AreEqual("Opens the VIP door.", entries[0].Body);
        }

        [Test]
        public void TryAdd_IgnoresTheSameIdTwiceEvenWithDifferentCase()
        {
            var entries = new List<NotebookEntry>();
            NotebookLogic.TryAdd(entries, "vip_keycard", "VIP Keycard", "");

            Assert.IsFalse(NotebookLogic.TryAdd(entries, "VIP_KEYCARD", "VIP Keycard again", ""));
            Assert.AreEqual(1, entries.Count);
        }

        [Test]
        public void TryAdd_UsesTheTitleWhenThereIsNoId()
        {
            var entries = new List<NotebookEntry>();

            Assert.IsTrue(NotebookLogic.TryAdd(entries, null, "Receipt", "Scribbled digits."));
            Assert.IsFalse(NotebookLogic.TryAdd(entries, " ", "receipt", ""));
        }

        [TestCase(null, null)]
        [TestCase("", "  ")]
        public void TryAdd_RejectsAnItemWithNoIdAndNoTitle(string id, string title)
        {
            var entries = new List<NotebookEntry>();

            Assert.IsFalse(NotebookLogic.TryAdd(entries, id, title, "body"));
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void TryAdd_FallsBackToTheIdWhenTheTitleIsEmpty()
        {
            var entries = new List<NotebookEntry>();
            NotebookLogic.TryAdd(entries, "clue_1", "", null);

            Assert.AreEqual("clue_1", entries[0].Title);
            Assert.AreEqual("", entries[0].Body);
        }

        [Test]
        public void KeyFor_PrefersTheIdOverTheTitle()
        {
            Assert.AreEqual("vip_keycard", NotebookLogic.KeyFor(" vip_keycard ", "VIP Keycard"));
            Assert.AreEqual("Receipt", NotebookLogic.KeyFor(null, " Receipt"));
            Assert.AreEqual("", NotebookLogic.KeyFor(null, null));
        }

        [TestCase(0, 0, -1)]
        [TestCase(3, -1, 0)]
        [TestCase(3, 0, 1)]
        [TestCase(3, 2, 0)]
        [TestCase(3, 7, 0)]
        public void NextIndex_StepsAndWraps(int count, int current, int expected)
        {
            Assert.AreEqual(expected, NotebookLogic.NextIndex(count, current));
        }

        [Test]
        public void FormatPage_EmptyShowsTheEmptyMessage()
        {
            Assert.AreEqual(NotebookLogic.EmptyPage, NotebookLogic.FormatPage(new List<NotebookEntry>(), -1, null));
        }

        [Test]
        public void FormatPage_MarksTheSelectionAndShowsOnlyItsNote()
        {
            string page = NotebookLogic.FormatPage(Entries("Keycard", "Napkin"), 1, null);

            Assert.AreEqual("   KEYCARD\n> NAPKIN\n   Napkin note", page);
        }

        [Test]
        public void FormatPage_MarksTheHeldItem()
        {
            string page = NotebookLogic.FormatPage(Entries("Keycard", "Napkin"), 1, "KEYCARD");

            StringAssert.StartsWith("   KEYCARD  (IN HAND)\n", page);
        }

        [Test]
        public void FormatPage_ScrollsSoTheSelectionIsOnThePage()
        {
            var entries = Entries("A", "B", "C", "D");

            string page = NotebookLogic.FormatPage(entries, 3, null, 2);

            Assert.AreEqual("   C\n> D\n   D note", page);
        }

        [Test]
        public void FormatPage_StartsAtTheTopWithNothingSelected()
        {
            string page = NotebookLogic.FormatPage(Entries("A", "B", "C"), -1, null, 2);

            Assert.AreEqual("   A\n   B", page);
        }

        [Test]
        public void FormatPage_CanLeaveOutTheSelectedNote()
        {
            string page = NotebookLogic.FormatPage(Entries("Keycard", "Napkin"), 1, null, showSelectedNote: false);

            Assert.AreEqual("   KEYCARD\n> NAPKIN", page);
        }

        [Test]
        public void FormatScreen_ShowsTheSelectedNameAndNote()
        {
            Assert.AreEqual("NAPKIN\nNapkin note", NotebookLogic.FormatScreen(Entries("Keycard", "Napkin"), 1));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void FormatScreen_NothingSelectedShowsNoItem(int selected)
        {
            Assert.AreEqual(NotebookLogic.EmptyScreen, NotebookLogic.FormatScreen(Entries("Keycard"), selected));
        }

        [Test]
        public void ShowsCard_WhenAKeycardIsInTheNotebook()
        {
            var entries = new List<NotebookEntry>();
            NotebookLogic.TryAdd(entries, "vip_keycard", "VIP Keycard", "");

            Assert.IsTrue(NotebookLogic.ShowsCard(entries, null));
            Assert.IsFalse(NotebookLogic.ShowsCard(entries, "VIP_KEYCARD"), "the only card is in the hand");
            Assert.IsFalse(NotebookLogic.ShowsCard(Entries("Napkin"), null), "no keycard at all");
        }
    }
}
