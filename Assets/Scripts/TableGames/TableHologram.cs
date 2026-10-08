using System.Collections;
using HighStakes.UI;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.TableGames
{
    /// <summary>
    /// A blue holographic board as wide as the table, standing on its dealer edge where the dealer would be (the same
    /// look as the watch HUD). It rises out of a projector strip along that edge when the player walks up to the table
    /// and sinks back into it when they leave. The table's presenter tells it what to show; a win counts up from +$0 to
    /// the amount won.
    /// </summary>
    public class TableHologram : MonoBehaviour
    {
        [Tooltip("The board, pivoted at the middle of its bottom edge so it grows up from the table.")]
        [SerializeField] Transform panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color winColor = Color.green;
        [SerializeField] Color loseColor = Color.red;
        [Tooltip("The board opens when the player's head is this close to the table centre (m, measured flat).")]
        [SerializeField] float showDistance = 1.8f;
        [Tooltip("And closes past this distance. Larger than Show Distance so it doesn't flicker at the edge.")]
        [SerializeField] float hideDistance = 2.2f;
        [SerializeField] float openSeconds = 0.3f;
        [SerializeField] float closeSeconds = 0.2f;
        [SerializeField] float countSeconds = 1.2f;

        Vector3 panelScale;
        float open;
        bool near;
        float pulse = 1f;
        XROrigin player;

        void Awake()
        {
            panelScale = panel.localScale;
            Idle();
            Place(0f);
        }

        void LateUpdate()
        {
            if (player == null || !player.isActiveAndEnabled) player = FindFirstObjectByType<XROrigin>();
            if (player != null && player.Camera != null)
            {
                Vector3 offset = player.Camera.transform.position - transform.position;
                float distance = new Vector2(offset.x, offset.z).magnitude;
                near = near ? distance <= hideDistance : distance <= showDistance;
            }
            open = WristDisplayLogic.StepOpen(open, near, Time.deltaTime, openSeconds, closeSeconds);
            Place(WristDisplayLogic.EaseOut(open));
        }

        // Raises the board out of the projector strip: full width, height growing from the bottom edge.
        void Place(float k)
        {
            bool visible = k > 0f;
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (!visible) return;
            panel.localScale = new Vector3(panelScale.x * pulse, panelScale.y * Mathf.Max(k, 0.001f) * pulse, panelScale.z);
        }

        /// <summary>Between rounds: the table's name and a prompt.</summary>
        public void Idle(string detail = "PRESS DEAL TO PLAY")
        {
            StopCount();
            Show("BLACKJACK", "", idleColor, detail);
        }

        /// <summary>A round that never started, such as a wager the wallet could not cover.</summary>
        public void Refused(string message)
        {
            StopCount();
            Show("BLACKJACK", "", idleColor, message, loseColor);
        }

        /// <summary>Counts up from +$0 to the chips won, pulsing, then holds the total. A new round cuts it short.</summary>
        public void Won(long net, string title, string detail)
        {
            StopCount();
            StartCoroutine(CountWin(net, title, detail));
        }

        IEnumerator CountWin(long net, string title, string detail)
        {
            Show(title, "+$0", winColor, detail);
            for (var t = 0f; t < countSeconds; t += Time.deltaTime)
            {
                amountText.text = "+$" + CountUp.Value(net, t, countSeconds).ToString("N0");
                pulse = 1f + 0.04f * Mathf.Sin(t * Mathf.PI * 6f);
                yield return null;
            }
            amountText.text = "+$" + net.ToString("N0");
            // A last pop when the count lands.
            for (var t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                pulse = 1f + 0.12f * Mathf.Sin(t / 0.25f * Mathf.PI);
                yield return null;
            }
            pulse = 1f;
        }

        public void Lost(long lost, string detail)
        {
            StopCount();
            Show("DEALER WINS", "-$" + lost.ToString("N0"), loseColor, detail);
        }

        public void Push(string detail)
        {
            StopCount();
            Show("PUSH", "$0", idleColor, detail);
        }

        // Every message replaces whatever was showing, including a win still counting.
        void StopCount()
        {
            StopAllCoroutines();
            pulse = 1f;
        }

        void Show(string title, string amount, Color amountColor, string detail, Color? detailColor = null)
        {
            titleText.text = title;
            amountText.text = amount;
            amountText.color = amountColor;
            detailText.text = detail;
            detailText.color = detailColor ?? idleColor;
        }
    }
}
