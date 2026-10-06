using UnityEngine;

namespace HighStakes.TableGames.Blackjack
{
    /// <summary>Immutable table definition. Runtime state lives in BlackjackGame and BlackjackRound.</summary>
    [CreateAssetMenu(menuName = "High Stakes/Tables/Blackjack Rules", fileName = "BlackjackRules")]
    public sealed class BlackjackRules : ScriptableObject
    {
        [Tooltip("Profit on a natural as a percentage of the stake: 150 is 3:2, 120 is 6:5.")]
        [SerializeField, Range(100, 200)] int blackjackPaysPercent = 150;

        [Tooltip("The dealer draws on soft 17 instead of standing on every 17.")]
        [SerializeField] bool dealerHitsSoft17;

        [SerializeField] bool doubleAfterSplit = true;

        [Header("Table limits")]
        [SerializeField, Min(1)] long minWager = 10;
        [SerializeField, Min(1)] long maxWager = 500;

        [Header("Bet when the player arrives")]
        [SerializeField, Min(1)] long wager = 10;

        public BlackjackRuleSet RuleSet => new BlackjackRuleSet(blackjackPaysPercent, dealerHitsSoft17, doubleAfterSplit);

        public WagerAmount NewWager() => new WagerAmount(wager, minWager, maxWager);
    }
}
