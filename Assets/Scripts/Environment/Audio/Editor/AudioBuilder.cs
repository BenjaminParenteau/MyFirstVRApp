using System.Collections.Generic;
using System.IO;
using HighStakes.Environment.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts the sound system into MVP_Main and builds Slot D's own cue set. Re-running replaces what it added;
/// <see cref="WorldBuilder"/>.BuildMain calls <see cref="AddTo"/> too, so a world rebuild never drops the audio.
/// <para>
/// This is the <b>gate</b> the other three slots were waiting on: once this has run, A, B and C can each build
/// their own cue set and call <c>Soundscape.PlayAt(...)</c> from their own files, touching nothing of anyone
/// else's. Their builders call <see cref="AudioImport"/> rather than copying import settings.
/// </para>
/// Slot D's clips come from <c>python tools/audio/generate_environment_audio.py</c>; this menu imports whatever
/// is there and is happy with none of it existing yet.
/// </summary>
public static class AudioBuilder
{
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string RootName = "Soundscape";

    /// <summary>Slot D's generated clips.</summary>
    const string ClipFolder = "Assets/Content/Environment/Audio";

    /// <summary>Slot D's cue set. Must be under a Resources/Audio folder — that is how the game finds it.</summary>
    const string SetPath = "Assets/Content/Environment/Resources/Audio/CueSet_Environment.asset";

    [MenuItem("High Stakes/Audio/Build Soundscape")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int imported = ImportClips();
        var set = BuildCueSet();

        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        AddTo(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Audio] MVP_Main has the Soundscape. Slot D cue set: {set.cues.Length} cue(s), " +
                  $"{imported} clip(s) (re)imported.\n" +
                  "Other slots: build your own cue set from your own menu and call Soundscape.PlayAt(...) — " +
                  "see docs/milestone6/ for your brief. Then run High Stakes > Audio > Validate Cues.");
    }

    /// <summary>Batch entry point (-executeMethod AudioBuilder.BuildBatch).</summary>
    public static void BuildBatch()
    {
        ImportClips();
        BuildCueSet();
        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        AddTo(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Add (or replace) the Soundscape object in a scene. Called by WorldBuilder.BuildMain as well.</summary>
    public static void AddTo(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == RootName) Object.DestroyImmediate(root);

        var holder = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(holder, scene);
        holder.AddComponent<Soundscape>();
    }

    /// <summary>
    /// Import every wav in Slot D's audio folder with the right settings for what it is. Returns how many
    /// actually changed. Other slots do the same for their own folder — call
    /// <see cref="AudioImport.Apply"/>, don't duplicate the settings.
    /// </summary>
    static int ImportClips()
    {
        if (!Directory.Exists(ClipFolder))
        {
            Debug.Log($"[Audio] No clips yet at {ClipFolder} — run " +
                      "'python tools/audio/generate_environment_audio.py' to synthesise them. " +
                      "The system works without them; areas are just silent.");
            return 0;
        }

        int changed = 0;
        foreach (var path in Directory.GetFiles(ClipFolder, "*.wav", SearchOption.AllDirectories))
        {
            string assetPath = path.Replace('\\', '/');
            if (AudioImport.Apply(assetPath, AudioImport.KindFor(Path.GetFileNameWithoutExtension(assetPath))))
                changed++;
        }
        return changed;
    }

    /// <summary>
    /// Create or update Slot D's cue set from the clips on disk, preserving any hand-tuned values on cues that
    /// already exist (so re-running the builder never undoes a volume someone set in the Inspector).
    /// </summary>
    static SoundCueSet BuildCueSet()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SetPath));

        var set = AssetDatabase.LoadAssetAtPath<SoundCueSet>(SetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<SoundCueSet>();
            set.slot = "D";
            AssetDatabase.CreateAsset(set, SetPath);
        }
        set.slot = "D";

        var existing = new Dictionary<string, SoundCue>();
        if (set.cues != null)
            foreach (var cue in set.cues)
                if (cue != null && !string.IsNullOrEmpty(cue.id)) existing[cue.id] = cue;

        var built = new List<SoundCue>();
        foreach (var spec in Specs)
        {
            var clips = LoadClips(spec.ClipNames);
            if (clips.Count == 0) continue;                      // not generated yet — skip, don't add a silent cue

            if (!existing.TryGetValue(spec.Id, out var cue))
            {
                cue = new SoundCue { id = spec.Id };
                cue.bus = spec.Bus;
                cue.volume = spec.Volume;
                cue.pitchJitter = spec.Jitter;
                cue.spatial = spec.Spatial;
                cue.minDistance = spec.MinDistance;
                cue.maxDistance = spec.MaxDistance;
                cue.loop = spec.Loop;
            }
            cue.clips = clips.ToArray();                          // clips always refreshed; tuning preserved
            built.Add(cue);
        }

        set.cues = built.ToArray();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return set;
    }

    static List<AudioClip> LoadClips(string[] names)
    {
        var clips = new List<AudioClip>();
        foreach (var name in names)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ClipFolder}/{name}.wav");
            if (clip != null) clips.Add(clip);
        }
        return clips;
    }

    struct Spec
    {
        public string Id;
        public string[] ClipNames;
        public SoundBus Bus;
        public float Volume;
        public float Jitter;
        public bool Spatial;
        public float MinDistance;
        public float MaxDistance;
        public bool Loop;
    }

    static Spec Bed(string id, string clip, SoundBus bus) => new Spec
    {
        Id = id, ClipNames = new[] { clip }, Bus = bus, Volume = 1f, Jitter = 0f,
        Spatial = false, MinDistance = 1f, MaxDistance = 500f, Loop = true,
    };

    static Spec OneShot(string id, string[] clips, float volume = 1f, float jitter = 0.5f,
                        float min = 0.6f, float max = 18f) => new Spec
    {
        Id = id, ClipNames = clips, Bus = SoundBus.Sfx, Volume = volume, Jitter = jitter,
        Spatial = true, MinDistance = min, MaxDistance = max, Loop = false,
    };

    /// <summary>
    /// Slot D's cues. Ids are <c>mus.</c>/<c>amb.</c>/<c>env.</c>/<c>dark.</c> only — other slots' cues go in
    /// their own sets. A cue whose clips don't exist yet is simply skipped, so this list can run ahead of the
    /// generator script.
    /// </summary>
    static readonly Spec[] Specs =
    {
        // music beds, crossfaded by area (SoundscapeLogic.BedsFor)
        Bed("mus.floor",  "MUS_Floor",    SoundBus.Music),
        Bed("mus.tense",  "MUS_Tense",    SoundBus.Music),
        Bed("mus.vault",  "MUS_Vault",    SoundBus.Music),
        Bed("mus.lobby",  "MUS_Lobby",    SoundBus.Music),
        Bed("mus.blackout", "MUS_Blackout", SoundBus.Music),

        // room tones — the "map" the blackout mode navigates by
        Bed("amb.casino_floor",  "AMB_CasinoFloor",  SoundBus.Ambience),
        Bed("amb.vip",           "AMB_Vip",          SoundBus.Ambience),
        Bed("amb.hallway",       "AMB_Hallway",      SoundBus.Ambience),
        Bed("amb.back_of_house", "AMB_BackOfHouse",  SoundBus.Ambience),
        Bed("amb.vault",         "AMB_Vault",        SoundBus.Ambience),
        Bed("amb.street",        "AMB_Street",       SoundBus.Ambience),

        // footsteps: four variants each, so walking never sounds like a drum machine
        OneShot("env.step.carpet",   new[] { "SFX_StepCarpet_1", "SFX_StepCarpet_2", "SFX_StepCarpet_3", "SFX_StepCarpet_4" }, 0.5f, 1.2f, 0.4f, 8f),
        OneShot("env.step.wood",     new[] { "SFX_StepWood_1", "SFX_StepWood_2", "SFX_StepWood_3", "SFX_StepWood_4" }, 0.55f, 1.2f, 0.4f, 10f),
        OneShot("env.step.concrete", new[] { "SFX_StepConcrete_1", "SFX_StepConcrete_2", "SFX_StepConcrete_3", "SFX_StepConcrete_4" }, 0.6f, 1.2f, 0.4f, 12f),
        OneShot("env.step.tile",     new[] { "SFX_StepTile_1", "SFX_StepTile_2", "SFX_StepTile_3", "SFX_StepTile_4" }, 0.6f, 1.2f, 0.4f, 12f),
        OneShot("env.step.metal",    new[] { "SFX_StepMetal_1", "SFX_StepMetal_2", "SFX_StepMetal_3", "SFX_StepMetal_4" }, 0.6f, 1.2f, 0.4f, 12f),

        // doors and travel
        OneShot("env.door.open",       new[] { "SFX_DoorOpen" }, 0.8f, 0.3f, 0.8f, 14f),
        OneShot("env.door.close",      new[] { "SFX_DoorClose" }, 0.8f, 0.3f, 0.8f, 14f),
        OneShot("env.teleport.arrive", new[] { "SFX_TeleportArrive" }, 0.5f, 0.2f),
        OneShot("env.travel.depart",   new[] { "SFX_TravelDepart" }, 0.6f, 0f),
        OneShot("env.travel.arrive",   new[] { "SFX_TravelArrive" }, 0.6f, 0f),

        // the blackout mode's own cues
        OneShot("dark.power.down",        new[] { "SFX_PowerDown" }, 1f, 0f, 1f, 40f),
        OneShot("dark.power.up",          new[] { "SFX_PowerUp" }, 1f, 0f, 1f, 40f),
        OneShot("dark.sonar.ping",        new[] { "SFX_SonarPing" }, 0.7f, 0f),
        OneShot("dark.sonar.return.soft", new[] { "SFX_SonarReturnSoft" }, 0.6f, 0.8f, 0.5f, 20f),
        OneShot("dark.sonar.return.hard", new[] { "SFX_SonarReturnHard" }, 0.6f, 0.8f, 0.5f, 20f),
    };
}
