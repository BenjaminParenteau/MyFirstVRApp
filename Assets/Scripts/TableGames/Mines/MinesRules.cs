using UnityEngine;

namespace HighStakes.TableGames.Mines
{
    /// <summary>Immutable table definition. Runtime state lives in MinesGame, MinesRound and MinesBet.</summary>
    [CreateAssetMenu(menuName = "High Stakes/Tables/Mines Rules", fileName = "MinesRules")]
    public sealed class MinesRules : ScriptableObject
    {
        [Tooltip("The house's cut in hundredths of a percent: 100 returns 99% of every wager over time.")]
        [SerializeField, Range(0, 9999)] int houseEdgeBasisPoints = 100;

        [Header("Table limits")]
        [SerializeField, Min(1)] long minWager = 10;
        [SerializeField, Min(1)] long maxWager = 500;
        [SerializeField, Range(MinesGame.MinMines, MinesGame.MaxMines)] int minMines = MinesGame.MinMines;
        [SerializeField, Range(MinesGame.MinMines, MinesGame.MaxMines)] int maxMines = MinesGame.MaxMines;

        [Header("Bet when the player arrives")]
        [SerializeField, Min(1)] long wager = 10;
        [SerializeField, Range(MinesGame.MinMines, MinesGame.MaxMines)] int mines = 3;

        public int HouseEdgeBasisPoints => houseEdgeBasisPoints;

        public MinesBet NewBet() => new MinesBet(wager, mines, minWager, maxWager, minMines, maxMines);
    }
}
