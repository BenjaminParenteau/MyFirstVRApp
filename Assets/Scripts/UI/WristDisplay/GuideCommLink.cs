using HighStakes.Dialogue;
using TMPro;
using UnityEngine;

namespace HighStakes.UI
{
    /// <summary>
    /// The watch's comm link with the Guide. While a line plays, the watch face lights up with THE GUIDE / COMM LINK
    /// and a cyan waveform that moves while he talks (even with the wrist down, so you know to look), and the watch
    /// hologram shows what he says. Display only: it observes the <see cref="GuideVoice"/> in the loaded scenes.
    /// </summary>
    public class GuideCommLink : MonoBehaviour
    {
        [Tooltip("Optional. Empty = the first GuideVoice in the loaded scenes.")]
        [SerializeField] private GuideVoice voice;
        [Tooltip("Covers the watch face while a line plays.")]
        [SerializeField] private GameObject link;
        [SerializeField] private Transform[] bars = new Transform[0];
        [Tooltip("Longest a waveform bar gets (m).")]
        [SerializeField] private float barLength = 0.01f;
        [Tooltip("Subtitle strip on the hologram panel.")]
        [SerializeField] private GameObject subtitle;
        [SerializeField] private TMP_Text subtitleText;

        private const float SearchInterval = 1f;

        private float nextSearchTime;
        private GuideLine shown;

        private void Awake() => Show(null);

        private void Update()
        {
            if (voice == null && Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + SearchInterval;
                voice = FindFirstObjectByType<GuideVoice>();
            }

            var line = voice != null ? voice.CurrentLine : null;
            if (line != shown)
                Show(line);
            if (line == null)
                return;

            bool talking = voice.IsTalking;
            for (int i = 0; i < bars.Length; i++)
            {
                float k = talking ? GuideLogic.WaveHeight(i, Time.unscaledTime) : 0.2f;
                var scale = bars[i].localScale;
                scale.z = barLength * k;
                bars[i].localScale = scale;
            }
        }

        private void Show(GuideLine line)
        {
            shown = line;
            if (link != null) link.SetActive(line != null);
            if (subtitle != null) subtitle.SetActive(line != null);
            if (subtitleText != null) subtitleText.text = line != null ? line.subtitle : string.Empty;
        }
    }
}
