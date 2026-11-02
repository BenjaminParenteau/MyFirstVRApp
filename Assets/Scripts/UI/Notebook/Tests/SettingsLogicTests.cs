using NUnit.Framework;

namespace HighStakes.UI.Tests
{
    public class SettingsLogicTests
    {
        [Test]
        public void Default_IsSmoothMoveSnapTurnStanding()
        {
            var c = SettingsLogic.Default;

            Assert.AreEqual("MOVE: SMOOTH", SettingsLogic.Label(c, SettingsLogic.Move));
            Assert.AreEqual("TURN: SNAP", SettingsLogic.Label(c, SettingsLogic.Turn));
            Assert.AreEqual("VIGNETTE: OFF", SettingsLogic.Label(c, SettingsLogic.Vignette));
            Assert.AreEqual("SEATED: OFF", SettingsLogic.Label(c, SettingsLogic.Seated));
        }

        [Test]
        public void Toggle_SwitchesOnlyThatRow()
        {
            var before = SettingsLogic.Default;

            var after = SettingsLogic.Toggle(before, SettingsLogic.Move);

            Assert.AreEqual("MOVE: TELEPORT", SettingsLogic.Label(after, SettingsLogic.Move));
            Assert.IsTrue(SettingsLogic.IsOn(after, SettingsLogic.Move));
            for (int row = 1; row < SettingsLogic.Rows; row++)
                Assert.AreEqual(SettingsLogic.Label(before, row), SettingsLogic.Label(after, row));
            Assert.AreEqual(before, SettingsLogic.Toggle(after, SettingsLogic.Move));
        }

        [Test]
        public void Bits_RoundTripEveryCombination()
        {
            for (int bits = 0; bits < 16; bits++)
                Assert.AreEqual(bits, SettingsLogic.ToBits(SettingsLogic.FromBits(bits)));
        }

        [Test]
        public void EveryRow_HasALabelAndADescription()
        {
            for (int row = 0; row < SettingsLogic.Rows; row++)
            {
                Assert.IsNotEmpty(SettingsLogic.Label(SettingsLogic.Default, row));
                Assert.IsNotEmpty(SettingsLogic.Describe(SettingsLogic.Default, row));
            }
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SettingsLogic.Label(SettingsLogic.Default, SettingsLogic.Rows));
        }
    }
}
