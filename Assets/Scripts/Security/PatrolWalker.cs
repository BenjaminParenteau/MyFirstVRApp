using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>
    /// Walks an NPC around a loop of waypoints: turn toward the next one, walk there, stop and look around, repeat.
    /// It only moves the character and drives its Animator's "Walking" flag; detection and suspicion are separate
    /// systems that will listen to it later (see Assets/Scripts/Security/README.md).
    /// </summary>
    public class PatrolWalker : MonoBehaviour
    {
        [SerializeField] Transform[] waypoints;
        [SerializeField] Animator animator;
        [Tooltip("Metres per second. Calibration knob: match the walk clip's stride so the feet don't slide.")]
        [SerializeField] float walkSpeed = 0.9f;
        [Tooltip("Degrees per second while turning toward the next waypoint.")]
        [SerializeField] float turnSpeed = 140f;
        [Tooltip("He only steps forward once he faces the waypoint within this angle, so he turns on the spot first.")]
        [SerializeField] float walkWithinAngle = 35f;
        [Tooltip("Seconds he stands and looks around at each waypoint (one look-around clip is 4.6 s).")]
        [SerializeField] float pauseSeconds = 4.6f;

        static readonly int Walking = Animator.StringToHash("Walking");

        int next;
        float pauseLeft;

        void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            if (pauseLeft > 0f)
            {
                pauseLeft -= Time.deltaTime;
                animator.SetBool(Walking, false);
                return;
            }

            Vector3 target = waypoints[next].position;
            Vector3 flat = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
            if (flat.magnitude < 0.05f)
            {
                next = (next + 1) % waypoints.Length;
                pauseLeft = pauseSeconds;
                return;
            }

            var facing = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
            animator.SetBool(Walking, true);
            if (Quaternion.Angle(transform.rotation, facing) <= walkWithinAngle)
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(target.x, transform.position.y, target.z), walkSpeed * Time.deltaTime);
        }
    }
}
