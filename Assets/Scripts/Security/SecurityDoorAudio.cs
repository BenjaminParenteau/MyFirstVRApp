using HighStakes.Environment;
using HighStakes.Environment.Audio;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>Sounds an attempt at a secured portal using its existing walk-up activation zone.</summary>
    [DefaultExecutionOrder(-10)]
    internal sealed class SecurityDoorAudio : MonoBehaviour
    {
        DoorPortal door;
        string entryCue;
        bool wasInZone;

        internal static void Attach(DoorPortal portal, string onEnter = null)
        {
            if (!Application.isPlaying || !portal || SecurityMonitorWall.LoadingRemote) return;
            var feedback = portal.GetComponent<SecurityDoorAudio>();
            if (!feedback) feedback = portal.gameObject.AddComponent<SecurityDoorAudio>();
            feedback.door = portal;
            feedback.entryCue = onEnter;
        }

        void Update()
        {
            var eyes = Camera.main;
            if (!door || !door.isActiveAndEnabled || !eyes) return;
            Vector3 head = door.transform.InverseTransformPoint(eyes.transform.position);
            bool inZone = Mathf.Abs(head.x) <= door.triggerWidth * 0.5f &&
                head.z <= -0.05f && head.z >= -door.triggerDepth;
            if (inZone && !wasInZone)
            {
                var position = door.transform.position + Vector3.up;
                if (door.locked) Soundscape.PlayAt("sec.bolt.locked", position);
                else if (entryCue != null) Soundscape.PlayAt(entryCue, position);
            }
            wasInZone = inZone;
        }
    }
}
