using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Environment
{
    /// <summary>
    /// Lives in MVP_Main, next to the one player rig, wallet and wrist UI, which stay loaded all game. Exactly one area
    /// scene (casino, VIP, a security wing) is loaded on top at a time: walking into a <see cref="DoorPortal"/> whose
    /// target is in another scene fades to black, loads that scene, unloads the old one, moves the rig to the target
    /// door's arrival point and fades back in. Area scenes are authored at the origin and keep their own _SoloTest rig,
    /// which switches itself off here because MVP_Main is the active scene while they load. With Wait For Start on, the
    /// first area loads only when the start screen calls <see cref="Begin"/>.
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        [Tooltip("Area scene loaded at start. The rig in MVP_Main already stands at its start position.")]
        [SerializeField] string startScene = "MVP_Casino";
        [Tooltip("Show the start screen (MVP_Main's lobby) first and load the start scene when it calls Begin.")]
        [SerializeField] bool waitForStart;
        [Tooltip("Black quad just in front of the headset camera, faded in while scenes swap.")]
        [SerializeField] Renderer fade;
        [SerializeField] float fadeSeconds = 0.25f;
        [Tooltip("Longest a single step of a swap may take before the swap carries on without it (s). A step that " +
                 "never finishes would otherwise leave the player on a black screen with no way out.")]
        [SerializeField] float stepTimeout = 8f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static SceneFlow Instance { get; private set; }

        /// <summary>The area scene currently loaded, or null while the first one loads.</summary>
        public string CurrentScene { get; private set; }

        public bool Busy { get; private set; }

        /// <summary>True while the start screen is up and no area has loaded yet.</summary>
        public bool WaitingForStart { get; private set; }

        /// <summary>
        /// Which part of a swap is running, for the play check's stall report. A swap has several waits in it and
        /// "Busy = true" on its own does not say which one is stuck.
        /// </summary>
        public string Stage { get; private set; } = "idle";

        MaterialPropertyBlock block;

        void Awake()
        {
            Instance = this;
            block = new MaterialPropertyBlock();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            if (waitForStart)
            {
                WaitingForStart = true;
                yield break;
            }
            Busy = true;
            SetFade(1f);
            yield return Load(startScene);
            CurrentScene = startScene;
            yield return Fade(1f, 0f);
            Busy = false;
        }

        /// <summary>
        /// Leaves the start screen: fades out, loads the start scene, calls <paramref name="atBlack"/> (hide the lobby,
        /// put the rig at the start) and fades back in. False unless the start screen is waiting.
        /// </summary>
        public bool Begin(System.Action atBlack)
        {
            if (!WaitingForStart || Busy) return false;
            WaitingForStart = false;
            StartCoroutine(BeginRoutine(atBlack));
            return true;
        }

        IEnumerator BeginRoutine(System.Action atBlack)
        {
            Busy = true;
            yield return Fade(0f, 1f);
            yield return Load(startScene);
            CurrentScene = startScene;
            atBlack?.Invoke();
            // The rig's teleport is queued; let it land before the view comes back.
            yield return null;
            yield return null;
            yield return Fade(1f, 0f);
            Busy = false;
        }

        /// <summary>Swaps to <paramref name="scene"/> and puts the rig at door <paramref name="arrivalId"/>. False while a swap is running.</summary>
        public bool Go(string scene, string arrivalId)
        {
            if (Busy) return false;
            StartCoroutine(Swap(scene, arrivalId));
            return true;
        }

        IEnumerator Swap(string scene, string arrivalId)
        {
            Busy = true;
            Stage = "fading out";
            yield return Fade(0f, 1f);

            var old = CurrentScene;
            if (old != scene)
            {
                Stage = "releasing the CCTV feeds";
                // A CCTV room is loaded for observation only; release it before loading a playable copy. Guarded,
                // because the wall's release waits on its own in-flight load while its loader waits on Busy being
                // false — the two can end up waiting on each other, and a swap that never ends strands the player
                // on a black screen. Leaving the camera room is where this bites (play check, 2026-11-11).
                yield return Guarded(HighStakes.Security.SecurityMonitorWall.ReleaseBeforeTravel(),
                    "releasing the CCTV feeds");
                Stage = $"loading {scene}";
                yield return Load(scene);
                CurrentScene = scene;
                Stage = $"unloading {old}";
                if (old != null) yield return SceneManager.UnloadSceneAsync(old);
                Stage = "settling light probes";
                LightProbes.TetrahedralizeAsync();
            }

            Stage = $"placing the rig at {arrivalId}";
            if (DoorPortal.TryGetArrival(arrivalId, out var arrival))
            {
                DoorPortal.MoveRig(arrival);
            }
            else
            {
                Debug.LogError($"[SceneFlow] No door '{arrivalId}' in {scene}.");
                DoorPortal.Unblock();
            }

            // The rig's teleport is queued; let it land before the view comes back.
            yield return null;
            yield return null;
            Stage = "fading in";
            yield return Fade(1f, 0f);
            Stage = "idle";
            Busy = false;
        }

        /// <summary>
        /// Runs one step of a swap, but gives up waiting on it after <see cref="stepTimeout"/> and carries on. The step
        /// itself is left running — it is abandoned, not cancelled — so a slow step still finishes its work late, while
        /// a step that never finishes can no longer wedge the swap. Losing a frame of tidy-up beats a player stuck on
        /// a black screen, which is the one failure a test session cannot recover from.
        /// </summary>
        IEnumerator Guarded(IEnumerator step, string what)
        {
            bool done = false;
            StartCoroutine(Finishing(step, () => done = true));

            // Counts frames as well as seconds, and publishes both on Stage: if a swap ever stalls here again, the
            // report distinguishes "this step is slow" from "the game loop itself stopped ticking".
            int frames = 0;
            float started = Time.unscaledTime;
            while (!done && Time.unscaledTime - started < stepTimeout)
            {
                frames++;
                Stage = $"{what} ({frames} frames, {Time.unscaledTime - started:0.0} s of {stepTimeout:0.#} s)";
                yield return null;
            }

            if (!done)
                Debug.LogError($"[SceneFlow] {what} did not finish within {stepTimeout:0.#} s ({frames} frames); " +
                               "carrying on with the swap so the player is not left on a black screen.");
        }

        static IEnumerator Finishing(IEnumerator step, System.Action onDone)
        {
            yield return step;
            onDone();
        }

        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            // The area's ambient light and lightmaps apply only while it is the active scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            LightProbes.TetrahedralizeAsync();
        }

        IEnumerator Fade(float from, float to)
        {
            for (var t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / fadeSeconds));
                yield return null;
            }
            SetFade(to);
        }

        void SetFade(float alpha)
        {
            if (fade == null) return;
            fade.enabled = alpha > 0f;
            block.SetColor(BaseColor, new Color(0f, 0f, 0f, alpha));
            fade.SetPropertyBlock(block);
        }
    }
}
