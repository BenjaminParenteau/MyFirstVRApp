using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace HighStakes.Economy.Tests
{
    public class ChipLedgerTests
    {
        [Test]
        public void AddThenSpend_UpdatesBalanceAndRaisesEachChange()
        {
            var ledger = new ChipLedger(100);
            var seen = new List<int>();
            ledger.BalanceChanged += seen.Add;

            ledger.Add(50);
            Assert.IsTrue(ledger.TrySpend(120));

            Assert.AreEqual(30, ledger.Balance);
            CollectionAssert.AreEqual(new[] { 150, 30 }, seen);
        }

        [Test]
        public void SpendMoreThanBalance_ReturnsFalseAndChangesNothing()
        {
            var ledger = new ChipLedger(100);
            bool raised = false;
            ledger.BalanceChanged += _ => raised = true;

            Assert.IsFalse(ledger.TrySpend(101));
            Assert.AreEqual(100, ledger.Balance);
            Assert.IsFalse(raised);
        }

        [Test]
        public void SpendExactBalance_LeavesZero()
        {
            var ledger = new ChipLedger(100);
            Assert.IsTrue(ledger.TrySpend(100));
            Assert.AreEqual(0, ledger.Balance);
        }

        [Test]
        public void ZeroAmounts_SucceedWithoutRaising()
        {
            var ledger = new ChipLedger(10);
            bool raised = false;
            ledger.BalanceChanged += _ => raised = true;

            ledger.Add(0);
            Assert.IsTrue(ledger.TrySpend(0));
            Assert.IsFalse(raised);
        }

        [Test]
        public void NegativeAmounts_Throw()
        {
            var ledger = new ChipLedger(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Add(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.TrySpend(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChipLedger(-1));
            Assert.AreEqual(10, ledger.Balance);
        }

        [Test]
        public void AddPastIntMax_Saturates()
        {
            var ledger = new ChipLedger(int.MaxValue - 5);
            ledger.Add(100);
            Assert.AreEqual(int.MaxValue, ledger.Balance);
        }
    }
}
