using HighStakes.Environment;
using HighStakes.TableGames;
using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// The watch hologram's TRAVEL column: one row per area, pressed to jump straight back to somewhere already
    /// reached on foot instead of walking the hallways again.
    /// <br/>
    /// Display and input only — every rule lives in <see cref="FastTravel"/> (Slot D), which this asks for each row's
    /// <see cref="TravelState"/> and calls to travel. The rows are children of the HUD panel, so they inherit the
    /// panel's open/close and the way <see cref="WatchHologram"/> turns it toward the eyes; nothing here places
    /// anything per frame.
    /// </summary>
    [DefaultExecutionOrder(110)] // after WatchHologram has placed and shown the panel this frame
    public class FastTravelWatch : MonoBehaviour
    {
        [Tooltip("Root of the travel column, hidden when fast travel is switched off or unavailable.")]
        [SerializeField] private GameObject column;
        [Tooltip("One row per FastTravelLogic destination, same order.")]
        [SerializeField] private TMP_Text[] rowLabels;
        [Tooltip("One per row, same order: the plate behind the label, tinted by state.")]
        [SerializeField] private Renderer[] rowPlates;
        [Tooltip("One per row, same order.")]
        [SerializeField] private PressableButton[] rowButtons;
        [Tooltip("One line under the rows: why the last press did nothing.")]
        [SerializeField] private TMP_Text reply;
        [SerializeField] private float replySeconds = 2f;

        [Header("Row colours")]
        [SerializeField] private Color ready = new Color(0.45f, 0.85f, 1f);
        [SerializeField] private Color here = new Color(1f, 0.78f, 0.35f);
        [SerializeField] private Color locked = new Color(0.35f, 0.5f, 0.65f);
        [SerializeField] private Color unknown = new Color(0.25f, 0.35f, 0.45f);

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock block;
        private float replyUntil;
        private TravelState[] shown;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            // -1 is no TravelState, so every row draws once on the first frame it is shown.
            shown = new TravelState[rowLabels != null ? rowLabels.Length : 0];
            for (int i = 0; i < shown.Length; i++) shown[i] = (TravelState)(-1);

            for (int i = 0; rowButtons != null && i < rowButtons.Length; i++)
            {
                int row = i; // captured per row, not per loop
                if (rowButtons[i] != null) rowButtons[i].Pressed.AddListener(() => Press(row));
            }
            if (reply != null) reply.text = string.Empty;
        }

        private void LateUpdate()
        {
            var travel = FastTravel.Instance;
            bool show = travel != null && travel.Available;
            if (column != null && column.activeSelf != show) column.SetActive(show);
            if (!show || rowLabels == null) return;

            for (int i = 0; i < shown.Length && i < travel.DestinationCount; i++)
            {
                var state = travel.StateOf(i);
                if (shown[i] == state) continue; // only redraw a row when it actually changes
                shown[i] = state;
                Draw(i, travel.DestinationAt(i), state);
            }

            if (reply != null && reply.text.Length > 0 && Time.unscaledTime > replyUntil)
                reply.text = string.Empty;
        }

        /// <summary>
        /// A row's label and tint. Somewhere never visited stays a "?" rather than naming it, so the watch doesn't
        /// hand the player a list of rooms they have not found yet — same rule as the notebook's map.
        /// </summary>
        private void Draw(int row, TravelDestination destination, TravelState state)
        {
            if (rowLabels[row] != null)
            {
                rowLabels[row].text = state == TravelState.Unknown ? "?" : destination.Label;
                rowLabels[row].color = Tint(state);
            }
            if (rowPlates != null && row < rowPlates.Length && rowPlates[row] != null)
            {
                var tint = Tint(state);
                block.SetColor(BaseColor, new Color(tint.r, tint.g, tint.b, state == TravelState.Ready ? 0.3f : 0.12f));
                rowPlates[row].SetPropertyBlock(block);
            }
        }

        private Color Tint(TravelState state)
        {
            switch (state)
            {
                case TravelState.Ready: return ready;
                case TravelState.Here: return here;
                case TravelState.Locked: return locked;
                default: return unknown;
            }
        }

        private void Press(int row)
        {
            var travel = FastTravel.Instance;
            if (travel == null) return;
            if (travel.TravelTo(row)) return;

            // Refused: say why rather than letting the press look broken.
            if (reply == null) return;
            reply.text = travel.LastRefusal ?? string.Empty;
            reply.color = locked;
            replyUntil = Time.unscaledTime + replySeconds;
        }
    }
}
