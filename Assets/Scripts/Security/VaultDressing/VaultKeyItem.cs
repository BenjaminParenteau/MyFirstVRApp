using HighStakes.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Security
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class VaultKeyItem : MonoBehaviour
    {
        public string keyCode = VaultLockLogic.KeyCode;
        public bool Seated { get; private set; }
        XRGrabInteractable grab;
        Vector3 rest;
        Quaternion restRotation;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            grab.selectEntered.AddListener(Taken);
            grab.selectExited.AddListener(Released);
            VaultKeySession.Key = this;
            rest = transform.position; restRotation = transform.rotation;
        }

        void Start() { rest = transform.position; restRotation = transform.rotation; }
        void OnDestroy()
        {
            if (grab) grab.selectEntered.RemoveListener(Taken);
            if (grab) grab.selectExited.RemoveListener(Released);
            if (VaultKeySession.Key == this) VaultKeySession.Key = null;
        }

        void Taken(SelectEnterEventArgs _) => Collect();
        void Released(SelectExitEventArgs _)
        {
            if (Seated) return;
            var body = GetComponent<Rigidbody>();body.isKinematic = false;body.useGravity = true;
        }

        public void Collect()
        {
            VaultKeySession.HasKey = true;
            if (transform.parent) transform.SetParent(null, true);
            var notebook = FindFirstObjectByType<NotebookPickups>();
            if (notebook && gameObject.scene != notebook.gameObject.scene)
                SceneManager.MoveGameObjectToScene(gameObject, notebook.gameObject.scene);
            else if (!notebook) DontDestroyOnLoad(gameObject);
        }

        public void Seat(Transform socket)
        {
            if (Seated) return;
            var notebook = FindFirstObjectByType<NotebookPickups>();
            if (notebook) notebook.ReleaseToWorld(gameObject);
            if (grab.isSelected && grab.interactionManager) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            grab.enabled = false;
            var pickup = GetComponent<PickupItem>();
            if (pickup) pickup.enabled = false;
            Seated = true;
            var body = GetComponent<Rigidbody>();body.isKinematic = true;body.useGravity = false;
            transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(gameObject, socket.gameObject.scene);
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }

        void Update()
        {
            // A dropped key cannot disappear through the level or become permanently unreachable.
            if (!Seated && !grab.isSelected && transform.position.y < -2)
            {
                var body = GetComponent<Rigidbody>(); body.linearVelocity = Vector3.zero;body.angularVelocity = Vector3.zero;
                body.isKinematic = true;body.useGravity = false;
                var camera = Camera.main;
                transform.SetPositionAndRotation(camera ? camera.transform.position + camera.transform.forward * .5f : rest, restRotation);
            }
        }
    }
}
