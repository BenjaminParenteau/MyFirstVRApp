using System;
using System.Linq;
using NUnit.Framework;
using HighStakes.TableGames.Tests;

namespace HighStakes.TableGames.Blackjack.Tests
{
    public class BlackjackRoundTests
    {
        /// <summary>The draw that deals a card: its index in a suit-ordered deck. Spades unless a test cares.</summary>
        static int C(int rank, Suit suit = Suit.Spades) => new Card(rank, suit).Index;

        const int A = 1, J = 11, Q = 12, K = 13;

        /// <summary>Deals in table order: player, dealer up, player, dealer hole, then every later card.</summary>
        static BlackjackRound Deal(TestWallet wallet, long wager, BlackjackRuleSet rules, params int[] ranks)
        {
            var draws = ranks.Select(rank => C(rank)).ToArray();
            Assert.IsTrue(new BlackjackGame(new FakeRng(draws), wallet, rules).TryStart(wager, out var round));
            return round;
        }

        static BlackjackRound Deal(TestWallet wallet, params int[] ranks) =>
            Deal(wallet, 10, BlackjackRuleSet.Standard, ranks);

        [Test]
        public void CardsAreDealtPlayerDealerPlayerDealer()
        {
            var round = Deal(new TestWallet(100), 9, 6, 7, 10);
            CollectionAssert.AreEqual(new[] { 9, 7 }, round.Hands[0].Cards.Select(c => c.Rank));
            Assert.AreEqual(6, round.DealerUpCard.Rank);
            Assert.IsTrue(round.IsPlayerTurn);
        }

        [Test]
        public void ANaturalPaysThreeToTwoAtOnce()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, A, 9, K, 7);

            Assert.AreEqual(BlackjackState.Settled, round.State);
            Assert.AreEqual(HandResult.Blackjack, round.Hands[0].Result);
            Assert.AreEqual(25, round.Payout, "10 back plus 15 profit");
            Assert.AreEqual(115, wallet.Balance);
            Assert.AreEqual("Blackjack natural", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void TheNaturalRoundsItsProfitDown()
        {
            var wallet = new TestWallet(100);
            Deal(wallet, 7, BlackjackRuleSet.Standard, A, 9, K, 7);
            Assert.AreEqual(100 - 7 + 7 + 10, wallet.Balance, "7 at 3:2 is 10.5 profit, paid as 10");
        }

        [Test]
        public void ADealerBlackjackEndsTheRoundBeforeThePlayerActs()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, 10, A, 9, K);

            Assert.AreEqual(BlackjackState.Settled, round.State);
            Assert.AreEqual(HandResult.Lose, round.Hands[0].Result);
            Assert.AreEqual(90, wallet.Balance);
        }

        [Test]
        public void TwoNaturalsPush()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, A, K, Q, A);
            Assert.AreEqual(HandResult.Push, round.Hands[0].Result);
            Assert.AreEqual(100, wallet.Balance);
        }

        [Test]
        public void HittingPastTwentyOneBustsAndTheDealerDoesNotDraw()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, 10, 6, 6, 10, 9, 5);
            round.Hit();

            Assert.AreEqual(BlackjackState.Settled, round.State);
            Assert.AreEqual(HandResult.Bust, round.Hands[0].Result);
            Assert.AreEqual(2, round.Dealer.Cards.Count, "the dealer had no reason to draw");
            Assert.AreEqual(90, wallet.Balance);
        }

        [Test]
        public void TheDealerDrawsToSeventeenAndThePlayerWinsEvenMoney()
        {
            var wallet = new TestWallet(100);
            // Player 10+9 = 19. Dealer 6+10 = 16, draws a 2 for 18.
            var round = Deal(wallet, 10, 6, 9, 10, 2);
            round.Stand();

            Assert.AreEqual(18, round.Dealer.Total);
            Assert.AreEqual(HandResult.Win, round.Hands[0].Result);
            Assert.AreEqual(110, wallet.Balance);
            Assert.AreEqual("Blackjack win", wallet.Ledger.Last().Reason);
        }

        [Test]
        public void EqualTotalsPush()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, 10, 10, 8, 8);
            round.Stand();
            Assert.AreEqual(HandResult.Push, round.Hands[0].Result);
            Assert.AreEqual(100, wallet.Balance);
        }

        [Test]
        public void ADealerBustPaysEveryStandingHand()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, 10, 6, 2, 10, 10);
            round.Stand();
            Assert.IsTrue(round.Dealer.IsBust);
            Assert.AreEqual(HandResult.Win, round.Hands[0].Result, "12 beats a busted dealer");
        }

        [Test]
        public void TheDealerStandsOnSoftSeventeenUnlessTheTableSaysOtherwise()
        {
            var stands = Deal(new TestWallet(100), 10, A, 8, 6, 3);
            stands.Stand();
            Assert.AreEqual(2, stands.Dealer.Cards.Count, "soft 17 stands");

            var hits = Deal(new TestWallet(100), 10, new BlackjackRuleSet(150, true, true), 10, A, 8, 6, 3);
            hits.Stand();
            Assert.AreEqual(3, hits.Dealer.Cards.Count, "soft 17 draws under H17");
            Assert.AreEqual(20, hits.Dealer.Total);
        }

        [Test]
        public void ReachingTwentyOneStandsOnItsOwn()
        {
            var round = Deal(new TestWallet(100), 10, 10, 5, 7, 6);
            round.Hit();
            Assert.AreEqual(21, round.Hands[0].Total);
            Assert.AreEqual(BlackjackState.Settled, round.State);
        }

        [Test]
        public void DoublingTakesAnotherStakeDealsOneCardAndStands()
        {
            var wallet = new TestWallet(100);
            // Player 6+5 = 11 doubles into a 10. Dealer 10+7 = 17.
            var round = Deal(wallet, 6, 10, 5, 7, 10);
            Assert.IsTrue(round.CanDouble);
            round.Double();

            var hand = round.Hands[0];
            Assert.IsTrue(hand.Doubled);
            Assert.AreEqual(3, hand.Cards.Count);
            Assert.AreEqual(20, hand.Stake);
            Assert.AreEqual(HandResult.Win, hand.Result);
            Assert.AreEqual(40, round.Payout);
            Assert.AreEqual(100 - 20 + 40, wallet.Balance);
            Assert.AreEqual("Blackjack double", wallet.Ledger[2].Reason);
        }

        [Test]
        public void OnlyTheFirstTwoCardsCanBeDoubledAndOnlyIfTheWalletCanCoverIt()
        {
            var afterHit = Deal(new TestWallet(100), 2, 10, 3, 7, 4);
            afterHit.Hit();
            Assert.IsFalse(afterHit.CanDouble, "three cards");
            Assert.Throws<InvalidOperationException>(() => afterHit.Double());

            var broke = Deal(new TestWallet(15), 6, 10, 5, 7);
            Assert.IsFalse(broke.CanDouble, "5 left cannot cover another 10");
        }

        [Test]
        public void SplittingMakesTwoHandsEachWithItsOwnStake()
        {
            var wallet = new TestWallet(100);
            // 8,8 against a dealer 10,7. Left hand draws 3 then 10; right hand draws 10.
            var round = Deal(wallet, 8, 10, 8, 7, 3, 10, 10);
            Assert.IsTrue(round.CanSplit);
            round.Split();

            Assert.AreEqual(2, round.Hands.Count);
            Assert.AreEqual(80, wallet.Balance, "a second stake of 10");
            Assert.AreEqual("Blackjack split", wallet.Ledger.Last().Reason);
            Assert.AreEqual(0, round.ActiveIndex);
            Assert.AreEqual(2, round.Hands[0].Cards.Count, "the left hand is dealt its second card at once");
            Assert.AreEqual(1, round.Hands[1].Cards.Count, "the right hand waits for its turn");

            round.Hit();    // left: 8+3+10 = 21, stands on its own and the right hand is dealt
            Assert.AreEqual(1, round.ActiveIndex);
            Assert.AreEqual(18, round.Hands[1].Total);
            round.Stand();

            Assert.AreEqual(HandResult.Win, round.Hands[0].Result, "21 beats 17");
            Assert.AreEqual(HandResult.Win, round.Hands[1].Result, "18 beats 17");
            Assert.AreEqual(20, round.Staked);
            Assert.AreEqual(40, round.Payout);
            Assert.AreEqual(120, wallet.Balance);
        }

        [Test]
        public void TwentyOneAfterASplitIsNotABlackjack()
        {
            var wallet = new TestWallet(100);
            // K,K split; the left draws an ace: 21 on two cards, paid even money.
            var round = Deal(wallet, K, 10, K, 7, A, 7);
            round.Split();
            Assert.AreEqual(21, round.Hands[0].Total);
            Assert.IsFalse(round.Hands[0].IsBlackjack);
            round.Stand();

            Assert.AreEqual(HandResult.Win, round.Hands[0].Result);
            Assert.AreEqual(20, round.Hands[0].Payout, "even money, not 3:2");
        }

        [Test]
        public void SplitAcesTakeOneCardEachAndStand()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, A, 10, A, 7, 9, 5);
            round.Split();

            Assert.AreEqual(BlackjackState.Settled, round.State, "no decisions after splitting aces");
            Assert.AreEqual(2, round.Hands[0].Cards.Count);
            Assert.AreEqual(2, round.Hands[1].Cards.Count);
            Assert.AreEqual(20, round.Hands[0].Total);
            Assert.AreEqual(16, round.Hands[1].Total);
        }

        [Test]
        public void APairSplitsOnceAndOnlyAPairOfTheSameRank()
        {
            var tenKing = Deal(new TestWallet(100), 10, 9, K, 7);
            Assert.IsFalse(tenKing.CanSplit, "a ten and a king are worth the same but are not a pair");

            var round = Deal(new TestWallet(100), 8, 10, 8, 7, 8);
            round.Split();
            Assert.IsFalse(round.CanSplit, "8,8 again on the left, but only one split per round");
            Assert.Throws<InvalidOperationException>(() => round.Split());
        }

        [Test]
        public void DoublingAfterASplitFollowsTheTable()
        {
            var allowed = Deal(new TestWallet(100), 5, 10, 5, 7, 6);
            allowed.Split();
            Assert.IsTrue(allowed.CanDouble, "5+6 after a split, double after split on");

            var refused = Deal(new TestWallet(100), 10, new BlackjackRuleSet(150, false, false), 5, 10, 5, 7, 6);
            refused.Split();
            Assert.IsFalse(refused.CanDouble);
        }

        [Test]
        public void TheHoleCardStaysHiddenUntilTheRoundSettles()
        {
            var round = Deal(new TestWallet(100), 10, 9, 8, 7);
            Assert.Throws<InvalidOperationException>(() => { _ = round.Dealer; });
            round.Stand();
            Assert.AreEqual(16, round.Dealer.Cards.Take(2).Sum(c => c.Points));
        }

        [Test]
        public void WalkingAwayStandsEveryOpenHandAndSettles()
        {
            var wallet = new TestWallet(100);
            var round = Deal(wallet, 8, 10, 8, 7, 3, 10);
            round.Split();

            round.Abandon();
            round.Abandon();

            Assert.AreEqual(BlackjackState.Settled, round.State);
            Assert.AreEqual(11, round.Hands[0].Total, "the left hand stood on 8+3");
            Assert.AreEqual(18, round.Hands[1].Total, "the right hand was dealt its second card and stood");
            Assert.AreEqual(HandResult.Lose, round.Hands[0].Result);
            Assert.AreEqual(HandResult.Win, round.Hands[1].Result);
        }

        [Test]
        public void ASettledRoundTakesNoMoreMoves()
        {
            var round = Deal(new TestWallet(100), 10, 10, 9, 8);
            round.Stand();
            Assert.IsFalse(round.CanHit);
            Assert.IsFalse(round.CanDouble);
            Assert.IsFalse(round.CanSplit);
            Assert.Throws<InvalidOperationException>(() => round.Hit());
            Assert.Throws<InvalidOperationException>(() => round.Stand());
        }

        /// <summary>
        /// Every way a round can move credits, mixed at random over a long session:
        /// wagers, doubles, splits, naturals, pushes. The wallet must still add up.
        /// </summary>
        [Test]
        public void ALongSessionWithSplitsAndDoublesReconcilesWithTheLedger()
        {
            const long opening = 1_000_000;
            var wallet = new TestWallet(opening);
            var game = new BlackjackGame(new SeededRng(4242), wallet, BlackjackRuleSet.Standard);
            var choices = new Random(99);
            long staked = 0, returned = 0, splits = 0, doubles = 0;

            for (var i = 0; i < 5000; i++)
            {
                Assert.IsTrue(game.TryStart(10, out var round));
                while (round.IsPlayerTurn)
                {
                    if (round.CanSplit && choices.Next(2) == 0) { round.Split(); splits++; }
                    else if (round.CanDouble && choices.Next(3) == 0) { round.Double(); doubles++; }
                    else if (round.Active.Total < 17) round.Hit();
                    else round.Stand();
                }
                staked += round.Staked;
                returned += round.Payout;
            }

            Assert.Greater(splits, 50, "the session exercised splits");
            Assert.Greater(doubles, 200, "the session exercised doubles");
            Assert.AreEqual(opening - staked + returned, wallet.Balance);
            Assert.AreEqual(wallet.Balance, wallet.Ledger.Sum(entry => entry.Delta));
        }
    }
}
