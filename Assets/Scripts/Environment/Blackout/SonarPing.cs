using System.Collections.Generic;
using HighStakes.Environment.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// The player's echolocation. Press the button and you make a sharp click; a moment later the room answers —
    /// each surface returning at its own delay (real speed of sound, out and back), its own pitch (closer reads
    /// higher) and its own timbre (carpet swallows it, concrete rings). That is enough to tell a wall two metres
    /// ahead from an open room, and a felt table from a steel door.
    /// <para>
    /// There is a deliberate cooldown. Without it the optimal strategy is to hold the button down and build a
    /// continuous picture of the room, which is just sight with extra steps — the mode is about acting on
    /// incomplete information, so the ping has to be a decision.
    /// </para>
    /// Lives on MVP_Main (added by the audio builder), not on the rig, so it survives area swaps; it finds the
    /// camera each time it fires.
    /// </summary>
    public class SonarPing : MonoBehaviour
    {
        [Tooltip("Seconds between pings. Short enough not to frustrate, long enough that spamming isn't a strategy.")]
        [SerializeField, Min(0.1f)] private float cooldown = 1.5f;

        [Tooltip("How far the ping reaches (m). Beyond this a surface returns nothing, which is itself information.")]
        [SerializeField, Min(1f)] private float range = 18f;

        [Tooltip("Layers the ping bounces off. Leave as everything except the aim-only teleport layer (31).")]
        [SerializeField] private LayerMask hits = ~(1 << 31);

        /// <summary>
        /// Set true by Pak's SOUND CUES accessibility option to allow pinging with the lights on. Off by default:
        /// in the lit game it is noise, but for a low-vision player it is the same help it is in the dark.
        /// </summary>
        public static bool AvailableWhenLit;

        /// <summary>How many times the player has pinged this run. Used by <see cref="BlackoutRun"/> scoring.</summary>
        public static int PingCount { get; private set; }

        /// <summary>Reset the counter at the start of a run.</summary>
        public static void ResetCount() => PingCount = 0;

        // The fan of directions a ping sweeps, in head space: dense ahead (where the player is going), sparse to
        // the sides, plus one down and one up so a table edge or a low beam reads differently from open floor.
        static readonly Vector3[] Fan =
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0.18f, 0f, 1f), new Vector3(-0.18f, 0f, 1f),
            new Vector3(0.45f, 0f, 1f), new Vector3(-0.45f, 0f, 1f),
            new Vector3(0.9f, 0f, 1f), new Vector3(-0.9f, 0f, 1f),
            new Vector3(1f, 0f, 0.3f), new Vector3(-1f, 0f, 0.3f),
            new Vector3(0f, -0.5f, 1f), new Vector3(0f, 0.45f, 1f),
            new Vector3(0f, 0f, -1f),
        };

        readonly List<Pending> pending = new List<Pending>();
        InputAction pingAction;
        float nextAllowed;

        void Awake()
        {
            // Inline bindings, same approach as the notebook and the watch (no shared input asset to conflict over).
            pingAction = new InputAction("Sonar Ping", InputActionType.Button);
            pingAction.AddBinding("<XRController>{RightHand}/primaryButton");
            pingAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            pingAction.AddBinding("<Keyboard>/space");
        }

        void OnEnable() => pingAction.Enable();

        void OnDisable()
        {
            pingAction.Disable();
            pending.Clear();
        }

        void OnDestroy() => pingAction.Dispose();

        void Update()
        {
            float now = Time.unscaledTime;

            if (pingAction.WasPressedThisFrame() && now >= nextAllowed && IsAvailable)
            {
                nextAllowed = now + cooldown;
                Fire(now);
            }

            // Returns arrive on their own schedule, so they are queued rather than played immediately.
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (now < pending[i].DueAt) continue;
                var p = pending[i];
                pending.RemoveAt(i);
                Soundscape.PlayAt(p.Cue, p.Position);
            }
        }

        bool IsAvailable => BlackoutMode.IsDark || AvailableWhenLit;

        void Fire(float now)
        {
            var head = Camera.main;
            if (head == null) return;

            PingCount++;
            Soundscape.Play("dark.sonar.ping");       // 2D: the click comes from the player, not the room

            Vector3 origin = head.transform.position;
            for (int i = 0; i < Fan.Length; i++)
            {
                Vector3 dir = head.transform.TransformDirection(Fan[i].normalized);
                if (!Physics.Raycast(origin, dir, out var hit, range, hits, QueryTriggerInteraction.Ignore))
                    continue;

                var hardness = SurfaceLogic.HardnessOf(SurfaceLogic.KindOf(NameOf(hit)));
                float volume = BlackoutLogic.ReturnVolume(hit.distance, range, hardness);
                if (volume <= 0.02f) continue;        // too faint to be information; don't spend a voice on it

                pending.Add(new Pending
                {
                    DueAt = now + BlackoutLogic.ReturnDelay(hit.distance),
                    Position = hit.point,
                    Cue = SurfaceLogic.SonarReturnCue(hardness),
                });
            }
        }

        /// <summary>
        /// What the ping hit, as a name we can classify. The material name is the better signal (the builders name
        /// materials by what they are), with the object name as a fallback.
        /// </summary>
        static string NameOf(RaycastHit hit)
        {
            var renderer = hit.collider != null ? hit.collider.GetComponent<Renderer>() : null;
            string material = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : null;
            string obj = hit.collider != null ? hit.collider.name : null;
            return string.IsNullOrEmpty(material) ? obj : material + " " + obj;
        }

        struct Pending
        {
            public float DueAt;
            public Vector3 Position;
            public string Cue;
        }
    }
}
