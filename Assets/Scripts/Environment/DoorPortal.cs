using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace HighStakes.Environment
{
    /// <summary>
    /// A door that carries the player to another door, so rooms and hallways don't have to be connected in the scene
    /// (the casino and the security hallways live in different places, or even different scenes). Walking up to the door
    /// is the player's input: the one player rig is moved to the target door's arrival point and turned to face away
    /// from it, like stepping through. Never add a second rig for this (StyleGuide §5c).
    ///
    /// Doors find each other by id through a static registry, so a door in one additively loaded scene can target a door
    /// in another: set this door's <see cref="targetId"/> to the other door's <see cref="portalId"/>.
    /// Works for the headset and for the XR Interaction Simulator (WASD), because it only looks at where the player's head is.
    /// </summary>
    public class DoorPortal : MonoBehaviour
    {
        [Tooltip("Unique id of this door, e.g. HallA_Start.")]
        public string portalId;
        [Tooltip("portalId of the door this one leads to.")]
        public string targetId;
        [Tooltip("Where the player stands after arriving through this door: in front of it, facing away from it.")]
        public Transform arrival;
        [Tooltip("A locked door does nothing. Slot B's keycard reader can call SetLocked(false).")]
        public bool locked;
        [Tooltip("Width of the activation zone in front of the door (m).")]
        public float triggerWidth = 1f;
        [Tooltip("How far in front of the door the player has to step to use it (m).")]
        public float triggerDepth = 0.8f;

        // Front of the door is local -Z; the player stands there.
        const float Cooldown = 1f;

        static readonly Dictionary<string, DoorPortal> Registry = new Dictionary<string, DoorPortal>();
        static float s_blockedUntil;
        static float s_nextOriginSearch;
        static XROrigin s_origin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Registry.Clear();
            s_blockedUntil = 0f;
            s_nextOriginSearch = 0f;
            s_origin = null;
        }

        public void SetLocked(bool value) => locked = value;

        void OnEnable()
        {
            if (string.IsNullOrEmpty(portalId)) return;
            if (Registry.TryGetValue(portalId, out var other) && other != null && other != this)
                Debug.LogWarning($"[DoorPortal] Two doors share the id '{portalId}'. The newest one wins.", this);
            Registry[portalId] = this;
        }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(portalId) && Registry.TryGetValue(portalId, out var current) && current == this)
                Registry.Remove(portalId);
        }

        void Update()
        {
            if (locked || Time.time < s_blockedUntil) return;
            var origin = FindOrigin();
            if (origin == null || origin.Camera == null) return;

            Vector3 head = transform.InverseTransformPoint(origin.Camera.transform.position);
            if (Mathf.Abs(head.x) > triggerWidth * 0.5f || head.z > -0.05f || head.z < -triggerDepth) return;
            Travel(origin);
        }

        void Travel(XROrigin origin)
        {
            if (!Registry.TryGetValue(targetId ?? string.Empty, out var target) || target == null || target.arrival == null)
            {
                Debug.LogWarning($"[DoorPortal] '{portalId}' leads to '{targetId}', which isn't loaded.", this);
                s_blockedUntil = Time.time + 2f;
                return;
            }
            s_blockedUntil = Time.time + Cooldown;
            Teleport(origin, target.arrival.position, target.arrival.rotation);
        }

        static void Teleport(XROrigin origin, Vector3 position, Quaternion rotation)
        {
            // Preferred: the rig's own teleport provider, so it behaves exactly like the player's blink teleport.
            var provider = origin.GetComponentInChildren<TeleportationProvider>(true);
            if (provider != null)
            {
                provider.QueueTeleportRequest(new TeleportRequest
                {
                    destinationPosition = position,
                    destinationRotation = rotation,
                    matchOrientation = MatchOrientation.TargetUpAndForward,
                });
                return;
            }

            // Fallback for a rig without one.
            origin.MatchOriginUpCameraForward(Vector3.up, rotation * Vector3.forward);
            origin.MoveCameraToWorldLocation(new Vector3(position.x, origin.Camera.transform.position.y, position.z));
            origin.transform.position = new Vector3(origin.transform.position.x, position.y, origin.transform.position.z);
        }

        static XROrigin FindOrigin()
        {
            if (s_origin != null && s_origin.isActiveAndEnabled) return s_origin;
            if (Time.unscaledTime < s_nextOriginSearch) return null;
            s_nextOriginSearch = Time.unscaledTime + 0.5f;
            s_origin = FindFirstObjectByType<XROrigin>();
            return s_origin;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = locked ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0f, 1f, -triggerDepth * 0.5f), new Vector3(triggerWidth, 2f, triggerDepth));
            if (arrival == null) return;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(arrival.position + Vector3.up * 0.05f, 0.1f);
            Gizmos.DrawRay(arrival.position + Vector3.up * 0.05f, arrival.forward * 0.6f);
        }
    }
}
