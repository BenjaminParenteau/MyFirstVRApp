namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// The four volume groups every cue belongs to. Deliberately an enum rather than an AudioMixer asset: a mixer
    /// is one shared file that all four slots would need a group inside, and its YAML does not merge
    /// (Progress_Vol1.md, Decisions 2026-11-17). Volumes live in PlayerPrefs and are set on the notebook's AUDIO
    /// page.
    /// <para>
    /// <b>Append-only</b>, like a Core contract: add a value at the end if the team agrees, never renumber or
    /// remove one. The numbers are persisted in PlayerPrefs keys, so renumbering silently moves players' settings.
    /// </para>
    /// </summary>
    public enum SoundBus
    {
        /// <summary>Area music beds. First thing ducked when something matters more.</summary>
        Music = 0,

        /// <summary>Room tone and looping emitters. In the blackout this is promoted, because it is the map.</summary>
        Ambience = 1,

        /// <summary>One-shots: everything that happens because the player did something.</summary>
        Sfx = 2,

        /// <summary>The Guide and the NPCs. Never ducked — if you can't hear him you can't play.</summary>
        Voice = 3,
    }
}
