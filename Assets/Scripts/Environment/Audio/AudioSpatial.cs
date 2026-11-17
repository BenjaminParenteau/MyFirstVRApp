using UnityEngine;

namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// The project's spatial audio defaults, in one place. Every source the soundscape plays goes through here, so
    /// "spatial audio" is one decision rather than sixty copies of it (StyleGuide §11).
    /// <para>
    /// Note on HRTF: Unity's built-in 3D audio gives panning and distance attenuation, not true binaural
    /// positioning. Real HRTF on Quest needs the Meta XR Audio SDK package — an open decision, logged in
    /// Progress_Vol1.md. <see cref="AudioSource.spatialize"/> is set anyway, so if that package is ever added
    /// every cue picks it up with no further change here.
    /// </para>
    /// </summary>
    public static class AudioSpatial
    {
        /// <summary>Doppler is always off: pitch-shifting the world around a teleporting player is nausea, not realism.</summary>
        public const float Doppler = 0f;

        /// <summary>Degrees of spread at distance, so a close sound doesn't collapse into one ear.</summary>
        public const float Spread = 15f;

        /// <summary>Apply the defaults plus this cue's own distances to a source.</summary>
        public static void Apply(AudioSource source, SoundCue cue)
        {
            if (source == null || cue == null) return;

            source.loop = cue.loop;
            source.dopplerLevel = Doppler;
            source.spatialBlend = cue.spatial ? 1f : 0f;
            source.spatialize = cue.spatial;
            source.spatializePostEffects = false;
            source.bypassReverbZones = false;
            source.playOnAwake = false;

            if (cue.spatial)
            {
                // Logarithmic, not linear: linear attenuation makes distance unreadable, and judging distance by
                // ear is the entire mechanic the blackout mode runs on.
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = Mathf.Max(0.05f, cue.minDistance);
                source.maxDistance = Mathf.Max(source.minDistance + 0.1f, cue.maxDistance);
                source.spread = Spread;
            }
            else
            {
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 500f;
                source.spread = 0f;
            }
        }

        /// <summary>
        /// Configure a source used for a non-positioned bed (music, room tone). Kept separate from
        /// <see cref="Apply"/> because beds are 2D by definition and loop forever.
        /// </summary>
        public static void ApplyBed(AudioSource source)
        {
            if (source == null) return;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.spatialize = false;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 500f;
            source.bypassReverbZones = true;   // a bed is already the sound of the room; don't reverb it again
        }
    }
}
