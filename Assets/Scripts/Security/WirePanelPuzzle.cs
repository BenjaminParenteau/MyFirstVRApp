using System.Collections;
using HighStakes.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HighStakes.Security
{
    /// <summary>
    /// A breaker panel with four colour-coded sockets and four loose patch wires (<see cref="WirePlug"/>). Every wire
    /// seated in the socket of its own colour reroutes the circuit: pays a chip reward once (<see cref="PuzzleSession"/>),
    /// cuts the vault camera feed (<see cref="CctvOutage"/>), shows a "task complete" card and swings open a cabinet with
    /// the Breaker Log inside, which holds half of the vault code and comes back after a door trip until it is collected.
    /// </summary>
    public sealed class WirePanelPuzzle : MonoBehaviour
    {
        [Tooltip("Parallel to socketColors: sockets[i] needs a wire whose ColorIndex is socketColors[i].")]
        [SerializeField] XRSocketInteractor[] sockets;
        [SerializeField] int[] socketColors;
        [SerializeField] TextMeshPro status;
        [Tooltip("Hinged cabinet door under the panel; its local X rotation opens it.")]
        [SerializeField] Transform cabinetLid;
        [SerializeField] float cabinetOpenAngle = 100f;
        [SerializeField] GameObject notePrefab;
        [Tooltip("Where the note sits inside the open cabinet.")]
        [SerializeField] Transform noteSeat;
        [SerializeField] int reward = 300;
        [Tooltip("Floats over the panel for a few seconds on solve.")]
        [SerializeField] HighStakes.Dialogue.SpeechBubble completeCard;
        [Tooltip("Area scene whose CCTV feed goes dark on solve (see CctvOutage).")]
        [SerializeField] string cutFeedScene = "MVP_Vault";

        const string IdleStatus = "CROSS-WIRED / CONNECT BY COLOUR";
        const string CheckingStatus = "CHECKING CIRCUIT...";
        const string SolvedStatus = "REROUTED / VAULT CAM OFFLINE";

        static bool solved;
        UnityAction<SelectEnterEventArgs>[] enterHandlers;
        UnityAction<SelectExitEventArgs>[] exitHandlers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => solved = false;

        void Start()
        {
            enterHandlers = new UnityAction<SelectEnterEventArgs>[sockets.Length];
            exitHandlers = new UnityAction<SelectExitEventArgs>[sockets.Length];
            for (int i = 0; i < sockets.Length; i++)
            {
                enterHandlers[i] = _ => Refresh();
                exitHandlers[i] = _ => Refresh();
                sockets[i].selectEntered.AddListener(enterHandlers[i]);
                sockets[i].selectExited.AddListener(exitHandlers[i]);
            }

            if (solved)
            {
                SetLid(1f);
                Show(SolvedStatus);
                PuzzleSession.SpawnUncollected(notePrefab, noteSeat, transform);
            }
            else
            {
                SetLid(0f);
                Show(IdleStatus);
            }
        }

        void OnDestroy()
        {
            if (enterHandlers == null) return;
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] == null) continue;
                sockets[i].selectEntered.RemoveListener(enterHandlers[i]);
                sockets[i].selectExited.RemoveListener(exitHandlers[i]);
            }
        }

        void Refresh()
        {
            if (solved) return;
            var plugged = new int[sockets.Length];
            var anyPlugged = false;
            for (int i = 0; i < sockets.Length; i++)
            {
                var plug = sockets[i].interactablesSelected.Count > 0
                    ? sockets[i].interactablesSelected[0].transform.GetComponent<WirePlug>()
                    : null;
                plugged[i] = plug != null ? plug.ColorIndex : -1;
                anyPlugged |= plugged[i] >= 0;
            }
            Show(anyPlugged ? CheckingStatus : IdleStatus);
            if (WirePanelLogic.IsSolved(plugged, socketColors)) Complete();
        }

        void Complete()
        {
            solved = true;
            Show(SolvedStatus);
            PuzzleSession.PayOnce("wire_panel", reward);
            CctvOutage.Cut(cutFeedScene);
            if (completeCard != null)
                completeCard.Say("<b>TASK COMPLETE</b>\n<size=70%>Breaker panel re-wired\n+" + reward + " chips\nVault camera offline</size>", 8f);
            StartCoroutine(OpenLid());
            PuzzleSession.SpawnUncollected(notePrefab, noteSeat, transform);
        }

        IEnumerator OpenLid()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
            {
                SetLid(WristDisplayLogic.EaseOut(t));
                yield return null;
            }
            SetLid(1f);
        }

        void SetLid(float open)
        {
            if (cabinetLid != null) cabinetLid.localRotation = Quaternion.Euler(cabinetOpenAngle * open, 0f, 0f);
        }

        void Show(string message)
        {
            if (status != null) status.text = message;
        }
    }
}
