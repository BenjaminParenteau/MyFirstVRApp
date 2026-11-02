using HighStakes.Environment;
using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// The start screen in MVP_Main: a small dark lobby with the title hologram and a console table. The player picks
    /// how to move and turn (the two comfort choices first-time VR players need before taking a step), then presses
    /// START NIGHT; <see cref="SceneFlow.Begin"/> fades out, loads the casino, and this hides the lobby and puts the rig
    /// at the casino's start. The same choices stay in the notebook's SETTINGS spread.
    /// </summary>
    public sealed class StartScreen : MonoBehaviour
    {
        [Tooltip("The lobby (floor, rail, hologram, console), hidden once the casino has loaded.")]
        [SerializeField] GameObject lobby;
        [Tooltip("Where the rig stands when the night starts (the casino's start).")]
        [SerializeField] Transform startPoint;
        [SerializeField] TMP_Text moveLabel;
        [SerializeField] TMP_Text turnLabel;
        [SerializeField] TMP_Text noteText;

        ComfortSettings settings;

        void OnEnable()
        {
            settings = ComfortSettings.Instance != null ? ComfortSettings.Instance : FindFirstObjectByType<ComfortSettings>();
            Refresh(-1);
        }

        public void ToggleMove() => Press(SettingsLogic.Move);

        public void ToggleTurn() => Press(SettingsLogic.Turn);

        void Press(int row)
        {
            if (settings != null) settings.Toggle(row);
            Refresh(row);
        }

        public void StartNight()
        {
            var flow = SceneFlow.Instance;
            if (flow == null) return;
            flow.Begin(() =>
            {
                DoorPortal.MoveRig(startPoint);
                lobby.SetActive(false);
            });
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Refresh(int pressed)
        {
            var choices = settings != null ? settings.Choices : SettingsLogic.Default;
            moveLabel.text = SettingsLogic.Label(choices, SettingsLogic.Move);
            turnLabel.text = SettingsLogic.Label(choices, SettingsLogic.Turn);
            noteText.text = pressed >= 0 ? SettingsLogic.Describe(choices, pressed)
                : "Choose how you move and turn, then press START NIGHT.";
        }
    }
}
