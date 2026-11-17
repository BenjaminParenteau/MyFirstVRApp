using System;
using System.Collections.Generic;
using HighStakes.Environment.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HighStakes.Environment.Blackout
{
    /// <summary>
    /// Turns the lights out for real, and puts them back exactly as they were.
    /// <para>
    /// <b>Why this is more than disabling lights:</b> every area in this game is lightmapped, so switching off the
    /// <see cref="Light"/> components leaves all the baked light sitting exactly where it was and the room stays
    /// lit. Going genuinely dark means taking the lightmaps, the ambient, the skybox, the reflection probes and
    /// the camera clear colour as well — and because <see cref="RenderSettings"/> and
    /// <see cref="LightmapSettings"/> are per-scene, it means doing that again for each area the player walks
    /// into while the power is out, and remembering each one separately so it can be given back.
    /// </para>
    /// <para>
    /// <b>Restore is the dangerous half.</b> A blackout that half-restores leaves the casino permanently dark,
    /// which is a game-over bug with no recovery — so every value is captured before it is touched, restoration is
    /// keyed per scene, and <c>Run Audio Play Check</c> asserts the lighting is identical after a blackout to
    /// before it. The mode also cannot stick: <see cref="BlackoutLogic.Advance"/> always recovers from a cancelled
    /// fade, and <see cref="OnDisable"/> restores unconditionally.
    /// </para>
    /// The transition itself is masked by MVP_Main's ScreenFade quad (the same one <see cref="SceneFlow"/> and
    /// <c>TeleportBlink</c> share): fade to black, do the real teardown behind it, fade back up to reveal a world
    /// that is actually unlit. Without the mask, killing lightmaps pops visibly.
    /// </summary>
    public sealed class BlackoutMode : MonoBehaviour
    {
        [Tooltip("The black quad in front of the headset camera, shared with SceneFlow. Masks the teardown.")]
        [SerializeField] private Renderer fade;

        [Tooltip("How often to re-apply darkness while it is dark, so an area loaded mid-blackout goes dark too (s).")]
        [SerializeField, Min(0.1f)] private float rescanSeconds = 0.4f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>The live instance, or null.</summary>
        public static BlackoutMode Instance { get; private set; }

        /// <summary>
        /// Raised when the world goes dark (true) and when the lights are back (false). Subscribe from your own
        /// component to hide anything of yours that emits light — or just add <see cref="HideDuringBlackout"/> to
        /// the object, which does it for you.
        /// </summary>
        public static event Action<bool> Changed;

        /// <summary>Where the blackout is in its cycle.</summary>
        public static BlackoutStage Stage { get; private set; } = BlackoutStage.Off;

        /// <summary>True once the world is actually dark (not during the fade out of it).</summary>
        public static bool IsDark => Stage == BlackoutStage.Dark;

        /// <summary>True whenever a blackout is in progress at all, including both fades.</summary>
        public static bool IsActive => Stage != BlackoutStage.Off;

        /// <summary>Why the lights are out, for the Guide and the log. Empty when they are on.</summary>
        public static string Reason { get; private set; } = string.Empty;

        bool wantDark;
        float stageElapsed;
        bool tornDown;
        float nextRescan;
        MaterialPropertyBlock block;

        readonly Dictionary<string, SceneLighting> savedScenes = new Dictionary<string, SceneLighting>();
        readonly List<Light> killedLights = new List<Light>();
        readonly List<ReflectionProbe> killedProbes = new List<ReflectionProbe>();
        readonly Dictionary<Camera, CameraClear> savedCameras = new Dictionary<Camera, CameraClear>();

        // ---------------------------------------------------------------- API

        /// <summary>
        /// Cut the power. <paramref name="reason"/> is free text for the Guide and the log ("breaker",
        /// "blackout run"). Safe to call when already dark.
        /// </summary>
        public static void Begin(string reason)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[Blackout] No BlackoutMode in the scene — run High Stakes > Audio > Build Soundscape.");
                return;
            }
            Reason = reason ?? string.Empty;
            Instance.wantDark = true;
        }

        /// <summary>Bring the power back. Safe to call when already lit.</summary>
        public static void End()
        {
            if (Instance != null) Instance.wantDark = false;
        }

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[Blackout] A second BlackoutMode is active on '{name}'. Keep one, in MVP_Main.", this);
                enabled = false;
                return;
            }
            Instance = this;
            block = new MaterialPropertyBlock();
        }

        void OnDisable()
        {
            // Unconditional: whatever state we were in, the player must not be left in the dark.
            if (tornDown) RestoreAll();
            SetFade(0f);
            Stage = BlackoutStage.Off;
            wantDark = false;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var next = BlackoutLogic.Advance(Stage, wantDark, stageElapsed);
            if (next != Stage)
            {
                Stage = next;
                stageElapsed = 0f;
                OnStageEntered(next);
            }
            else
            {
                stageElapsed += dt;
            }

            // The mask follows the darkness ramp; at full dark it is transparent again and the world itself is black.
            float darkness = BlackoutLogic.Darkness(Stage, stageElapsed);
            SetFade(Stage == BlackoutStage.Dark ? 0f : darkness);

            Soundscape.SetBlackout(BlackoutLogic.IsDarkEnoughToNeedSound(Stage));

            if (Stage == BlackoutStage.Dark && Time.unscaledTime >= nextRescan)
            {
                nextRescan = Time.unscaledTime + rescanSeconds;
                ApplyDarkness();        // an area loaded mid-blackout arrives fully lit; darken it too
            }
        }

        void OnStageEntered(BlackoutStage stage)
        {
            switch (stage)
            {
                case BlackoutStage.Dark:
                    // The screen is already masked to black, so the teardown is invisible.
                    ApplyDarkness();
                    tornDown = true;
                    nextRescan = Time.unscaledTime + rescanSeconds;
                    Changed?.Invoke(true);
                    Debug.Log($"[Blackout] Lights out ({Reason}).");
                    break;

                case BlackoutStage.PowerUp:
                    if (tornDown)
                    {
                        RestoreAll();
                        tornDown = false;
                        Changed?.Invoke(false);
                    }
                    Debug.Log("[Blackout] Power restored.");
                    break;

                case BlackoutStage.Off:
                    Reason = string.Empty;
                    break;
            }
        }

        // ---------------------------------------------------------------- going dark

        /// <summary>
        /// Darken everything currently loaded, capturing anything not already captured. Idempotent, so it is safe
        /// to call repeatedly — which is how an area loaded during a blackout gets darkened.
        /// </summary>
        void ApplyDarkness()
        {
            CaptureActiveSceneIfNew();

            // Realtime lights.
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light == null || !light.enabled) continue;
                light.enabled = false;
                killedLights.Add(light);
            }

            // Baked light. This is the one people forget: without it, a lightmapped room stays lit.
            if (LightmapSettings.lightmaps != null && LightmapSettings.lightmaps.Length > 0)
                LightmapSettings.lightmaps = new LightmapData[0];

            // Ambient, skybox, reflections, fog — each a way for light to survive the above.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientSkyColor = Color.black;
            RenderSettings.ambientEquatorColor = Color.black;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = false;

            foreach (var probe in FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (probe == null || !probe.enabled) continue;
                probe.enabled = false;
                killedProbes.Add(probe);
            }

            // Cameras must clear to black, or the skybox-less background shows the clear colour.
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null || savedCameras.ContainsKey(cam)) continue;
                savedCameras[cam] = new CameraClear { Flags = cam.clearFlags, Background = cam.backgroundColor };
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void CaptureActiveSceneIfNew()
        {
            string key = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(key) || savedScenes.ContainsKey(key)) return;

            savedScenes[key] = new SceneLighting
            {
                Lightmaps = LightmapSettings.lightmaps,
                LightmapsMode = LightmapSettings.lightmapsMode,
                AmbientMode = RenderSettings.ambientMode,
                Ambient = RenderSettings.ambientLight,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                AmbientIntensity = RenderSettings.ambientIntensity,
                Skybox = RenderSettings.skybox,
                ReflectionMode = RenderSettings.defaultReflectionMode,
                CustomReflection = RenderSettings.customReflectionTexture,
                ReflectionIntensity = RenderSettings.reflectionIntensity,
                Fog = RenderSettings.fog,
            };
        }

        // ---------------------------------------------------------------- putting it back

        void RestoreAll()
        {
            foreach (var light in killedLights)
                if (light != null) light.enabled = true;
            killedLights.Clear();

            foreach (var probe in killedProbes)
                if (probe != null) probe.enabled = true;
            killedProbes.Clear();

            foreach (var pair in savedCameras)
                if (pair.Key != null)
                {
                    pair.Key.clearFlags = pair.Value.Flags;
                    pair.Key.backgroundColor = pair.Value.Background;
                }
            savedCameras.Clear();

            // Restore the scene that is active now, and remember the rest in case the player walked back.
            string active = SceneManager.GetActiveScene().name;
            if (savedScenes.TryGetValue(active, out var saved)) Apply(saved);
            else if (savedScenes.Count > 0)
            {
                // The player ended the blackout somewhere we never captured. Restoring another scene's values here
                // would be wrong, so leave render settings alone and say so rather than guessing.
                Debug.LogWarning($"[Blackout] No captured lighting for active scene '{active}'; " +
                                 "lights and probes restored, render settings left as the scene authored them.");
            }
            savedScenes.Clear();
        }

        static void Apply(SceneLighting s)
        {
            LightmapSettings.lightmapsMode = s.LightmapsMode;
            LightmapSettings.lightmaps = s.Lightmaps;
            RenderSettings.ambientMode = s.AmbientMode;
            RenderSettings.ambientLight = s.Ambient;
            RenderSettings.ambientSkyColor = s.AmbientSky;
            RenderSettings.ambientEquatorColor = s.AmbientEquator;
            RenderSettings.ambientGroundColor = s.AmbientGround;
            RenderSettings.ambientIntensity = s.AmbientIntensity;
            RenderSettings.skybox = s.Skybox;
            RenderSettings.defaultReflectionMode = s.ReflectionMode;
            RenderSettings.customReflectionTexture = s.CustomReflection;
            RenderSettings.reflectionIntensity = s.ReflectionIntensity;
            RenderSettings.fog = s.Fog;
        }

        void SetFade(float value)
        {
            if (fade == null) return;
            fade.enabled = value > 0.001f;
            block.SetColor(BaseColor, new Color(0f, 0f, 0f, value));
            fade.SetPropertyBlock(block);
        }

        struct CameraClear
        {
            public CameraClearFlags Flags;
            public Color Background;
        }

        class SceneLighting
        {
            public LightmapData[] Lightmaps;
            public LightmapsMode LightmapsMode;
            public AmbientMode AmbientMode;
            public Color Ambient;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public float AmbientIntensity;
            public Material Skybox;
            public DefaultReflectionMode ReflectionMode;
            public Texture CustomReflection;
            public float ReflectionIntensity;
            public bool Fog;
        }
    }
}
