using UnityEngine;

namespace HighStakes.TableGames.Dice
{
    /// <summary>Immutable table definition. Runtime state lives in DiceGame and DiceBet.</summary>
    [CreateAssetMenu(menuName = "High Stakes/Tables/Dice Rules", fileName = "DiceRules")]
    public sealed class DiceRules : ScriptableObject
    {
        [Tooltip("The house's cut in hundredths of a percent: 100 is 1%, so roll over 50 pays 1.98x.")]
        [SerializeField, Range(0, 9999)] int houseEdgeBasisPoints = 100;

        [Header("Table limits")]
        [SerializeField, Min(1)] long minWager = 10;
        [SerializeField, Min(1)] long maxWager = 500;

        [Header("Bet when the player arrives")]
        [SerializeField, Min(1)] long wager = 10;
        [SerializeField, Range(DiceGame.MinTarget, DiceGame.MaxTarget)] int target = 50;

        public int HouseEdgeBasisPoints => houseEdgeBasisPoints;

        public DiceBet NewBet() => new DiceBet(wager, target, minWager, maxWager);
    }
}
