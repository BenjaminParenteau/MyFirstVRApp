using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.TableGames
{
    /// <summary>
    /// A thing the player can press. Wraps the XR toolkit so table code never references it directly. Fires on
    /// select (ray or direct interactor) and dips. Haptics come from the rig's controllers, which buzz on any select.
    /// A value button (a target step, a chip size) can repeat while held, faster the longer it is held.
    /// </summary>
    // ponytail: no sound, keyboard fallback or swipe (vrstake has them); add them when a table needs one.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PressableButton : MonoBehaviour
    {
        [SerializeField] float pressDip = 0.006f;
        [SerializeField] float pressSeconds = 0.08f;

        [Header("Hold to repeat")]
        [Tooltip("Keep pressing while held: after a delay, then faster and faster. For value steps, never for bets.")]
        [SerializeField] bool repeatWhileHeld;
        [SerializeField, Min(0f)] float repeatDelay = 0.4f;
        [SerializeField, Min(0.01f)] float repeatInterval = 0.15f;
        [SerializeField, Min(0.01f)] float fastestInterval = 0.04f;
        [SerializeField, Min(0f)] float rampSeconds = 1.5f;

        public UnityEvent Pressed = new UnityEvent();

        XRSimpleInteractable interactable;
        HoldRepeat hold;
        Coroutine dip;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            hold = new HoldRepeat(repeatDelay, repeatInterval, fastestInterval, rampSeconds);
            interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDestroy() => interactable.selectEntered.RemoveListener(OnSelectEntered);

        void OnSelectEntered(SelectEnterEventArgs _)
        {
            hold.Begin();
            Fire();
        }

        void Update()
        {
            if (!repeatWhileHeld || !interactable.isSelected) return;
            for (var due = hold.Advance(Time.deltaTime); due > 0; due--) Fire();
        }

        void Fire()
        {
            Pressed.Invoke();
            if (dip == null && isActiveAndEnabled) dip = StartCoroutine(Dip());
        }

        IEnumerator Dip()
        {
            var rest = transform.localPosition;
            transform.localPosition = rest - Vector3.up * pressDip;
            yield return new WaitForSeconds(pressSeconds);
            transform.localPosition = rest;
            dip = null;
        }
    }
}
