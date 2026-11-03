using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// The Black Ledger the player carries out of the vault: the record of what the syndicate skimmed and from whom.
    /// <see cref="LedgerHandoff"/> reads <see cref="Title"/> so it can fade the red title to white when the ledger is
    /// handed over to the investigator.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class BlackLedger : MonoBehaviour
    {
        [Tooltip("The ledger's title, printed in red while it is still leverage.")]
        [SerializeField] TextMeshPro title;

        public TextMeshPro Title => title;
    }
}
