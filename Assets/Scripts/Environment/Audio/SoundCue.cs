using System;
using UnityEngine;

namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// One sound the game can play, addressed by a string id. Cues live inside their owning slot's
    /// <see cref="SoundCueSet"/>, so adding one never touches a file another slot owns
    /// (docs/milestone6/README.md rule 3).
    /// </summary>
    [Serializable]
    public class SoundCue
    {
        [Tooltip("Stable id callers use, e.g. table.chip.bet. The prefix says who owns it (rule 4) and " +
                 "High Stakes > Audio > Validate Cues enforces that. Never rename one that is in use.")]
        public string id;

        [Tooltip("One or more clips. SEVERAL is the point for anything that fires often: the game picks a " +
                 "different one each time, so fifty chip clinks don't sound like a machine gun.")]
        public AudioClip[] clips = Array.Empty<AudioClip>();

        [Tooltip("Which volume group this belongs to.")]
        public SoundBus bus = SoundBus.Sfx;

        [Tooltip("This cue's own level, before the bus volume.")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Random pitch spread per firing, in semitones. Costs nothing and does as much as extra clips " +
                 "to stop repetition fatigue. 0 = always the same pitch (use for musical cues like keypad tones).")]
        [Range(0f, 6f)] public float pitchJitter = 0.5f;

        [Tooltip("Positioned in 3D. Off for UI and the Guide's earpiece only — a sound you can't locate is a " +
                 "sound you can't navigate by, which matters most in the blackout mode.")]
        public bool spatial = true;

        [Tooltip("Below this distance the sound stops getting louder. Keeps a prop from becoming painful when " +
                 "you lean into it, which players do in VR.")]
        [Min(0.05f)] public float minDistance = 0.6f;

        [Tooltip("Beyond this it is silent. 18 m for things you should hear across a room, ~6 m for a small prop.")]
        [Min(0.2f)] public float maxDistance = 18f;

        [Tooltip("Looping emitter (room tone, a hum, a wheel). Play through Soundscape.Loop, not PlayAt.")]
        public bool loop;

        /// <summary>True if this cue has at least one clip assigned (an empty cue is a silent bug).</summary>
        public bool HasClips
        {
            get
            {
                if (clips == null) return false;
                for (int i = 0; i < clips.Length; i++)
                    if (clips[i] != null) return true;
                return false;
            }
        }

        /// <summary>Clips actually assigned, ignoring empty array slots left behind in the Inspector.</summary>
        public int ClipCount
        {
            get
            {
                if (clips == null) return 0;
                int n = 0;
                for (int i = 0; i < clips.Length; i++)
                    if (clips[i] != null) n++;
                return n;
            }
        }

        /// <summary>The nth non-null clip, so a gap in the array can't play silence.</summary>
        public AudioClip ClipAt(int index)
        {
            if (clips == null || index < 0) return null;
            int n = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null) continue;
                if (n == index) return clips[i];
                n++;
            }
            return null;
        }
    }
}
