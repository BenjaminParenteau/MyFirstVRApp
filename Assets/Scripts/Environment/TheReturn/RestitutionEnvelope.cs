using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// One pay envelope on the break-room rack, for one member of staff the syndicate skimmed. Its socket takes only a
    /// cash bundle: pushing one in by hand seals the envelope, lights the name in gold, chimes, buzzes the controller
    /// that placed it and reports to <see cref="RestitutionManager"/>, which warms the room a step toward sunrise.
    ///
    /// The bundle is locked in place rather than left grabbable, so restitution is one-way: what has been paid back
    /// cannot be taken back out again.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class RestitutionEnvelope : MonoBehaviour, IXRSelectFilter
    {
        [Header("Who this envelope belongs to")]
        [Tooltip("The person being paid back. Fills both labels.")]
        public string staffName = "Rosa";
        [Tooltip("Their job and shift, e.g. 'Cleaning, Night Shift'.")]
        public string staffRole = "Cleaning, Night Shift";

        [Header("Wiring")]
        [SerializeField] XRSocketInteractor socket;
        [Tooltip("Only objects with this tag can be put in the envelope.")]
        [SerializeField] string acceptedTag = "CashBundle";
        [Tooltip("Where the bundle is parented once it is in; defaults to this object.")]
        [SerializeField] Transform seat;

        [Header("Sealed look")]
        [SerializeField] Renderer envelopeRenderer;
        [Tooltip("Swapped in once the envelope is filled and sealed.")]
        [SerializeField] Material sealedMaterial;
        [Tooltip("The name in gold, switched on when the envelope is filled.")]
        [SerializeField] TextMeshPro nameLabel;
        [Tooltip("The name printed on the empty envelope, so the player can tell whose it is. Always visible.")]
        [SerializeField] TextMeshPro printedLabel;
        [SerializeField] Color labelColor = new Color32(0xFF, 0xC2, 0x7A, 0xFF);
        [Tooltip("Strength of the soft gold glow around the lit name.")]
        [SerializeField] float labelGlow = 0.4f;

        [Header("Feedback")]
        [SerializeField] AudioSource chime;
        [Range(0f, 1f)] [SerializeField] float hapticAmplitude = 0.5f;
        [SerializeField] float hapticSeconds = 0.12f;

        static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        static readonly int GlowPower = Shader.PropertyToID("_GlowPower");

        /// <summary>True once a bundle has been put in; an envelope is only ever filled once.</summary>
        public bool Filled { get; private set; }

        // --- IXRSelectFilter: the socket asks this before it accepts anything ---

        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
            => !Filled && IsCashBundle(interactable);

        void Awake()
        {
            if (socket == null) socket = GetComponent<XRSocketInteractor>();
            RefreshLabels();
            if (nameLabel != null) nameLabel.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (socket == null) return;
            socket.selectFilters.Add(this);
            socket.selectEntered.AddListener(OnBundlePlaced);
        }

        void OnDisable()
        {
            if (socket == null) return;
            socket.selectFilters.Remove(this);
            socket.selectEntered.RemoveListener(OnBundlePlaced);
        }

        void OnValidate() => RefreshLabels();

        /// <summary>Only a cash bundle counts. Compared as a string so an undefined tag cannot throw.</summary>
        bool IsCashBundle(IXRSelectInteractable interactable)
        {
            if (interactable?.transform == null) return false;
            return string.IsNullOrEmpty(acceptedTag) || interactable.transform.gameObject.tag == acceptedTag;
        }

        void OnBundlePlaced(SelectEnterEventArgs args)
        {
            if (Filled || !IsCashBundle(args.interactableObject)) return;
            Filled = true;

            LockInPlace(args.interactableObject.transform);
            Seal();
            Chime();
            Pulse(args.interactorObject);

            // The envelope takes nothing else now.
            if (socket != null) socket.socketActive = false;

            if (RestitutionManager.Instance != null) RestitutionManager.Instance.EnvelopeFilled();
            else Debug.LogWarning("[Return] No RestitutionManager in the scene; the room will not warm up.", this);
        }

        /// <summary>
        /// Pins the bundle in the envelope: parked on the seat, physics off, then no longer grabbable. Done in that
        /// order because disabling the interactable makes the socket let go, and a live Rigidbody would drop to the floor.
        /// </summary>
        void LockInPlace(Transform bundle)
        {
            if (bundle == null) return;

            var body = bundle.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }

            bundle.SetParent(seat != null ? seat : transform, true);

            var grab = bundle.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
        }

        /// <summary>The sealed envelope and the name lit in gold: this person has been made whole.</summary>
        void Seal()
        {
            if (envelopeRenderer != null && sealedMaterial != null) envelopeRenderer.sharedMaterial = sealedMaterial;
            if (nameLabel == null) return;

            nameLabel.gameObject.SetActive(true);
            nameLabel.color = labelColor;

            // TMP's distance-field shaders carry a glow channel; skipped quietly on a material that has none.
            var mat = nameLabel.fontMaterial;
            if (mat == null) return;
            if (mat.HasProperty(GlowColor)) mat.SetColor(GlowColor, labelColor);
            if (mat.HasProperty(GlowPower)) mat.SetFloat(GlowPower, labelGlow);
        }

        void Chime()
        {
            if (chime != null) chime.Play();
        }

        /// <summary>A short pulse on the hand that placed the bundle, so the seal is felt as well as heard.</summary>
        void Pulse(IXRSelectInteractor interactor)
        {
            if (interactor is XRBaseInputInteractor input && input.SendHapticImpulse(hapticAmplitude, hapticSeconds)) return;

            // Fallback for an interactor without its own haptic channel: go straight to that hand's device.
            if (interactor == null) return;
            var node = interactor.handedness == InteractorHandedness.Left ? XRNode.LeftHand : XRNode.RightHand;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, hapticAmplitude, hapticSeconds);
        }

        /// <summary>Both labels read from <see cref="staffName"/> and <see cref="staffRole"/>, so the names live in one place.</summary>
        public void RefreshLabels()
        {
            string text = $"<b>{staffName}</b>\n<size=70%>{staffRole}</size>";
            if (nameLabel != null) nameLabel.text = text;
            if (printedLabel != null) printedLabel.text = text;
        }
    }
}
