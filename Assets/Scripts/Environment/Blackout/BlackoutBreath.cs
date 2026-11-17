using HighStakes.Environment.Audio;
using UnityEngine;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// The player's own breathing, in the dark.
    /// <para>
    /// Everything else in this mode is the player listening to the <i>room</i>. This is the one cue that comes
    /// from inside them, and it does a job nothing else does: in the dark a VR player loses all sense of their own
    /// body — no hands in view, no horizon, no floor — and their breath gives it back. It also reports how the run
    /// is going without a word or a number: it quickens when they are pressed into a corner or have just walked
    /// into something, and settles when they find open space.
    /// </para>
    /// <para>
    /// Deliberately restrained. It is mixed well under the sonar returns and the Guide (it must never mask the
    /// information the player is straining for), and the rate is capped short of panic —
    /// <see cref="BlackoutLogic.BreathPeriod"/> bottoms out at 2.4 s per breath, because breathing audio tips into
    /// horror-movie territory very easily and this is a heist, not a jump scare. It can also be switched off
    /// entirely: some players find any breathing track intrusive, and that is a legitimate preference rather than
    /// a bug.
    /// </para>
    /// </summary>
    public class BlackoutBreath : MonoBehaviour
    {
        [Tooltip("Off for players who find a breathing track intrusive. Exposed on the AUDIO settings page.")]
        [SerializeField] private bool enabledByDefault = true;

        [Tooltip("Distance that counts as 'pressed in' and raises stress (m). Matches the proximity tone's near band.")]
        [SerializeField, Min(0.1f)] private float crowdedDistance = 0.9f;

        [Tooltip("Layers that count as solid.")]
        [SerializeField] private LayerMask solid = ~(1 << 31);

        /// <summary>
        /// Player preference, settable from Pak's AUDIO page. Starts from the serialized default.
        /// </summary>
        public static bool Allowed { get; set; } = true;

        /// <summary>Current stress, 0..1. Readable so the Guide or a future suspicion system can use it.</summary>
        public static float StressLevel { get; private set; }

        float stress;
        float cycleStartedAt = -1f;
        bool exhaled;
        int lastBumpCount;

        void OnEnable()
        {
            Allowed = enabledByDefault;
            stress = 0f;
            StressLevel = 0f;
            cycleStartedAt = -1f;
            lastBumpCount = BlackoutProximity.BumpCount;
        }

        void Update()
        {
            if (!BlackoutMode.IsDark || !Allowed)
            {
                // Reset between runs so the next blackout starts calm rather than inheriting old tension.
                stress = 0f;
                StressLevel = 0f;
                cycleStartedAt = -1f;
                return;
            }

            var head = Camera.main;
            if (head == null) return;

            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // A bump is detected by watching the proximity counter rather than re-casting the same rays.
            int bumps = BlackoutProximity.BumpCount;
            bool justBumped = bumps > lastBumpCount;
            lastBumpCount = bumps;

            bool crowded = Physics.CheckSphere(head.transform.position, crowdedDistance, solid,
                                               QueryTriggerInteraction.Ignore);

            stress = BlackoutLogic.Stress(stress, dt, crowded, justBumped);
            StressLevel = stress;

            float period = BlackoutLogic.BreathPeriod(stress);
            if (cycleStartedAt < 0f)
            {
                cycleStartedAt = now;
                exhaled = false;
                Breathe("dark.breath.in");
                return;
            }


            float into = now - cycleStartedAt;

            if (!exhaled && into >= BlackoutLogic.ExhaleAt(period))
            {
                exhaled = true;
                Breathe("dark.breath.out");
            }

            if (into >= period)
            {
                cycleStartedAt = now;
                exhaled = false;
                Breathe("dark.breath.in");
            }
        }

        void Breathe(string cue)
        {
            // 2D: it is the player's own breath, so it must not pan when they turn their head. Volume rides the
            // stress level, capped by BreathVolume so it can never climb over the sonar returns or the Guide.
            Soundscape.Play(cue, BlackoutLogic.BreathVolume(stress));
        }
    }
}
