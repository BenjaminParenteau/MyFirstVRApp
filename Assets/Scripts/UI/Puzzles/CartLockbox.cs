using System.Collections;
using System.Collections.Generic;
using HighStakes.Inventory;
using HighStakes.TableGames;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HighStakes.UI
{
    /// <summary>
    /// The housekeeping cart's lockbox: a 12-key pad (1-9, CLR, 0, OK). The right code swings the lid open and a staff
    /// keycard is waiting inside, to be picked up into the notebook and touched to the STAFF ONLY reader. A wrong code
    /// just clears. Solved once per session (it stays open when the casino reloads), and the card comes back only if it
    /// was lost: never while one exists anywhere (the notebook keeps it hidden in MVP_Main) or once its door is open.
    /// </summary>
    public sealed class CartLockbox : MonoBehaviour
    {
        [Tooltip("1-9, CLR, 0, OK, in that order.")]
        [SerializeField] private PressableButton[] keys;
        [SerializeField] private TextMeshPro display;
        [SerializeField] private TextMeshPro status;
        [SerializeField] private string code = "472";
        [Tooltip("Hinged lid; its local X rotation opens it.")]
        [SerializeField] private Transform lid;
        [SerializeField] private float lidOpenAngle = 105f;
        [SerializeField] private Keycard cardPrefab;
        [Tooltip("Where the card sits inside the open box.")]
        [SerializeField] private Transform cardSeat;

        private const string Prompt = "ENTER CODE";
        private const float PressCooldown = 0.12f; // one press per key, even if the ray and the hand both select it
        private static readonly HashSet<string> solved = new HashSet<string>();

        private UnityAction[] handlers;
        private string entry = "";
        private float nextPress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => solved.Clear();

        private bool Solved => cardPrefab != null && solved.Contains(cardPrefab.AccessId);

        private void Start()
        {
            handlers = new UnityAction[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int key = i;
                handlers[i] = () => PressKey(key);
                keys[i].Pressed.AddListener(handlers[i]);
            }

            if (Solved)
            {
                SetLid(1f);
                SpawnCardIfLost();
                Show(LockboxLogic.Display(code, code.Length), "OPEN");
            }
            else
            {
                SetLid(0f);
                Show(LockboxLogic.Display(entry, code.Length), Prompt);
            }
        }

        private void OnDestroy()
        {
            if (handlers == null)
                return;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] != null)
                    keys[i].Pressed.RemoveListener(handlers[i]);
            }
        }

        private void PressKey(int key)
        {
            if (Solved || Time.unscaledTime < nextPress)
                return;
            nextPress = Time.unscaledTime + PressCooldown;

            if (key == 9) // CLR
            {
                entry = "";
                Show(LockboxLogic.Display(entry, code.Length), Prompt);
            }
            else if (key == 11) // OK
            {
                Check();
            }
            else
            {
                entry = LockboxLogic.Press(entry, key == 10 ? 0 : key + 1, code.Length);
                Show(LockboxLogic.Display(entry, code.Length), Prompt);
            }
        }

        private void Check()
        {
            if (!LockboxLogic.Opens(entry, code))
            {
                entry = "";
                Show(LockboxLogic.Display(entry, code.Length), "WRONG CODE");
                return;
            }

            solved.Add(cardPrefab.AccessId);
            Show(LockboxLogic.Display(code, code.Length), "OPEN");
            StartCoroutine(OpenLid());
            SpawnCardIfLost();
        }

        private void SpawnCardIfLost()
        {
            if (cardPrefab == null || cardSeat == null || KeycardAccess.Has(cardPrefab.AccessId))
                return;
            foreach (var card in FindObjectsByType<Keycard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (card.AccessId == cardPrefab.AccessId)
                    return; // waiting in the box, in the notebook or in the hand
            }
            Instantiate(cardPrefab, cardSeat.position, cardSeat.rotation, transform);
        }

        private IEnumerator OpenLid()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
            {
                SetLid(WristDisplayLogic.EaseOut(t));
                yield return null;
            }
            SetLid(1f);
        }

        private void SetLid(float open)
        {
            if (lid != null)
                lid.localRotation = Quaternion.Euler(lidOpenAngle * open, 0f, 0f);
        }

        private void Show(string digits, string message)
        {
            if (display != null) display.text = digits;
            if (status != null) status.text = message;
        }
    }
}
