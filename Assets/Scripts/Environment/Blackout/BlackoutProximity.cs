using HighStakes.Environment.Audio;
using UnityEngine;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// A quiet tone that rises as the player nears something solid. This is the difference between navigating in
    /// the dark and groping in it: the sonar ping tells you about the room when you ask, and this tells you
    /// continuously that a wall is getting close.
    /// <para>
    /// It is deliberately subtle and deliberately not an alarm — it should read as the room closing in, not as a
    /// warning beep, because in VR a sharp proximity alert makes people stop moving altogether, and a player who
    /// stops moving never finishes the run.
    /// </para>
    /// Also counts bumps (getting closer than the near distance), which <see cref="BlackoutRun"/> scores.
    /// </summary>
    public class BlackoutProximity : MonoBehaviour
    {
        [Tooltip("At or inside this distance the tone is at full (m) and a bump is counted.")]
        [SerializeField, Min(0.1f)] private float near = 0.45f;

        [Tooltip("Beyond this the tone is silent (m). Keep it short, or an interior is wall-to-wall tone.")]
        [SerializeField, Min(0.2f)] private float far = 1.6f;

        [Tooltip("Layers that count as solid. Everything except the aim-only teleport layer (31).")]
        [SerializeField] private LayerMask solid = ~(1 << 31);

        /// <summary>Times the player has come within <c>near</c> of something this run.</summary>
        public static int BumpCount { get; private set; }

        /// <summary>Reset at the start of a run.</summary>
        public static void ResetCount() => BumpCount = 0;

        // Four directions at head height plus forward-down; enough to feel a wall, cheap enough to run every frame.
        static readonly Vector3[] Probes =
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3(0f, -0.4f, 1f),
        };

        Soundscape.LoopHandle tone = Soundscape.LoopHandle.Dead;
        bool wasBumping;

        void OnDisable() => StopTone();

        void Update()
        {
            if (!BlackoutMode.IsDark)
            {
                StopTone();
                return;
            }

            var head = Camera.main;
            if (head == null)
            {
                StopTone();
                return;
            }

            float nearest = far;
            for (int i = 0; i < Probes.Length; i++)
            {
                Vector3 dir = head.transform.TransformDirection(Probes[i].normalized);
                if (Physics.Raycast(head.transform.position, dir, out var hit, far, solid, QueryTriggerInteraction.Ignore))
                    nearest = Mathf.Min(nearest, hit.distance);
            }

            float volume = BlackoutLogic.ProximityVolume(nearest, near, far);
            if (volume <= 0.001f)
            {
                StopTone();
                wasBumping = false;
                return;
            }

            if (!tone.IsPlaying)
                tone = Soundscape.Loop("dark.proximity", head.transform);

            tone.SetVolumeScale(volume);
            tone.SetPitch(BlackoutLogic.ProximityPitch(nearest, near, far));

            // Count a bump once per approach, not once per frame.
            bool bumping = nearest <= near;
            if (bumping && !wasBumping) BumpCount++;
            wasBumping = bumping;
        }

        void StopTone()
        {
            if (tone.IsPlaying) tone.Stop();
            tone = Soundscape.LoopHandle.Dead;
        }
    }
}
