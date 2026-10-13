using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Roulette
{
    /// <summary>
    /// Scene glue between the roulette table and a RouletteGame. Pressing a spot stacks the current chip on it; SPIN
    /// takes every chip on the layout, spins the wheel and drops the ball into the pocket the game already drew, then
    /// the dealer board announces it. Bets stay on the layout for the next spin until CLEAR.
    /// </summary>
    public sealed class RoulettePresenter : MonoBehaviour
    {
        [Header("Chips")]
        [SerializeField] PressableButton halveButton;
        [SerializeField] PressableButton doubleButton;
        [SerializeField] PressableButton clearButton;
        [SerializeField] PressableButton spinButton;
        [SerializeField] TMP_Text chipText;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text balanceText;
        [SerializeField, Min(1)] long minChip = 5;
        [SerializeField, Min(1)] long maxChip = 500;
        [SerializeField, Min(1)] long startChip = 10;

        [Header("Wheel")]
        [Tooltip("Spins about its local Y axis; the pockets are laid out on it clockwise from zero.")]
        [SerializeField] Transform wheelHead;
        [Tooltip("A child of the wheel's base, at the base's centre height.")]
        [SerializeField] Transform ball;
        [SerializeField] float trackRadius = 0.27f;
        [SerializeField] float pocketRadius = 0.2f;
        [SerializeField] float spinSeconds = 5f;

        [Header("Round result")]
        [SerializeField] TableHologram hologram;

        RouletteSpot[] spots;
        readonly Dictionary<RouletteSpot, long> bets = new Dictionary<RouletteSpot, long>();
        WagerAmount chip;
        IChipWallet wallet;
        RouletteGame game;
        RouletteSpin rolling;
        bool spinning;
        float wheelAngle;
        float ballAngle;
        float ballHeight;

        // Controls are children of this prefab and die with it, so no unsubscribe.
        void Awake()
        {
            spots = GetComponentsInChildren<RouletteSpot>(true);
            foreach (var spot in spots)
            {
                var s = spot;
                s.Button.Pressed.AddListener(() => AddChip(s));
                s.Show(0);
            }
            halveButton.Pressed.AddListener(() => ChangeChip(c => c.Halve()));
            doubleButton.Pressed.AddListener(() => ChangeChip(c => c.Double()));
            clearButton.Pressed.AddListener(Clear);
            spinButton.Pressed.AddListener(Spin);
            ballHeight = ball.localPosition.y;
        }

        void Start()
        {
            chip = new WagerAmount(startChip, minChip, maxChip);
            Bind(FindWallet());
            PlaceBall(ballAngle, trackRadius);
            Show();
        }

        // A spin left rolling (scene unload, quit) still pays: the pocket was drawn when the bets were taken.
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

        bool EnsureGame()
        {
            if (wallet == null) Bind(FindWallet());
            if (wallet == null)
            {
                Debug.LogError("[Roulette] No active IChipWallet in the loaded scenes; the table cannot take bets.", this);
                hologram.Refused("No chip wallet");
                return false;
            }
            if (game == null)
            {
                var seed = System.Environment.TickCount;
                Debug.Log($"[Roulette] Seed {seed}");
                game = new RouletteGame(new SeededRng(seed), new ChipWalletTableAdapter(wallet));
            }
            return true;
        }

        void ChangeChip(System.Action<WagerAmount> change)
        {
            if (spinning) return;
            change(chip);
            Show();
        }

        void AddChip(RouletteSpot spot)
        {
            if (spinning) return;
            bets.TryGetValue(spot, out var stake);
            bets[spot] = stake + chip.Value;
            spot.Show(bets[spot]);
            Show();
        }

        void Clear()
        {
            if (spinning) return;
            foreach (var spot in bets.Keys) spot.Show(0);
            bets.Clear();
            hologram.Idle();
            Show();
        }

        void Spin()
        {
            if (spinning) return;
            if (bets.Count == 0)
            {
                hologram.Refused("PLACE A BET FIRST");
                return;
            }
            if (!EnsureGame()) return;
            var layout = bets.Select(kv => (kv.Key.Bet, kv.Value)).ToList();
            if (!game.TrySpin(layout, out var result))
            {
                hologram.Refused("Not enough chips");
                return;
            }
            StartCoroutine(Run(result));
        }

        IEnumerator Run(RouletteSpin spin)
        {
            spinning = true;
            rolling = spin;
            hologram.Idle("NO MORE BETS");

            // The wheel turns clockwise and slows; the ball runs the other way round the track, drops in, and ends in
            // the drawn pocket exactly as the wheel stops. The game drew the pocket already: the animation follows it.
            float slot = RouletteWheel.SlotOf(spin.Pocket) * 360f / RouletteWheel.Pockets;
            float wheelFrom = wheelAngle, wheelTo = wheelAngle + 720f + Random.Range(0f, 360f);
            float ballFrom = ballAngle, ballTo = wheelTo + slot;
            ballTo -= 360f * Mathf.Ceil((ballTo - (ballFrom - 1440f)) / 360f); // at least four laps the other way

            for (float t = 0f; t < spinSeconds; t += Time.deltaTime)
            {
                float k = t / spinSeconds;
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                wheelAngle = Mathf.Lerp(wheelFrom, wheelTo, ease);
                wheelHead.localRotation = Quaternion.Euler(0f, wheelAngle, 0f);
                ballAngle = Mathf.Lerp(ballFrom, ballTo, 1f - Mathf.Pow(1f - k, 2.2f));
                float drop = Mathf.SmoothStep(0f, 1f, (k - 0.55f) / 0.35f);
                float bounce = k > 0.6f && k < 0.9f ? 0.01f * Mathf.Abs(Mathf.Sin(k * 60f)) * (0.9f - k) * 3f : 0f;
                PlaceBall(ballAngle, Mathf.Lerp(trackRadius, pocketRadius, drop), bounce);
                yield return null;
            }
            wheelAngle = wheelTo;
            wheelHead.localRotation = Quaternion.Euler(0f, wheelAngle, 0f);
            ballAngle = ballTo;
            PlaceBall(ballAngle, pocketRadius);

            spin.Settle(); // the winnings arrive with the ball
            rolling = null;
            Announce(spin);
            spinning = false;
            Show();
        }

        void PlaceBall(float angle, float radius, float lift = 0f)
        {
            var a = angle * Mathf.Deg2Rad;
            ball.localPosition = new Vector3(Mathf.Sin(a) * radius, ballHeight + lift, Mathf.Cos(a) * radius);
        }

        void Announce(RouletteSpin spin)
        {
            Debug.Log($"[Roulette] {spin.Pocket} {RouletteWheel.ColourName(spin.Pocket)}: staked {spin.Staked}, paid {spin.Payout}");
            var title = $"{spin.Pocket} {RouletteWheel.ColourName(spin.Pocket)}";
            var detail = spin.Winners.Count == 0
                ? $"BET ${spin.Staked:N0}"
                : string.Join("  ·  ", spin.Winners.Select(w => $"{w.Bet.Label} PAYS ${w.Paid:N0}"));
            if (spin.Net > 0) hologram.Won(spin.Net, title, detail);
            else if (spin.Net == 0) hologram.Push(detail, title);
            else hologram.Lost(-spin.Net, detail, title);
        }

        void Show()
        {
            chipText.text = chip == null ? "" : chip.Value.ToString("N0");
            long total = bets.Values.Sum();
            totalText.text = total > 0 ? "BET " + total.ToString("N0") : "";
            balanceText.text = wallet == null ? "" : "CHIPS " + wallet.Balance.ToString("N0");
        }
    }
}
