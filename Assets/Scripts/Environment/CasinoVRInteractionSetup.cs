using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Environment
{
    // Scene-local composition of XRI features; does not modify shared prefabs.
    [DefaultExecutionOrder(-9000)]
    public class CasinoVRInteractionSetup : MonoBehaviour
    {
        [SerializeField] GameObject floor;
        [SerializeField] GameObject[] pickupProps;
        [SerializeField] GameObject simulator;

        void Awake()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null && origin.GetComponent<RoomPlayerCollision>() == null)
                origin.gameObject.AddComponent<RoomPlayerCollision>();

            if (simulator != null)
                simulator.SetActive(Application.isEditor && !XRSettings.isDeviceActive &&
                    FindFirstObjectByType<XRInteractionSimulator>() == null);

            // Editor walking stays on the ground instead of flying the simulated headset.
            var activeSimulator = FindFirstObjectByType<XRInteractionSimulator>();
            if (activeSimulator != null)
                activeSimulator.translateYSpeed = 0f;

            var manager = FindFirstObjectByType<XRInteractionManager>();
            if (manager == null)
                manager = new GameObject("Casino_XRInteractionManager").AddComponent<XRInteractionManager>();

            var area = floor.AddComponent<CasinoTeleportationArea>();
            area.interactionManager = manager;
            // Matches the teleport-only layer used by the shared rig's teleport ray.
            area.interactionLayers = 1 << 31;

            foreach (var prop in pickupProps)
            {
                prop.layer = LayerMask.NameToLayer("Interactable");
                var body = prop.AddComponent<Rigidbody>();
                body.mass = 0.02f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var grab = prop.AddComponent<XRGrabInteractable>();
                grab.interactionManager = manager;
                grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                grab.useDynamicAttach = true;
            }

            // The visual concept has an open entrance. Prevent walking off its floor.
            var boundary = new GameObject("Casino_EntranceCollisionBoundary");
            boundary.transform.SetParent(transform, false);
            boundary.transform.localPosition = new Vector3(0, 2, -9);
            boundary.AddComponent<BoxCollider>().size = new Vector3(18, 4, 0.15f);
        }
    }
}
