using System.Collections.Generic;
using HighStakes.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace HighStakes.UI
{
    /// <summary>
    /// The notebook's items spread: the right page lists the player's pickups (keycards, clues, intel notes) as boxed
    /// rows, the left page ("ITEM") shows the selected one (icon, name, kind, note), and a keycard sits in the card slot
    /// while one is in the notebook. While the items spread is open,
    /// lets them step through with left X (B on the keyboard) and take the selected one into the hand with right B
    /// (F), which also closes the notebook. Selecting the item already in the hand puts it back. It observes every
    /// IInventoryEvents in the loaded scenes (the pickup prototype, a test mock, later Slot B's inventory) and
    /// rechecks when an area scene loads; a pickup reported twice is written once. It never changes inventory itself:
    /// taking and putting back go through <see cref="NotebookPickups"/>.
    /// </summary>
    public class NotebookDisplay : MonoBehaviour
    {
        [Tooltip("Right page: one row per item, top to bottom (NotebookLogic.MaxVisible of them).")]
        [SerializeField] private NotebookRow[] rows;
        [Tooltip("Right page: shown instead of the rows while the notebook is empty.")]
        [SerializeField] private TMP_Text emptyText;
        [Header("Left page: the selected item")]
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemKind;
        [SerializeField] private TMP_Text itemNote;
        [SerializeField] private TMP_Text hintText;
        [Tooltip("Icon in the portrait box for keycards.")]
        [SerializeField] private GameObject iconCard;
        [Tooltip("Icon in the portrait box for everything else.")]
        [SerializeField] private GameObject iconNote;
        [Tooltip("The keycard in the card slot. Shown while a keycard is in the notebook (not in the hand).")]
        [SerializeField] private GameObject slotCard;
        [SerializeField] private NotebookOpen book;
        [SerializeField] private NotebookPickups pickups;
        [Tooltip("Item controls only work while the items spread shows.")]
        [SerializeField] private NotebookPages pages;

        private readonly List<NotebookEntry> entries = new List<NotebookEntry>();
        private readonly List<IInventoryEvents> sources = new List<IInventoryEvents>();
        private InputAction nextAction;
        private InputAction takeAction;
        private int selected = -1;

        /// <summary>Entries written so far, oldest first.</summary>
        public IReadOnlyList<NotebookEntry> Entries => entries;

        private void Awake()
        {
            nextAction = new InputAction("Notebook Next", InputActionType.Button);
            nextAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            nextAction.AddBinding("<Keyboard>/b");
            takeAction = new InputAction("Notebook Take", InputActionType.Button);
            takeAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            takeAction.AddBinding("<Keyboard>/f");
        }

        private void OnEnable()
        {
            nextAction.Enable();
            takeAction.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (pickups != null) pickups.Changed += Refresh;
            BindAll();
            Refresh();
        }

        private void OnDisable()
        {
            nextAction.Disable();
            takeAction.Disable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (pickups != null) pickups.Changed -= Refresh;
            foreach (var source in sources)
                source.ItemAdded -= OnItemAdded;
            sources.Clear();
        }

        private void OnDestroy()
        {
            nextAction.Dispose();
            takeAction.Dispose();
        }

        private void Update()
        {
            if (book == null || !book.IsOpen || entries.Count == 0 || (pages != null && !pages.ShowingItems))
                return;

            if (nextAction.WasPressedThisFrame())
            {
                selected = NotebookLogic.NextIndex(entries.Count, selected);
                Refresh();
            }
            if (takeAction.WasPressedThisFrame() && pickups != null && selected >= 0)
            {
                string key = entries[selected].Id;
                if (NotebookLogic.SameKey(key, pickups.HeldKey))
                    pickups.PutAway();
                else if (pickups.TakeOut(key))
                    book.SetOpen(false);
                Refresh();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindAll();

        private void BindAll()
        {
            // Drop sources whose scene was unloaded, then add any new ones
            sources.RemoveAll(source => source is Object unityObject && unityObject == null);
            foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is IInventoryEvents source && !sources.Contains(source))
                {
                    sources.Add(source);
                    source.ItemAdded += OnItemAdded;
                }
            }
        }

        private void OnItemAdded(ItemData item)
        {
            if (item == null || !NotebookLogic.TryAdd(entries, item.id, item.displayName, item.description))
                return;
            selected = entries.Count - 1; // the newest pickup is ready to take
            Refresh();
        }

        private void Refresh()
        {
            string heldKey = pickups != null ? pickups.HeldKey : null;

            // Right page: the rows in view, the selected one inked with its circle filled
            int rowCount = rows != null ? rows.Length : 0;
            NotebookLogic.VisibleRange(entries.Count, selected, rowCount, out int first, out int last);
            for (int r = 0; r < rowCount; r++)
            {
                if (rows[r] == null) continue;
                int i = first + r;
                if (i < last)
                    rows[r].Show(NotebookLogic.RowLabel(entries[i], heldKey), i == selected, i == selected, false);
                else
                    rows[r].Hide();
            }
            if (emptyText != null)
                emptyText.gameObject.SetActive(entries.Count == 0);

            // Left page: the selected item
            bool any = selected >= 0 && selected < entries.Count;
            var entry = any ? entries[selected] : default;
            if (itemName != null) itemName.text = any ? entry.Title.ToUpperInvariant() : NotebookLogic.EmptyScreen;
            if (itemKind != null) itemKind.text = any ? NotebookLogic.KindOf(entry.Id) : "";
            if (itemNote != null) itemNote.text = any ? entry.Body : "Pick things up and they're written in here.";
            bool card = any && NotebookLogic.KindOf(entry.Id) == "KEYCARD";
            if (iconCard != null) iconCard.SetActive(card);
            if (iconNote != null) iconNote.SetActive(any && !card);
            if (hintText != null)
            {
                string hint = XRSettings.isDeviceActive ? "X  NEXT    B  TAKE / PUT BACK" : "B  NEXT    F  TAKE / PUT BACK";
                hintText.text = entries.Count > 0 ? hint : "";
            }

            if (slotCard != null)
                slotCard.SetActive(NotebookLogic.ShowsCard(entries, heldKey));
        }
    }
}
