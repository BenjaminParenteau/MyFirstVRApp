using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Security
{
    /// <summary>Reads hand motion without letting a grabbable drag the mechanism off its bearings.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class VaultRotaryHandle : MonoBehaviour
    {
        public VaultKeyLock owner;
        public bool keyCylinder;
        XRSimpleInteractable interactable;
        IXRSelectInteractor hand;
        Vector2 previous;
        bool tracking;
        public bool Held => hand != null;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(Grab);
            interactable.selectExited.AddListener(Release);
        }
        void OnDestroy()
        {
            if (!interactable) return;
            interactable.selectEntered.RemoveListener(Grab);interactable.selectExited.RemoveListener(Release);
        }
        void Grab(SelectEnterEventArgs args)
        {
            hand = args.interactorObject; tracking = false;
            // A normal ray click or direct grab completes the short assisted turn; wrist precision is optional.
            if (owner) { if (keyCylinder) owner.UseKey(); else owner.UseWheel(); }
        }
        void Release(SelectExitEventArgs _) { hand = null; tracking = false; }
        void Update()
        {
            if (hand == null || !owner) return;
            var attach = hand.GetAttachTransform(interactable);
            if (!attach) return;
            // The fixed mechanism root supplies the reference frame; turning the visual never feeds back into input.
            Vector3 vector = keyCylinder ? owner.transform.InverseTransformDirection(attach.up)
                : owner.transform.InverseTransformPoint(attach.position) - new Vector3(0, 1.4f, -.36f);
            var current = new Vector2(vector.x, vector.y);
            if (current.sqrMagnitude < (keyCylinder ? .04f : .0064f)) { tracking = false;return; }
            if (tracking)
            {
                float delta = -Vector2.SignedAngle(previous, current);
                // Clockwise, viewed from the -Z approach, is negative local Z rotation.
                delta = Mathf.Clamp(delta, -180 * Time.deltaTime, 180 * Time.deltaTime);
                if (keyCylinder) owner.TurnKey(Mathf.Abs(delta)); else owner.TurnWheel(Mathf.Abs(delta));
            }
            previous = current;tracking = true;
        }
    }
}
