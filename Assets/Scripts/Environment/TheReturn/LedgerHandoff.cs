using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// The evidence tray on the investigator's desk. Its socket takes only the Black Ledger: placing it there hands the
    /// record to someone with the authority to act on it, instead of keeping it as leverage. The ledger's red title
    /// fades to white as it is handed over — the thing that was held over people becomes evidence in the open — and the
    /// ending is set to Restoration.
    /// </summary>
    public sealed class LedgerHandoff : MonoBehaviour, IXRSelectFilter
    {
        [Header("Wiring")]
        [SerializeField] XRSocketInteractor tray;
        [Tooltip("Only the object with this tag can be handed over.")]
        [SerializeField] string acceptedTag = "BlackLedger";
        [Tooltip("Where the ledger is parented once it is in the tray; defaults to this object.")]
        [SerializeField] Transform seat;
        [SerializeField] ReturnEnding ending;

        [Header("Title fade")]
        [SerializeField] Color fromColor = new Color32(0xC4, 0x2B, 0x22, 0xFF);
        [SerializeField] Color toColor = Color.white;
        [SerializeField] float fadeSeconds = 2f;

        [Header("Feedback")]
        [SerializeField] AudioSource chime;

        /// <summary>True once the ledger has been put in the tray.</summary>
        public bool HandedOver { get; private set; }

        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
            => !HandedOver && IsLedger(interactable);

        void Awake()
        {
            if (tray == null) tray = GetComponent<XRSocketInteractor>();
        }

        void OnEnable()
        {
            if (tray == null) return;
            tray.selectFilters.Add(this);
            tray.selectEntered.AddListener(OnLedgerPlaced);
        }

        void OnDisable()
        {
            if (tray == null) return;
            tray.selectFilters.Remove(this);
            tray.selectEntered.RemoveListener(OnLedgerPlaced);
        }

        bool IsLedger(IXRSelectInteractable interactable)
        {
            if (interactable?.transform == null) return false;
            return string.IsNullOrEmpty(acceptedTag) || interactable.transform.gameObject.tag == acceptedTag;
        }

        void OnLedgerPlaced(SelectEnterEventArgs args)
        {
            if (HandedOver || !IsLedger(args.interactableObject)) return;
            HandedOver = true;

            var ledgerTransform = args.interactableObject.transform;
            LockInPlace(ledgerTransform);
            if (tray != null) tray.socketActive = false;
            if (chime != null) chime.Play();

            var title = ledgerTransform.GetComponent<BlackLedger>()?.Title
                        ?? ledgerTransform.GetComponentInChildren<TextMeshPro>();
            if (title != null) StartCoroutine(FadeTitle(title));

            var target = ending != null ? ending : ReturnEnding.Instance;
            if (target != null) target.SetRestoration();
            else Debug.LogWarning("[Return] No ReturnEnding in the scene; the ending card will not show.", this);
        }

        /// <summary>Red to white over <see cref="fadeSeconds"/>: the record stops being a threat and becomes testimony.</summary>
        IEnumerator FadeTitle(TextMeshPro title)
        {
            // Start from whatever it is now, so this still reads correctly if the colour was tuned in the Inspector.
            Color start = title.color == toColor ? fromColor : title.color;
            for (var t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                title.color = Color.Lerp(start, toColor, Mathf.SmoothStep(0f, 1f, t / fadeSeconds));
                yield return null;
            }
            title.color = toColor;
        }

        /// <summary>
        /// Pins the ledger in the tray: parked on the seat, physics off, then no longer grabbable. That order matters,
        /// because disabling the interactable makes the socket let go and a live Rigidbody would fall off the desk.
        /// </summary>
        void LockInPlace(Transform ledger)
        {
            if (ledger == null) return;

            var body = ledger.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }

            ledger.SetParent(seat != null ? seat : transform, true);

            var grab = ledger.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
        }
    }
}
