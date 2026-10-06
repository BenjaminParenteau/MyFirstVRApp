namespace HighStakes.TableGames
{
    /// <summary>
    /// Source of randomness for table game logic. Implementations must be deterministic for a given seed so outcomes
    /// can be replayed in tests.
    /// </summary>
    public interface IRng
    {
        /// <summary>Returns an integer in [minInclusive, maxExclusive).</summary>
        int Next(int minInclusive, int maxExclusive);
    }
}
