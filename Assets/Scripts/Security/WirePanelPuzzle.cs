using System.Collections;
using System.Runtime.CompilerServices;
using HighStakes.Core;
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
    /// seated in the socket of its own colour restores the circuit: pays a chip reward (claim follows the wallet
    /// across area visits, like Inventory/BarPuzzle) and swings open a cabinet with a note inside, dropped once per
    /// session (like UI/Puzzles/CartLockbox) so leaving and coming back doesn't pay out or drop the note twice.
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

        const string IdleStatus = "CROSS-WIRED / CONNECT BY COLOUR";
        const string CheckingStatus = "CHECKING CIRCUIT...";
        const string SolvedStatus = "CIRCUIT RESTORED";

        static ConditionalWeakTable<IChipWallet, Claim> claims = new ConditionalWeakTable<IChipWallet, Claim>();
        sealed class Claim { public bool Paid; }
        static bool solved;
        static bool noteSpawned;
        UnityAction<SelectEnterEventArgs>[] enterHandlers;
        UnityAction<SelectExitEventArgs>[] exitHandlers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            claims = new ConditionalWeakTable<IChipWallet, Claim>();
            solved = false;
            noteSpawned = false;
        }

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
                SpawnNoteIfMissing();
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
            Pay();
            StartCoroutine(OpenLid());
            SpawnNoteIfMissing();
        }

        void Pay()
        {
            IChipWallet wallet = null;
            foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component is IChipWallet candidate) { wallet = candidate; break; }
            if (wallet == null) return;
            var claim = claims.GetValue(wallet, _ => new Claim());
            if (claim.Paid || wallet.Balance > int.MaxValue - reward) return;
            claim.Paid = true;
            wallet.Add(reward);
        }

        void SpawnNoteIfMissing()
        {
            if (noteSpawned || notePrefab == null || noteSeat == null) return;
            noteSpawned = true;
            Instantiate(notePrefab, noteSeat.position, noteSeat.rotation, transform);
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
