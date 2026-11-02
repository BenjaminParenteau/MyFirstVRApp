using System;
using System.Collections.Generic;

namespace HighStakes.Dialogue
{
    /// <summary>
    /// Which of the Guide's lines plays next. Each line id plays at most once per session, and lines asked for while
    /// another one is playing wait their turn so the Guide never talks over himself. Unity-free so it can be tested.
    /// </summary>
    public sealed class GuideQueue
    {
        private readonly HashSet<string> requested = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> waiting = new Queue<string>();

        /// <summary>The line playing now, or null.</summary>
        public string Current { get; private set; }

        /// <summary>Lines waiting behind the current one.</summary>
        public int Waiting => waiting.Count;

        /// <summary>Asks for a line. False if the id is empty or was already asked for this session.</summary>
        public bool Request(string id)
        {
            if (string.IsNullOrEmpty(id) || !requested.Add(id))
                return false;
            waiting.Enqueue(id);
            return true;
        }

        /// <summary>Starts the next waiting line if nothing is playing. Returns the line started, or null.</summary>
        public string StartNext()
        {
            if (Current != null || waiting.Count == 0)
                return null;
            Current = waiting.Dequeue();
            return Current;
        }

        /// <summary>The current line has ended.</summary>
        public void Finish() => Current = null;
    }

    /// <summary>Timing and waveform maths for the Guide's lines.</summary>
    public static class GuideLogic
    {
        /// <summary>Comfortable subtitle reading speed (words per second).</summary>
        public const float WordsPerSecond = 2.7f;

        /// <summary>Shortest time any line stays up.</summary>
        public const float MinSeconds = 2f;

        public const int WaveBars = 5;

        /// <summary>
        /// How long a line stays up: as long as its audio, or as long as it takes to read the subtitle, whichever is
        /// longer, and never under <see cref="MinSeconds"/>.
        /// </summary>
        public static float LineSeconds(string subtitle, float clipSeconds)
        {
            float reading = CountWords(subtitle) / WordsPerSecond;
            return Math.Max(MinSeconds, Math.Max(clipSeconds, reading));
        }

        public static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Height (0.2–1) of one waveform bar at a moment in time while the Guide talks: each bar bobs at its own
        /// speed so the bars look like speech without analysing the audio (cheap on Quest).
        /// </summary>
        public static float WaveHeight(int bar, float time)
        {
            double speed = 9.0 + 3.7 * bar;
            double phase = 1.3 * bar;
            double wobble = Math.Abs(Math.Sin(time * speed + phase)) * (0.6 + 0.4 * Math.Abs(Math.Sin(time * 2.1 + bar)));
            return (float)(0.2 + 0.8 * wobble);
        }
    }
}
