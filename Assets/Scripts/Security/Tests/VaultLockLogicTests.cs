using NUnit.Framework;
namespace HighStakes.Security.Tests
{
    public class VaultLockLogicTests
    {
        [Test] public void WrongKeyAndEmptyLockCannotMoveBolts()
        {
            var state = new VaultLockLogic();Assert.IsFalse(state.Insert("VIP"));
            state.TurnKey(45);state.TurnWheel(90);Assert.IsFalse(state.KeySeated);Assert.IsFalse(state.IsOpen);
        }
        [Test] public void ShortKeyTurnArmsWithoutTimedHold()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(45);Assert.IsTrue(state.Armed);Assert.IsFalse(state.IsOpen);
        }
        [Test] public void PartialTurnKeepsProgress()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(20);Assert.IsFalse(state.Armed);Assert.AreEqual(20, state.KeyAngle);
            state.TurnKey(25);Assert.IsTrue(state.Armed);
        }
        [Test] public void QuarterWheelTurnOpensAndStaysOpen()
        {
            var state = new VaultLockLogic();Assert.IsTrue(state.Insert(VaultLockLogic.KeyCode));
            Assert.IsFalse(state.Insert(VaultLockLogic.KeyCode));state.TurnWheel(90);Assert.AreEqual(0, state.WheelAngle);
            state.TurnKey(45);state.TurnWheel(89);Assert.IsFalse(state.IsOpen);
            state.TurnWheel(1);Assert.IsTrue(state.IsOpen);state.TurnWheel(-90);Assert.IsTrue(state.IsOpen);
        }
        [Test] public void TravelStopsAndInvalidInputDoNotBypassLock()
        {
            var state = new VaultLockLogic();state.Insert(VaultLockLogic.KeyCode);
            state.TurnKey(-90);Assert.AreEqual(0, state.KeyAngle);state.TurnKey(float.NaN);Assert.IsFalse(state.Armed);
            state.TurnKey(200);Assert.AreEqual(45, state.KeyAngle);state.TurnWheel(float.PositiveInfinity);Assert.IsFalse(state.IsOpen);
            state.TurnWheel(20);state.TurnWheel(-100);Assert.AreEqual(0, state.WheelAngle);
        }
    }
}
