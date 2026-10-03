using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.Environment
{
    // Tracked movement (including simulator WASD) does not pass through XRI locomotion.
    // Sweep that displacement too, then offset the origin by the blocked portion.
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(XROrigin))]
    public class RoomPlayerCollision : MonoBehaviour
    {
        XROrigin origin;
        CharacterController controller;
        Vector3 previousHead;
        bool initialized;

        void Awake()
        {
            origin = GetComponent<XROrigin>();
            controller = origin.Origin.GetComponent<CharacterController>();
            if (controller == null)
                controller = origin.Origin.AddComponent<CharacterController>();
            controller.enabled = true;
            controller.detectCollisions = true;
            controller.radius = 0.25f;
            controller.skinWidth = 0.02f;
            controller.stepOffset = 0.15f;
            controller.minMoveDistance = 0f;
        }

        void OnEnable() => initialized = false;

        void LateUpdate()
        {
            if (origin.Camera == null || !controller.enabled) return;
            var root = origin.Origin.transform;
            var head = root.InverseTransformPoint(origin.Camera.transform.position);
            if (!initialized)
            {
                previousHead = head;
                initialized = true;
            }

            controller.height = Mathf.Max(controller.radius * 2f, head.y);
            var centerY = controller.height * 0.5f + controller.skinWidth;
            controller.center = new Vector3(previousHead.x, centerY, previousHead.z);
            var trackingMotion = root.TransformVector(new Vector3(
                head.x - previousHead.x, 0f, head.z - previousHead.z));
            if (trackingMotion.sqrMagnitude > 0f)
            {
                controller.Move(trackingMotion);
                root.position -= trackingMotion;
            }
            controller.center = new Vector3(head.x, centerY, head.z);
            previousHead = head;
        }
    }
}
