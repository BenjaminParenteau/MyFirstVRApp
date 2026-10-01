using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// Attaches this object to the player's left controller at runtime. The watch is never placed inside the rig
    /// in a scene (StyleGuide §5c): it finds the one active XROrigin and parents itself to it, so it works both in
    /// MVP_UI's solo-test rig and in MVP_Main's rig. Visuals stay hidden until it is attached.
    /// </summary>
    public class WristAttach : MonoBehaviour
    {
        [Tooltip("Child of the XR Origin to attach to.")]
        [SerializeField] private string controllerName = "Left Controller";
        [Tooltip("Hidden until attached, so the watch never floats at the world origin.")]
        [SerializeField] private GameObject visuals;
        [Tooltip("Position on the controller. Default sits on the back of the left wrist. Tune in Play mode, then copy back.")]
        [SerializeField] private Vector3 localPosition = new Vector3(-0.035f, -0.01f, -0.09f);
        [Tooltip("Rotation on the controller. Default turns the face (+Y) toward the back of the left hand (-X).")]
        [SerializeField] private Vector3 localEulerAngles = new Vector3(0f, 0f, 90f);

        private const float SearchInterval = 1f;

        private Transform controller;
        private float nextSearchTime;

        public bool IsAttached => controller != null && transform.parent == controller;

        private void Awake()
        {
            if (visuals != null)
                visuals.SetActive(false);
        }

        private void Update()
        {
            if (IsAttached || Time.unscaledTime < nextSearchTime)
                return;

            nextSearchTime = Time.unscaledTime + SearchInterval;
            TryAttach();
        }

        private void TryAttach()
        {
            // Only active rigs: a slice scene's solo-test rig is switched off inside MVP_Main
            var origin = FindFirstObjectByType<XROrigin>();
            controller = origin != null ? FindChild(origin.transform, controllerName) : null;

            if (controller == null)
            {
                if (visuals != null) visuals.SetActive(false);
                return;
            }

            transform.SetParent(controller, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
            if (visuals != null) visuals.SetActive(true);
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name == childName)
                    return t;
            }
            return null;
        }
    }
}
