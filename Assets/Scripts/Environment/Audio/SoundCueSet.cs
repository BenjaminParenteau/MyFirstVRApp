using UnityEngine;

namespace HighStakes.Environment.Audio
{
    /// <summary>
    /// One slot's sounds. <b>There is deliberately no global list of cues</b> — each slot owns a set in its own
    /// <c>Resources/Audio/</c> folder and <see cref="Soundscape"/> discovers all of them at load, so four people
    /// can add sounds all day without ever editing the same file (docs/milestone6/README.md rule 3).
    /// <para>
    /// Where each one lives:
    /// <list type="bullet">
    /// <item>A — <c>Assets/Content/Tables/Resources/Audio/CueSet_Tables.asset</c></item>
    /// <item>B — <c>Assets/Content/Progression/Resources/Audio/CueSet_Security.asset</c></item>
    /// <item>C — <c>Assets/Content/UI/Resources/Audio/CueSet_UI.asset</c></item>
    /// <item>D — <c>Assets/Content/Environment/Resources/Audio/CueSet_Environment.asset</c></item>
    /// </list>
    /// Build yours from your own Editor menu; don't hand-edit someone else's.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "CueSet", menuName = "High Stakes/Sound Cue Set")]
    public class SoundCueSet : ScriptableObject
    {
        [Tooltip("Which slot owns this set (A/B/C/D). Used by Validate Cues to check every id is in a namespace " +
                 "this slot actually owns.")]
        public string slot = "D";

        [Tooltip("The cues. Ids must use your slot's namespace prefix.")]
        public SoundCue[] cues = new SoundCue[0];
    }
}
