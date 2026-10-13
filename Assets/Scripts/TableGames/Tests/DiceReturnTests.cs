using HighStakes.TableGames.Dice;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    /// <summary>
    /// Dice held to the house rules every table keeps: the wager leaves before the roll, a wager the wallet can't cover
    /// never rolls, a win pays once, and over a long session the player gets back (1 - edge) of what they stake.
    /// </summary>
    public class DiceReturnTests
    {
        const int Edge = 100;
        const long Wager = 100;
        const int Seed = 20261013;
        const int Rounds = 200_000;

        [Test]
        public void TheWagerIsTakenBeforeTheRoll()
        {
            var wallet = new TestWallet(Wager * 2);
            Assert.Throws<ThrowingRng.Rolled>(() => new DiceGame(new ThrowingRng(), wallet, Edge).TryRoll(Wager, 50, out _));
            Assert.AreEqual(Wager, wallet.Balance, "the wager must leave the wallet before the dice are rolled");
        }

        [Test]
        public void AWagerTheWalletCannotCoverIsRefusedWithoutARoll()
        {
            var wallet = new TestWallet(Wager - 1);
            // ThrowingRng: any roll at all fails the test.
            Assert.IsFalse(new DiceGame(new ThrowingRng(), wallet, Edge).TryRoll(Wager, 50, out var round));
            Assert.IsNull(round);
            Assert.AreEqual(Wager - 1, wallet.Balance);
            Assert.AreEqual(1, wallet.Ledger.Count, "only the opening balance");
        }

        [Test]
        public void AWinIsPaidOnceHoweverOftenItSettles()
        {
            var wallet = new TestWallet(Wager);
            Assert.IsTrue(new DiceGame(new FakeRng(100), wallet, Edge).TryRoll(Wager, 50, out var round));
            Assert.IsFalse(round.Settled);
            round.Settle();
            round.Settle();
            Assert.IsTrue(round.Settled);
            Assert.AreEqual(198, wallet.Balance, "100 staked, 198 back once");
            Assert.AreEqual(3, wallet.Ledger.Count);
        }

        /// <summary>
        /// A win comes (100 - target)/100 of the time and returns (10000 - edge)/(100 * (100 - target)) per chip, so
        /// whatever the target only the edge survives: 0.99. A 100 chip wager pays 198 and 1980 exactly at these
        /// targets, so rounding down doesn't skew it. Per round the return's standard deviation is 0.99 at target 50
        /// and 4.3 at target 95 (a rare, large payout); over 200k rounds each tolerance is over 4 standard errors out.
        /// </summary>
        [TestCase(50, 0.01)]
        [TestCase(95, 0.04)]
        public void ALongSessionReturnsOneMinusTheEdge(int target, double tolerance)
        {
            var wallet = new TestWallet(Wager * Rounds);
            var game = new DiceGame(new SeededRng(Seed), wallet, Edge);
            long staked = 0, returned = 0;
            for (var i = 0; i < Rounds; i++)
            {
                Assert.IsTrue(game.TryRoll(Wager, target, out var round), "a funded wallet must not be refused");
                round.Settle();
                staked += round.Wager;
                returned += round.Payout;
            }
            Assert.That(returned / (double)staked, Is.EqualTo(1 - Edge / 10000.0).Within(tolerance),
                $"return over {Rounds} rounds at target {target}, seed {Seed}");
        }
    }
}
