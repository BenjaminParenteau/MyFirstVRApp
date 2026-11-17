using HighStakes.Dialogue;
using HighStakes.Environment.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Security
{
    /// <summary>
    /// The camera room's tape archive. The night duty log says when the vault keypad was reset and on which camera;
    /// the deck's monitor lists which camera number watches what. Seating a <see cref="VideoTape"/> in the deck plays
    /// it on the monitor; the right one (the vault approach camera at the right time) pays a chip reward, shows a
    /// "task complete" card and prints a still of the night manager at the vault keypad into the tray, a notebook
    /// pickup with half of the vault code. Solved once per session; the still comes back until it is collected.
    /// </summary>
    public sealed class TapeDeckPuzzle : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor deck;
        [Tooltip("VideoTape.Label of the tape that solves it.")]
        [SerializeField] string answerLabel = "CAM 04 · 02:40";
        [SerializeField] TextMeshPro screen;
        [Tooltip("Shown with no tape in the deck: the camera index.")]
        [TextArea] [SerializeField] string idleText;
        [SerializeField] GameObject stillPrefab;
        [Tooltip("Where the printed still lands, in the printer's tray.")]
        [SerializeField] Transform printSeat;
        [SerializeField] int reward = 300;
        [SerializeField] SpeechBubble completeCard;

        static bool solved;
        Soundscape.LoopHandle playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => solved = false;

        void Start()
        {
            deck.selectEntered.AddListener(OnInserted);
            deck.selectExited.AddListener(OnEjected);
            Idle();
            if (solved) PuzzleSession.SpawnUncollected(stillPrefab, printSeat, transform);
        }

        void OnDestroy()
        {
            if (deck == null) return;
            deck.selectEntered.RemoveListener(OnInserted);
            deck.selectExited.RemoveListener(OnEjected);
        }

        void OnInserted(SelectEnterEventArgs args)
        {
            if (!isActiveAndEnabled) return;
            var tape = args.interactableObject.transform.GetComponent<VideoTape>();
            if (tape == null) return;
            StopPlayback();
            Soundscape.PlayAt("sec.tape.insert", deck.transform.position);
            playing = Soundscape.Loop("sec.tape.play", deck.transform);
            Show("<b>PLAYING  " + tape.Label + "</b>\n\n" + tape.Footage);
            if (!solved && tape.Label == answerLabel) Complete();
        }

        void OnEjected(SelectExitEventArgs _)
        {
            StopPlayback();
            Idle();
        }

        void OnDisable() => StopPlayback();

        void StopPlayback()
        {
            playing?.Stop();
            playing = null;
        }

        void Idle() => Show(idleText);

        void Complete()
        {
            solved = true;
            PuzzleSession.PayOnce("tape_archive", reward);
            if (completeCard != null)
                completeCard.Say("<b>TASK COMPLETE</b>\n<size=70%>Vault keypad footage found\n+" + reward + " chips\nStill printed</size>", 8f);
            if (printSeat != null) Soundscape.PlayAt("sec.printer", printSeat.position);
            PuzzleSession.SpawnUncollected(stillPrefab, printSeat, transform);
        }

        void Show(string text)
        {
            if (screen != null) screen.text = text;
        }
    }
}
