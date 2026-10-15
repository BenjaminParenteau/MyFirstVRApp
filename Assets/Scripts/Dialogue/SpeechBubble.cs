using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.Dialogue
{
    /// <summary>
    /// A line of speech floating over an NPC's head for a few seconds, always turned toward the player.
    /// </summary>
    // ponytail: one line at a time, no voice. Route through the shared dialogue/trigger system when it exists.
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] GameObject bubble;
        [SerializeField] TMPro.TMP_Text text;

        float hideAt;
        Transform eyes;

        void Awake() => bubble.SetActive(false);

        public void Say(string line, float seconds = 5f)
        {
            text.text = line;
            hideAt = Time.time + seconds;
            bubble.SetActive(true);
        }

        void LateUpdate()
        {
            if (!bubble.activeSelf) return;
            if (Time.time >= hideAt)
            {
                bubble.SetActive(false);
                return;
            }
            if (eyes == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) eyes = origin.Camera.transform;
            }
            if (eyes != null)
            {
                var away = bubble.transform.position - eyes.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f) bubble.transform.rotation = Quaternion.LookRotation(away); // text reads from the player's side
            }
        }
    }
}
