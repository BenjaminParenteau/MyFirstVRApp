using HighStakes.Environment.Audio;
using UnityEngine;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// Marks something findable by ear — the way out, the breaker, the keypad. A positioned looping cue, so
    /// ordinary 3D audio does the direction work: the player turns their head and hears where it is.
    /// <para>
    /// Optionally also ticks, faster the closer the player is to facing it. That turns "which way is out" into
    /// something you can hear yourself getting right. It is a bearing, not a route — the beacon never says
    /// "go left", and the Guide is under the same rule (his brief says the moment he gives directions the mode
    /// stops being about sound and becomes about following instructions).
    /// </para>
    /// Attached by a builder, never placed as an AudioSource in a scene file (StyleGuide §11, rule 1).
    /// </summary>
    public class AudioBeacon : MonoBehaviour
    {
        [Tooltip("Looping cue for the beacon itself. Slot D's dark. namespace.")]
        [SerializeField] private string cueId = "dark.beacon";

        [Tooltip("On: audible with the lights on too. Off: only in the dark, which is the usual case.")]
        [SerializeField] private bool alwaysAudible;

        [Tooltip("Also tick, faster as the player faces it. Use on the current objective only — several ticking " +
                 "beacons at once is noise, not guidance.")]
        [SerializeField] private bool guidanceTick;

        [Tooltip("Tick cue, played 2D so its rate is the information rather than its position.")]
        [SerializeField] private string tickCueId = "dark.sonar.ping";

        Soundscape.LoopHandle loop = Soundscape.LoopHandle.Dead;
        float nextTick;

        /// <summary>Turn the guidance tick on or off at runtime, so only the live objective ticks.</summary>
        public void SetGuidance(bool on)
        {
            guidanceTick = on;
            nextTick = 0f;
        }

        void OnDisable() => StopLoop();

        void Update()
        {
            bool shouldSound = alwaysAudible || BlackoutMode.IsDark;
            if (!shouldSound)
            {
                StopLoop();
                return;
            }

            if (!loop.IsPlaying)
                loop = Soundscape.Loop(cueId, transform);

            if (!guidanceTick) return;

            var head = Camera.main;
            if (head == null) return;

            // Signed angle on the horizontal plane: negative is to the player's left.
            Vector3 toMe = transform.position - head.transform.position;
            toMe.y = 0f;
            if (toMe.sqrMagnitude < 0.01f) return;
            Vector3 forward = head.transform.forward;
            forward.y = 0f;
            float angle = Vector3.SignedAngle(forward, toMe, Vector3.up);

            float rate = BlackoutLogic.BearingTickRate(angle);
            if (rate <= 0f) return;

            if (Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + 1f / rate;
                Soundscape.Play(tickCueId);
            }
        }

        void StopLoop()
        {
            if (loop.IsPlaying) loop.Stop();
            loop = Soundscape.LoopHandle.Dead;
        }
    }
}
