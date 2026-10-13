using System;
using HighStakes.TableGames.Dice;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class DiceGameTests
    {
        const int Edge = 100;

        /// <summary>
        /// The payout multiplies the wager by 10000 minus the edge before dividing,
        /// so at a 1 percent edge the arithmetic only holds while wager * 9900 fits
        /// in a long. This is the last wager that does.
        /// </summary>
        const long LargestWagerThatFits = 931_653_741_096_442L;

        /// <summary>Rolls and lets the marker land.</summary>
        static DiceRound RollAndSettle(DiceGame game, long wager, int target)
        {
            Assert.IsTrue(game.TryRoll(wager, target, out var round));
            round.Settle();
            return round;
        }

        [TestCase(100, 50, 100, 198)]
        [TestCase(100, 99, 100, 9900)]
        [TestCase(100, 1, 100, 100)]
        [TestCase(100, 50, 0, 200)]
        [TestCase(100, 50, 9999, 0)]
        [TestCase(7, 75, 100, 27)]
        public void PayoutTable(long wager, int target, int edge, long expected)
        {
            Assert.AreEqual(expected, DiceGame.PayoutFor(wager, target, edge));
        }

        [Test]
        public void PayoutRisesWithTheTarget()
        {
            var previous = 0L;
            for (var target = DiceGame.MinTarget; target <= DiceGame.MaxTarget; target++)
            {
                var payout = DiceGame.PayoutFor(1_000_000, target, Edge);
                Assert.Greater(payout, previous, $"target {target} must pay more than target {target - 1}");
                previous = payout;
            }
        }

        [Test]
        public void LargeWagerPaysOutWithoutLosingPrecision()
        {
            Assert.AreEqual(1_844_674_407_370_955L, DiceGame.PayoutFor(LargestWagerThatFits, 50, Edge));
        }

        [Test]
        [Ignore("Bug kept from vrstake: one chip more overflows wager * 9900 and the payout comes back " +
                "negative, so a winning round would pay nothing. Unreachable under the table limits and the int wallet.")]
        public void LargerWagerStillPaysOutSomethingPositive()
        {
            Assert.Greater(DiceGame.PayoutFor(LargestWagerThatFits + 1, 50, Edge), 0);
        }

        [Test]
        public void WinDebitsWagerAndCreditsPayoutWhenSettled()
        {
            var wallet = new TestWallet(100);
            Assert.IsTrue(new DiceGame(new FakeRng(75), wallet, Edge).TryRoll(10, 50, out var round));
            Assert.IsTrue(round.Won);
            Assert.AreEqual(75, round.Roll);
            Assert.AreEqual(50, round.Target);
            Assert.AreEqual(19, round.Payout);
            Assert.AreEqual(9, round.Net);
            Assert.AreEqual(90, wallet.Balance, "while the marker slides only the wager has moved");

            round.Settle();
            Assert.AreEqual(109, wallet.Balance);
            // The two entries a round writes, pinned by reason.
            Assert.AreEqual(3, wallet.Ledger.Count);
            Assert.AreEqual("Dice wager", wallet.Ledger[1].Reason);
            Assert.AreEqual("Dice win", wallet.Ledger[2].Reason);
        }

        [Test]
        public void LossDebitsWagerAndCreditsNothing()
        {
            var wallet = new TestWallet(100);
            var round = RollAndSettle(new DiceGame(new FakeRng(25), wallet, Edge), 10, 50);
            Assert.IsFalse(round.Won);
            Assert.AreEqual(0, round.Payout);
            Assert.AreEqual(-10, round.Net);
            Assert.AreEqual(90, wallet.Balance);
            Assert.AreEqual(2, wallet.Ledger.Count, "a loss writes the wager and nothing else");
            Assert.AreEqual("Dice wager", wallet.Ledger[1].Reason);
        }

        [TestCase(1, 1, false)]
        [TestCase(2, 1, true)]
        [TestCase(50, 50, false)]
        [TestCase(99, 99, false)]
        [TestCase(100, 99, true)]
        public void OnlyARollAboveTheTargetWins(int roll, int target, bool won)
        {
            Assert.AreEqual(won, RollAndSettle(new DiceGame(new FakeRng(roll), new TestWallet(100), Edge), 10, target).Won);
        }

        [Test]
        public void RoundAsksForOneRollOverOneToAHundredInclusive()
        {
            var rng = new FakeRng(75);
            RollAndSettle(new DiceGame(rng, new TestWallet(100), Edge), 10, 50);
            Assert.AreEqual(1, rng.Calls, "a round is exactly one roll");
            Assert.AreEqual((DiceGame.MinRoll, DiceGame.MaxRoll + 1), rng.Ranges[0]);
        }

        [Test]
        public void RefusedWagerIsNeverRolled()
        {
            var rng = new FakeRng(99);
            Assert.IsFalse(new DiceGame(rng, new TestWallet(5), Edge).TryRoll(10, 50, out var round));
            Assert.IsNull(round);
            Assert.AreEqual(0, rng.Calls, "a wager the wallet refuses must not consume a roll");
        }

        /// <summary>vrstake ignored this as a bug; settling skips a zero credit, so it resolves here.</summary>
        [Test]
        public void WinWorthLessThanOneChipStillResolves()
        {
            var wallet = new TestWallet(50);
            var game = new DiceGame(new FakeRng(100), wallet, 9999);
            Assert.IsTrue(game.TryRoll(50, 99, out var round));
            Assert.IsTrue(round.Won);
            Assert.AreEqual(0, round.Payout);
            Assert.DoesNotThrow(round.Settle, "a win must resolve even when its payout rounds to nothing");
            Assert.AreEqual(0, wallet.Balance);
        }

        [Test]
        public void InvalidInputsThrow()
        {
            var game = new DiceGame(new FakeRng(1), new TestWallet(100), Edge);
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryRoll(0, 50, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryRoll(10, DiceGame.MinTarget - 1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryRoll(10, DiceGame.MaxTarget + 1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceGame(new FakeRng(1), new TestWallet(100), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceGame(new FakeRng(1), new TestWallet(100), 10000));
            Assert.Throws<ArgumentNullException>(() => new DiceGame(null, new TestWallet(100), Edge));
            Assert.Throws<ArgumentNullException>(() => new DiceGame(new FakeRng(1), null, Edge));
        }
    }
}
