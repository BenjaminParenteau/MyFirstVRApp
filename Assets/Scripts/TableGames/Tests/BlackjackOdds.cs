using System.Collections.Generic;

namespace HighStakes.TableGames.Blackjack.Tests
{
    /// <summary>
    /// Exact return of a fixed strategy under the infinite deck: every card is an
    /// independent draw, so the dealer's and the player's final totals can be
    /// enumerated rather than sampled. Written from the rules, not from the game,
    /// so the return tests measure the game against an independent answer.
    /// </summary>
    static class BlackjackOdds
    {
        /// <summary>Final totals are 17 to 21, and 22 stands for any bust.</summary>
        const int Bust = 22;

        static double Chance(int points) => points == 10 ? 4.0 / 13 : 1.0 / 13;

        static int Best(int hard, bool ace) => ace && hard + 10 <= 21 ? hard + 10 : hard;

        static bool Soft(int hard, bool ace) => ace && hard + 10 <= 21;

        /// <summary>
        /// Credits returned per credit staked by a player who hits below 17 and,
        /// optionally, doubles any hard 10 or 11 on the first two cards. Never splits.
        /// </summary>
        public static double Return(BlackjackRuleSet rules, bool doubleHard10And11)
        {
            var dealerMemo = new Dictionary<(int, bool), Dictionary<int, double>>();
            var playerMemo = new Dictionary<(int, bool), Dictionary<int, double>>();
            double returned = 0, staked = 0;

            for (var up = 1; up <= 10; up++)
            for (var hole = 1; hole <= 10; hole++)
            {
                var dealerBlackjack = (up == 1 && hole == 10) || (up == 10 && hole == 1);
                var dealer = Dealer(up + hole, up == 1 || hole == 1, rules.DealerHitsSoft17, dealerMemo);

                for (var first = 1; first <= 10; first++)
                for (var second = 1; second <= 10; second++)
                {
                    var weight = Chance(up) * Chance(hole) * Chance(first) * Chance(second);
                    var hard = first + second;
                    var ace = first == 1 || second == 1;
                    var playerBlackjack = Best(hard, ace) == 21;

                    if (dealerBlackjack)
                    {
                        returned += weight * (playerBlackjack ? 1 : 0);
                        staked += weight;
                        continue;
                    }
                    if (playerBlackjack)
                    {
                        returned += weight * (1 + rules.BlackjackPaysPercent / 100.0);
                        staked += weight;
                        continue;
                    }

                    Dictionary<int, double> final;
                    var stake = 1;
                    if (doubleHard10And11 && !ace && (hard == 10 || hard == 11))
                    {
                        stake = 2;
                        final = new Dictionary<int, double>();
                        for (var card = 1; card <= 10; card++)
                            AddTo(final, Clamp(Best(hard + card, card == 1)), Chance(card));
                    }
                    else
                    {
                        final = HitBelow17(hard, ace, playerMemo);
                    }

                    foreach (var p in final)
                    foreach (var d in dealer)
                        returned += weight * p.Value * d.Value * Settle(p.Key, d.Key, stake);
                    staked += weight * stake;
                }
            }
            return returned / staked;
        }

        static double Settle(int player, int dealer, int stake)
        {
            if (player == Bust) return 0;
            if (dealer == Bust || player > dealer) return 2 * stake;
            return player == dealer ? stake : 0;
        }

        static int Clamp(int total) => total > 21 ? Bust : total;

        static void AddTo(Dictionary<int, double> into, int total, double chance) =>
            into[total] = (into.TryGetValue(total, out var had) ? had : 0) + chance;

        static Dictionary<int, double> Dealer(int hard, bool ace, bool hitsSoft17,
            Dictionary<(int, bool), Dictionary<int, double>> memo)
        {
            if (memo.TryGetValue((hard, ace), out var known)) return known;
            var total = Best(hard, ace);
            var result = new Dictionary<int, double>();
            if (total > 21) result[Bust] = 1;
            else if (total > 17 || (total == 17 && !(hitsSoft17 && Soft(hard, ace)))) result[total] = 1;
            else
                for (var card = 1; card <= 10; card++)
                foreach (var next in Dealer(hard + card, ace || card == 1, hitsSoft17, memo))
                    AddTo(result, next.Key, Chance(card) * next.Value);
            memo[(hard, ace)] = result;
            return result;
        }

        static Dictionary<int, double> HitBelow17(int hard, bool ace, Dictionary<(int, bool), Dictionary<int, double>> memo)
        {
            if (memo.TryGetValue((hard, ace), out var known)) return known;
            var total = Best(hard, ace);
            var result = new Dictionary<int, double>();
            if (total > 21) result[Bust] = 1;
            else if (total >= 17) result[total] = 1;
            else
                for (var card = 1; card <= 10; card++)
                foreach (var next in HitBelow17(hard + card, ace || card == 1, memo))
                    AddTo(result, next.Key, Chance(card) * next.Value);
            memo[(hard, ace)] = result;
            return result;
        }
    }
}
