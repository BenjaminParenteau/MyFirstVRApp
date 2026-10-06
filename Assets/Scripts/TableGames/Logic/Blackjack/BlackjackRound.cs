using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HighStakes.TableGames.Blackjack
{
    public enum BlackjackState
    {
        /// <summary>The player is acting on the active hand.</summary>
        PlayerTurn,

        /// <summary>The dealer has played and every hand has been paid.</summary>
        Settled,
    }

    /// <summary>
    /// One round of Blackjack, from the deal to the settlement. The only way a round
    /// moves credits after its wager: a double or split takes a matching stake, and
    /// settling pays each hand.
    /// </summary>
    public sealed class BlackjackRound
    {
        readonly IRng rng;
        readonly ITableWallet wallet;
        readonly BlackjackRuleSet rules;
        readonly List<BlackjackHand> hands = new List<BlackjackHand>();
        readonly ReadOnlyCollection<BlackjackHand> handsView;
        readonly BlackjackHand dealer = new BlackjackHand(0, false);

        public long Wager { get; }
        public BlackjackState State { get; private set; } = BlackjackState.PlayerTurn;
        public bool IsPlayerTurn => State == BlackjackState.PlayerTurn;

        /// <summary>The player's hands: one, or two after a split.</summary>
        public IReadOnlyList<BlackjackHand> Hands => handsView;

        public int ActiveIndex { get; private set; }

        /// <summary>The hand the player is acting on. None once the round is settled.</summary>
        public BlackjackHand Active => IsPlayerTurn ? hands[ActiveIndex] : null;

        public Card DealerUpCard => dealer.Cards[0];

        /// <summary>The dealer's whole hand. Only once the round is settled, so the hole card cannot leak.</summary>
        public BlackjackHand Dealer
        {
            get
            {
                if (IsPlayerTurn) throw new InvalidOperationException("The hole card stays hidden until the round is settled.");
                return dealer;
            }
        }

        /// <summary>Every credit the round has taken: the wager plus any doubles and splits.</summary>
        public long Staked
        {
            get
            {
                long total = 0;
                foreach (var hand in hands) total += hand.Stake;
                return total;
            }
        }

        /// <summary>Everything credited when the round settled. Zero until then.</summary>
        public long Payout
        {
            get
            {
                long total = 0;
                foreach (var hand in hands) total += hand.Payout;
                return total;
            }
        }

        internal BlackjackRound(IRng rng, ITableWallet wallet, BlackjackRuleSet rules, long wager)
        {
            this.rng = rng;
            this.wallet = wallet;
            this.rules = rules;
            handsView = hands.AsReadOnly();
            Wager = wager;

            var hand = new BlackjackHand(wager, false);
            hands.Add(hand);
            Deal(hand);
            Deal(dealer);
            Deal(hand);
            Deal(dealer);

            // The dealer peeks: a dealer blackjack ends the round before the player acts,
            // and so does the player's own.
            if (dealer.IsBlackjack || hand.IsBlackjack)
            {
                hand.Finished = true;
                Settle();
            }
        }

        public bool CanHit => IsPlayerTurn;
        public bool CanStand => IsPlayerTurn;

        public bool CanDouble => IsPlayerTurn
                                 && Active.Cards.Count == 2
                                 && (!Active.FromSplit || rules.DoubleAfterSplit)
                                 && wallet.Balance >= Active.Stake;

        /// <summary>A pair of the same rank, once per round. A ten and a king are not a pair.</summary>
        public bool CanSplit => IsPlayerTurn
                                && hands.Count == 1
                                && Active.CanBeSplit
                                && wallet.Balance >= Active.Stake;

        public void Hit()
        {
            RequireTurn();
            Deal(Active);
            if (Active.Total >= 21) Finish();
        }

        public void Stand()
        {
            RequireTurn();
            Finish();
        }

        /// <summary>Doubles the stake on the active hand, deals it exactly one more card, and ends it.</summary>
        public void Double()
        {
            if (!CanDouble) throw new InvalidOperationException("This hand cannot be doubled now.");
            var hand = Active;
            if (!wallet.TryDebit(hand.Stake, "Blackjack double"))
                throw new InvalidOperationException("The wallet refused the double.");
            hand.Stake *= 2;
            hand.Doubled = true;
            Deal(hand);
            Finish();
        }

        /// <summary>
        /// Splits a pair into two hands with a stake each. Each hand gets its second card
        /// when it comes up; split aces get one card each and stand.
        /// </summary>
        public void Split()
        {
            if (!CanSplit) throw new InvalidOperationException("This hand cannot be split now.");
            var pair = hands[0];
            if (!wallet.TryDebit(pair.Stake, "Blackjack split"))
                throw new InvalidOperationException("The wallet refused the split.");

            var right = new BlackjackHand(pair.Stake, true);
            right.Add(pair.TakeSecond());
            var left = new BlackjackHand(pair.Stake, true);
            left.Add(pair.Cards[0]);
            hands[0] = left;
            hands.Add(right);

            Deal(left);
            if (left.Cards[0].IsAce)
            {
                Deal(right);
                left.Finished = true;
                right.Finished = true;
                Advance();
            }
            else if (left.Total == 21)
            {
                Finish();
            }
        }

        /// <summary>
        /// Ends a round the player walked away from: every open hand stands, the dealer
        /// plays, and the round settles as it would have. No new information is used.
        /// </summary>
        public void Abandon()
        {
            while (IsPlayerTurn) Stand();
        }

        void Deal(BlackjackHand hand) => hand.Add(Card.FromIndex(rng.Next(0, Card.DeckSize)));

        void Finish()
        {
            hands[ActiveIndex].Finished = true;
            Advance();
        }

        /// <summary>Moves to the next hand that needs the player, dealing a split hand its second card; plays the dealer when none is left.</summary>
        void Advance()
        {
            while (ActiveIndex < hands.Count && hands[ActiveIndex].Finished)
            {
                ActiveIndex++;
                if (ActiveIndex >= hands.Count) break;
                var next = hands[ActiveIndex];
                if (next.Cards.Count == 1)
                {
                    Deal(next);
                    if (next.Total == 21) next.Finished = true;
                }
            }

            if (ActiveIndex < hands.Count) return;
            ActiveIndex = hands.Count - 1;
            PlayDealer();
            Settle();
        }

        void PlayDealer()
        {
            var anyStanding = false;
            foreach (var hand in hands) anyStanding |= !hand.IsBust;
            if (!anyStanding) return;

            while (dealer.Total < 17 || (rules.DealerHitsSoft17 && dealer.Total == 17 && dealer.IsSoft))
                Deal(dealer);
        }

        void Settle()
        {
            State = BlackjackState.Settled;
            var first = hands[0];

            if (dealer.IsBlackjack)
            {
                if (first.IsBlackjack) Pay(first, HandResult.Push, first.Stake, "Blackjack push");
                else first.Result = HandResult.Lose;
                return;
            }

            if (first.IsBlackjack)
            {
                // Priced by BlackjackGame before the wager was taken, so this cannot overflow.
                var profit = (long)((decimal)first.Stake * rules.BlackjackPaysPercent / 100);
                Pay(first, HandResult.Blackjack, first.Stake + profit, "Blackjack natural");
                return;
            }

            var dealerTotal = dealer.Total;
            foreach (var hand in hands)
            {
                if (hand.IsBust) hand.Result = HandResult.Bust;
                else if (dealer.IsBust || hand.Total > dealerTotal) Pay(hand, HandResult.Win, hand.Stake * 2, "Blackjack win");
                else if (hand.Total == dealerTotal) Pay(hand, HandResult.Push, hand.Stake, "Blackjack push");
                else hand.Result = HandResult.Lose;
            }
        }

        void Pay(BlackjackHand hand, HandResult result, long amount, string reason)
        {
            hand.Result = result;
            hand.Payout = amount;
            wallet.Credit(amount, reason);
        }

        void RequireTurn()
        {
            if (!IsPlayerTurn) throw new InvalidOperationException("The round is settled.");
        }
    }
}
