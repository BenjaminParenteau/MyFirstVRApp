using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// Which way The Return ended, and the world-space card that says so. Two outcomes are possible and the player
    /// chooses freely between them: pay everyone back and hand the ledger to the investigator
    /// (<see cref="Ending.Restoration"/>), or walk out of the door with the money (<see cref="Ending.Kept"/>).
    ///
    /// Nothing here punishes or lectures: the room itself answers. Restoration leaves the sunrise up; keeping the money
    /// drains the warmth back out and shows the night skyline through the window. The ending is shown on a panel in
    /// this scene — no scene is ever loaded, so this works with the scene played on its own.
    /// </summary>
    public sealed class ReturnEnding : MonoBehaviour
    {
        public enum Ending { Undecided, Restoration, Kept }

        [Header("Ending card")]
        [Tooltip("World-space panel holding the ending text. Hidden until the ending is decided.")]
        [SerializeField] GameObject panel;
        [SerializeField] TextMeshPro headline;
        [SerializeField] TextMeshPro body;

        [Header("Scene response")]
        [Tooltip("Drains the room back to vault blue on the Kept ending.")]
        [SerializeField] RestitutionManager lighting;
        [Tooltip("City lights behind the window, shown on the Kept ending.")]
        [SerializeField] GameObject nightSkyline;

        [Header("Text")]
        [SerializeField] string restorationHeadline = "THE RETURN";
        [TextArea] [SerializeField] string restorationBody =
            "Every envelope filled. The ledger in the investigator's tray.\n"
            + "What was taken has been given back, and the room is light.";
        [SerializeField] string keptHeadline = "KEPT";
        [TextArea] [SerializeField] string keptBody =
            "You left with the money, and the night stayed dark.\n"
            + "The names on the rack are still owed.";

        [Header("Events")]
        public UnityEvent OnRestoration;
        public UnityEvent OnKept;

        public static ReturnEnding Instance { get; private set; }

        /// <summary>The ending so far. Decided once and then left alone.</summary>
        public Ending Current { get; private set; } = Ending.Undecided;

        public bool Decided => Current != Ending.Undecided;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[Return] A second ReturnEnding is active. The newest one wins.", this);
            Instance = this;

            if (panel != null) panel.SetActive(false);
            if (nightSkyline != null) nightSkyline.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// The ledger is in the investigator's tray: truth and accountability over leverage. Walking out is what has
        /// the last word, though, so this never overrides a Kept ending already taken.
        /// </summary>
        public void SetRestoration()
        {
            if (Current != Ending.Undecided) return;
            Current = Ending.Restoration;
            Show(restorationHeadline, restorationBody);
            OnRestoration?.Invoke();
        }

        /// <summary>
        /// The player walked out without finishing: the room loses the light their earlier envelopes earned and the
        /// window goes back to night. Shown, not punished.
        ///
        /// Leaving the room is the decision that counts, so this overrides an earlier Restoration — handing the ledger
        /// over and then walking out with seven of the eight envelopes still empty is the Kept ending.
        /// </summary>
        public void SetKept()
        {
            if (Current == Ending.Kept) return;
            Current = Ending.Kept;
            if (lighting != null) lighting.HoldDark();
            if (nightSkyline != null) nightSkyline.SetActive(true);
            Show(keptHeadline, keptBody);
            OnKept?.Invoke();
        }

        void Show(string head, string text)
        {
            if (headline != null) headline.text = head;
            if (body != null) body.text = text;
            if (panel != null) panel.SetActive(true);
        }
    }
}
