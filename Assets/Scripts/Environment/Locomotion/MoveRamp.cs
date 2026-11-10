using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace HighStakes.Environment
{
    /// <summary>
    /// Eases smooth movement up to speed instead of snapping to it. A stick that goes from standing still to 3 m/s in
    /// one frame is the strongest motion-sickness trigger in the smooth mode (the eyes see an acceleration the inner ear
    /// doesn't), so the first third of a second ramps in. Top speed is unchanged, so the mode still feels the same to
    /// walk around in.
    /// </summary>
    public sealed class MoveRamp : MonoBehaviour
    {
        [Tooltip("How long the ramp from standing still to full speed takes (s). 0 disables it.")]
        [SerializeField] float rampSeconds = 0.3f;

        ContinuousMoveProvider move;
        float topSpeed;
        float moving;

        /// <summary>Full speed, as set on the rig's move provider. Changing it keeps the ramp working.</summary>
        public float TopSpeed
        {
            get => topSpeed;
            set => topSpeed = value;
        }

        void OnDisable()
        {
            if (move != null) move.moveSpeed = topSpeed;
            move = null;
        }

        void Update()
        {
            if (move == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin == null) return;
                move = origin.GetComponentInChildren<ContinuousMoveProvider>(true);
                if (move == null) return;
                topSpeed = move.moveSpeed;
            }

            if (rampSeconds <= 0f) return;

            if (move.locomotionState == LocomotionState.Moving) moving += Time.deltaTime;
            else moving = 0f;

            move.moveSpeed = topSpeed * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moving / rampSeconds));
        }
    }
}
