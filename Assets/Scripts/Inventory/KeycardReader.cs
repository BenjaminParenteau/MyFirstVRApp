using HighStakes.Environment;
using TMPro;
using UnityEngine;

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

        void OnEnable()
        {
            KeycardAccess.Granted += OnGranted;
            Refresh();
        }

        void OnDisable() => KeycardAccess.Granted -= OnGranted;

        void OnTriggerEnter(Collider other)
        {
            var card = other.GetComponentInParent<Keycard>();
            if (card != null && card.AccessId == accessId) KeycardAccess.Grant(accessId);
        }

        void OnGranted(string id)
        {
            if (id == accessId) Refresh();
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
