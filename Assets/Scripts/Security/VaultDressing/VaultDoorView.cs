using HighStakes.Environment;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace HighStakes.Security
{
    /// <summary>Shows the destination treasury through the open portal instead of the islands' empty sky.</summary>
    public sealed class VaultDoorView : MonoBehaviour
    {
        public DoorPortal portal;
        Camera capture;
        RenderTexture texture;
        Material material;
        GameObject surface;
        float next;
        XROrigin player;

        void Start()
        {
            texture = new RenderTexture(768, 1024, 24) { name = "Vault entrance view" };
            texture.Create();
            var template = Resources.Load<Material>("VaultDoorViewMaterial");
            material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap", texture);
            surface = GameObject.CreatePrimitive(PrimitiveType.Quad);surface.name = "View into the treasury";
            surface.transform.SetParent(transform, false);surface.transform.localPosition = new Vector3(0, 1.35f, .03f);
            surface.transform.localScale = new Vector3(2, 2.7f, 1);surface.layer = 2;
            Destroy(surface.GetComponent<Collider>());surface.GetComponent<Renderer>().sharedMaterial = material;
            var cameraObject = new GameObject("Treasury destination view camera");cameraObject.transform.SetParent(transform, false);
            capture = cameraObject.AddComponent<Camera>();capture.enabled = false;capture.nearClipPlane = .05f;
            capture.fieldOfView = 72;capture.aspect = .75f;capture.cullingMask = ~(1 << 2);capture.targetTexture = texture;
            surface.SetActive(false);
        }

        void Update()
        {
            if (!capture || !portal) return;
            bool show = VaultKeySession.Lock.IsOpen;
            surface.SetActive(show);
            if (!show || Time.unscaledTime < next || !DoorPortal.TryGetArrival(portal.targetId, out var arrival)) return;
            if (!player) player = FindFirstObjectByType<XROrigin>();
            if (player && player.Camera && (player.Camera.transform.position - transform.position).sqrMagnitude > 36) return;
            next = Time.unscaledTime + .25f;
            capture.transform.SetPositionAndRotation(arrival.position + Vector3.up * 1.6f, arrival.rotation);
            // Destination capture must not show a second copy of the player's hands after crossing the portal.
            var renderers = player ? player.GetComponentsInChildren<Renderer>() : new Renderer[0];
            var hidden = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { hidden[i] = renderers[i].forceRenderingOff;renderers[i].forceRenderingOff = true; }
            try { capture.Render(); }
            finally { for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].forceRenderingOff = hidden[i]; }
        }

        void OnDestroy()
        {
            if (capture) capture.targetTexture = null;
            if (texture) { texture.Release();Destroy(texture); }
            if (material) Destroy(material);
        }
    }
}
