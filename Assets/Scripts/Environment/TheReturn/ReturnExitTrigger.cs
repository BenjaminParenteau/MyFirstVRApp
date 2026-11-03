using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// The volume just inside the break-room door. Walking into it is the player leaving, and that is what decides the
    /// ending: everyone paid back and the ledger handed over leaves the sunrise up (Restoration); anything less and the
    /// player is walking out with money that is not theirs (Kept).
    ///
    /// The door itself stays inert — nothing is loaded and no portal is used, so this works with the scene played on
    /// its own. Like <see cref="DoorPortal"/>, the player's head is what is tested rather than a collision, because the
    /// XR rig walks through trigger volumes without reporting anything (and it has to work for simulator WASD too).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ReturnExitTrigger : MonoBehaviour
    {
        [SerializeField] ReturnEnding ending;
        [Tooltip("Counts the filled envelopes. Found at runtime if left empty.")]
        [SerializeField] RestitutionManager lighting;
        [Tooltip("Whether the ledger made it into the investigator's tray.")]
        [SerializeField] LedgerHandoff handoff;

        BoxCollider zone;
        XROrigin origin;
        float nextSearch;
        bool fired;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void Update()
        {
            if (fired) return;

            var head = Head();
            if (head == null || !zone.bounds.Contains(head.position)) return;

            fired = true;
            Decide();
        }

        void Decide()
        {
            var target = ending != null ? ending : ReturnEnding.Instance;
            if (target == null)
            {
                Debug.LogWarning("[Return] No ReturnEnding in the scene; the ending cannot be shown.", this);
                return;
            }

            var meter = lighting != null ? lighting : RestitutionManager.Instance;
            bool everyonePaid = meter != null && meter.AllReturned;
            bool ledgerHandedOver = handoff != null && handoff.HandedOver;

            if (everyonePaid && ledgerHandedOver) target.SetRestoration();
            else target.SetKept();
        }

        Transform Head()
        {
            if (origin != null && origin.Camera != null) return origin.Camera.transform;
            if (Time.unscaledTime < nextSearch) return null;
            nextSearch = Time.unscaledTime + 0.5f;
            origin = FindFirstObjectByType<XROrigin>();
            return origin != null && origin.Camera != null ? origin.Camera.transform : null;
        }

        void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
