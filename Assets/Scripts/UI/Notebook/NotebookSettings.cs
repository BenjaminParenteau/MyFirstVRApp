using HighStakes.Environment;
using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// The notebook's SETTINGS spread: a COMFORT page with one pressable row per choice (move, turn, vignette, seated)
    /// and a GAME page with RESUME and QUIT GAME. Pressing a comfort row switches it in <see cref="ComfortSettings"/>;
    /// the row shows the new choice, a tick on the comfortable option, and the note box says what it does.
    /// </summary>
    public class NotebookSettings : MonoBehaviour
    {
        [Tooltip("One row per comfort choice, in SettingsLogic order (move, turn, vignette, seated).")]
        [SerializeField] private NotebookRow[] comfortRows;
        [Tooltip("RESUME and QUIT GAME, in that order.")]
        [SerializeField] private NotebookRow[] gameRows;
        [SerializeField] private TMP_Text noteText;
        [SerializeField] private NotebookOpen book;

        private ComfortSettings settings;
        private int lastRow = -1;

        private void OnEnable()
        {
            settings = ComfortSettings.Instance != null ? ComfortSettings.Instance : FindFirstObjectByType<ComfortSettings>();
            if (settings != null) settings.Changed += OnChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (settings != null) settings.Changed -= OnChanged;
        }

        private void OnChanged(ComfortChoices _) => Refresh();

        /// <summary>Called by a comfort row's button.</summary>
        public void PressComfort(int row)
        {
            lastRow = row;
            if (settings != null) settings.Toggle(row); // raises Changed, which refreshes
            else Refresh();
        }

        /// <summary>Closes the notebook and goes back to the game.</summary>
        public void Resume() => book.SetOpen(false);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Refresh()
        {
            var choices = settings != null ? settings.Choices : SettingsLogic.Default;
            for (int i = 0; i < comfortRows.Length && i < SettingsLogic.Rows; i++)
                comfortRows[i].Show(SettingsLogic.Label(choices, i), i == lastRow, SettingsLogic.IsOn(choices, i), settings == null);
            if (gameRows.Length > 0) gameRows[0].Show("RESUME", false, false, false);
            if (gameRows.Length > 1) gameRows[1].Show("QUIT GAME", false, false, false);
            if (noteText != null)
                noteText.text = settings == null ? "Comfort settings are only available in the full game (MVP_Main)."
                    : lastRow >= 0 ? SettingsLogic.Describe(choices, lastRow) : SettingsLogic.Intro;
        }
    }
}
