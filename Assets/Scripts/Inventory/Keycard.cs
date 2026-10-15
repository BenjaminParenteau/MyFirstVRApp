using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Inventory
{
    /// <summary>
    /// A grabbable keycard for one access id. It hangs where it was handed over until the player takes it; once let go
    /// it falls like any card. Touching it to a <see cref="KeycardReader"/> for the same id opens that door.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class Keycard : MonoBehaviour
    {
        [SerializeField] string accessId = "VIP";

        public string AccessId => accessId;

        XRGrabInteractable grab;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDestroy() => grab.selectExited.RemoveListener(OnReleased);

        // The grab restores the body's float-in-place settings on release; from then on it is a normal card.
        void OnReleased(SelectExitEventArgs _)
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}
