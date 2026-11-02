using System.Collections.Generic;
using HighStakes.Dialogue;
using HighStakes.Environment;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.UI
{
    /// <summary>
    /// The notebook's case-file spread. Left page: a map of the areas (MapLogic) where each room is drawn in once the
    /// player has been there and a red pin marks the current one, with a "YOU ARE HERE" box naming it. Right page: the
    /// Guide's leads (LeadsLogic) as boxed rows, the newest on top and older ones ticked off, and a box quoting what the
    /// Guide said with the current lead. It only observes: the current area from SceneFlow (so a CCTV feed loading the camera room in the
    /// background doesn't count as a visit) and lines from every GuideVoice in the loaded scenes. Lives on the notebook
    /// in MVP_Main, so what it has written survives door trips.
    /// </summary>
    public class NotebookCaseFile : MonoBehaviour
    {
        [Tooltip("One label per MapLogic.Areas entry, same order.")]
        [SerializeField] private TMP_Text[] roomLabels;
        [Tooltip("One ink outline per MapLogic.Areas entry, same order. Shown once the room is visited.")]
        [SerializeField] private GameObject[] roomOutlines;
        [Tooltip("One line per MapLogic.Paths entry, same order. Shown once both rooms are visited.")]
        [SerializeField] private GameObject[] paths;
        [Tooltip("Red pin, moved to the current room. Hidden when the current scene isn't on the map.")]
        [SerializeField] private Transform pin;
        [Tooltip("\"YOU ARE HERE\" box: the current room's name and its MapLogic note.")]
        [SerializeField] private TMP_Text hereName;
        [SerializeField] private TMP_Text hereNote;
        [Tooltip("Lead rows, top to bottom (LeadsLogic.Rows of them).")]
        [SerializeField] private NotebookRow[] leadRows;
        [Tooltip("Shown instead of the rows until the Guide gives a lead.")]
        [SerializeField] private TMP_Text noLeadsText;
        [Tooltip("\"GUIDE\" box: the line that gave the current lead.")]
        [SerializeField] private TMP_Text guideQuote;
        [Tooltip("Visited room names: the notebook's brown ink (UITextures.InkColor).")]
        [SerializeField] private Color inkColor = new Color(0.24f, 0.16f, 0.09f, 1f);
        [SerializeField] private Color unknownColor = new Color(0.24f, 0.16f, 0.09f, 0.35f);

        private readonly HashSet<string> visited = new HashSet<string>();
        private readonly List<string> leads = new List<string>();
        private readonly List<string> quotes = new List<string>(); // the Guide's words for each lead, same order
        private readonly List<GuideVoice> voices = new List<GuideVoice>();
        private Vector3[] pinSpots;
        private string currentScene;

        private void Awake()
        {
            // Each room's outline sits at the room's centre on the map; the pin keeps its own height above the page.
            if (pin == null || roomOutlines == null)
                return;
            pinSpots = new Vector3[roomOutlines.Length];
            for (int i = 0; i < roomOutlines.Length; i++)
            {
                var centre = roomOutlines[i] != null ? roomOutlines[i].transform.localPosition : pin.localPosition;
                pinSpots[i] = new Vector3(centre.x, pin.localPosition.y, centre.z);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindVoices();
            Refresh();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (var voice in voices)
            {
                if (voice != null) voice.LineStarted -= OnLineStarted;
            }
            voices.Clear();
        }

        private void Update()
        {
            string scene = SceneFlow.Instance != null ? SceneFlow.Instance.CurrentScene : null;
            if (scene == currentScene)
                return;
            currentScene = scene;
            MapLogic.Visit(visited, scene);
            Refresh();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindVoices();

        private void BindVoices()
        {
            voices.RemoveAll(voice => voice == null);
            foreach (var voice in FindObjectsByType<GuideVoice>(FindObjectsSortMode.None))
            {
                if (voices.Contains(voice))
                    continue;
                voices.Add(voice);
                voice.LineStarted += OnLineStarted;
            }
        }

        private void OnLineStarted(GuideLine line)
        {
            if (line == null || !LeadsLogic.Add(leads, line.lead))
                return;
            quotes.Add(line.subtitle ?? "");
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < MapLogic.Areas.Length; i++)
            {
                bool known = visited.Contains(MapLogic.Areas[i].Scene);
                if (roomLabels != null && i < roomLabels.Length && roomLabels[i] != null)
                {
                    roomLabels[i].text = MapLogic.LabelFor(i, visited);
                    roomLabels[i].color = known ? inkColor : unknownColor;
                }
                if (roomOutlines != null && i < roomOutlines.Length && roomOutlines[i] != null)
                    roomOutlines[i].SetActive(known);
            }
            for (int i = 0; paths != null && i < paths.Length && i < MapLogic.Paths.Length; i++)
            {
                if (paths[i] != null)
                    paths[i].SetActive(MapLogic.ShowsPath(i, visited));
            }
            if (pin != null)
            {
                int pinAt = MapLogic.IndexOf(currentScene);
                pin.gameObject.SetActive(pinAt >= 0 && pinSpots != null);
                if (pinAt >= 0 && pinSpots != null)
                    pin.localPosition = pinSpots[pinAt] + PinOffset(pinAt);
            }
            int here = MapLogic.IndexOf(currentScene);
            if (hereName != null) hereName.text = here >= 0 ? MapLogic.Areas[here].Label : "";
            if (hereNote != null) hereNote.text = here >= 0 ? MapLogic.Areas[here].Note : MapLogic.OffMap;

            // Leads: the newest inked with an open circle, older ones ticked and faded
            int rowCount = leadRows != null ? leadRows.Length : 0;
            var order = LeadsLogic.NewestFirst(leads.Count, rowCount);
            for (int r = 0; r < rowCount; r++)
            {
                if (leadRows[r] == null) continue;
                if (r < order.Length)
                    leadRows[r].Show(leads[order[r]], r == 0, r > 0, r > 0);
                else
                    leadRows[r].Hide();
            }
            if (noLeadsText != null)
                noLeadsText.gameObject.SetActive(leads.Count == 0);
            if (guideQuote != null)
                guideQuote.text = quotes.Count > 0 ? "\"" + quotes[quotes.Count - 1] + "\"" : "";
        }

        // The pin sits in the room's top-right corner, inside the outline, clear of the centred label. Map space: -X is
        // the reader's right, -Z the top of the page (see NotebookBuilder).
        private static Vector3 PinOffset(int area)
        {
            var a = MapLogic.Areas[area];
            return new Vector3(-(a.Width / 2f - 0.0035f), 0f, -(a.Height / 2f - 0.0035f));
        }
    }
}
