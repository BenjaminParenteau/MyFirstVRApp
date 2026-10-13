using System;

namespace HighStakes.TableGames
{
    /// <summary>
    /// When a held button presses itself again: first after a delay long enough
    /// that a normal press never repeats, then at an interval that shortens to a
    /// fastest rate over a ramp, so a long hold covers ground quickly.
    /// </summary>
    public sealed class HoldRepeat
    {
        readonly float delay;
        readonly float interval;
        readonly float fastest;
        readonly float ramp;
        float held;
        float next;

        public HoldRepeat(float delay, float interval, float fastest, float ramp)
        {
            if (delay < 0f) throw new ArgumentOutOfRangeException(nameof(delay));
            if (fastest <= 0f || interval < fastest) throw new ArgumentOutOfRangeException(nameof(fastest), "The fastest interval must be positive and no slower than the first.");
            if (ramp < 0f) throw new ArgumentOutOfRangeException(nameof(ramp));
            this.delay = delay;
            this.interval = interval;
            this.fastest = fastest;
            this.ramp = ramp;
            Begin();
        }

        /// <summary>A new hold starts: the press itself has just happened.</summary>
        public void Begin()
        {
            held = 0f;
            next = delay;
        }

        /// <summary>The hold went on for another stretch of time; returns how many repeats fell inside it.</summary>
        public int Advance(float seconds)
        {
            held += seconds;
            var due = 0;
            while (held >= next)
            {
                due++;
                next += IntervalAt(next);
            }
            return due;
        }

        float IntervalAt(float time)
        {
            var progress = ramp <= 0f ? 1f : Math.Min(1f, Math.Max(0f, (time - delay) / ramp));
            return interval + (fastest - interval) * progress;
        }
    }
}
