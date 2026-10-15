using HighStakes.Dialogue;
using HighStakes.Security;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Inventory
{
    /// <summary>
    /// Lets the player get a keycard from an NPC: select the NPC (point and grip), and they stop, turn to the player,
    /// say a line and hold the card out in front of them to be taken. Once the card has opened its door they just
    /// wave the player through.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class KeycardGiver : MonoBehaviour
    {
        [SerializeField] Keycard cardPrefab;
        [SerializeField] SpeechBubble speech;
        [SerializeField] PatrolWalker patrol;
        [Tooltip("Seconds the NPC stops and faces the player when spoken to.")]
        [SerializeField] float attentionSeconds = 6f;
        [Tooltip("Where the card is held out: this far toward the player, at this height.")]
        [SerializeField] float handDistance = 0.45f;
        [SerializeField] float handHeight = 1.1f;
        [SerializeField, TextArea] string giveLine = "Welcome to The Meridian. Take this card, the VIP lounge is yours tonight.";
        [SerializeField, TextArea] string waitingLine = "Your card's right there. The lounge door is at the back.";
        [SerializeField, TextArea] string doneLine = "You're on the list. Enjoy the lounge.";

        XRSimpleInteractable interactable;
        Keycard handedOut;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSpokenTo);
        }

        void OnDestroy() => interactable.selectEntered.RemoveListener(OnSpokenTo);

        void OnSpokenTo(SelectEnterEventArgs _)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            var player = origin != null && origin.Camera != null ? origin.Camera.transform.position : transform.position + transform.forward;
            if (patrol != null) patrol.Pause(attentionSeconds, player);

            if (KeycardAccess.Has(cardPrefab.AccessId))
            {
                speech.Say(doneLine);
                return;
            }
            if (handedOut != null)
            {
                speech.Say(waitingLine);
                return;
            }

            var toPlayer = player - transform.position;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward;
            var at = transform.position + toPlayer * handDistance + Vector3.up * handHeight;
            // Face up, its long edge across the player's view and its top away from them, so it reads like a card
            // being offered across a counter.
            handedOut = Instantiate(cardPrefab, at, Quaternion.LookRotation(Vector3.down, -toPlayer), transform.parent);
            speech.Say(giveLine);
        }
    }
}
