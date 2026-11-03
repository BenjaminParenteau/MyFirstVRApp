using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// The Guide's last question, asked once every envelope has been filled: "Why did you really take this job?" A
    /// world-space panel appears an arm's length in front of the player at eye height with two answers, pointed at and
    /// clicked with the rig's UI ray.
    ///
    /// Neither answer is scored and neither is punished — the point is that the player says out loud what their motive
    /// was, which is where repentance starts. Both answers lead to the same closing beat and the same end screen; only
    /// the line the Guide says back is different.
    /// </summary>
    public sealed class GuideQuestion : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Root of the world-space canvas. Hidden until the question is asked.")]
        [SerializeField] GameObject panel;
        [Tooltip("Faded out before the end screen.")]
        [SerializeField] CanvasGroup fade;
        [SerializeField] TextMeshProUGUI speakerLabel;
        [SerializeField] TextMeshProUGUI lineLabel;
        [Tooltip("Holds the two answer buttons; switched off once one is picked.")]
        [SerializeField] GameObject answerRow;
        [SerializeField] Button moneyButton;
        [SerializeField] Button peopleButton;

        [Header("Voice")]
        [SerializeField] AudioSource voice;
        [Tooltip("Optional: the Guide's recording of the question. Silent if left empty.")]
        [SerializeField] AudioClip questionClip;
        [Tooltip("Optional: played under the closing line.")]
        [SerializeField] AudioClip closingClip;

        [Header("Lines")]
        [SerializeField] string speaker = "THE GUIDE";
        [TextArea] [SerializeField] string question = "Why did you really take this job?";
        [SerializeField] string moneyAnswer = "For the money";
        [SerializeField] string peopleAnswer = "Because people were being hurt";
        [TextArea] [SerializeField] string moneyClosing =
            "For the money. I believe you.\nAnd you still gave it all back. That counts for something.";
        [TextArea] [SerializeField] string peopleClosing =
            "Then you already knew what tonight was for.\nEight people will be paid who weren't going to be.";

        [Header("Placement and timing")]
        [Tooltip("How far in front of the player's eyes the panel appears (m).")]
        [SerializeField] float distance = 1f;
        [Tooltip("How long the closing line stays up before the end screen.")]
        [SerializeField] float closingSeconds = 4f;
        [SerializeField] float fadeSeconds = 0.6f;

        [Header("Hand-off")]
        [SerializeField] ReturnEnding ending;

        /// <summary>Which answer the player gave, or empty until they do.</summary>
        public string Answer { get; private set; } = string.Empty;

        bool asked;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (moneyButton != null) moneyButton.onClick.AddListener(() => Choose(moneyAnswer, moneyClosing));
            if (peopleButton != null) peopleButton.onClick.AddListener(() => Choose(peopleAnswer, peopleClosing));
        }

        /// <summary>
        /// Hooked to <see cref="RestitutionManager.OnAllReturned"/>. Puts the panel an arm's length in front of the
        /// player's eyes, turned to face them, and asks the question.
        /// </summary>
        public void Ask()
        {
            if (asked) return;
            asked = true;

            PlaceInFrontOfPlayer();

            if (speakerLabel != null) speakerLabel.text = speaker;
            if (lineLabel != null) lineLabel.text = question;
            if (answerRow != null) answerRow.SetActive(true);
            if (fade != null) fade.alpha = 1f;
            if (panel != null) panel.SetActive(true);

            Play(questionClip);
        }

        /// <summary>
        /// Placed once, where the player is standing when the last envelope is sealed, rather than followed every
        /// frame: UI that chases the head is a reliable way to make people feel sick in VR.
        /// </summary>
        void PlaceInFrontOfPlayer()
        {
            var head = Head();
            if (head == null) return;

            // Flattened forward, so the panel is upright at eye height even if the player is looking up or down.
            var forward = head.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            var root = panel != null ? panel.transform : transform;
            root.position = head.position + forward * distance;
            root.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        void Choose(string answer, string closing)
        {
            if (!string.IsNullOrEmpty(Answer)) return; // answered already
            Answer = answer;

            if (answerRow != null) answerRow.SetActive(false);
            if (lineLabel != null) lineLabel.text = closing;
            Play(closingClip);

            StartCoroutine(ToEndScreen());
        }

        IEnumerator ToEndScreen()
        {
            yield return new WaitForSeconds(closingSeconds);

            if (fade != null)
            {
                for (var t = 0f; t < fadeSeconds; t += Time.deltaTime)
                {
                    fade.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fadeSeconds);
                    yield return null;
                }
                fade.alpha = 0f;
            }
            if (panel != null) panel.SetActive(false);

            var target = ending != null ? ending : ReturnEnding.Instance;
            if (target != null) target.SetRestoration();
            else Debug.LogWarning("[Return] No ReturnEnding in the scene; the end screen will not show.", this);
        }

        /// <summary>The one rig's camera, the same way the rest of the project finds it; Camera.main as a fallback.</summary>
        static Transform Head()
        {
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null && origin.Camera != null) return origin.Camera.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        void Play(AudioClip clip)
        {
            if (voice == null || clip == null) return;
            voice.clip = clip;
            voice.Play();
        }
    }
}
