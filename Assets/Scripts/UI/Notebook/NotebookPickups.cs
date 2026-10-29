using System;
using System.Collections.Generic;
using HighStakes.Core;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.UI
{
    /// <summary>
    /// Prototype inventory (IInventoryEvents) until Slot B's real one exists. Look at a collectable and a prompt
    /// appears above it; press right B (F on the keyboard) and it goes into the notebook: the object is hidden and kept
    /// in MVP_Main's scene, so it survives area swaps. The notebook can then take it back out into the right hand
    /// (<see cref="TakeOut"/>); pressing the same button with the notebook closed puts the held item away again.
    /// A collectable is anything with a <see cref="PickupItem"/>, or with a component named in
    /// <see cref="byComponent"/> (Slot B's "Keycard"), matched by name so UI code doesn't depend on Inventory code.
    /// </summary>
    public class NotebookPickups : MonoBehaviour, IInventoryEvents
    {
        [Serializable]
        private struct ComponentItem
        {
            [Tooltip("Component type name on the collectable, e.g. Keycard.")]
            public string componentName;
            public ItemData item;
        }

        private sealed class Stored
        {
            public ItemData Item;
            public GameObject Object;
            public string Key => NotebookLogic.KeyFor(Item.id, Item.displayName);
        }

        [Tooltip("Objects without a PickupItem that still count, by component name.")]
        [SerializeField] private ComponentItem[] byComponent;
        [SerializeField] private NotebookOpen book;
        [Tooltip("Prompt shown above the looked-at collectable.")]
        [SerializeField] private Transform prompt;
        [SerializeField] private TMP_Text promptText;
        [Tooltip("Controller the taken item is held in (a child of the XR Origin).")]
        [SerializeField] private string handName = "Right Controller";
        [Tooltip("How far away a collectable can be looked at and picked up (metres).")]
        [SerializeField] private float reach = 2.5f;
        [Tooltip("Radius of the look check, so a small card doesn't need pixel-perfect aim (metres).")]
        [SerializeField] private float aimRadius = 0.1f;

        private readonly List<Stored> stored = new List<Stored>();
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private InputAction useAction;
        private Stored held;
        private XRBaseInteractor heldBy;
        private int takenFrame = -1;
        private GameObject target;
        private ItemData targetItem;

        public event Action<ItemData> ItemAdded;

        /// <summary>Raised when an item goes into or comes out of the hand.</summary>
        public event Action Changed;

        /// <summary>Key of the item in the hand (see NotebookLogic.KeyFor), or null.</summary>
        public string HeldKey => held?.Key;

        private void Awake()
        {
            useAction = new InputAction("Notebook Use", InputActionType.Button);
            useAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            useAction.AddBinding("<Keyboard>/f");
            ShowPrompt(false);
        }

        private void OnEnable()
        {
            useAction.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            useAction.Disable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // An area scene comes back fresh after a door trip: hide the collectables already in the notebook, so the same
        // note isn't lying there again.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var pickup in root.GetComponentsInChildren<PickupItem>(true))
                {
                    if (pickup.Item != null && collected.Contains(NotebookLogic.KeyFor(pickup.Item.id, pickup.Item.displayName)))
                        pickup.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy() => useAction.Dispose();

        private void Update()
        {
            var eyes = WristRaiseVisibility.FindEyes();
            bool bookOpen = book != null && book.IsOpen;
            FindTarget(eyes != null && !bookOpen ? eyes.transform : null);
            UpdatePrompt(eyes);

            // While the notebook is open this button means "take", which NotebookDisplay handles; and the press that
            // took an item out must not put it straight back.
            if (!useAction.WasPressedThisFrame() || bookOpen || Time.frameCount == takenFrame)
                return;

            if (held != null)
                PutAway();
            else if (target != null)
                PickUp(target, targetItem);
        }

        // ------------------------------------------------------------- looking

        private void FindTarget(Transform eyes)
        {
            target = null;
            targetItem = null;
            if (eyes == null)
                return;

            int count = Physics.SphereCastNonAlloc(eyes.position, aimRadius, eyes.forward, hits, reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, HitDistance.Instance);
            var origin = eyes.GetComponentInParent<XROrigin>();
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i].collider.transform;
                if (origin != null && hit.IsChildOf(origin.transform))
                    continue; // the player's own controllers and body
                if (held != null && held.Object != null && hit.IsChildOf(held.Object.transform))
                    continue; // what's already in the hand
                if (Collectable(hit, out target, out targetItem))
                    return;
                return; // something else is in the way
            }
        }

        private bool Collectable(Transform hit, out GameObject owner, out ItemData item)
        {
            for (var t = hit; t != null; t = t.parent)
            {
                var pickup = t.GetComponent<PickupItem>();
                if (pickup != null && pickup.Item != null)
                {
                    owner = t.gameObject;
                    item = pickup.Item;
                    return true;
                }
                if (byComponent == null)
                    continue;
                foreach (var entry in byComponent)
                {
                    if (entry.item != null && !string.IsNullOrEmpty(entry.componentName) && t.GetComponent(entry.componentName) != null)
                    {
                        owner = t.gameObject;
                        item = entry.item;
                        return true;
                    }
                }
            }
            owner = null;
            item = null;
            return false;
        }

        private void UpdatePrompt(Camera eyes)
        {
            if (target == null || eyes == null)
            {
                ShowPrompt(false);
                return;
            }

            // Just above the top of the object, turned to face the eyes
            var top = target.transform.position;
            foreach (var r in target.GetComponentsInChildren<Renderer>())
                top.y = Mathf.Max(top.y, r.bounds.max.y);
            prompt.position = top + Vector3.up * 0.08f;
            prompt.rotation = Quaternion.LookRotation(prompt.position - eyes.transform.position);
            if (promptText != null)
                promptText.text = (XRSettings.isDeviceActive ? "B" : "F") + "  PICK UP  " + targetItem.displayName.ToUpperInvariant();
            ShowPrompt(true);
        }

        private void ShowPrompt(bool show)
        {
            if (prompt != null && prompt.gameObject.activeSelf != show)
                prompt.gameObject.SetActive(show);
        }

        // ------------------------------------------------------------- storing

        private void PickUp(GameObject obj, ItemData item)
        {
            DropFromAnyHand(obj);
            // Keep it with the notebook in MVP_Main, so it isn't destroyed when the area it came from unloads
            obj.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
            obj.SetActive(false);

            var entry = new Stored { Item = item, Object = obj };
            stored.RemoveAll(s => NotebookLogic.SameKey(s.Key, entry.Key) && s.Object == null);
            if (!stored.Exists(s => s.Object == obj))
                stored.Add(entry);
            collected.Add(entry.Key);
            ItemAdded?.Invoke(item);
            Changed?.Invoke();
        }

        /// <summary>
        /// Takes the stored item with this key out into the right hand, putting away whatever was held. Returns false
        /// if it can't be held (only seen in a mock, or its object is gone, or no hand was found).
        /// </summary>
        public bool TakeOut(string key)
        {
            var entry = stored.Find(s => NotebookLogic.SameKey(s.Key, key) && s.Object != null);
            var interactable = entry?.Object.GetComponent<IXRSelectInteractable>();
            var hand = FindHand();
            if (interactable == null || hand == null)
                return false;

            if (held != null)
                PutAway();

            entry.Object.SetActive(true);
            var attach = hand.GetAttachTransform(interactable);
            entry.Object.transform.SetPositionAndRotation(attach.position, attach.rotation);
            // Held until put away: no need to keep the grip pressed, and it can't be dropped and lost
            hand.StartManualInteraction(interactable);

            held = entry;
            heldBy = hand;
            takenFrame = Time.frameCount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Puts the item in the hand back into the notebook.</summary>
        public void PutAway()
        {
            if (held == null)
                return;

            if (held.Object != null)
            {
                if (heldBy != null && heldBy.isPerformingManualInteraction)
                    heldBy.EndManualInteraction();
                DropFromAnyHand(held.Object);
                held.Object.SetActive(false);
            }
            held = null;
            heldBy = null;
            Changed?.Invoke();
        }

        private static void DropFromAnyHand(GameObject obj)
        {
            var interactable = obj.GetComponent<XRBaseInteractable>();
            if (interactable != null && interactable.isSelected && interactable.interactionManager != null)
                interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)interactable);
        }

        private XRBaseInteractor FindHand()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null)
                return null;

            XRBaseInteractor fallback = null;
            foreach (var t in origin.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != handName)
                    continue;
                foreach (var interactor in t.GetComponentsInChildren<XRBaseInputInteractor>())
                {
                    if (interactor is NearFarInteractor)
                        return interactor;
                    if (fallback == null)
                        fallback = interactor;
                }
            }
            return fallback;
        }

        private sealed class HitDistance : IComparer<RaycastHit>
        {
            public static readonly HitDistance Instance = new HitDistance();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
