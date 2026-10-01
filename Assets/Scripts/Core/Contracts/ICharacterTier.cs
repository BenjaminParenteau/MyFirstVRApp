using System;

namespace HighStakes.Core
{
    /// <summary>
    /// The player's current status tier. Implemented by Slot B (watches the wallet); C's hand/wrist visuals and
    /// wrist display subscribe, and door readers query <see cref="Current"/>.
    /// </summary>
    public interface ICharacterTier
    {
        TierDefinition Current { get; }

        /// <summary>Raised when the tier changes, with the new tier.</summary>
        event Action<TierDefinition> TierChanged;
    }
}
