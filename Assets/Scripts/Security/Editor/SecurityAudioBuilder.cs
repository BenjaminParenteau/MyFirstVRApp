using System;
using System.Collections.Generic;
using System.IO;
using HighStakes.Environment.Audio;
using UnityEditor;
using UnityEngine;

public static class SecurityAudioBuilder
{
    const string Clips = "Assets/Content/Progression/Audio";
    const string SetPath = "Assets/Content/Progression/Resources/Audio/CueSet_Security.asset";

    [MenuItem("High Stakes/Audio/Build Security Audio")]
    public static void Build()
    {
        var cues = new List<SoundCue>
        {
            Cue("sec.reader.ok", 0.72f, 7),
            Cue("sec.reader.deny", 0.68f, 7),
            Cue("sec.reader.idle", 0.34f, 5, true),
            Cue("sec.bolt.unlock", 0.72f, 8),
            Cue("sec.bolt.locked", 0.72f, 8),
            Cue("sec.door.heavy", 0.70f, 10),
            Cue("sec.drawer.open", 0.42f, 6),
            Cue("item.pickup", 0.55f, 6),
            Cue("item.pickup.key", 0.68f, 6),
            Cue("sec.keypad.ok", 0.84f, 9),
            Cue("sec.keypad.bad", 0.85f, 9),
            Cue("sec.keypad.lockout", 0.88f, 12),
            Cue("sec.wire.seat", 0.57f, 6),
            Cue("sec.wire.wrong", 0.63f, 6),
            Cue("sec.wire.solved", 0.54f, 8),
            Cue("sec.breaker.throw", 0.90f, 12),
            Cue("sec.tape.insert", 0.66f, 7),
            Cue("sec.tape.play", 0.19f, 5, true),
            Cue("sec.printer", 0.63f, 8),
            Cue("sec.cctv.hum", 0.20f, 8, true),
            Cue("sec.cctv.static", 0.57f, 9),
            Cue("sec.radio.chatter", 0.26f, 10, true),
            Cue("sec.fluorescent", 0.14f, 4, true),
            Cue("sec.vault.door", 0.73f, 11),
            Cue("sec.vault.hum", 0.26f, 10, true),
            Cue("sec.alarm.lockdown", 0.66f, 20, true, SoundBus.Sfx),
            Cue("sec.counting", 0.26f, 5, true),
        };
        for (int digit = 0; digit <= 9; digit++)
        {
            cues.Add(Cue($"sec.keypad.key.{digit}", 0.70f, 7, fixedPitch: true));
            cues.Add(Cue($"sec.keypad.spoken.{digit}", 0.78f, 7, fixedPitch: true));
        }

        if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(SetPath).Replace('\\', '/')))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SetPath));
            AssetDatabase.Refresh();
        }
        var set = AssetDatabase.LoadAssetAtPath<SoundCueSet>(SetPath);
        if (!set)
        {
            set = ScriptableObject.CreateInstance<SoundCueSet>();
            AssetDatabase.CreateAsset(set, SetPath);
        }
        set.slot = "B";
        set.cues = cues.ToArray();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Security Audio] Built {cues.Count} spatial cues in {SetPath}.");
    }

    static SoundCue Cue(string id, float gain, float distance, bool loop = false,
        SoundBus bus = SoundBus.Ambience, bool fixedPitch = false)
    {
        var clips = fixedPitch
            ? new[] { Load(id, loop), Load(id + "_2", loop) }
            : new[] { Load(id, loop) };
        return new SoundCue
        {
            id = id,
            clips = clips,
            bus = loop ? bus : SoundBus.Sfx,
            volume = gain,
            pitchJitter = fixedPitch || loop ? 0f : 0.25f,
            spatial = true,
            minDistance = 0.6f,
            maxDistance = distance,
            loop = loop,
        };
    }

    static AudioClip Load(string name, bool loop)
    {
        string path = $"{Clips}/{name}.wav";
        if (!AssetDatabase.LoadAssetAtPath<AudioClip>(path))
            throw new InvalidOperationException($"Missing security clip {path}. Run tools/audio/generate_security_audio.py.");
        AudioImport.Apply(path, loop ? AudioImport.Kind.Loop : AudioImport.Kind.OneShot);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
