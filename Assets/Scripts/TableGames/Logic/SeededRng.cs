using System;

namespace HighStakes.TableGames
{
    // ponytail: System.Random. Swap for a hash-based commit/reveal scheme if provably fair results are ever wanted.
    public sealed class SeededRng : IRng
    {
        readonly Random random;

        public int Seed { get; }

        public SeededRng(int seed)
        {
            Seed = seed;
            random = new Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
    }
}
