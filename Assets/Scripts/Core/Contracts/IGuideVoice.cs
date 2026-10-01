namespace HighStakes.Core
{
    /// <summary>
    /// The Guide's earpiece voice. Implemented by Slot C; B calls it on tier-up.
    /// </summary>
    public interface IGuideVoice
    {
        /// <summary>Plays the voice line with this id (ids are defined by Slot C's line data).</summary>
        void Play(string lineId);
    }
}
