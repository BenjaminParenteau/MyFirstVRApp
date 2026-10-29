using System.Text;
using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.Inventory;
using TMPro;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>
    /// The checklist panel on the hub's VAULT door: the door stays locked until every <see cref="required"/> item is in
    /// the notebook (<see cref="ItemLedger"/>), so the vault is always the last room. Each line says where the item is
    /// found. Re-checks whenever an item is collected and when the hub loads again.
    /// </summary>
    public sealed class VaultClearance : MonoBehaviour
    {
        [SerializeField] DoorPortal door;
        [SerializeField] ItemData[] required;
        [Tooltip("Parallel to required: the room each item comes from, e.g. SECURITY.")]
        [SerializeField] string[] foundIn;
        [SerializeField] TextMeshPro checklist;
        [SerializeField] TextMeshPro status;
        [SerializeField] Renderer statusLight;
        [SerializeField] Material lockedMaterial;
        [SerializeField] Material openMaterial;

        void OnEnable()
        {
            ItemLedger.Added += OnAdded;
            Refresh();
        }

        void OnDisable() => ItemLedger.Added -= OnAdded;

        void OnAdded(ItemData _) => Refresh();

        void Refresh()
        {
            int have = 0;
            var lines = new StringBuilder();
            for (int i = 0; i < required.Length; i++)
            {
                bool found = ItemLedger.Has(required[i]);
                if (found) have++;
                string where = foundIn != null && i < foundIn.Length ? foundIn[i] : "";
                lines.Append(found ? "<color=#6BE38A>FOUND</color>    " : "<color=#E36B6B>MISSING</color>  ")
                    .Append(required[i] != null ? required[i].displayName.ToUpperInvariant() : "?")
                    .Append("  <size=70%>· ").Append(where).Append("</size>\n");
            }

            bool open = have == required.Length;
            if (door != null) door.SetLocked(!open);
            if (checklist != null) checklist.text = lines.ToString();
            if (status != null) status.text = open ? "CLEARANCE COMPLETE · ENTER" : $"LOCKED · {have}/{required.Length} INTEL";
            if (statusLight != null) statusLight.sharedMaterial = open ? openMaterial : lockedMaterial;
        }
    }
}
