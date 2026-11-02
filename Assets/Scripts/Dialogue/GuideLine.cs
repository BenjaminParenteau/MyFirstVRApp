using UnityEngine;

namespace HighStakes.Dialogue
{
    /// <summary>
    /// One thing the Guide says in the player's earpiece. Assets live in Assets/Content/UI/Guide/ and are built by
    /// High Stakes > UI > Build Guide Voice. Swap the clip for a real recording any time; the subtitle stays.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_GuideLine", menuName = "High Stakes/Guide Line")]
    public class GuideLine : ScriptableObject
    {
        [Tooltip("Stable id that triggers and IGuideVoice.Play use. Never rename once in use.")]
        public string id;

        [Tooltip("What the Guide says; shown on the watch hologram while the line plays.")]
        [TextArea(2, 5)] public string subtitle;

        [Tooltip("Optional. Without a clip the line is subtitle-only.")]
        public AudioClip clip;

        [Tooltip("Optional. A short objective the notebook's leads page writes down when this line plays; empty = no lead.")]
        public string lead;
    }
}
