using NUnit.Framework;

namespace HighStakes.Security.Tests
{
    public class WirePanelLogicTests
    {
        [Test]
        public void AllMatched_Solves()
        {
            Assert.IsTrue(WirePanelLogic.IsSolved(new[] { 0, 1, 2, 3 }, new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void AnyEmptySocket_DoesNotSolve()
        {
            Assert.IsFalse(WirePanelLogic.IsSolved(new[] { 0, 1, -1, 3 }, new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void WrongColour_DoesNotSolve()
        {
            Assert.IsFalse(WirePanelLogic.IsSolved(new[] { 1, 0, 2, 3 }, new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void MismatchedLengths_DoesNotSolve()
        {
            Assert.IsFalse(WirePanelLogic.IsSolved(new[] { 0, 1, 2 }, new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void Nulls_DoNotSolve()
        {
            Assert.IsFalse(WirePanelLogic.IsSolved(null, new[] { 0 }));
            Assert.IsFalse(WirePanelLogic.IsSolved(new[] { 0 }, null));
        }

        [Test]
        public void Empty_DoesNotSolve()
        {
            Assert.IsFalse(WirePanelLogic.IsSolved(new int[0], new int[0]));
        }
    }
}
