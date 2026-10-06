using System;
using System.Collections;
using System.Collections.Generic;
using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Blackjack
{
    /// <summary>
    /// Scene glue between the Blackjack table and a BlackjackRound. After every move
    /// it lays the table out from the round: cards already down stay or slide, new
    /// ones fly from the shoe in dealing order, and the hole card turns over when
    /// the round settles.
    /// </summary>
    public sealed class BlackjackPresenter : MonoBehaviour
    {
        [SerializeField] BlackjackRules rules;

        [Header("Wager row")]
        [SerializeField] PressableButton dealButton;
        [SerializeField] PressableButton halveButton;
        [SerializeField] PressableButton doubleWagerButton;
        [SerializeField] TMP_Text wagerText;
        [SerializeField] TMP_Text balanceText;

        [Header("Moves")]
        [SerializeField] PressableButton hitButton;
        [SerializeField] PressableButton standButton;
        [SerializeField] PressableButton doubleButton;
        [SerializeField] PressableButton splitButton;
        [Tooltip("Hit, stand, double, split: dimmed while the move is not allowed.")]
        [SerializeField] TMP_Text[] moveLabels;
        [SerializeField] Color unavailable = new Color(1f, 1f, 1f, 0.2f);

        [Header("Layout, in table space")]
        [SerializeField] float feltHeight = 0.76f;
        [SerializeField] float playerZ = -0.02f;
        [SerializeField] float dealerZ = 0.22f;
        [SerializeField] float cardStep = 0.045f;
        [SerializeField] float splitOffset = 0.18f;
        [SerializeField] Transform shoe;
        [SerializeField] CardView[] cardPool;
        [SerializeField] TMP_Text[] handTexts;
        [SerializeField] TMP_Text dealerText;
        [SerializeField] Transform activeMarker;
        [SerializeField] float dealSeconds = 0.25f;

        [Header("Round result")]
        [SerializeField] TMP_Text resultText;
        [SerializeField] float popSeconds = 0.25f;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color winColor = new Color(0f, 0.9f, 0.1f);
        [SerializeField] Color loseColor = new Color(0.7f, 0.73f, 0.83f);

        IChipWallet wallet;
        BlackjackGame game;
        WagerAmount wager;
        BlackjackRound round;
        bool animating;
        Vector3 resultRestScale;
        readonly Dictionary<string, CardView> onTable = new Dictionary<string, CardView>();

        bool Between => round == null || round.State == BlackjackState.Settled;
        bool CanMove => round != null && round.IsPlayerTurn && !animating;

        // Controls are children of this prefab and die with it, so no unsubscribe.
        void Awake()
        {
            dealButton.Pressed.AddListener(Deal);
            halveButton.Pressed.AddListener(() => ChangeWager(w => w.Halve()));
            doubleWagerButton.Pressed.AddListener(() => ChangeWager(w => w.Double()));
            hitButton.Pressed.AddListener(() => Move(r => r.CanHit, r => r.Hit()));
            standButton.Pressed.AddListener(() => Move(r => r.CanStand, r => r.Stand()));
            doubleButton.Pressed.AddListener(() => Move(r => r.CanDouble, r => r.Double()));
            splitButton.Pressed.AddListener(() => Move(r => r.CanSplit, Split));
            foreach (var card in cardPool) card.gameObject.SetActive(false);
            resultRestScale = resultText.transform.localScale;
            ClearResult();
        }

        void Start()
        {
            wager = rules.NewWager();
            // Quietly, so the balance shows on arrival; Deal is what complains if there is still no wallet.
            Bind(FindWallet());
            Show();
        }

        // A hand left open settles before the session ends: every hand stands and the dealer plays.
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

        /// <summary>Built on the first deal, since the wallet may live in a scene that loads after this table.</summary>
        bool EnsureGame()
        {
            if (game != null) return true;
            Bind(FindWallet());
            if (wallet == null)
            {
                Debug.LogError("[Blackjack] No active IChipWallet in the loaded scenes; the table cannot take bets.", this);
                Refused("No chip wallet");
                return false;
            }
            // System. is needed: inside HighStakes, Environment is the HighStakes.Environment namespace.
            var seed = System.Environment.TickCount;
            Debug.Log($"[Blackjack] Seed {seed}");
            game = new BlackjackGame(new SeededRng(seed), new ChipWalletTableAdapter(wallet), rules.RuleSet);
            return true;
        }

        void ChangeWager(Action<WagerAmount> change)
        {
            if (wager == null || !Between || animating) return;
            change(wager);
            Show();
        }

        void Deal()
        {
            if (wager == null || !Between || animating) return;
            ClearResult();
            if (!EnsureGame()) return;
            if (!game.TryStart(wager.Value, out var started))
            {
                Refused("Not enough chips");
                return;
            }
            round = started;
            foreach (var card in onTable.Values) card.gameObject.SetActive(false);
            onTable.Clear();
            StartCoroutine(Lay());
        }

        void Move(Func<BlackjackRound, bool> allowed, Action<BlackjackRound> move)
        {
            if (!CanMove || !allowed(round)) return;
            move(round);
            StartCoroutine(Lay());
        }

        /// <summary>The second card of the pair becomes the first card of the right hand, so it slides rather than being dealt again.</summary>
        void Split(BlackjackRound r)
        {
            r.Split();
            if (onTable.TryGetValue(Key(0, 1), out var card))
            {
                onTable.Remove(Key(0, 1));
                onTable[Key(1, 0)] = card;
            }
        }

        static string Key(int hand, int index) => $"P{hand}-{index}";
        static string DealerKey(int index) => $"D{index}";

        /// <summary>Everything that should be on the table, in the order a dealer would put it there. A null face is the hole card.</summary>
        List<(string Key, Card? Face, Vector3 Position)> Layout()
        {
            var cards = new List<(string, Card?, Vector3)>();
            var hands = round.Hands;
            var dealerCards = round.IsPlayerTurn ? null : round.Dealer.Cards;
            var dealerCount = round.IsPlayerTurn ? 2 : dealerCards.Count;
            var most = dealerCount;
            foreach (var hand in hands) most = Math.Max(most, hand.Cards.Count);

            for (var i = 0; i < most; i++)
            {
                for (var h = 0; h < hands.Count; h++)
                    if (i < hands[h].Cards.Count)
                        cards.Add((Key(h, i), hands[h].Cards[i], CardAt(HandCentre(h), playerZ, i)));
                if (i < dealerCount)
                {
                    Card? face = dealerCards != null ? dealerCards[i]
                        : i == 0 ? round.DealerUpCard : (Card?)null;
                    cards.Add((DealerKey(i), face, CardAt(0f, dealerZ, i)));
                }
            }
            return cards;
        }

        float HandCentre(int hand) => round.Hands.Count == 1 ? 0f : hand == 0 ? -splitOffset : splitOffset;

        /// <summary>Cards fan to the right of the hand's spot, each a little above the last so they never fight.</summary>
        Vector3 CardAt(float centre, float z, int index) =>
            new Vector3(centre - cardStep / 2f + index * cardStep, feltHeight + 0.001f + index * 0.0012f, z);

        static void Face(CardView card, Card? face)
        {
            if (face.HasValue) card.ShowFace(face.Value);
            else card.ShowBack();
        }

        IEnumerator Lay()
        {
            animating = true;
            Show();
            foreach (var (key, face, position) in Layout())
            {
                if (onTable.TryGetValue(key, out var card))
                {
                    if (!Nullable.Equals(card.Shown, face)) yield return TurnOver(card, face);
                    if (card.transform.localPosition != position) yield return Fly(card.transform, card.transform.localPosition, position, dealSeconds * 0.6f);
                    continue;
                }

                card = Take();
                if (card == null) continue;
                Face(card, face);
                onTable[key] = card;
                yield return Fly(card.transform, shoe.localPosition, position, dealSeconds);
            }

            animating = false;
            Show();
            if (round.State == BlackjackState.Settled) yield return Announce();
        }

        CardView Take()
        {
            foreach (var card in cardPool)
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                    return card;
                }
            Debug.LogWarning("[Blackjack] Out of cards to show; the hand is still played in full.");
            return null;
        }

        IEnumerator Fly(Transform card, Vector3 from, Vector3 to, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = 1f - Mathf.Pow(1f - t / seconds, 3f);
                card.localPosition = Vector3.LerpUnclamped(from, to, k) + Vector3.up * (0.03f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            card.localPosition = to;
        }

        /// <summary>
        /// Lifts the card and rolls it half a turn about Z. The new face goes on while the card is edge-on, and the
        /// second quarter-turn comes back from the other side, so it lands flat and readable.
        /// </summary>
        IEnumerator TurnOver(CardView card, Card? face)
        {
            var t = card.transform;
            var rest = t.localPosition;
            var swapped = false;
            for (var e = 0f; e < dealSeconds; e += Time.deltaTime)
            {
                var k = e / dealSeconds;
                if (k >= 0.5f && !swapped)
                {
                    Face(card, face);
                    swapped = true;
                }
                t.localPosition = rest + Vector3.up * (0.04f * Mathf.Sin(k * Mathf.PI));
                t.localRotation = Quaternion.Euler(0f, 0f, swapped ? 180f * (k - 1f) : 180f * k);
                yield return null;
            }
            if (!swapped) Face(card, face);
            t.localPosition = rest;
            t.localRotation = Quaternion.identity;
        }

        IEnumerator Announce()
        {
            var net = round.Payout - round.Staked;
            Debug.Log($"[Blackjack] staked {round.Staked}, paid {round.Payout}, dealer {round.Dealer.Total}, " +
                      $"hands {string.Join(", ", HandSummaries())}");
            if (net > 0) Result($"+{net:N0}", winColor);
            else if (net == 0) Result("PUSH", idleColor);
            else Result($"-{-net:N0}", loseColor);
            yield return Pop();
        }

        IEnumerable<string> HandSummaries()
        {
            foreach (var hand in round.Hands) yield return $"{hand.Total} {hand.Result}";
        }

        void ClearResult() => Result("", idleColor);

        /// <summary>A round that never started, such as a wager the wallet could not cover.</summary>
        void Refused(string message) => Result(message, loseColor);

        void Result(string message, Color color)
        {
            resultText.text = message;
            resultText.color = color;
        }

        IEnumerator Pop()
        {
            var t = resultText.transform;
            for (var elapsed = 0f; elapsed < popSeconds; elapsed += Time.deltaTime)
            {
                t.localScale = resultRestScale * (1f + 0.5f * Mathf.Sin(elapsed / popSeconds * Mathf.PI));
                yield return null;
            }
            t.localScale = resultRestScale;
        }

        void ShowBalance() => balanceText.text = wallet == null ? "" : "CHIPS " + wallet.Balance.ToString("N0");

        void Show()
        {
            wagerText.text = wager.Value.ToString("N0");
            ShowBalance();
            Dim(0, CanMove && round.CanHit);
            Dim(1, CanMove && round.CanStand);
            Dim(2, CanMove && round.CanDouble);
            Dim(3, CanMove && round.CanSplit);

            for (var h = 0; h < handTexts.Length; h++)
            {
                var shown = round != null && h < round.Hands.Count;
                handTexts[h].gameObject.SetActive(shown);
                if (!shown) continue;
                var p = handTexts[h].transform.localPosition;
                handTexts[h].transform.localPosition = new Vector3(HandCentre(h), p.y, p.z);
                handTexts[h].text = Describe(round.Hands[h]);
            }

            dealerText.text = round == null ? ""
                : round.IsPlayerTurn ? (round.DealerUpCard.IsAce ? "11" : round.DealerUpCard.Points.ToString())
                : round.Dealer.IsBlackjack ? "BLACKJACK" : round.Dealer.IsBust ? $"{round.Dealer.Total} BUST" : round.Dealer.Total.ToString();

            var marking = round != null && round.IsPlayerTurn && round.Hands.Count > 1;
            activeMarker.gameObject.SetActive(marking);
            if (marking)
            {
                var m = activeMarker.localPosition;
                activeMarker.localPosition = new Vector3(HandCentre(round.ActiveIndex), m.y, m.z);
            }
        }

        static string Describe(BlackjackHand hand)
        {
            var total = hand.IsSoft && !hand.Finished ? $"{hand.Total - 10} / {hand.Total}" : hand.Total.ToString();
            switch (hand.Result)
            {
                case HandResult.Blackjack: return "BLACKJACK";
                case HandResult.Win: return $"{total}  WIN";
                case HandResult.Push: return $"{total}  PUSH";
                case HandResult.Lose: return $"{total}  LOSE";
                case HandResult.Bust: return $"{total}  BUST";
                default: return hand.Doubled ? $"{total}  x2" : total;
            }
        }

        void Dim(int move, bool available)
        {
            if (move < moveLabels.Length) moveLabels[move].color = available ? Color.white : unavailable;
        }
    }
}
