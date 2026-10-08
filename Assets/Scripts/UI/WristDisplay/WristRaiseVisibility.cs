using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// Lights the watch face only when the player raises their wrist and looks at it (GDD: "raise your wrist for
    /// chips"), so nothing sits in view during play.
    /// </summary>
    public class WristRaiseVisibility : MonoBehaviour
    {
        [Tooltip("The part that turns on and off (screen and text).")]
        [SerializeField] private GameObject face;
        [Tooltip("Transform whose up axis (+Y) points out of the watch face.")]
        [SerializeField] private Transform faceNormal;
        [Tooltip("Face turns on when it is within this angle of pointing at the eyes.")]
        [SerializeField, Range(5f, 80f)] private float showAngle = 35f;
        [Tooltip("Face turns off past this angle. Larger than Show Angle so it doesn't flicker at the edge.")]
        [SerializeField, Range(5f, 90f)] private float hideAngle = 50f;
        [Tooltip("Face stays off if the wrist is farther than this from the eyes (metres).")]
        [SerializeField] private float maxDistance = 0.6f;
        [Tooltip("Testing only: keep the face on regardless of wrist angle.")]
        [SerializeField] private bool alwaysVisible;

        private Camera eyes;
        private bool shown = true;

        /// <summary>True while the player is looking at the raised watch. The hologram opens and closes with it.</summary>
        public bool IsShown => shown;

        private void Awake()
        {
            if (faceNormal == null)
                faceNormal = transform;
            SetShown(false);
        }

        private void OnValidate()
        {
            hideAngle = Mathf.Max(hideAngle, showAngle);
        }

        private void LateUpdate()
        {
            SetShown(alwaysVisible || IsBeingLookedAt());
        }

        private bool IsBeingLookedAt()
        {
            if (eyes == null || !eyes.isActiveAndEnabled)
                eyes = FindEyes();
            if (eyes == null)
                return false;

            Vector3 toEyes = eyes.transform.position - faceNormal.position;
            float distance = toEyes.magnitude;
            float facingDot = distance > 0.0001f ? Vector3.Dot(faceNormal.up, toEyes / distance) : 0f;

            return WristDisplayLogic.ShouldShowFace(shown, facingDot, distance,
                Mathf.Cos(showAngle * Mathf.Deg2Rad), Mathf.Cos(hideAngle * Mathf.Deg2Rad), maxDistance);
        }

        internal static Camera FindEyes()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            return origin != null && origin.Camera != null ? origin.Camera : Camera.main;
        }

        private void SetShown(bool value)
        {
            if (shown == value)
                return;
            shown = value;
            if (face != null)
                face.SetActive(value);
        }
    }
}
