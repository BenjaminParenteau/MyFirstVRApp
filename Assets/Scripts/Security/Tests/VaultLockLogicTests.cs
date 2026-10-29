using NUnit.Framework;

namespace HighStakes.Security.Tests
{
    public class VaultLockLogicTests
    {
        [Test] public void WrongKeyAndEmptyLockCannotMoveBolts()
        {
            var state = new VaultLockLogic();Assert.IsFalse(state.Insert("VIP"));
            state.TurnKey(90);state.Tick(2, true);state.TurnWheel(270);
            Assert.IsFalse(state.KeySeated);Assert.IsFalse(state.IsOpen);Assert.AreEqual(0, state.WheelAngle);
        }
        [Test] public void PartialTurnAndEarlyReleaseDoNotArm()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(70);state.Tick(2, true);Assert.IsFalse(state.Armed);
            state.TurnKey(20);state.Tick(.3f, true);state.Tick(.2f, false);
            Assert.IsFalse(state.Armed);Assert.Less(state.KeyAngle, 90);
        }
        [Test] public void KeyRequiresContinuousHoldAtStop()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(90);state.Tick(.4f, true);state.TurnKey(-10);state.TurnKey(10);state.Tick(.4f, true);
            Assert.IsFalse(state.Armed);state.Tick(.21f, true);Assert.IsTrue(state.Armed);
        }
        [Test] public void CapturedKeyThenThreeQuarterWheelTurnOpens()
        {
            var state = new VaultLockLogic();Assert.IsTrue(state.Insert(VaultLockLogic.KeyCode));
            Assert.IsFalse(state.Insert(VaultLockLogic.KeyCode));state.TurnWheel(270);Assert.AreEqual(0, state.WheelAngle);
            state.TurnKey(90);state.Tick(.61f, true);state.Tick(5, false);Assert.IsTrue(state.Armed);
            state.TurnWheel(269);Assert.IsFalse(state.IsOpen);state.TurnWheel(1);Assert.IsTrue(state.IsOpen);
            state.TurnWheel(-270);Assert.IsTrue(state.IsOpen);Assert.AreEqual(270, state.WheelAngle);
        }
        [Test] public void WheelCanReverseBeforeOpeningAndCannotGoPastStops()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(-90);Assert.AreEqual(0, state.KeyAngle);state.TurnKey(200);Assert.AreEqual(90, state.KeyAngle);
            state.Tick(.61f, true);state.TurnWheel(80);state.TurnWheel(-100);Assert.AreEqual(0, state.WheelAngle);
            state.TurnWheel(float.NaN);state.TurnKey(float.PositiveInfinity);Assert.IsFalse(state.IsOpen);
        }
    }
}
