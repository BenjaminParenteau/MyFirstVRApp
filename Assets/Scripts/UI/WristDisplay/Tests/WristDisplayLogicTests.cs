using NUnit.Framework;

namespace HighStakes.UI.Tests
{
    public class WristDisplayLogicTests
    {
        [TestCase(0, "$0")]
        [TestCase(100, "$100")]
        [TestCase(25000, "$25,000")]
        [TestCase(1234567, "$1,234,567")]
        [TestCase(-1500, "-$1,500")]
        [TestCase(int.MinValue, "-$2,147,483,648")]
        public void FormatChips_UsesDollarSignAndCommas(int chips, string expected)
        {
            Assert.AreEqual(expected, WristDisplayLogic.FormatChips(chips));
        }

        [TestCase("Known High Roller", "KNOWN HIGH ROLLER")]
        [TestCase("  Ordinary Night ", "ORDINARY NIGHT")]
        public void FormatTier_UppercasesAndTrims(string name, string expected)
        {
            Assert.AreEqual(expected, WristDisplayLogic.FormatTier(name));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FormatTier_MissingName_ShowsPlaceholder(string name)
        {
            Assert.AreEqual(WristDisplayLogic.NoValue, WristDisplayLogic.FormatTier(name));
        }

        private const float Show = 0.8f, Hide = 0.6f, MaxDistance = 0.6f;

        [Test]
        public void ShouldShowFace_LookingStraightAtIt_Shows()
        {
            Assert.IsTrue(WristDisplayLogic.ShouldShowFace(false, 1f, 0.3f, Show, Hide, MaxDistance));
        }

        [Test]
        public void ShouldShowFace_FacingAway_Hides()
        {
            Assert.IsFalse(WristDisplayLogic.ShouldShowFace(true, -0.5f, 0.3f, Show, Hide, MaxDistance));
        }

        [Test]
        public void ShouldShowFace_TooFarFromEyes_Hides()
        {
            Assert.IsFalse(WristDisplayLogic.ShouldShowFace(true, 1f, 0.9f, Show, Hide, MaxDistance));
        }

        [Test]
        public void ShouldShowFace_BetweenThresholds_KeepsCurrentState()
        {
            // 0.7 is between hide (0.6) and show (0.8): no flicker either way
            Assert.IsFalse(WristDisplayLogic.ShouldShowFace(false, 0.7f, 0.3f, Show, Hide, MaxDistance));
            Assert.IsTrue(WristDisplayLogic.ShouldShowFace(true, 0.7f, 0.3f, Show, Hide, MaxDistance));
        }
    }
}
