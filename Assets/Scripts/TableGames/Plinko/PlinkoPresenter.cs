using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighStakes.Core;
using TMPro;
using UnityEngine;

namespace HighStakes.TableGames.Plinko
{
    /// <summary>
    /// Scene glue between the Plinko table and a PlinkoGame. The board is drawn inside the dealer hologram, under its
    /// text: the pegs as one mesh, the buckets with their multipliers, and the balls. A ball follows the path the game
    /// already chose, peg by peg; physics never decides a bucket. Several balls can be in flight, and each one pays
    /// when it lands.
    /// </summary>
    public sealed class PlinkoPresenter : MonoBehaviour
    {
        [SerializeField] PlinkoRules rules;

        [Header("Wager row")]
        [SerializeField] PressableButton dropButton;
        [SerializeField] PressableButton halveButton;
        [SerializeField] PressableButton doubleButton;
        [SerializeField] TMP_Text wagerText;
        [SerializeField] TMP_Text balanceText;

        [Header("Board choice")]
        [SerializeField] PressableButton fewerRowsButton;
        [SerializeField] PressableButton moreRowsButton;
        [SerializeField] TMP_Text fewerRowsLabel;
        [SerializeField] TMP_Text moreRowsLabel;
        [SerializeField] TMP_Text rowsText;
        [Tooltip("Low, medium, high, in PlinkoRisk order.")]
        [SerializeField] PressableButton[] riskButtons;
        [SerializeField] TMP_Text[] riskLabels;
        [Tooltip("Risks not chosen, and row steps past the end.")]
        [SerializeField] Color unselected = new Color(1f, 1f, 1f, 0.3f);

        [Header("Board, in hologram board space")]
        [Tooltip("The hologram's Board: X across, Y up from its bottom edge, facing the player at -Z.")]
        [SerializeField] Transform board;
        [Tooltip("Rebuilt as one mesh whenever the row count changes, so every peg and bucket bar is one draw call.")]
        [SerializeField] MeshFilter pegs;
        [Tooltip("One per bucket of the largest board, left to right; the unused ones hide.")]
        [SerializeField] TMP_Text[] bucketLabels;
        [Tooltip("Copied for each ball in flight.")]
        [SerializeField] Transform ballTemplate;
        [SerializeField] float boardWidth = 1.1f;
        [Tooltip("Where balls enter, just under the hologram's text.")]
        [SerializeField] float pegTop = 0.72f;
        [Tooltip("The middle of the bucket row.")]
        [SerializeField] float bucketY = 0.06f;
        [SerializeField] float secondsPerRow = 0.09f;
        [SerializeField] float bounceHeight = 0.35f;
        [Tooltip("Buckets paying 10x or more.")]
        [SerializeField] Color highColor = new Color(1f, 0.8f, 0.25f);
        [Tooltip("Buckets paying 1x or more.")]
        [SerializeField] Color winColor = Color.green;
        [SerializeField] Color loseColor = Color.red;

        [Header("Round result")]
        [SerializeField] TableHologram hologram;

        IChipWallet wallet;
        PlinkoGame game;
        PlinkoBet bet;
        Mesh pegMesh;
        readonly List<PlinkoDrop> falling = new List<PlinkoDrop>();
        readonly Stack<Transform> idleBalls = new Stack<Transform>();
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        // Controls are children of this prefab and die with it, so no unsubscribe.
        void Awake()
        {
            dropButton.Pressed.AddListener(Drop);
            halveButton.Pressed.AddListener(() => ChangeBet(b => b.HalveWager()));
            doubleButton.Pressed.AddListener(() => ChangeBet(b => b.DoubleWager()));
            // The board cannot change shape under a ball that is still falling.
            fewerRowsButton.Pressed.AddListener(() => ChangeBoard(b => b.StepRows(-1)));
            moreRowsButton.Pressed.AddListener(() => ChangeBoard(b => b.StepRows(+1)));
            for (var i = 0; i < riskButtons.Length; i++)
            {
                var risk = (PlinkoRisk)i;
                riskButtons[i].Pressed.AddListener(() => ChangeBoard(b => b.Risk = risk));
            }
            ballTemplate.gameObject.SetActive(false);
            pegMesh = new Mesh { name = "PlinkoPegs" };
            pegs.sharedMesh = pegMesh;
        }

        void Start()
        {
            bet = rules.NewBet();
            // Quietly, so the balance shows on arrival; Drop is what complains if there is still no wallet.
            Bind(FindWallet());
            ShowBet();
        }

        // A ball left falling (scene unload, quit) still pays: its bucket was drawn when the wager was taken.
        void OnApplicationQuit() => SettleAll();

        void OnDestroy()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            SettleAll();
            Destroy(pegMesh);
        }

        void SettleAll()
        {
            foreach (var drop in falling) drop.Settle();
            falling.Clear();
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

        void OnBalanceChanged(int _) => ShowBalance();

        /// <summary>Built on the first drop, since the wallet may live in a scene that loads after this table.</summary>
        bool EnsureGame()
        {
            if (wallet == null) Bind(FindWallet());
            if (wallet == null)
            {
                Debug.LogError("[Plinko] No active IChipWallet in the loaded scenes; the table cannot take bets.", this);
                hologram.Refused("No chip wallet");
                return false;
            }
            if (game == null)
            {
                // System. is needed: inside HighStakes, Environment is the HighStakes.Environment namespace.
                var seed = System.Environment.TickCount;
                Debug.Log($"[Plinko] Seed {seed}");
                game = new PlinkoGame(new SeededRng(seed), new ChipWalletTableAdapter(wallet), rules.Paytable());
            }
            return true;
        }

        void ChangeBet(Action<PlinkoBet> change)
        {
            if (bet == null) return;
            change(bet);
            ShowBet();
        }

        void ChangeBoard(Action<PlinkoBet> change)
        {
            if (falling.Count == 0) ChangeBet(change);
        }

        void Drop()
        {
            if (bet == null || !EnsureGame()) return;
            // The wager and board are captured in the drop: the player can change the wager while the ball falls.
            if (!game.TryDrop(bet.Wager, bet.Rows, bet.Risk, out var drop))
            {
                hologram.Refused("Not enough chips");
                return;
            }
            StartCoroutine(Fall(drop, bet.Risk));
        }

        IEnumerator Fall(PlinkoDrop drop, PlinkoRisk risk)
        {
            falling.Add(drop);
            var ball = idleBalls.Count > 0 ? idleBalls.Pop() : Instantiate(ballTemplate, board);
            var rows = drop.Path.Count;
            var d = Spacing(rows);
            var h = RowHeight(rows);
            ball.localScale = Vector3.one * Mathf.Min(d, h) * 0.5f;
            ball.gameObject.SetActive(true);

            // Over the middle peg of the top row, then after row r (rights - (r + 1) / 2) spacings from the centre, just
            // above a peg of the next row; after the last row, straight down into the bucket.
            var from = new Vector3(0f, pegTop - 0.5f * h, -0.01f);
            var rights = 0;
            for (var r = 0; r <= rows; r++)
            {
                var landing = r == rows;
                if (!landing && drop.Path[r]) rights++;
                var to = landing
                    ? new Vector3(from.x, bucketY, -0.01f)
                    : new Vector3((rights - (r + 1) / 2f) * d, pegTop - (r + 1.5f) * h, -0.01f);
                for (var t = 0f; t < secondsPerRow; t += Time.deltaTime)
                {
                    var k = t / secondsPerRow;
                    var bounce = landing ? 0f : h * bounceHeight * 4f * k * (1f - k);
                    ball.localPosition = Vector3.Lerp(from, to, k) + Vector3.up * bounce;
                    yield return null;
                }
                ball.localPosition = from = to;
            }

            ball.gameObject.SetActive(false);
            idleBalls.Push(ball);
            falling.Remove(drop);
            drop.Settle(); // the return arrives with the ball
            Announce(drop, risk);
            StartCoroutine(Pop(bucketLabels[drop.Bucket].transform));
        }

        IEnumerator Pop(Transform label)
        {
            const float seconds = 0.35f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                label.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(t / seconds * Mathf.PI));
                yield return null;
            }
            label.localScale = Vector3.one;
        }

        void Announce(PlinkoDrop drop, PlinkoRisk risk)
        {
            Debug.Log($"[Plinko] {drop.Path.Count} rows {risk}: bucket {drop.Bucket} at {Format(drop.Multiplier)}, wager {drop.Wager}, payout {drop.Payout}");
            var title = Format(drop.Multiplier);
            var detail = $"BET ${drop.Wager:N0}  ·  PAYS ${drop.Payout:N0}";
            if (drop.Net > 0) hologram.Won(drop.Net, title, detail);
            else if (drop.Net == 0) hologram.Push(detail, title);
            else hologram.Lost(-drop.Net, detail, title);
        }

        void ShowBet()
        {
            wagerText.text = bet.Wager.ToString("N0");
            rowsText.text = bet.Rows + " ROWS";
            fewerRowsLabel.color = bet.CanRemoveRows ? Color.white : unselected;
            moreRowsLabel.color = bet.CanAddRows ? Color.white : unselected;
            for (var i = 0; i < riskLabels.Length; i++) riskLabels[i].color = (PlinkoRisk)i == bet.Risk ? Color.white : unselected;
            Layout(bet.Rows, bet.Multipliers);
            ShowBalance();
        }

        void ShowBalance() => balanceText.text = wallet == null ? "" : "CHIPS " + wallet.Balance.ToString("N0");

        float Spacing(int rows) => boardWidth / (rows + 1);
        float RowHeight(int rows) => (pegTop - bucketY) / (rows + 1);

        /// <summary>
        /// Row r has r + 3 pegs and the bottom row spans the board; bucket k sits under the gap the ball leaves through
        /// after k rights, with a bar under its multiplier. Pegs and bars go into one mesh.
        /// </summary>
        void Layout(int rows, long[] multipliers)
        {
            var d = Spacing(rows);
            var h = RowHeight(rows);
            vertices.Clear();
            triangles.Clear();

            var peg = Mathf.Min(d, h) * 0.14f;
            for (var r = 0; r < rows; r++)
            for (var i = 0; i < r + 3; i++)
                Diamond(new Vector3((i - (r + 2) / 2f) * d, pegTop - (r + 1) * h, -0.002f), peg);

            var fontSize = Mathf.Min(0.35f, d * 2.6f);
            for (var k = 0; k < bucketLabels.Length; k++)
            {
                var label = bucketLabels[k];
                var used = k <= rows;
                label.gameObject.SetActive(used);
                if (!used) continue;
                var x = (k - rows / 2f) * d;
                Bar(new Vector3(x, bucketY - h * 0.45f, -0.002f), d * 0.85f, 0.004f);
                label.transform.localPosition = new Vector3(x, bucketY, -0.003f);
                label.transform.localScale = Vector3.one;
                label.rectTransform.sizeDelta = new Vector2(d, h * 0.8f);
                label.fontSize = fontSize;
                label.text = Format(multipliers[k]);
                label.color = multipliers[k] >= 1000 ? highColor : multipliers[k] >= 100 ? winColor : loseColor;
            }

            pegMesh.Clear();
            pegMesh.SetVertices(vertices);
            pegMesh.SetTriangles(triangles, 0);
            pegMesh.RecalculateBounds();
        }

        void Diamond(Vector3 centre, float size)
        {
            var first = vertices.Count;
            vertices.Add(centre + new Vector3(0f, size, 0f));
            vertices.Add(centre + new Vector3(size, 0f, 0f));
            vertices.Add(centre + new Vector3(0f, -size, 0f));
            vertices.Add(centre + new Vector3(-size, 0f, 0f));
            Quad(first);
        }

        void Bar(Vector3 centre, float width, float height)
        {
            var first = vertices.Count;
            vertices.Add(centre + new Vector3(-width / 2f, height / 2f, 0f));
            vertices.Add(centre + new Vector3(width / 2f, height / 2f, 0f));
            vertices.Add(centre + new Vector3(width / 2f, -height / 2f, 0f));
            vertices.Add(centre + new Vector3(-width / 2f, -height / 2f, 0f));
            Quad(first);
        }

        // Two triangles over the four corners just added, in order round the edge.
        void Quad(int first)
        {
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        static string Format(long hundredths) =>
            hundredths >= 10000 ? $"{hundredths / 100:N0}×" : $"{hundredths / 100}.{hundredths % 100:00}×";
    }
}
