using System.Collections.Generic;
using UnityEngine;

namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// The game's one sound system. Keep exactly one in <c>MVP_Main</c> (added by
    /// <b>High Stakes &gt; Audio &gt; Build Soundscape</b>); it survives door swaps, so the mix and the bus volumes
    /// don't restart every time the player walks through a door.
    /// <para>
    /// <b>Everyone calls it the same way, from their own files, one line per sound:</b>
    /// <code>
    /// using HighStakes.Environment.Audio;
    ///
    /// Soundscape.PlayAt("table.chip.bet", spot.position);   // 3D one-shot where it happened
    /// Soundscape.Play("ui.press");                          // 2D, for UI and anything "in your head"
    /// Soundscape.PlayOn("table.roulette.ball", ball);       // 3D one-shot following a moving transform
    /// var hum = Soundscape.Loop("sec.cctv.hum", wall);      // looping emitter; hum.Stop() to end it
    /// </code>
    /// All of it is safe to call when no Soundscape exists — it no-ops. That matters: three other slots have
    /// EditMode tests and solo scenes with no <c>MVP_Main</c> in them, and none of them should have to care.
    /// An unknown id logs <b>once</b> and carries on rather than throwing or spamming a frame-rate hole.
    /// </para>
    /// <para>
    /// There is no AudioMixer and no global cue list by design — see <see cref="SoundBus"/> and
    /// <see cref="SoundCueSet"/>, and docs/milestone6/README.md for why.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class Soundscape : MonoBehaviour
    {
        [Header("One-shot pool")]
        [Tooltip("Pooled 3D sources for one-shots. 24 is comfortably inside the 32-real-voice budget (§7) with " +
                 "room for the beds and the looping emitters.")]
        [SerializeField, Min(4)] private int poolSize = 24;

        [Header("Beds")]
        [Tooltip("Seconds to crossfade music and room tone when the area changes. Door travel already fades the " +
                 "view, so this roughly matches it.")]
        [SerializeField, Min(0f)] private float crossfadeSeconds = 1.5f;

        [Tooltip("Follow SceneFlow and swap the beds automatically as the player moves between areas.")]
        [SerializeField] private bool followSceneFlow = true;

        /// <summary>The live instance, or null. Prefer the static helpers over touching this.</summary>
        public static Soundscape Instance { get; private set; }

        readonly Dictionary<string, SoundCue> cues = new Dictionary<string, SoundCue>();
        readonly HashSet<string> warned = new HashSet<string>();
        readonly float[] busVolume = new float[BusLogic.BusCount];

        AudioSource[] pool;
        Transform[] poolFollow;
        float[] poolStarted;
        int[] lastVariant;                      // per-cue last clip index, so nothing repeats twice running

        BedPlayer music;
        BedPlayer ambience;
        string currentArea;

        readonly List<LoopHandle> loops = new List<LoopHandle>();
        bool blackout;
        float duckBlend;                        // 0 = normal mix, 1 = full blackout mix

        /// <summary>How many cues were discovered across all four slots' sets. Logged at startup; used by checks.</summary>
        public int CueCount => cues.Count;

        /// <summary>The area whose beds are playing, or null before the first area loads.</summary>
        public string CurrentArea => currentArea;

        /// <summary>True while the blackout mix is applied.</summary>
        public static bool IsBlackout => Instance != null && Instance.blackout;

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[Soundscape] A second Soundscape is active on '{name}'. Keep exactly one, in " +
                               "MVP_Main. Disabling this one.", this);
                enabled = false;
                return;
            }
            Instance = this;

            for (int i = 0; i < busVolume.Length; i++)
            {
                var bus = (SoundBus)i;
                busVolume[i] = BusLogic.Clamp01(PlayerPrefs.GetFloat(BusLogic.Key(bus), BusLogic.Default(bus)));
            }

            Discover();
            BuildPool();

            music = new BedPlayer(transform, "Music");
            ambience = new BedPlayer(transform, "Ambience");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Find every slot's cue set. <c>Resources.LoadAll</c> merges all <c>Resources/Audio/</c> folders in the
        /// project, which is exactly why the four sets can live in four different slots' content folders and still
        /// all be found with no shared file listing them.
        /// </summary>
        void Discover()
        {
            cues.Clear();
            variantSlot.Clear();
            var sets = Resources.LoadAll<SoundCueSet>("Audio");
            foreach (var set in sets)
            {
                if (set == null || set.cues == null) continue;
                foreach (var cue in set.cues)
                {
                    if (cue == null || string.IsNullOrEmpty(cue.id)) continue;
                    if (cues.ContainsKey(cue.id))
                    {
                        // Two slots named the same cue. Validate Cues reports this properly with both owners.
                        Debug.LogError($"[Soundscape] Duplicate cue id '{cue.id}' (also in '{set.name}'). " +
                                       "Run High Stakes > Audio > Validate Cues.", set);
                        continue;
                    }
                    cues.Add(cue.id, cue);
                }
            }
            lastVariant = new int[cues.Count];
            for (int i = 0; i < lastVariant.Length; i++) lastVariant[i] = -1;

            // Give each cue a stable slot in lastVariant, so variant memory costs no dictionary churn per play.
            int n = 0;
            foreach (var kv in cues) variantSlot[kv.Key] = n++;

            Debug.Log($"[Soundscape] {cues.Count} cues from {sets.Length} cue set(s).");
        }

        readonly Dictionary<string, int> variantSlot = new Dictionary<string, int>();

        void BuildPool()
        {
            pool = new AudioSource[poolSize];
            poolFollow = new Transform[poolSize];
            poolStarted = new float[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"OneShot_{i:00}");
                go.transform.SetParent(transform, false);
                pool[i] = go.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                poolStarted[i] = float.NegativeInfinity;
            }
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Ease the blackout duck rather than switching it, so the lights going out doesn't click the mix.
            float target = blackout ? 1f : 0f;
            if (!Mathf.Approximately(duckBlend, target))
                duckBlend = Mathf.MoveTowards(duckBlend, target, dt / 0.6f);

            if (followSceneFlow) FollowArea();

            music.Update(dt, BusFor(SoundBus.Music), Duck(SoundBus.Music));
            ambience.Update(dt, BusFor(SoundBus.Ambience), Duck(SoundBus.Ambience));

            // One-shots that follow a moving object, and freeing finished voices.
            for (int i = 0; i < pool.Length; i++)
            {
                if (poolFollow[i] != null)
                {
                    if (!pool[i].isPlaying) poolFollow[i] = null;
                    else pool[i].transform.position = poolFollow[i].position;
                }
            }

            for (int i = loops.Count - 1; i >= 0; i--)
            {
                if (!loops[i].Tick(BusFor(loops[i].Bus), Duck(loops[i].Bus)))
                    loops.RemoveAt(i);
            }
        }

        void FollowArea()
        {
            var flow = SceneFlow.Instance;
            string area = flow != null ? flow.CurrentScene : null;
            if (area == currentArea) return;

            currentArea = area;
            var beds = SoundscapeLogic.BedsFor(area);
            music.PlayBed(Find(beds.Music), crossfadeSeconds);
            ambience.PlayBed(Find(beds.Ambience), crossfadeSeconds);
        }

        float BusFor(SoundBus bus) => busVolume[(int)bus];

        float Duck(SoundBus bus) => BusLogic.Blend(1f, BusLogic.BlackoutDuck(bus), duckBlend);

        SoundCue Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cues.TryGetValue(id, out var cue)) return cue;
            if (warned.Add(id))
                Debug.LogWarning($"[Soundscape] No cue '{id}'. Owner: slot {SoundscapeLogic.OwnerOf(id) ?? "?"}. " +
                                 "Add it to that slot's cue set and run High Stakes > Audio > Validate Cues.");
            return null;
        }

        // ---------------------------------------------------------------- the API

        /// <summary>Play a 2D one-shot — UI, or anything that belongs to the player rather than the room.</summary>
        public static void Play(string cueId) => PlayInternal(cueId, null, null);

        /// <summary>Play a 3D one-shot at a world position. The normal way to sound an interaction.</summary>
        public static void PlayAt(string cueId, Vector3 position) => PlayInternal(cueId, position, null);

        /// <summary>
        /// Play a 3D one-shot that follows a moving transform for its duration (a ball, a card, a cart).
        /// Safe if the transform is destroyed mid-sound.
        /// </summary>
        public static void PlayOn(string cueId, Transform follow)
            => PlayInternal(cueId, follow != null ? follow.position : (Vector3?)null, follow);

        static void PlayInternal(string cueId, Vector3? position, Transform follow)
        {
            var s = Instance;
            if (s == null) return;                       // no soundscape loaded: silently do nothing, by design

            var cue = s.Find(cueId);
            if (cue == null || !cue.HasClips) return;

            if (cue.loop)
            {
                Debug.LogWarning($"[Soundscape] '{cueId}' is a looping cue — use Soundscape.Loop, not Play/PlayAt.");
                return;
            }

            int voice = s.TakeVoice();
            if (voice < 0) return;

            var src = s.pool[voice];
            int slot = s.variantSlot.TryGetValue(cueId, out int vs) ? vs : -1;
            int last = slot >= 0 ? s.lastVariant[slot] : -1;
            int pick = SoundscapeLogic.PickVariant(cue.ClipCount, last, Random.value);
            if (pick < 0) return;
            if (slot >= 0) s.lastVariant[slot] = pick;

            AudioSpatial.Apply(src, cue);
            src.clip = cue.ClipAt(pick);
            src.pitch = SoundscapeLogic.PitchFor(cue.pitchJitter, Random.value);
            src.volume = BusLogic.Gain(s.BusFor(cue.bus), cue.volume, s.Duck(cue.bus));
            src.transform.position = position ?? Vector3.zero;
            s.poolFollow[voice] = follow;
            s.poolStarted[voice] = Time.unscaledTime;
            src.Play();
        }

        int TakeVoice()
        {
            if (pool == null) return -1;
            var busy = busyScratch ?? (busyScratch = new bool[pool.Length]);
            for (int i = 0; i < pool.Length; i++) busy[i] = pool[i].isPlaying;
            int voice = SoundscapeLogic.PickVoice(busy, poolStarted);
            if (voice >= 0 && pool[voice].isPlaying) pool[voice].Stop();    // stealing the oldest
            return voice;
        }

        bool[] busyScratch;

        /// <summary>
        /// Start a looping emitter at (and following) a transform. Returns a handle; call
        /// <see cref="LoopHandle.Stop"/> when the thing stops making noise. Always safe, even with no Soundscape —
        /// you get back a dead handle rather than null, so callers never need a null check.
        /// </summary>
        public static LoopHandle Loop(string cueId, Transform at)
        {
            var s = Instance;
            if (s == null) return LoopHandle.Dead;

            var cue = s.Find(cueId);
            if (cue == null || !cue.HasClips) return LoopHandle.Dead;

            var go = new GameObject($"Loop_{cueId}");
            go.transform.SetParent(s.transform, false);
            var src = go.AddComponent<AudioSource>();
            var cueLoop = cue;
            AudioSpatial.Apply(src, cueLoop);
            src.loop = true;
            src.clip = cueLoop.ClipAt(0);
            src.volume = BusLogic.Gain(s.BusFor(cueLoop.bus), cueLoop.volume, s.Duck(cueLoop.bus));
            if (at != null) src.transform.position = at.position;
            src.Play();

            var handle = new LoopHandle(src, at, cueLoop);
            s.loops.Add(handle);
            return handle;
        }

        /// <summary>Set a bus volume (0..1) and save it. Called by the notebook's AUDIO page.</summary>
        public static void SetBusVolume(SoundBus bus, float volume)
        {
            volume = BusLogic.Clamp01(volume);
            PlayerPrefs.SetFloat(BusLogic.Key(bus), volume);
            if (Instance != null) Instance.busVolume[(int)bus] = volume;
        }

        /// <summary>Current bus volume (0..1). Falls back to the saved value, then the default, with no instance.</summary>
        public static float GetBusVolume(SoundBus bus)
            => Instance != null
                ? Instance.busVolume[(int)bus]
                : BusLogic.Clamp01(PlayerPrefs.GetFloat(BusLogic.Key(bus), BusLogic.Default(bus)));

        /// <summary>
        /// Switch the blackout mix on or off: music almost gone, room tone and effects up, voice untouched.
        /// Called by <c>BlackoutMode</c>; eased over ~0.6 s so it doesn't click.
        /// </summary>
        public static void SetBlackout(bool on)
        {
            if (Instance != null) Instance.blackout = on;
        }

        /// <summary>True if a cue with this id was discovered. For play checks and the validator.</summary>
        public static bool Has(string cueId)
            => Instance != null && !string.IsNullOrEmpty(cueId) && Instance.cues.ContainsKey(cueId);

        // ---------------------------------------------------------------- helpers

        /// <summary>A running looping emitter. Stop it when the thing stops.</summary>
        public class LoopHandle
        {
            /// <summary>A handle that does nothing, returned when there is no soundscape to play on.</summary>
            public static readonly LoopHandle Dead = new LoopHandle(null, null, null);

            readonly AudioSource source;
            readonly Transform follow;
            readonly SoundCue cue;

            internal LoopHandle(AudioSource source, Transform follow, SoundCue cue)
            {
                this.source = source;
                this.follow = follow;
                this.cue = cue;
            }

            internal SoundBus Bus => cue != null ? cue.bus : SoundBus.Ambience;

            float volumeScale = 1f;

            /// <summary>True while this handle is actually making noise.</summary>
            public bool IsPlaying => source != null && source.isPlaying;

            /// <summary>
            /// Live pitch for a running loop — a wheel slowing down, a proximity tone rising. Safe on a dead handle.
            /// </summary>
            public void SetPitch(float pitch)
            {
                if (source != null) source.pitch = pitch;
            }

            /// <summary>
            /// Scale this loop's volume on top of its cue level and bus (0..1), for a loop that fades with
            /// something — distance, speed, how close a wall is. Safe on a dead handle.
            /// </summary>
            public void SetVolumeScale(float scale)
            {
                volumeScale = scale < 0f ? 0f : scale;
            }

            /// <summary>Stop and clean up. Safe to call twice, and safe on a dead handle.</summary>
            public void Stop()
            {
                if (source == null) return;
                source.Stop();
                // Object.Destroy explicitly: LoopHandle is a plain nested class, not a MonoBehaviour, so it has
                // no inherited Destroy of its own.
                Object.Destroy(source.gameObject);
            }

            /// <summary>Returns false once this loop is finished and should be dropped from the list.</summary>
            internal bool Tick(float busVolume, float duck)
            {
                if (source == null || cue == null) return false;
                if (follow != null) source.transform.position = follow.position;
                source.volume = BusLogic.Gain(busVolume, cue.volume * volumeScale, duck);
                return true;
            }
        }

        /// <summary>Two sources and a crossfade, used for the music bed and the room tone.</summary>
        class BedPlayer
        {
            readonly AudioSource a;
            readonly AudioSource b;
            bool aIsCurrent;
            float elapsed;
            float duration;
            bool fading;

            internal BedPlayer(Transform parent, string label)
            {
                a = Make(parent, label + "_A");
                b = Make(parent, label + "_B");
            }

            static AudioSource Make(Transform parent, string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var src = go.AddComponent<AudioSource>();
                AudioSpatial.ApplyBed(src);
                return src;
            }

            AudioSource Current => aIsCurrent ? a : b;
            AudioSource Previous => aIsCurrent ? b : a;

            internal void PlayBed(SoundCue cue, float crossfade)
            {
                var clip = cue != null ? cue.ClipAt(0) : null;
                if (Current.clip == clip && Current.isPlaying) return;      // already the right bed

                aIsCurrent = !aIsCurrent;
                Current.clip = clip;
                Current.volume = 0f;
                if (clip != null) Current.Play();

                duration = crossfade;
                elapsed = 0f;
                fading = true;
            }

            internal void Update(float dt, float busVolume, float duck)
            {
                var cur = Current;
                var prev = Previous;

                if (fading)
                {
                    elapsed += dt;
                    SoundscapeLogic.Crossfade(elapsed, duration, out float outGain, out float inGain);
                    cur.volume = BusLogic.Gain(busVolume, inGain, duck);
                    prev.volume = BusLogic.Gain(busVolume, outGain, duck);
                    if (elapsed >= duration)
                    {
                        fading = false;
                        if (prev.isPlaying) prev.Stop();
                        prev.clip = null;
                    }
                }
                else
                {
                    cur.volume = BusLogic.Gain(busVolume, 1f, duck);
                }
            }
        }
    }
}
