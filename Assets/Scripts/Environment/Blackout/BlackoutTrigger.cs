using HighStakes.Environment.Audio;
using HighStakes.Security;
using UnityEngine;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// Starts the blackout when the player throws the breaker — the heist's own power cut, on the route they were
    /// already taking to the vault, caused by something they chose to do.
    /// <para>
    /// <b>It touches nothing of Slot B's.</b> Ashton's <c>WirePanelPuzzle</c> already announces itself publicly:
    /// on solve it calls <c>CctvOutage.Cut(scene)</c>, which raises the static <see cref="CctvOutage.WentDown"/>
    /// event. Listening to that is a complete signal that the breaker has been thrown, so his file needs no
    /// change at all and no new contract had to be agreed. (His brief originally asked him to expose the puzzle's
    /// solved state for this — he no longer needs to.)
    /// </para>
    /// Lives on MVP_Main so it hears the breaker whichever area the player is in.
    /// </summary>
    public class BlackoutTrigger : MonoBehaviour
    {
        [Tooltip("Cut the lights when the breaker takes a camera feed down. The story trigger.")]
        [SerializeField] private bool onBreakerThrown = true;

        [Tooltip("Master switch. Off holds the whole feature back — e.g. for a test round measuring something else.")]
        [SerializeField] private bool allowed = true;

        [Tooltip("Seconds between the breaker being thrown and the lights dying. A beat of nothing happening, so " +
                 "the player connects the two.")]
        [SerializeField, Min(0f)] private float delaySeconds = 1.1f;

        [Tooltip("How long the power stays out in the story beat (s). 0 = until something else ends it.")]
        [SerializeField, Min(0f)] private float holdSeconds = 0f;

        float fireAt = -1f;
        float endAt = -1f;

        void OnEnable()
        {
            if (onBreakerThrown) CctvOutage.WentDown += OnFeedCut;
        }

        void OnDisable()
        {
            CctvOutage.WentDown -= OnFeedCut;
        }

        void OnFeedCut(string sceneName)
        {
            if (!allowed || BlackoutMode.IsActive) return;
            fireAt = Time.unscaledTime + delaySeconds;
            Debug.Log($"[Blackout] Breaker thrown (feed '{sceneName}' down) — lights out in {delaySeconds:0.0}s.");
        }

        void Update()
        {
            float now = Time.unscaledTime;

            if (fireAt >= 0f && now >= fireAt)
            {
                fireAt = -1f;
                Soundscape.PlayAt("sec.breaker.throw", transform.position);   // Ashton's cue, his to author
                BlackoutMode.Begin("breaker");
                endAt = holdSeconds > 0f ? now + holdSeconds + BlackoutLogic.PowerDownSeconds : -1f;
            }

            if (endAt >= 0f && now >= endAt)
            {
                endAt = -1f;
                BlackoutMode.End();
            }
        }

        /// <summary>Cut the power from anywhere (a test, a menu row, a future story beat).</summary>
        public void Fire(string reason)
        {
            if (!allowed) return;
            BlackoutMode.Begin(reason);
        }
    }
}
