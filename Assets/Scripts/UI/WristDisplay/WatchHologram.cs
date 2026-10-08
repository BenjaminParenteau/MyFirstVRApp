using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// The blue holographic HUD the watch projects while the player looks at it: a panel that grows up out of the
    /// watch face on a beam of light, turned toward the eyes so it always reads, and folds back into the watch when
    /// they look away. Display only: it shows what the watch's <see cref="WristDisplay"/> found (bankroll, tier) plus
    /// chips won or lost since the watch first saw the wallet.
    /// </summary>
    [DefaultExecutionOrder(100)] // after WristRaiseVisibility has decided this frame
    public class WatchHologram : MonoBehaviour
    {
        [SerializeField] private WristRaiseVisibility visibility;
        [SerializeField] private WristDisplay display;
        [Tooltip("Centre of the watch face: the beam starts here.")]
        [SerializeField] private Transform emitter;
        [SerializeField] private Transform panel;
        [SerializeField] private Transform beam;
        [SerializeField] private TMP_Text bankrollText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private TMP_Text sessionText;
        [Tooltip("How far above the watch the panel floats when fully open (m).")]
        [SerializeField] private float height = 0.1f;
        [Tooltip("Half the panel's height (m), so the beam meets its bottom edge.")]
        [SerializeField] private float panelHalfHeight = 0.04f;
        [SerializeField] private float beamWidth = 0.012f;
        [SerializeField] private float openSeconds = 0.2f;
        [SerializeField] private float closeSeconds = 0.12f;

        private float open;
        private Vector3 panelScale;
        private Camera eyes;
        private int? sessionStart;
        private int lastBalance = int.MinValue;
        private string lastTier;

        private void Awake()
        {
            panelScale = panel.localScale;
            SetVisible(false);
        }

        private void LateUpdate()
        {
            open = WristDisplayLogic.StepOpen(open, visibility.IsShown, Time.unscaledDeltaTime, openSeconds, closeSeconds);
            SetVisible(open > 0f);
            if (open <= 0f)
                return;

            if (eyes == null || !eyes.isActiveAndEnabled)
                eyes = WristRaiseVisibility.FindEyes();
            if (eyes == null)
                return;

            Refresh();

            // Project straight up from the face, leaning a little toward the eyes, and grow while rising.
            float k = WristDisplayLogic.EaseOut(open);
            Vector3 from = emitter.position;
            Vector3 toEyes = Vector3.ProjectOnPlane(eyes.transform.position - from, Vector3.up).normalized;
            Vector3 centre = from + (Vector3.up * height + toEyes * 0.02f) * k;
            panel.position = centre;
            panel.rotation = Quaternion.LookRotation(centre - eyes.transform.position, Vector3.up);
            panel.localScale = panelScale * Mathf.Max(k, 0.001f);

            Vector3 to = centre - panel.up * (panelHalfHeight * k);
            Vector3 span = to - from;
            beam.position = from + span / 2f;
            beam.rotation = Quaternion.FromToRotation(Vector3.up, span);
            beam.localScale = new Vector3(beamWidth * k, span.magnitude / 2f, beamWidth * k); // Unity's cylinder is 2 m tall
        }

        private void Refresh()
        {
            var wallet = display != null ? display.Wallet : null;
            int balance = wallet != null ? wallet.Balance : int.MinValue;
            if (wallet != null && sessionStart == null)
                sessionStart = balance;
            if (balance != lastBalance)
            {
                lastBalance = balance;
                bankrollText.text = wallet != null ? WristDisplayLogic.FormatChips(balance) : WristDisplayLogic.NoValue;
                sessionText.text = wallet != null ? WristDisplayLogic.FormatNet(balance - sessionStart.Value) : WristDisplayLogic.NoValue;
            }

            var current = display != null && display.Tier != null ? display.Tier.Current : null;
            string tier = WristDisplayLogic.FormatTier(current != null ? current.displayName : null);
            if (tier != lastTier)
            {
                lastTier = tier;
                tierText.text = tier;
            }
        }

        private void SetVisible(bool visible)
        {
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (beam.gameObject.activeSelf != visible) beam.gameObject.SetActive(visible);
        }
    }
}
