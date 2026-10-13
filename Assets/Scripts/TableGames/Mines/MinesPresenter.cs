using System;
using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Mines
{
    /// <summary>
    /// Scene glue between the Mines table and a MinesRound. START takes the wager and hides the mines; each of the 25
    /// tiles turns over when pressed, and CASH OUT takes what the safe tiles are worth. When the round ends the whole
    /// grid shows where the mines were, and the dealer board announces the result.
    /// </summary>
    // ponytail: press one tile at a time; vrstake's swipe-across-the-grid needs a swipe mode on PressableButton.
    public sealed class MinesPresenter : MonoBehaviour
    {
        [SerializeField] MinesRules rules;

        [Header("Bet")]
        [SerializeField] PressableButton halveButton;
        [SerializeField] PressableButton doubleButton;
        [SerializeField] PressableButton fewerButton;
        [SerializeField] PressableButton moreButton;
        [SerializeField] PressableButton startButton;
        [SerializeField] PressableButton cashOutButton;
        [Tooltip("START and CASH OUT: dimmed while the button does nothing.")]
        [SerializeField] TMP_Text startLabel;
        [SerializeField] TMP_Text cashOutLabel;
        [SerializeField] Color unavailable = new Color(1f, 1f, 1f, 0.2f);

        [Header("Readouts")]
        [SerializeField] TMP_Text wagerText;
        [SerializeField] TMP_Text minesText;
        [SerializeField] TMP_Text balanceText;
        [Tooltip("What cashing out now pays, as a multiple of the wager.")]
        [SerializeField] TMP_Text multiplierText;
        [Tooltip("What the next safe tile would pay.")]
        [SerializeField] TMP_Text nextText;

        [Header("Tiles, in grid order from the far left corner")]
        [SerializeField] PressableButton[] tiles;
        [SerializeField] Renderer[] tileBodies;
        [SerializeField] TMP_Text[] tileLabels;
        [SerializeField] Material hiddenMaterial;
        [SerializeField] Material gemMaterial;
        [SerializeField] Material mineMaterial;
        [SerializeField] Color gemTextColor = Color.black;
        [SerializeField] Color mineTextColor = Color.white;
        [Tooltip("Tiles the player never turned, shown when the round ends, are faded to this alpha.")]
        [SerializeField, Range(0f, 1f)] float unpickedAlpha = 0.4f;

        [Header("Round result")]
        [SerializeField] TableHologram hologram;

        IChipWallet wallet;
        MinesGame game;
        MinesBet bet;
        MinesRound round;
        Color startColor;
        Color cashOutColor;

        bool Open => round != null && round.IsOpen;

        // Controls are children of this prefab and die with it, so no unsubscribe.
        void Awake()
        {
            halveButton.Pressed.AddListener(() => ChangeBet(b => b.HalveWager()));
            doubleButton.Pressed.AddListener(() => ChangeBet(b => b.DoubleWager()));
            fewerButton.Pressed.AddListener(() => ChangeBet(b => b.FewerMines()));
            moreButton.Pressed.AddListener(() => ChangeBet(b => b.MoreMines()));
            startButton.Pressed.AddListener(StartRound);
            cashOutButton.Pressed.AddListener(CashOut);
            // The builder's label colours, restored when a dimmed button comes back.
            startColor = startLabel.color;
            cashOutColor = cashOutLabel.color;
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = i;
                tiles[i].Pressed.AddListener(() => Reveal(tile));
                Hide(tile);
            }
        }

        void Start()
        {
            bet = rules.NewBet();
            // Quietly, so the balance shows on arrival; START is what complains if there is still no wallet.
            Bind(FindWallet());
            Show();
        }

        // A round left open settles before the session ends: cashed out, or refunded if nothing was revealed.
        void OnApplicationQuit() => round?.Abandon();

        // Unsubscribe first: the abandoned round's credit would otherwise write to texts being destroyed with us.
        void OnDestroy()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            round?.Abandon();
        }

        /// <summary>The first active wallet in the loaded scenes, found by interface the way the wrist display does it.</summary>
        static IChipWallet FindWallet()
        {
            foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (candidate is IChipWallet found) return found;
            return null;
        }

        void Bind(IChipWallet found)
        {
            if (wallet != null || found == null) return;
            wallet = found;
            wallet.BalanceChanged += OnBalanceChanged;
        }

        void OnBalanceChanged(int _) => ShowBalance();

        /// <summary>Built on the first START, since the wallet may live in a scene that loads after this table.</summary>
        bool EnsureGame()
        {
            if (game != null) return true;
            Bind(FindWallet());
            if (wallet == null)
            {
                Debug.LogError("[Mines] No active IChipWallet in the loaded scenes; the table cannot take bets.", this);
                hologram.Refused("No chip wallet");
                return false;
            }
            // System. is needed: inside HighStakes, Environment is the HighStakes.Environment namespace.
            var seed = System.Environment.TickCount;
            Debug.Log($"[Mines] Seed {seed}");
            game = new MinesGame(new SeededRng(seed), new ChipWalletTableAdapter(wallet), rules.HouseEdgeBasisPoints);
            return true;
        }

        void ChangeBet(Action<MinesBet> change)
        {
            if (bet == null || Open) return;
            change(bet);
            Show();
        }

        void StartRound()
        {
            if (bet == null || Open) return;
            if (!EnsureGame()) return;
            if (!game.TryStart(bet.Wager, bet.Mines, out var started))
            {
                hologram.Refused("Not enough chips");
                return;
            }
            round = started;
            for (var i = 0; i < tiles.Length; i++) Hide(i);
            hologram.Idle($"{round.Mines} {MinesWord(round.Mines)}  ·  PICK A TILE");
            Show();
        }

        void Reveal(int tile)
        {
            if (!Open)
            {
                if (round == null) hologram.Idle("PRESS START TO PLAY");
                return;
            }
            if (round.IsRevealed(tile)) return;
            var safe = round.Reveal(tile);
            Turn(tile, safe, true);
            if (round.IsOpen)
            {
                hologram.Idle($"{round.Safe} SAFE  ·  CASH OUT FOR {round.CashOutValue:N0}");
                Show();
            }
            else End();
        }

        void CashOut()
        {
            if (!Open || round.Safe == 0) return;
            round.CashOut();
            End();
        }

        /// <summary>Shows the whole grid and announces the round; the table takes the next bet.</summary>
        void End()
        {
            for (var i = 0; i < tiles.Length; i++)
                if (!round.IsRevealed(i))
                    Turn(i, !round.IsMine(i), false);

            Debug.Log($"[Mines] {round.Mines} mines, {round.Safe} safe, {round.State}, wager {round.Wager}, payout {round.Payout}");
            var net = round.Payout - round.Wager;
            var detail = $"{round.Safe} SAFE  ·  {round.Mines} {MinesWord(round.Mines)}";
            if (round.State == MinesState.Busted) hologram.Lost(round.Wager, detail, "MINE!");
            else if (net > 0) hologram.Won(net, $"CASHED OUT AT {Format(MinesGame.MultiplierFor(round.Mines, round.Safe, rules.HouseEdgeBasisPoints))}", detail);
            else hologram.Push(detail, "CASHED OUT");
            Show();
        }

        void Hide(int tile)
        {
            tileBodies[tile].sharedMaterial = hiddenMaterial;
            tileLabels[tile].text = "";
        }

        /// <summary>A gem or a mine, at full strength if the player picked it, faded if the round's end exposed it.</summary>
        void Turn(int tile, bool safe, bool picked)
        {
            tileBodies[tile].sharedMaterial = safe ? gemMaterial : mineMaterial;
            var label = tileLabels[tile];
            label.text = safe ? "GEM" : "MINE";
            var color = safe ? gemTextColor : mineTextColor;
            if (!picked) color.a *= unpickedAlpha;
            label.color = color;
        }

        void ShowBalance() => balanceText.text = wallet == null ? "" : "CHIPS " + wallet.Balance.ToString("N0");

        void Show()
        {
            var edge = rules.HouseEdgeBasisPoints;
            wagerText.text = bet.Wager.ToString("N0");
            minesText.text = $"{bet.Mines} {MinesWord(bet.Mines)}";
            ShowBalance();

            var open = Open;
            var canCashOut = open && round.Safe > 0;
            startLabel.color = open ? unavailable : startColor;
            cashOutLabel.color = canCashOut ? cashOutColor : unavailable;
            cashOutLabel.text = canCashOut ? $"CASH OUT {round.CashOutValue:N0}" : "CASH OUT";

            if (!open)
            {
                // Between rounds, what the bet on the felt would bring for the first safe tile.
                multiplierText.text = "";
                nextText.text = $"FIRST TILE {Format(MinesGame.MultiplierFor(bet.Mines, 1, edge))}  ·  PAYS {MinesGame.PayoutFor(bet.Wager, bet.Mines, 1, edge):N0}";
                return;
            }
            multiplierText.text = round.Safe == 0 ? "NOW -" : $"NOW {Format(MinesGame.MultiplierFor(round.Mines, round.Safe, edge))}";
            nextText.text = round.NextMultiplier > 0
                ? $"NEXT TILE {Format(round.NextMultiplier)}  ·  PAYS {MinesGame.PayoutFor(round.Wager, round.Mines, round.Safe + 1, edge):N0}"
                : "";
        }

        static string MinesWord(int mines) => mines == 1 ? "MINE" : "MINES";

        static string Format(long hundredths) => $"{hundredths / 100:N0}.{hundredths % 100:00}x";
    }
}
