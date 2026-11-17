using HighStakes.Environment;
using HighStakes.Environment.Audio;
using HighStakes.Security;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Inventory
{
    /// <summary>
    /// A card reader that keeps a door locked until a <see cref="Keycard"/> for its access id touches it (a trigger
    /// collider on this object). Access is remembered for the session, so the door stays open after the area reloads.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KeycardReader : MonoBehaviour
    {
        [SerializeField] string accessId = "VIP";
        [SerializeField] DoorPortal door;
        [SerializeField] Renderer statusLight;
        [SerializeField] Material lockedMaterial;
        [SerializeField] Material openMaterial;
        [SerializeField] TMP_Text sign;
        [SerializeField] string lockedText = "VIP  ·  KEYCARD REQUIRED";
        [SerializeField] string openText = "ACCESS GRANTED";

        Soundscape.LoopHandle beacon;
        float nextScan;
        bool hasStarted;

        void Start()
        {
            SecurityDoorAudio.Attach(door, accessId == "STAFF" ? "sec.door.heavy" : null);
            hasStarted = true;
            StartBeacon();
        }

        void StartBeacon()
        {
            if (!KeycardAccess.Has(accessId) && beacon == null)
                beacon = Soundscape.Loop("sec.reader.idle", transform);
        }

        void StopBeacon()
        {
            beacon?.Stop();
            beacon = null;
        }

        void OnEnable()
        {
            KeycardAccess.Granted += OnGranted;
            Refresh();
            if (hasStarted) StartBeacon();
        }

        void OnDisable()
        {
            KeycardAccess.Granted -= OnGranted;
            StopBeacon();
        }

        void OnTriggerEnter(Collider other)
        {
            if (KeycardAccess.Has(accessId)) return;
            var card = other.GetComponentInParent<Keycard>();
            if (card && card.AccessId == accessId)
            {
                KeycardAccess.Grant(accessId);
                return;
            }

            var hand = card ? null : other.GetComponentInParent<XRDirectInteractor>();
            if ((card || (hand && !hand.hasSelection)) && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.35f;
                Soundscape.PlayAt("sec.reader.deny", transform.position);
            }
        }

        void OnGranted(string id)
        {
            if (id != accessId) return;
            StopBeacon();
            Soundscape.PlayAt("sec.reader.ok", transform.position);
            if (door) Soundscape.PlayAt("sec.bolt.unlock", door.transform.position + Vector3.up);
            Refresh();
        }

        void Refresh()
        {
            bool open = KeycardAccess.Has(accessId);
            door.SetLocked(!open);
            statusLight.sharedMaterial = open ? openMaterial : lockedMaterial;
            if (sign != null) sign.text = open ? openText : lockedText;
        }
    }
}
