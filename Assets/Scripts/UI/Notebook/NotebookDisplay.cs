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
    /// The notebook's two pages: the right page lists the player's pickups (keycards, clues, intel notes), the blue screen
    /// on the left page shows the selected one, and a keycard sits in the card slot while one is in the notebook. While
    /// the notebook is open,
    /// lets them step through with left X (B on the keyboard) and take the selected one into the hand with right B
    /// (F), which also closes the notebook. Selecting the item already in the hand puts it back. It observes every
    /// IInventoryEvents in the loaded scenes (the pickup prototype, a test mock, later Slot B's inventory) and
    /// rechecks when an area scene loads; a pickup reported twice is written once. It never changes inventory itself:
    /// taking and putting back go through <see cref="NotebookPickups"/>.
    /// </summary>
    public class NotebookDisplay : MonoBehaviour
    {
        [Tooltip("Right page: the list of notes.")]
        [SerializeField] private TMP_Text pageText;
        [Tooltip("Left page: the blue screen showing the selected item.")]
        [SerializeField] private TMP_Text screenText;
        [Tooltip("The keycard in the card slot. Shown while a keycard is in the notebook (not in the hand).")]
        [SerializeField] private GameObject slotCard;
        [SerializeField] private NotebookOpen book;
        [SerializeField] private NotebookPickups pickups;

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
            if (book == null || !book.IsOpen || entries.Count == 0)
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

            if (pageText != null)
            {
                string page = "<b>NOTES</b>\n" + NotebookLogic.FormatPage(entries, selected, heldKey, showSelectedNote: false);
                if (entries.Count > 0)
                    page += XRSettings.isDeviceActive ? "\n\n<size=70%>X  NEXT     B  TAKE / PUT BACK</size>" : "\n\n<size=70%>B  NEXT     F  TAKE / PUT BACK</size>";
                pageText.text = page;
            }
            if (screenText != null)
            {
                // Name large, note smaller underneath
                string screen = NotebookLogic.FormatScreen(entries, selected);
                int split = screen.IndexOf('\n');
                string name = split < 0 ? screen : screen.Substring(0, split);
                string note = split < 0 ? "" : "\n" + screen.Substring(split + 1);
                screenText.text = "<size=60%>ITEM</size>\n<size=130%>" + name + "</size>" + note;
            }
            if (slotCard != null)
                slotCard.SetActive(NotebookLogic.ShowsCard(entries, heldKey));
        }
    }
}
