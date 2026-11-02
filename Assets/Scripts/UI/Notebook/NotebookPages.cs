using UnityEngine;
using UnityEngine.InputSystem;

namespace HighStakes.UI
{
    /// <summary>
    /// Flips the open notebook between its spreads: 0 = items (item list and selected item), 1 = case file (map and
    /// leads), 2 = settings (comfort and game). Press a side tab on the book's outer edge (poke or point and pull the trigger) to jump to its
    /// spread, or cycle with left thumbstick click on the controller, U on the keyboard (M is the simulator's menu button). Each
    /// spread is a left-page and a right-page object; only the current pair is active. NotebookOpen still hides all
    /// page content while the cover moves, so this only chooses which spread shows once it is open.
    /// </summary>
    public class NotebookPages : MonoBehaviour
    {
        [Tooltip("Left-page content per spread, on the cover's underside.")]
        [SerializeField] private GameObject[] leftSpreads;
        [Tooltip("Right-page content per spread. Same order as Left Spreads.")]
        [SerializeField] private GameObject[] rightSpreads;
        [SerializeField] private NotebookOpen book;

        [Header("Side tabs")]
        [Tooltip("One tab per spread down the book's outer edge; pressing one (a PressableButton) calls ShowSpread.")]
        [SerializeField] private Transform[] tabs;
        [Tooltip("The coloured face of each tab, same order as Tabs.")]
        [SerializeField] private Renderer[] tabFaces;
        [SerializeField] private Material tabMaterial;
        [SerializeField] private Material activeTabMaterial;
        [Tooltip("How much further the open spread's tab sticks out (metres, in the tabs' parent space).")]
        [SerializeField] private Vector3 activeTabOffset = new Vector3(0.006f, 0f, 0f);
        [Tooltip("The open spread's icon on the bookmark ribbon, one per spread.")]
        [SerializeField] private GameObject[] ribbonIcons;

        private InputAction flipAction;
        private Vector3[] tabRest;

        /// <summary>The spread showing: 0 = items, 1 = case file, 2 = settings. Kept while the notebook is closed.</summary>
        public int Current { get; private set; }

        /// <summary>True while the items spread shows; item controls only work then.</summary>
        public bool ShowingItems => Current == 0;

        private void Awake()
        {
            flipAction = new InputAction("Notebook Flip Page", InputActionType.Button);
            flipAction.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
            flipAction.AddBinding("<Keyboard>/u");
            tabRest = new Vector3[tabs != null ? tabs.Length : 0];
            for (int i = 0; i < tabRest.Length; i++)
                tabRest[i] = tabs[i] != null ? tabs[i].localPosition : Vector3.zero;
            ShowSpread(0);
        }

        private void OnEnable() => flipAction.Enable();

        private void OnDisable() => flipAction.Disable();

        private void OnDestroy() => flipAction.Dispose();

        private void Update()
        {
            if (book != null && book.IsOpen && flipAction.WasPressedThisFrame())
                ShowSpread((Current + 1) % Mathf.Max(1, leftSpreads.Length));
        }

        /// <summary>Shows one spread (0 = items, 1 = case file, 2 = settings) and marks its tab. Called by the bookmark tabs.</summary>
        public void ShowSpread(int spread)
        {
            if (spread < 0 || spread >= leftSpreads.Length)
                return;
            Current = spread;
            for (int i = 0; i < leftSpreads.Length; i++)
            {
                if (leftSpreads[i] != null) leftSpreads[i].SetActive(i == spread);
                if (i < rightSpreads.Length && rightSpreads[i] != null) rightSpreads[i].SetActive(i == spread);
            }
            // The open spread's tab is brass and sticks out further, like a bookmark pulled out
            for (int i = 0; i < tabRest.Length; i++)
            {
                if (tabs[i] != null)
                    tabs[i].localPosition = tabRest[i] + (i == spread ? activeTabOffset : Vector3.zero);
                if (tabFaces != null && i < tabFaces.Length && tabFaces[i] != null)
                    tabFaces[i].sharedMaterial = i == spread ? activeTabMaterial : tabMaterial;
            }
            for (int i = 0; ribbonIcons != null && i < ribbonIcons.Length; i++)
            {
                if (ribbonIcons[i] != null) ribbonIcons[i].SetActive(i == spread);
            }
        }
    }
}
