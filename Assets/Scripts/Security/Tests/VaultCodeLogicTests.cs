using System.Collections.Generic;
using NUnit.Framework;

namespace HighStakes.Security.Tests
{
    public class VaultCodeLogicTests
    {
        [Test]
        public void RightCode_Opens_AndClearsFailures()
        {
            int failures = 2;
            Assert.AreEqual(VaultAttempt.Open, VaultCodeLogic.Submit("5813", "5813", ref failures, 3));
            Assert.AreEqual(0, failures);
        }

        [Test]
        public void ShortEntry_IsNotAFailure()
        {
            int failures = 0;
            Assert.AreEqual(VaultAttempt.Incomplete, VaultCodeLogic.Submit("58", "5813", ref failures, 3));
            Assert.AreEqual(0, failures);
        }

        [Test]
        public void WrongCode_CountsAFailure()
        {
            int failures = 0;
            Assert.AreEqual(VaultAttempt.Wrong, VaultCodeLogic.Submit("1358", "5813", ref failures, 3));
            Assert.AreEqual(1, failures);
            Assert.AreEqual(2, VaultCodeLogic.AttemptsLeft(failures, 3));
        }

        [Test]
        public void LastAllowedFailure_LocksDown_AndStartsOver()
        {
            int failures = 2;
            Assert.AreEqual(VaultAttempt.Lockdown, VaultCodeLogic.Submit("0000", "5813", ref failures, 3));
            Assert.AreEqual(0, failures);
        }

        // The clues the player can find: the Breaker Log (worn keys 1 3 5 8; the 8 is pressed right after the first
        // key) and the CAM 04 still (first key 5, last key 3). Together they must leave exactly one code, and neither
        // alone may be enough. Keep these in step with BackRoomPuzzlesBuilder / WirePanelBuilder if the code changes.
        [Test]
        public void Clues_TogetherGiveExactlyTheVaultCode_ButNeitherAlone()
        {
            var both = new List<string>();
            int breakerOnly = 0, stillOnly = 0;
            for (int n = 0; n < 10000; n++)
            {
                string c = n.ToString("0000");
                bool breaker = UsesExactly(c, "1358") && c[1] == '8';
                bool still = c[0] == '5' && c[3] == '3';
                if (breaker) breakerOnly++;
                if (still) stillOnly++;
                if (breaker && still) both.Add(c);
            }
            CollectionAssert.AreEqual(new[] { "5813" }, both);
            Assert.Greater(breakerOnly, 1);
            Assert.Greater(stillOnly, 1);
        }

        static bool UsesExactly(string code, string keys)
        {
            var sorted = code.ToCharArray();
            System.Array.Sort(sorted);
            return new string(sorted) == keys;
        }
    }
}
