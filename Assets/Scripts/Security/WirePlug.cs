using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Security
{
    /// <summary>A grabbable patch-cable end for one wire colour. <see cref="WirePanelPuzzle"/> reads <see cref="ColorIndex"/> once it is seated in a socket.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class WirePlug : MonoBehaviour
    {
        [SerializeField] int colorIndex;

        public int ColorIndex => colorIndex;
    }
}
