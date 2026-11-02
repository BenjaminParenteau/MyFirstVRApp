using System;
using HighStakes.Core;
using UnityEngine;
using UnityEngine.XR;

namespace HighStakes.Dialogue
{
    /// <summary>
    /// The Guide's earpiece: plays a <see cref="GuideLine"/> by id, one at a time and each at most once per session,
    /// as non-spatial audio (it sounds like it's in your ear), with a short buzz on the left controller when a line
    /// starts. Lines are asked for by <see cref="GuideTrigger"/>s or any script through <see cref="IGuideVoice"/>.
    /// Keep one in MVP_Main so the session memory survives door trips. The watch's GuideCommLink shows what it says.
    /// </summary>
    public class GuideVoice : MonoBehaviour, IGuideVoice
    {
        [SerializeField] private GuideLine[] lines = Array.Empty<GuideLine>();
        [Tooltip("Non-spatial (2D) source the lines play through.")]
        [SerializeField] private AudioSource earpiece;
        [Tooltip("Silence between two lines (s).")]
        [SerializeField, Min(0f)] private float gapSeconds = 0.6f;
        [SerializeField, Range(0f, 1f)] private float buzzAmplitude = 0.35f;
        [SerializeField, Min(0f)] private float buzzSeconds = 0.12f;

        private readonly GuideQueue queue = new GuideQueue();
        private float lineEndsAt;
        private float nextLineAt;

        /// <summary>Raised when a line starts, with the line.</summary>
        public event Action<GuideLine> LineStarted;

        /// <summary>Raised when the current line's time is up.</summary>
        public event Action LineFinished;

        /// <summary>The line playing now (audio or subtitle still up), or null.</summary>
        public GuideLine CurrentLine { get; private set; }

        /// <summary>True while the current line's audio is playing (the watch waveform moves only then).</summary>
        public bool IsTalking => CurrentLine != null && earpiece != null && earpiece.isPlaying;

        public void Play(string lineId)
        {
            if (Find(lineId) == null)
            {
                Debug.LogWarning($"[Guide] No line with id '{lineId}' on {name}.", this);
                return;
            }
            queue.Request(lineId);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (CurrentLine != null && now >= lineEndsAt)
            {
                CurrentLine = null;
                queue.Finish();
                nextLineAt = now + gapSeconds;
                LineFinished?.Invoke();
            }

            if (CurrentLine == null && now >= nextLineAt)
            {
                string next = queue.StartNext();
                if (next != null)
                    Begin(Find(next), now);
            }
        }

        private void Begin(GuideLine line, float now)
        {
            CurrentLine = line;
            float clipSeconds = 0f;
            if (line.clip != null && earpiece != null)
            {
                earpiece.clip = line.clip;
                earpiece.Play();
                clipSeconds = line.clip.length;
            }
            lineEndsAt = now + GuideLogic.LineSeconds(line.subtitle, clipSeconds);
            Buzz();
            LineStarted?.Invoke(line);
        }

        private void Buzz()
        {
            var hand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (hand.isValid && hand.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                hand.SendHapticImpulse(0, buzzAmplitude, buzzSeconds);
        }

        private GuideLine Find(string lineId)
        {
            foreach (var line in lines)
            {
                if (line != null && line.id == lineId)
                    return line;
            }
            return null;
        }
    }
}
