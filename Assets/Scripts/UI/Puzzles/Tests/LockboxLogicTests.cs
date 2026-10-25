using NUnit.Framework;

namespace HighStakes.UI.Tests
{
    public class LockboxLogicTests
    {
        [Test]
        public void Press_AddsDigitsUpToTheCodeLength()
        {
            string entry = "";
            foreach (int digit in new[] { 4, 7, 2, 9 })
                entry = LockboxLogic.Press(entry, digit, 3);

            Assert.AreEqual("472", entry);
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void Press_IgnoresAnythingButADigit(int digit)
        {
            Assert.AreEqual("4", LockboxLogic.Press("4", digit, 3));
        }

        [Test]
        public void Press_StartsFromNothing()
        {
            Assert.AreEqual("0", LockboxLogic.Press(null, 0, 3));
        }

        [TestCase("472", true)]
        [TestCase("47", false)]
        [TestCase("274", false)]
        [TestCase("", false)]
        public void Opens_OnlyOnTheExactCode(string entry, bool expected)
        {
            Assert.AreEqual(expected, LockboxLogic.Opens(entry, "472"));
        }

        [Test]
        public void Opens_NeverWithoutACode()
        {
            Assert.IsFalse(LockboxLogic.Opens("", ""));
        }

        [TestCase("", "_ _ _")]
        [TestCase("47", "4 7 _")]
        [TestCase("472", "4 7 2")]
        public void Display_ShowsTypedDigitsAndBlanks(string entry, string expected)
        {
            Assert.AreEqual(expected, LockboxLogic.Display(entry, 3));
        }
    }
}
