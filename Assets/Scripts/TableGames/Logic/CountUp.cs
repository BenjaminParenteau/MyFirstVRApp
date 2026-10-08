using System;

namespace HighStakes.TableGames
{
    /// <summary>The number a win animation shows while counting from 0 up to the amount won.</summary>
    public static class CountUp
    {
        /// <summary>
        /// Value after <paramref name="elapsed"/> of <paramref name="duration"/> seconds: eases out (fast, then slowing),
        /// never passes <paramref name="target"/>, and lands on it exactly at the end.
        /// </summary>
        public static long Value(long target, float elapsed, float duration)
        {
            if (duration <= 0f || elapsed >= duration) return target;
            if (elapsed <= 0f) return 0;
            double t = elapsed / duration;
            double eased = 1 - Math.Pow(1 - t, 3);
            return (long)Math.Floor(target * eased);
        }
    }
}
