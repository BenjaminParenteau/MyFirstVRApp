using NUnit.Framework;
using HighStakes.TableGames.Tests;

namespace HighStakes.TableGames.Blackjack.Tests
{
    /// <summary>
    /// Blackjack held to its declared return by a player who mimics the dealer: hit
    /// below 17, never split, and optionally double every hard 10 or 11. The return
    /// depends on the strategy, so BlackjackOdds computes it exactly from the rules
    /// rather than from the game.
    /// </summary>
    public class BlackjackReturnTests
    {
        static readonly BlackjackRuleSet Rules = BlackjackRuleSet.Standard;

        const long Wager = 100;
        const int Seed = 20240501;

        /// <summary>
        /// A round's return has a standard deviation near 0.98, or 1.06 per chip
        /// staked when doubling: 50000 rounds put the 0.02 tolerance over 4 standard
        /// deviations out.
        /// </summary>
        const int Rounds = 50000;

        static BlackjackRound Play(BlackjackGame game, bool doubleHard10And11)
        {
            Assert.IsTrue(game.TryStart(Wager, out var round), "a funded wallet must not be refused");
            while (round.IsPlayerTurn)
            {
                var hand = round.Active;
                if (doubleHard10And11 && round.CanDouble && !hand.IsSoft && (hand.Total == 10 || hand.Total == 11)) round.Double();
                else if (hand.Total < 17) round.Hit();
                else round.Stand();
            }
            return round;
        }

        /// <summary>
        /// The exact calculation agrees with an independent reference: a Python model
        /// of the same rules, itself checked against 8 million simulated rounds.
        /// </summary>
        [TestCase(false, 0.943254)]
        [TestCase(true, 0.959695)]
        public void TheExactReturnAgreesWithTheReferenceModel(bool doubleHard10And11, double reference)
        {
            Assert.AreEqual(reference, BlackjackOdds.Return(Rules, doubleHard10And11), 5e-6);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TheMeasuredReturnMatchesTheExactOne(bool doubleHard10And11)
        {
            // Funded for every round to double, so the strategy is never refused part way through.
            var wallet = new TestWallet(Wager * 2 * (Rounds + 1));
            var game = new BlackjackGame(new SeededRng(Seed), wallet, Rules);

            long staked = 0, returned = 0;
            for (var i = 0; i < Rounds; i++)
            {
                var round = Play(game, doubleHard10And11);
                staked += round.Staked;
                returned += round.Payout;
            }

            Assert.That(returned / (double)staked, Is.EqualTo(BlackjackOdds.Return(Rules, doubleHard10And11)).Within(0.02),
                $"return over {Rounds} rounds at seed {Seed}");
        }

        [Test]
        public void TheWagerIsTakenBeforeTheOutcomeIsKnown()
        {
            var wallet = new TestWallet(Wager * 2);
            var game = new BlackjackGame(new ThrowingRng(), wallet, Rules);

            Assert.Throws<ThrowingRng.Rolled>(() => game.TryStart(Wager, out _));
            Assert.AreEqual(Wager, wallet.Balance, "the wager must leave the wallet before a card is dealt");
        }
    }
}
