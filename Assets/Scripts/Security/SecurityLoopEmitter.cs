using HighStakes.Environment.Audio;
using UnityEngine;

namespace HighStakes.Security
{
    internal sealed class SecurityLoopEmitter : MonoBehaviour
    {
        string cue;
        Soundscape.LoopHandle playback;

        internal static void Attach(Transform anchor, string cueId)
        {
            if (!anchor || SecurityMonitorWall.LoadingRemote) return;
            var emitter = anchor.gameObject.AddComponent<SecurityLoopEmitter>();
            emitter.cue = cueId;
            emitter.Play();
        }

        void OnEnable() => Play();

        void Play()
        {
            if (isActiveAndEnabled && cue != null) playback = Soundscape.Loop(cue, transform);
        }

        void OnDisable()
        {
            playback?.Stop();
            playback = null;
        }
    }
}
