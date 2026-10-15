using System.Runtime.CompilerServices;
using HighStakes.Core;
using HighStakes.TableGames;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HighStakes.Inventory
{
    /// <summary>Four-digit bar float lock. Claims follow the shared wallet across room visits.</summary>
    public sealed class BarPuzzle : MonoBehaviour
    {
        [SerializeField] PressableButton[] keys;
        [SerializeField] TextMeshPro display;
        [SerializeField] TextMeshPro status;
        const int Reward = 500;
        const string Pin = "8642";
        static ConditionalWeakTable<IChipWallet, Claim> claims = new ConditionalWeakTable<IChipWallet, Claim>();
        sealed class Claim { public bool Paid; }
        UnityAction[] handlers;
        string entry = "";
        IChipWallet wallet;
        float nextPress;
        bool completed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => claims = new ConditionalWeakTable<IChipWallet, Claim>();

        void Start()
        {
            handlers = new UnityAction[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int key = i;
                handlers[i] = () => PressKey(key);
                keys[i].Pressed.AddListener(handlers[i]);
            }
            Refresh();
            ResolveWallet();
        }

        void OnDestroy()
        {
            if (handlers == null) return;
            for (int i = 0; i < keys.Length; i++)
                if (keys[i]) keys[i].Pressed.RemoveListener(handlers[i]);
        }

        void PressKey(int key)
        {
            if (completed || Time.unscaledTime < nextPress) return;
            nextPress = Time.unscaledTime + .12f;
            if (key == 11) { Submit(); return; }
            if (key == 9) entry = "";
            else if (entry.Length < 4) entry += key == 10 ? "0" : (key + 1).ToString();
            status.text = "ENTER 4-DIGIT PIN";
            Refresh();
        }

        void Refresh() => display.text = string.Join(" ", entry.PadRight(4, '_').ToCharArray());

        void ResolveWallet()
        {
            wallet = null;
            foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component is IChipWallet candidate) { wallet = candidate; break; }
            if (wallet != null && claims.GetValue(wallet, _ => new Claim()).Paid) Complete();
        }

        public void Submit()
        {
            if (completed) return;
            ResolveWallet();
            if (completed) return;
            if (entry.Length != 4) { status.text = "4 DIGITS REQUIRED"; return; }
            if (entry != Pin)
            {
                entry = "";
                Refresh();
                status.text = "INVALID PIN / TRY AGAIN";
                return;
            }
            if (wallet == null) { status.text = "TRANSFER UNAVAILABLE / RETRY"; return; }
            if (wallet.Balance > int.MaxValue - Reward) { status.text = "BANKROLL FULL / RETRY"; return; }
            var claim = claims.GetValue(wallet, _ => new Claim());
            claim.Paid = true;
            wallet.Add(Reward);
            Complete();
        }

        void Complete()
        {
            completed = true;
            display.text = "+ $500";
            status.text = "TRANSFER COMPLETE";
            display.color = new Color32(60, 200, 110, 255);
            // Keep the physical keypad visible, but ignore all later input.
        }
    }
}
