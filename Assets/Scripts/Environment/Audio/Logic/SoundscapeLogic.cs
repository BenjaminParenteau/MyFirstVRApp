using System;

namespace HighStakes.Environment.Audio
{
    /// <summary>Which music bed and room tone an area plays. Both ids may be null, meaning "silence here".</summary>
    public struct AreaBeds
    {
        public string Music;
        public string Ambience;

        public AreaBeds(string music, string ambience)
        {
            Music = music;
            Ambience = ambience;
        }
    }

    /// <summary>
    /// The soundscape's decisions, with no Unity in them so they can be tested: which beds an area gets, the
    /// crossfade curve, which clip variant to use, and which pooled voice to take when they are all busy.
    /// </summary>
    public static class SoundscapeLogic
    {
        /// <summary>
        /// Area scene name to its beds. The room tone is the important half: every area must be identifiable in two
        /// seconds with your eyes shut, because in the blackout mode that is literally what it is for
        /// (StyleGuide §11). An unknown scene gets the floor beds rather than silence, so a new area is never
        /// mute by accident — the validator is what tells us to add it properly.
        /// </summary>
        public static AreaBeds BedsFor(string sceneName)
        {
            switch (sceneName)
            {
                case "MVP_Casino": return new AreaBeds("mus.floor", "amb.casino_floor");
                case "MVP_VIP": return new AreaBeds("mus.floor", "amb.vip");
                case "MVP_SecurityHall": return new AreaBeds("mus.tense", "amb.hallway");
                case "MVP_SecurityOffice": return new AreaBeds("mus.tense", "amb.back_of_house");
                case "MVP_CameraRoom": return new AreaBeds("mus.tense", "amb.back_of_house");
                case "MVP_Vault": return new AreaBeds("mus.vault", "amb.vault");
                case "MVP_Main": return new AreaBeds("mus.lobby", null);     // start screen, before the night begins
                case null:
                case "": return new AreaBeds(null, null);
                default: return new AreaBeds("mus.floor", "amb.casino_floor");
            }
        }

        /// <summary>True if this scene name has beds written for it (the validator reports the ones that don't).</summary>
        public static bool IsKnownArea(string sceneName)
        {
            switch (sceneName)
            {
                case "MVP_Casino":
                case "MVP_VIP":
                case "MVP_SecurityHall":
                case "MVP_SecurityOffice":
                case "MVP_CameraRoom":
                case "MVP_Vault":
                case "MVP_Main": return true;
                default: return false;
            }
        }

        /// <summary>
        /// Equal-power crossfade between the outgoing and incoming bed. Linear gain on both at once dips in the
        /// middle (the two signals are uncorrelated, so their powers add, not their amplitudes) and the player
        /// hears a hole as they walk through a door. Returns gains in 0..1.
        /// </summary>
        public static void Crossfade(float elapsed, float duration, out float outGain, out float inGain)
        {
            float t = duration <= 0f ? 1f : BusLogic.Clamp01(elapsed / duration);
            outGain = (float)Math.Cos(t * Math.PI * 0.5);
            inGain = (float)Math.Sin(t * Math.PI * 0.5);
        }

        /// <summary>
        /// Pick which clip of a multi-clip cue to play, never twice in a row.
        /// <para>
        /// This is why <c>SoundCue.clips</c> is an array: a cue with one clip fired fifty times a session sounds
        /// like a machine gun. <paramref name="last"/> is the index used previously (-1 if none);
        /// <paramref name="random01"/> is a 0..1 roll.
        /// </para>
        /// </summary>
        public static int PickVariant(int clipCount, int last, float random01)
        {
            if (clipCount <= 1) return clipCount - 1;                 // 0 for one clip, -1 for none
            if (last < 0 || last >= clipCount)
                return Index(clipCount, random01);

            // Roll across the other clips only, then map back, so "not the last one" costs no retry loop.
            int pick = Index(clipCount - 1, random01);
            return pick >= last ? pick + 1 : pick;
        }

        static int Index(int count, float random01)
        {
            int i = (int)(BusLogic.Clamp01(random01) * count);
            return i >= count ? count - 1 : i;
        }

        /// <summary>
        /// Pitch multiplier for one firing. <paramref name="jitterSemitones"/> is the ± range from the cue.
        /// Pitch variation does as much as clip variants to stop repetition fatigue, and costs nothing.
        /// </summary>
        public static float PitchFor(float jitterSemitones, float random01)
        {
            if (jitterSemitones <= 0f) return 1f;
            float semis = (BusLogic.Clamp01(random01) * 2f - 1f) * jitterSemitones;
            return (float)Math.Pow(2.0, semis / 12.0);
        }

        /// <summary>
        /// Choose a pooled one-shot voice. Prefers a free one; if every voice is busy it steals the one that
        /// started longest ago, which is the least likely to still be audible. Returns -1 only for an empty pool.
        /// </summary>
        public static int PickVoice(bool[] busy, float[] startedAt)
        {
            if (busy == null || busy.Length == 0) return -1;
            for (int i = 0; i < busy.Length; i++)
                if (!busy[i]) return i;

            int oldest = 0;
            for (int i = 1; i < busy.Length; i++)
                if (startedAt[i] < startedAt[oldest]) oldest = i;
            return oldest;
        }

        /// <summary>
        /// The owning slot of a cue id, from its namespace prefix — the mechanism that keeps four people's cues
        /// from colliding (docs/milestone6/README.md rule 4). Returns null for an id in no known namespace, which
        /// is what <b>Validate Cues</b> reports.
        /// </summary>
        public static string OwnerOf(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return null;
            int dot = cueId.IndexOf('.');
            string head = dot < 0 ? cueId : cueId.Substring(0, dot);
            switch (head)
            {
                case "table":
                case "cage": return "A";
                case "sec":
                case "item": return "B";
                case "ui":
                case "vo": return "C";
                case "env":
                case "mus":
                case "amb":
                case "dark": return "D";
                default: return null;
            }
        }

        /// <summary>
        /// True if a cue should be positioned in 3D. Interface and the Guide's earpiece are 2D on purpose: an
        /// earpiece that pans when you turn your head is wrong, and UI belongs to the player, not the room.
        /// </summary>
        public static bool ShouldBeSpatial(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return false;
            if (cueId.StartsWith("ui.", StringComparison.Ordinal)) return false;
            if (cueId.StartsWith("vo.guide", StringComparison.Ordinal)) return false;   // vo.pitboss IS 3D
            if (cueId.StartsWith("mus.", StringComparison.Ordinal)) return false;

            // The blackout's two "inside the player" cues: the sonar click they make themselves, and their own
            // breathing. Both must stay centred when the head turns — everything else in dark. is in the room.
            if (cueId.StartsWith("dark.breath", StringComparison.Ordinal)) return false;
            if (cueId == "dark.sonar.ping") return false;
            return true;
        }
    }
}
