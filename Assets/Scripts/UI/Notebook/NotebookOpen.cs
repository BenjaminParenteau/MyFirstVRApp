using UnityEngine;
using UnityEngine.InputSystem;

namespace HighStakes.UI
{
    /// <summary>
    /// Opens and closes the notebook with a button: left Y on the controller, N on the keyboard (simulator). On open it
    /// appears just ahead of and below the eyes, page turned toward them, and follows the player while open: it trails
    /// smoothly as you walk, and only swings back in front once you turn your head well away, so glancing around
    /// doesn't drag it along. The cover swings a full 180 degrees about the centre spine into a two-page spread (blue
    /// screen on the left, notes on the right) and the page text appears once it is mostly open.
    /// Reuses the hologram's open/close easing from WristDisplayLogic. The player's view is never moved.
    /// </summary>
    public class NotebookOpen : MonoBehaviour
    {
        [Tooltip("Everything visible. Hidden while the notebook is closed.")]
        [SerializeField] private GameObject visuals;
        [Tooltip("Pivot on the spine. Its local Z rotation swings the cover; 0 = closed.")]
        [SerializeField] private Transform lid;
        [Tooltip("Text on both pages. Shown only once the cover is mostly open.")]
        [SerializeField] private GameObject[] pageContent;
        [Tooltip("Cover angle when fully open (degrees). 180 lays it flat as the left page.")]
        [SerializeField, Range(90f, 180f)] private float openAngle = 180f;
        [SerializeField] private float openSeconds = 0.45f;
        [SerializeField] private float closeSeconds = 0.3f;
        [Tooltip("The page text appears once the cover is this far open (0-1).")]
        [SerializeField, Range(0f, 1f)] private float textAt = 0.8f;
        [Tooltip("How far in front of the eyes the notebook opens (metres). The open spread is about 30 cm wide.")]
        [SerializeField] private float distance = 0.32f;
        [Tooltip("How far below eye height the notebook opens (metres).")]
        [SerializeField] private float drop = 0.1f;
        [Tooltip("How quickly the open notebook catches up as you move (higher = tighter, lower = floatier).")]
        [SerializeField, Min(0.1f)] private float followSpeed = 8f;
        [Tooltip("Turn your head more than this away from the notebook (degrees) and it swings back in front.")]
        [SerializeField, Range(5f, 90f)] private float recenterAngle = 30f;

        private const float RecenteredAngle = 2f;

        private InputAction toggleAction;
        private bool wantOpen;
        private float open;
        private Vector3 ahead;     // flat direction from the head the notebook sits along
        private bool recentering;

        /// <summary>True from the open press until the close press (the cover may still be moving).</summary>
        public bool IsOpen => wantOpen;

        private void Awake()
        {
            toggleAction = new InputAction("Toggle Notebook", InputActionType.Button);
            toggleAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            toggleAction.AddBinding("<Keyboard>/n");
            Apply();
        }

        private void OnEnable() => toggleAction.Enable();

        private void OnDisable() => toggleAction.Disable();

        private void OnDestroy() => toggleAction.Dispose();

        private void Update()
        {
            if (toggleAction.WasPressedThisFrame())
                SetOpen(!wantOpen);

            if (wantOpen)
                Follow(Time.unscaledDeltaTime);

            open = WristDisplayLogic.StepOpen(open, wantOpen, Time.unscaledDeltaTime, openSeconds, closeSeconds);
            Apply();
        }

        public void SetOpen(bool value)
        {
            if (value && !wantOpen)
            {
                var head = Head();
                if (head == null)
                    return; // no player camera yet
                ahead = FlatForward(head);
                recentering = false;
                Pose(head, out var position, out var rotation);
                transform.SetPositionAndRotation(position, rotation);
            }
            wantOpen = value;
        }

        // Trail the head: always keep up with where the player is, but only turn to face a new direction once they
        // have looked well away from the notebook.
        private void Follow(float deltaTime)
        {
            var head = Head();
            if (head == null)
                return;

            var facing = FlatForward(head);
            float off = Vector3.Angle(ahead, facing);
            if (off > recenterAngle)
                recentering = true;
            else if (off < RecenteredAngle)
                recentering = false;

            float t = 1f - Mathf.Exp(-followSpeed * deltaTime); // same feel at any frame rate
            if (recentering)
                ahead = Vector3.Slerp(ahead, facing, t).normalized;

            Pose(head, out var position, out var rotation);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, position, t),
                Quaternion.Slerp(transform.rotation, rotation, t));
        }

        // Ahead of and below the eyes along `ahead`. Notebook space: +Y out of the page (toward the eyes), -Z toward
        // the top of the page (up).
        private void Pose(Transform head, out Vector3 position, out Quaternion rotation)
        {
            position = head.position + ahead * distance + Vector3.down * drop;
            var toEyes = (head.position - position).normalized;
            var pageDown = Vector3.ProjectOnPlane(Vector3.down, toEyes).normalized;
            rotation = Quaternion.LookRotation(pageDown, toEyes);
        }

        private static Transform Head()
        {
            var eyes = WristRaiseVisibility.FindEyes();
            return eyes != null ? eyes.transform : null;
        }

        private static Vector3 FlatForward(Transform head)
        {
            var flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.ProjectOnPlane(head.up, Vector3.up); // looking straight up or down
            return flat.normalized;
        }

        private void Apply()
        {
            if (visuals != null)
                visuals.SetActive(wantOpen || open > 0f);
            // The closed cover lies over the right page (-X); it swings up and over the spine to the reader's left (+X)
            if (lid != null)
                lid.localRotation = Quaternion.Euler(0f, 0f, -openAngle * WristDisplayLogic.EaseOut(open));
            if (pageContent != null)
            {
                foreach (var content in pageContent)
                {
                    if (content != null)
                        content.SetActive(open >= textAt);
                }
            }
        }
    }
}
