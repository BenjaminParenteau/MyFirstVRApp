using System;
using System.Linq;
using UnityEngine;

namespace HighStakes.TableGames.Plinko
{
    /// <summary>One board's bucket multipliers, in hundredths, left to right.</summary>
    [Serializable]
    public struct PlinkoBoard
    {
        public int rows;
        public PlinkoRisk risk;
        public long[] multipliers;

        public PlinkoBoard(int rows, PlinkoRisk risk, long[] multipliers)
        {
            this.rows = rows;
            this.risk = risk;
            this.multipliers = multipliers;
        }
    }

    /// <summary>Immutable table definition. Runtime state lives in PlinkoGame and PlinkoBet.</summary>
    [CreateAssetMenu(menuName = "High Stakes/Tables/Plinko Rules", fileName = "PlinkoRules")]
    public sealed class PlinkoRules : ScriptableObject
    {
        // Only what a new asset starts with: the shipped boards, every one returning 0.99 before rounding. Tune the asset.
        [SerializeField] PlinkoBoard[] boards = StandardBoards();

        [Header("Table limits")]
        [SerializeField, Min(1)] long minWager = 10;
        [SerializeField, Min(1)] long maxWager = 500;

        [Header("Bet when the player arrives")]
        [SerializeField, Min(1)] long wager = 10;
        [SerializeField, Range(PlinkoPaytable.MinRows, PlinkoPaytable.MaxRows)] int rows = 8;
        [SerializeField] PlinkoRisk risk = PlinkoRisk.Low;

        public PlinkoPaytable Paytable() => new PlinkoPaytable(boards.Select(b => (b.rows, b.risk, b.multipliers)));

        public PlinkoBet NewBet() => new PlinkoBet(wager, minWager, maxWager, rows, risk, Paytable());

        static PlinkoBoard[] StandardBoards()
        {
            var standard = PlinkoPaytable.Standard();
            return standard.Rows
                .SelectMany(count => ((PlinkoRisk[])Enum.GetValues(typeof(PlinkoRisk))).Select(r => new PlinkoBoard(count, r, standard.For(count, r))))
                .ToArray();
        }
    }
}
