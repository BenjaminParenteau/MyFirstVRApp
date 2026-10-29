using System.Collections;
using HighStakes.Dialogue;
using HighStakes.TableGames;
using HighStakes.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HighStakes.Security
{
    /// <summary>
    /// The final lock: the VAULT 03 keypad in front of the treasury door. The code is never written down anywhere; it
    /// is worked out from the Breaker Log (security office) and the CAM 04 still (camera room). Too many wrong codes
    /// trip a lockdown that blinks the lamps red and ignores the keypad for a while. The right code turns the lamps
    /// green, pays the big reward, shows the heist-complete card and opens the drawer holding the Black Ledger, which
    /// comes back after a door trip until it is in the notebook. Cracked once per session.
    /// </summary>
    public sealed class VaultKeypad : MonoBehaviour
    {
        [Tooltip("1-9, CLR, 0, OK, in that order.")]
        [SerializeField] PressableButton[] keys;
        [SerializeField] TextMeshPro display;
        [SerializeField] TextMeshPro status;
        [SerializeField] string code = "5813";
        [SerializeField] int maxFailures = 3;
        [SerializeField] float lockdownSeconds = 15f;
        [SerializeField] Renderer[] lamps;
        [SerializeField] Material lockedMaterial;
        [SerializeField] Material openMaterial;
        [SerializeField] Material offMaterial;
        [Tooltip("Hinged drawer lid; its local X rotation opens it.")]
        [SerializeField] Transform drawerLid;
        [SerializeField] float drawerOpenAngle = 80f;
        [SerializeField] GameObject ledgerPrefab;
        [SerializeField] Transform ledgerSeat;
        [SerializeField] int reward = 5000;
        [SerializeField] SpeechBubble completeCard;

        const string Prompt = "ENTER VAULT CODE";
        const float PressCooldown = 0.12f; // one press per key, even if the ray and the hand both select it

        static bool cracked;
        static int failures;
        static float lockedUntil;

        UnityAction[] handlers;
        string entry = "";
        float nextPress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            cracked = false;
            failures = 0;
            lockedUntil = 0f;
        }

        bool LockedDown => Time.time < lockedUntil;

        void Start()
        {
            handlers = new UnityAction[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int key = i;
                handlers[i] = () => PressKey(key);
                keys[i].Pressed.AddListener(handlers[i]);
            }

            if (cracked)
            {
                SetLid(1f);
                SetLamps(openMaterial);
                Show(LockboxLogic.Display(code, code.Length), "ACCESS GRANTED");
                PuzzleSession.SpawnUncollected(ledgerPrefab, ledgerSeat, transform);
            }
            else
            {
                SetLid(0f);
                SetLamps(lockedMaterial);
                Show(LockboxLogic.Display(entry, code.Length), Prompt);
            }
        }

        void OnDestroy()
        {
            if (handlers == null) return;
            for (int i = 0; i < keys.Length; i++)
                if (keys[i] != null) keys[i].Pressed.RemoveListener(handlers[i]);
        }

        void Update()
        {
            if (cracked || lockedUntil <= 0f) return;
            if (LockedDown)
            {
                // Lamps blink red twice a second while the keypad is dead.
                SetLamps(Mathf.Repeat(Time.time, 0.5f) < 0.25f ? lockedMaterial : offMaterial);
                Show(LockboxLogic.Display("", code.Length), "LOCKDOWN  " + Mathf.CeilToInt(lockedUntil - Time.time));
                return;
            }
            lockedUntil = 0f;
            SetLamps(lockedMaterial);
            Show(LockboxLogic.Display(entry, code.Length), Prompt);
        }

        void PressKey(int key)
        {
            if (cracked || LockedDown || Time.unscaledTime < nextPress) return;
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

        void Check()
        {
            switch (VaultCodeLogic.Submit(entry, code, ref failures, maxFailures))
            {
                case VaultAttempt.Incomplete:
                    Show(LockboxLogic.Display(entry, code.Length), code.Length + " DIGITS");
                    return;
                case VaultAttempt.Wrong:
                    entry = "";
                    int left = VaultCodeLogic.AttemptsLeft(failures, maxFailures);
                    Show(LockboxLogic.Display(entry, code.Length), "WRONG · " + left + (left == 1 ? " TRY LEFT" : " TRIES LEFT"));
                    return;
                case VaultAttempt.Lockdown:
                    entry = "";
                    lockedUntil = Time.time + lockdownSeconds;
                    return;
                case VaultAttempt.Open:
                    Crack();
                    return;
            }
        }

        void Crack()
        {
            cracked = true;
            SetLamps(openMaterial);
            Show(LockboxLogic.Display(code, code.Length), "ACCESS GRANTED");
            PuzzleSession.PayOnce("vault", reward);
            if (completeCard != null)
                completeCard.Say("<b>VAULT CRACKED</b>\n<size=70%>The Black Ledger is yours\n+" + reward + " chips\nNow get out.</size>", 10f);
            StartCoroutine(OpenLid());
            PuzzleSession.SpawnUncollected(ledgerPrefab, ledgerSeat, transform);
        }

        IEnumerator OpenLid()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                SetLid(WristDisplayLogic.EaseOut(t));
                yield return null;
            }
            SetLid(1f);
        }

        void SetLid(float open)
        {
            if (drawerLid != null) drawerLid.localRotation = Quaternion.Euler(drawerOpenAngle * open, 0f, 0f);
        }

        void SetLamps(Material material)
        {
            if (material == null) return;
            foreach (var lamp in lamps)
                if (lamp != null && lamp.sharedMaterial != material) lamp.sharedMaterial = material;
        }

        void Show(string digits, string message)
        {
            if (display != null) display.text = digits;
            if (status != null) status.text = message;
        }
    }
}
