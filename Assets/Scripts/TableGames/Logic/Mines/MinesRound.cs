using System;

namespace HighStakes.TableGames.Mines
{
    public enum MinesState
    {
        /// <summary>The wager is taken and tiles can be revealed.</summary>
        Open,

        /// <summary>A mine was revealed. The wager is lost.</summary>
        Busted,

        /// <summary>The payout for the safe tiles was credited.</summary>
        CashedOut,

        /// <summary>Left before any tile was revealed. The wager was handed back.</summary>
        Refunded,
    }

    /// <summary>One round of Mines, from the wager to its end. The only way a round moves chips after its wager.</summary>
    public sealed class MinesRound
    {
        readonly ITableWallet wallet;
        readonly bool[] isMine;
        readonly bool[] revealed = new bool[MinesGame.Tiles];
        readonly int houseEdgeBasisPoints;

        public long Wager { get; }
        public int Mines { get; }
        public MinesState State { get; private set; } = MinesState.Open;

        /// <summary>Safe tiles revealed so far.</summary>
        public int Safe { get; private set; }

        /// <summary>What was credited when the round ended. Zero while open and on a bust.</summary>
        public long Payout { get; private set; }

        internal MinesRound(ITableWallet wallet, long wager, int mines, bool[] isMine, int houseEdgeBasisPoints)
        {
            this.wallet = wallet;
            this.isMine = isMine;
            this.houseEdgeBasisPoints = houseEdgeBasisPoints;
            Wager = wager;
            Mines = mines;
        }

        public bool IsOpen => State == MinesState.Open;
        public bool IsRevealed(int tile) => revealed[Check(tile)];

        /// <summary>What cashing out now would pay. Zero before the first safe tile.</summary>
        public long CashOutValue => Safe == 0 ? 0 : MinesGame.PayoutFor(Wager, Mines, Safe, houseEdgeBasisPoints);

        /// <summary>The multiplier the next safe tile would bring, in hundredths. Zero once every safe tile is out.</summary>
        public long NextMultiplier => Safe < MinesGame.Tiles - Mines
            ? MinesGame.MultiplierFor(Mines, Safe + 1, houseEdgeBasisPoints)
            : 0;

        /// <summary>Where the mines are. Only once the round is over, so an open round cannot leak them.</summary>
        public bool IsMine(int tile)
        {
            if (IsOpen) throw new InvalidOperationException("The mines stay hidden until the round ends.");
            return isMine[Check(tile)];
        }

        /// <summary>
        /// Turns a tile over. True if it was safe. A mine busts the round; the last safe tile cashes out on its own,
        /// since there is nothing left to win.
        /// </summary>
        public bool Reveal(int tile)
        {
            RequireOpen();
            if (revealed[Check(tile)]) throw new InvalidOperationException($"Tile {tile} is already revealed.");
            revealed[tile] = true;

            if (isMine[tile])
            {
                State = MinesState.Busted;
                return false;
            }

            Safe++;
            if (Safe == MinesGame.Tiles - Mines) CashOut();
            return true;
        }

        public void CashOut()
        {
            RequireOpen();
            if (Safe == 0) throw new InvalidOperationException("Reveal a tile before cashing out.");
            Payout = CashOutValue;
            State = MinesState.CashedOut;
            if (Payout > 0) wallet.Credit(Payout, "Mines win");
        }

        /// <summary>
        /// Ends a round the player walked away from: cashed out if any tile was revealed, otherwise the wager is handed
        /// back. Nothing about the mines was learned before the first reveal, so the refund changes no odds.
        /// </summary>
        public void Abandon()
        {
            if (!IsOpen) return;
            if (Safe > 0)
            {
                CashOut();
                return;
            }
            Payout = Wager;
            State = MinesState.Refunded;
            wallet.Credit(Wager, "Mines refund");
        }

        void RequireOpen()
        {
            if (!IsOpen) throw new InvalidOperationException($"The round is over: {State}.");
        }

        static int Check(int tile)
        {
            if (tile < 0 || tile >= MinesGame.Tiles) throw new ArgumentOutOfRangeException(nameof(tile));
            return tile;
        }
    }
}
