using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Security
{
    /// <summary>
    /// A labelled CCTV tape from the camera room's archive. <see cref="TapeDeckPuzzle"/> plays its
    /// <see cref="Footage"/> when it is seated in the deck. It lies where it was archived until picked up; once let
    /// go it falls like any object (as Inventory/Keycard does).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class VideoTape : MonoBehaviour
    {
        [Tooltip("Camera and time, as written on the tape, e.g. CAM 04 · 02:40.")]
        [SerializeField] string label = "CAM 04 · 02:40";
        [Tooltip("What the deck's monitor shows while this tape plays.")]
        [TextArea] [SerializeField] string footage;

        public string Label => label;
        public string Footage => footage;

        XRGrabInteractable grab;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDestroy() => grab.selectExited.RemoveListener(OnReleased);

        void OnReleased(SelectExitEventArgs _)
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}
