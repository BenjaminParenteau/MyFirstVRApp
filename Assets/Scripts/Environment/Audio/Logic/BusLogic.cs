using System;

namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// The volume rules, with no Unity in them so they can be tested: defaults, clamping, PlayerPrefs key names,
    /// how a cue's own volume combines with its bus, and the blackout duck.
    /// </summary>
    public static class BusLogic
    {
        /// <summary>Number of buses, for iterating without reflection.</summary>
        public const int BusCount = 4;

        /// <summary>PlayerPrefs key for a bus volume. Stable string, not the enum number, so a reorder is survivable.</summary>
        public static string Key(SoundBus bus) => "hs.audio.vol." + Name(bus);

        /// <summary>Lowercase name used in the PlayerPrefs key and in validator output.</summary>
        public static string Name(SoundBus bus)
        {
            switch (bus)
            {
                case SoundBus.Music: return "music";
                case SoundBus.Ambience: return "ambience";
                case SoundBus.Sfx: return "sfx";
                case SoundBus.Voice: return "voice";
                default: return "bus" + (int)bus;
            }
        }

        /// <summary>
        /// Shipping defaults. Pak owns these numbers (they are her mix) — these are the starting guess, and round 3
        /// is expected to argue with them. Music sits under ambience on purpose: the room tone carries the place.
        /// </summary>
        public static float Default(SoundBus bus)
        {
            switch (bus)
            {
                case SoundBus.Music: return 0.70f;
                case SoundBus.Ambience: return 0.80f;
                case SoundBus.Sfx: return 1.00f;
                case SoundBus.Voice: return 1.00f;
                default: return 1f;
            }
        }

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        /// <summary>
        /// Final linear volume for a source: the cue's own level, its bus, and any duck.
        /// Clamped so a mis-set cue can't exceed full scale and clip the mix.
        /// </summary>
        public static float Gain(float busVolume, float cueVolume, float duck = 1f)
            => Clamp01(Clamp01(busVolume) * (cueVolume < 0f ? 0f : cueVolume) * (duck < 0f ? 0f : duck));

        /// <summary>
        /// The blackout mix. Music almost gone (dropping it is most of the effect), ambience and effects up,
        /// because in the dark they are the only information the player has. Voice is never touched.
        /// </summary>
        public static float BlackoutDuck(SoundBus bus)
        {
            switch (bus)
            {
                case SoundBus.Music: return 0.15f;
                case SoundBus.Ambience: return 1.25f;
                case SoundBus.Sfx: return 1.15f;
                case SoundBus.Voice: return 1.00f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Ease a duck in and out rather than switching it, so the lights going out doesn't also click the mix.
        /// <paramref name="t"/> is 0..1 through the transition.
        /// </summary>
        public static float Blend(float from, float to, float t)
        {
            t = Clamp01(t);
            return from + (to - from) * (t * t * (3f - 2f * t));   // smoothstep
        }

        /// <summary>
        /// Linear volume to decibels, for readable validator and log output only (we never set dB on a source).
        /// </summary>
        public static float ToDecibels(float linear)
            => linear <= 0.0001f ? -80f : (float)(20.0 * Math.Log10(linear));
    }
}
