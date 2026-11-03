using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HighStakes.Environment.TheReturn
{
    /// <summary>
    /// Drives the dark-to-dawn lighting of The Return: the room starts in the cold blue of the vault, and every pay
    /// envelope the player fills by hand warms it one step, until the eighth brings sunrise through the window. The
    /// player's own acts of restitution are what bring light into the room, so nothing here is on a timer — the state
    /// is only ever moved by <see cref="EnvelopeFilled"/>.
    ///
    /// One of these lives in the scene (singleton, reachable as <see cref="Instance"/>) so the envelopes don't each
    /// need a reference wired to it.
    ///
    /// The room's baked lightmaps come from the vault and stay cold (they cannot be re-baked on a machine without a
    /// GPU, StyleGuide §9), so the warm-up is realtime on top of that bake: the two lights, the volume's Color
    /// Adjustments colour filter, and the scene's ambient light. That reads as dawn without a re-bake.
    /// </summary>
    public class RestitutionManager : MonoBehaviour
    {
        [Header("Lights")]
        [Tooltip("The room's main light. Starts cold and dim, ends warm and bright.")]
        [SerializeField] Light mainLight;
        [Tooltip("The light at the window that becomes the sunrise.")]
        [SerializeField] Light windowLight;

        [Header("Post-processing")]
        [Tooltip("Volume whose profile has a Color Adjustments override; its colour filter is tinted with the lights.")]
        [SerializeField] Volume volume;

        [Header("Sunrise (all envelopes filled)")]
        [Tooltip("Dust motes in the window's light shafts. Switched off until every envelope is filled.")]
        [SerializeField] ParticleSystem dustMotes;
        [Tooltip("The light-shaft mesh at the window. Switched off until every envelope is filled.")]
        [SerializeField] GameObject lightShafts;

        [Header("Tuning")]
        [Tooltip("Envelopes to fill before sunrise: one per member of staff the syndicate skimmed.")]
        [SerializeField] int totalEnvelopes = 8;
        [Tooltip("Cold vault blue the room starts in.")]
        [SerializeField] Color coldColor = new Color32(0x3A, 0x5A, 0x8C, 0xFF);
        [Tooltip("Warm sunrise the room ends in.")]
        [SerializeField] Color warmColor = new Color32(0xFF, 0xC2, 0x7A, 0xFF);
        [SerializeField] float startIntensity = 0.3f;
        [SerializeField] float endIntensity = 1.4f;
        [Tooltip("How long one envelope's step of warming takes.")]
        [SerializeField] float stepSeconds = 1.5f;
        [Tooltip("How long the room takes to drain back to vault blue on the Kept ending.")]
        [SerializeField] float coolSeconds = 2.5f;

        [Header("Events")]
        [Tooltip("Fired once, when the last envelope is filled.")]
        public UnityEvent OnAllReturned;

        public static RestitutionManager Instance { get; private set; }

        /// <summary>Envelopes filled so far.</summary>
        public int Filled { get; private set; }

        public int Total => totalEnvelopes;

        /// <summary>0 at the start, 1 once every envelope is filled.</summary>
        public float Progress => totalEnvelopes <= 0 ? 1f : (float)Filled / totalEnvelopes;

        /// <summary>True once the last envelope has been filled.</summary>
        public bool AllReturned => Filled >= totalEnvelopes;

        ColorAdjustments colorAdjustments;
        Coroutine warming;
        Color ambientBefore;
        bool held;

        void Awake()
        {
            // The warm-up drives RenderSettings, which is scene state: remember it so leaving Play mode in the Editor
            // does not leave the scene saved at sunrise.
            ambientBefore = RenderSettings.ambientLight;

            if (Instance != null && Instance != this)
                Debug.LogWarning("[Return] A second RestitutionManager is active. The newest one wins.", this);
            Instance = this;

            // Volume.profile hands back a per-instance copy, so tinting it never dirties the shared style-kit asset.
            if (volume != null && !volume.profile.TryGet(out colorAdjustments))
                Debug.LogWarning("[Return] The volume's profile has no Color Adjustments override; the room will warm with lights only.", this);

            Apply(coldColor, startIntensity);
            ShowSunrise(false);
        }

        void OnDestroy()
        {
            RenderSettings.ambientLight = ambientBefore;
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// One more envelope has been filled and sealed: warms the room a step toward sunrise over
        /// <see cref="stepSeconds"/>, and fires <see cref="OnAllReturned"/> on the last one. Extra calls past the last
        /// envelope are ignored, so an envelope cannot be counted twice.
        /// </summary>
        public void EnvelopeFilled()
        {
            if (AllReturned || held) return;
            Filled++;

            if (warming != null) StopCoroutine(warming);
            warming = StartCoroutine(WarmToProgress());
        }

        /// <summary>
        /// The Kept ending: the player is leaving with the money, so the warmth their earlier envelopes earned drains
        /// back out to vault blue and the room stops responding. No further envelope can warm it.
        /// </summary>
        public void HoldDark()
        {
            held = true;
            if (warming != null) StopCoroutine(warming);
            ShowSunrise(false);
            warming = StartCoroutine(Cool());
        }

        IEnumerator Cool()
        {
            Color fromColor = CurrentColor();
            float fromIntensity = CurrentIntensity();
            for (var t = 0f; t < coolSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / coolSeconds);
                Apply(Color.Lerp(fromColor, coldColor, k), Mathf.Lerp(fromIntensity, startIntensity, k));
                yield return null;
            }
            Apply(coldColor, startIntensity);
            warming = null;
        }

        /// <summary>
        /// Lerps from whatever the room looks like now to what this many filled envelopes should look like, so an
        /// envelope filled while the previous step is still running picks up smoothly instead of snapping.
        /// </summary>
        IEnumerator WarmToProgress()
        {
            bool last = AllReturned;
            Color fromColor = CurrentColor();
            float fromIntensity = CurrentIntensity();
            Color toColor = Color.Lerp(coldColor, warmColor, Progress);
            float toIntensity = Mathf.Lerp(startIntensity, endIntensity, Progress);

            for (var t = 0f; t < stepSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / stepSeconds);
                Apply(Color.Lerp(fromColor, toColor, k), Mathf.Lerp(fromIntensity, toIntensity, k));
                yield return null;
            }
            Apply(toColor, toIntensity);
            warming = null;

            if (!last) yield break;
            ShowSunrise(true);
            OnAllReturned?.Invoke();
        }

        void Apply(Color color, float intensity)
        {
            if (mainLight != null)
            {
                mainLight.color = color;
                mainLight.intensity = intensity;
            }
            if (windowLight != null)
            {
                windowLight.color = color;
                windowLight.intensity = intensity;
            }
            // The cold bake underneath only shifts if the ambient term moves with the lights.
            RenderSettings.ambientLight = color * Mathf.Lerp(0.25f, 0.6f, Mathf.InverseLerp(startIntensity, endIntensity, intensity));
            if (colorAdjustments != null) colorAdjustments.colorFilter.value = color;
        }

        Color CurrentColor()
        {
            if (mainLight != null) return mainLight.color;
            if (windowLight != null) return windowLight.color;
            return coldColor;
        }

        float CurrentIntensity()
        {
            if (mainLight != null) return mainLight.intensity;
            if (windowLight != null) return windowLight.intensity;
            return startIntensity;
        }

        /// <summary>The window's light shafts and the dust drifting in them: only once everyone has been paid back.</summary>
        void ShowSunrise(bool on)
        {
            if (lightShafts != null) lightShafts.SetActive(on);
            if (dustMotes == null) return;
            if (on) dustMotes.Play();
            else dustMotes.Clear();
            dustMotes.gameObject.SetActive(on);
        }
    }
}
