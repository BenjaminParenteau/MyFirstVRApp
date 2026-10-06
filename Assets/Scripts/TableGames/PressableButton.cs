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
    /// </summary>
    // ponytail: no sound, keyboard fallback, hold-to-repeat or swipe (vrstake has all four); add them when a table needs one.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PressableButton : MonoBehaviour
    {
        [SerializeField] float pressDip = 0.006f;
        [SerializeField] float pressSeconds = 0.08f;

        public UnityEvent Pressed = new UnityEvent();

        XRSimpleInteractable interactable;
        Coroutine dip;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDestroy() => interactable.selectEntered.RemoveListener(OnSelectEntered);

        void OnSelectEntered(SelectEnterEventArgs _)
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
