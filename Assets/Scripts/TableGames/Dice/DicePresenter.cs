using System.Collections;
using System.Linq;
using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Dice
{
    /// <summary>
    /// Scene glue between the dice table and a DiceGame. - and + step the target (hold to run), ROLL takes the wager
    /// and rolls, the bar's marker slides to the number, and only then is a win paid and announced on the dealer board.
    /// </summary>
    public sealed class DicePresenter : MonoBehaviour
    {
        [SerializeField] DiceRules rules;

        [Header("Wager row")]
        [SerializeField] PressableButton rollButton;
        [SerializeField] PressableButton halveButton;
        [SerializeField] PressableButton doubleButton;
        [SerializeField] TMP_Text wagerText;
        [SerializeField] TMP_Text balanceText;

        [Header("Target")]
        [SerializeField] PressableButton lowerButton;
        [SerializeField] PressableButton raiseButton;
        [SerializeField] TMP_Text targetText;
        [SerializeField] TMP_Text winChanceText;
        [SerializeField] TMP_Text multiplierText;
        [SerializeField] DiceBar bar;

        [Header("Round result")]
        [SerializeField] TableHologram hologram;

        IChipWallet wallet;
        DiceGame game;
        DiceBet bet;
        DiceRound rolling;

        // Controls are children of this prefab and die with it, so no unsubscribe.
        void Awake()
        {
            rollButton.Pressed.AddListener(Roll);
            halveButton.Pressed.AddListener(() => ChangeBet(b => b.HalveWager()));
            doubleButton.Pressed.AddListener(() => ChangeBet(b => b.DoubleWager()));
            lowerButton.Pressed.AddListener(() => StepTarget(-1));
            raiseButton.Pressed.AddListener(() => StepTarget(1));
        }

        void Start()
        {
            bet = rules.NewBet();
            Bind(FindWallet());
            bar.ShowTarget(bet.Target);
            Show();
        }

        // A roll left sliding (scene unload, quit) still pays: the number was drawn when the wager was taken.
        void OnApplicationQuit() => rolling?.Settle();

        void OnDestroy()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            rolling?.Settle();
        }

        static IChipWallet FindWallet() =>
            FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IChipWallet>().FirstOrDefault();

        void Bind(IChipWallet found)
        {
            if (found == null || found == wallet) return;
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            wallet = found;
            wallet.BalanceChanged += OnBalanceChanged;
            game = null;
        }

        void OnBalanceChanged(int _) => Show();

        /// <summary>Built on the first roll, since the wallet may live in a scene that loads after this table.</summary>
        bool EnsureGame()
        {
            if (wallet == null) Bind(FindWallet());
            if (wallet == null)
            {
                Debug.LogError("[Dice] No active IChipWallet in the loaded scenes; the table cannot take bets.", this);
                hologram.Refused("No chip wallet");
                return false;
            }
            if (game == null)
            {
                // System. is needed: inside HighStakes, Environment is the HighStakes.Environment namespace.
                var seed = System.Environment.TickCount;
                Debug.Log($"[Dice] Seed {seed}");
                game = new DiceGame(new SeededRng(seed), new ChipWalletTableAdapter(wallet), rules.HouseEdgeBasisPoints);
            }
            return true;
        }

        // The wager may change while the marker slides: the round already holds the one it took.
        void ChangeBet(System.Action<DiceBet> change)
        {
            if (bet == null) return;
            change(bet);
            Show();
        }

        // Not while the marker slides, so the bar under it still shows the target it is judged against.
        void StepTarget(int step)
        {
            if (bet == null || rolling != null) return;
            bet.SetTarget(bet.Target + step);
            bar.ShowTarget(bet.Target);
            Show();
        }

        void Roll()
        {
            if (bet == null || rolling != null) return;
            if (!EnsureGame()) return;
            if (!game.TryRoll(bet.Wager, bet.Target, out var round))
            {
                hologram.Refused("Not enough chips");
                return;
            }
            StartCoroutine(Reveal(round));
        }

        IEnumerator Reveal(DiceRound round)
        {
            rolling = round;
            hologram.Idle("GOOD LUCK");
            yield return bar.SlideMarkerTo(round.Roll, round.Won);

            round.Settle(); // the winnings arrive with the marker
            rolling = null;
            Announce(round);
            Show();
        }

        void Announce(DiceRound round)
        {
            Debug.Log($"[Dice] rolled {round.Roll} over {round.Target}, wager {round.Wager}, payout {round.Payout}");
            var title = $"ROLLED {round.Roll}";
            var detail = $"OVER {round.Target}  ·  BET ${round.Wager:N0}";
            if (round.Net > 0) hologram.Won(round.Net, title, detail);
            else if (round.Net == 0) hologram.Push(detail, title);
            else hologram.Lost(-round.Net, detail, title);
        }

        void Show()
        {
            if (bet == null) return;
            var edge = rules.HouseEdgeBasisPoints;
            var multiplier = (10000 - edge) / (100.0 * (DiceGame.MaxRoll - bet.Target));
            wagerText.text = bet.Wager.ToString("N0");
            targetText.text = bet.Target.ToString();
            winChanceText.text = $"{DiceGame.MaxRoll - bet.Target:F2}%";
            multiplierText.text = $"{multiplier:F4}x";
            balanceText.text = wallet == null ? "" : "CHIPS " + wallet.Balance.ToString("N0");
        }
    }
}
