using HighStakes.Environment;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HighStakes.Security
{
    public sealed class VaultKeyLock : MonoBehaviour
    {
        public Transform socket, cylinder, wheelVisual;
        public Transform[] bolts;
        public Renderer indicator;
        public Material lockedMaterial, armedMaterial, openMaterial;
        public VaultRotaryHandle keyHandle, wheelHandle;
        public DoorPortal portal;
        public VaultDoorMotion motion;
        AudioSource audioSource;
        static AudioClip click, latch, boltSound;
        Vector3[] boltRest;
        bool wasArmed, wasOpen;
        int detent;
        float nextFeedback;
        XROrigin origin;
        HighStakes.UI.NotebookOpen notebook;
        bool turningKey, turningWheel;
        VaultLockLogic State => VaultKeySession.Lock;

        public static void Install(GameObject entrance, DoorPortal door)
        {
            var prefab = Resources.Load<GameObject>("VaultKeyMechanism");
            if (!prefab) { door.SetLocked(true); Debug.LogError("Vault key mechanism prefab is missing.");return; }
            var fixture = Instantiate(prefab, entrance.transform);
            fixture.transform.localPosition = Vector3.zero;fixture.transform.localRotation = Quaternion.identity;
            var owner = fixture.GetComponent<VaultKeyLock>();owner.portal = door;
            var wheel = new GameObject("Rotating bolt handwheel").transform;
            wheel.SetParent(fixture.transform, false);wheel.localPosition = new Vector3(0, 1.4f, -.36f);
            var parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform part in entrance.transform)
                if (part.name == "Handwheel" || part.name == "Wheel spoke" || part.name == "Wheel hub" || part.name == "Handwheel crank grip") parts.Add(part);
            foreach (var part in parts) part.SetParent(wheel, true);
            owner.wheelVisual = wheel;
            var motion = entrance.AddComponent<VaultDoorMotion>();motion.Prepare();owner.motion = motion;
            var view = door.gameObject.AddComponent<VaultDoorView>();view.portal = door;
            door.SetLocked(!VaultKeySession.Lock.IsOpen);
        }

        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();audioSource.spatialBlend = 1;
            audioSource.rolloffMode = AudioRolloffMode.Linear;audioSource.minDistance = .6f;audioSource.maxDistance = 8;
            if (!click) { click = Sound("Key ward click", .08f, 1800, .16f);latch = Sound("Safety latch", .3f, 220, .4f);boltSound = Sound("Bolt detent", .16f, 85, .28f); }
            boltRest = new Vector3[bolts.Length];for (int i = 0; i < bolts.Length; i++) boltRest[i] = bolts[i].localPosition;
            wasArmed = State.Armed; wasOpen = State.IsOpen;
            notebook = FindFirstObjectByType<HighStakes.UI.NotebookOpen>();
            if (State.KeySeated && (!VaultKeySession.Key || !VaultKeySession.Key.Seated))
            {
                // The key remains captive when returning to this area after an unload.
                var prefab = Resources.Load<GameObject>("VaultCustodianKey");
                if (prefab) Instantiate(prefab).GetComponent<VaultKeyItem>().Seat(socket);
            }
            Refresh();
        }

        void Update()
        {
            if (!portal) return;
            // Always enforce the interlock on entry; the room's exit is deliberately independent.
            portal.SetLocked(!State.IsOpen || !motion || motion.OpenFraction < .9f);
            bool near = (!notebook || !notebook.IsOpen) && LookingAtLock();
            var keyboard = Keyboard.current;
            if (!State.KeySeated)
            {
                var key = VaultKeySession.Key;
                if (key && key.gameObject.activeInHierarchy && !key.Seated)
                {
                    bool close = Vector3.Distance(key.transform.position, socket.position) < .3f;
                    if (close && (key.GetComponent<XRGrabInteractable>().isSelected || VaultKeySession.HasKey)) UseKey();
                }
            }
            if (near && keyboard != null && !State.IsOpen)
            {
                if (keyboard.enterKey.wasPressedThisFrame) UseKey();
                if (keyboard.rightArrowKey.wasPressedThisFrame) UseWheel();
            }
            if (turningKey && !State.Armed) TurnKey(150 * Time.deltaTime);
            if (turningWheel && State.Armed && !State.IsOpen) TurnWheel(140 * Time.deltaTime);
            if (State.Armed && !wasArmed) { Play(latch); wasArmed = true; }
            if (State.IsOpen && !wasOpen) { Play(latch);wasOpen = true; }
            Refresh();
        }

        public bool UseKey()
        {
            if (!State.KeySeated)
            {
                var key = VaultKeySession.Key;
                if (!key || (!VaultKeySession.HasKey && !key.GetComponent<XRGrabInteractable>().isSelected)) return false;
                if (!State.Insert(key.keyCode)) return false;
                // Works with a key in hand OR safely stored in the notebook; no menu fiddling at the door.
                key.Seat(socket);key.gameObject.SetActive(true);Play(latch);
            }
            turningKey = true;
            return true;
        }

        public void UseWheel()
        {
            if (!UseKey()) return;
            turningWheel = true;
        }

        bool LookingAtLock()
        {
            if (!origin) origin = FindFirstObjectByType<XROrigin>();
            if (!origin || !origin.Camera) return false;
            var eyes = origin.Camera.transform;
            Vector3 target = transform.TransformPoint(new Vector3(.4f, 1.4f, -.35f));
            var delta = target - eyes.position;
            return delta.sqrMagnitude < 4 && Vector3.Dot(eyes.forward, delta.normalized) > .75f
                && transform.InverseTransformPoint(eyes.position).z < -.1f;
        }

        public void TurnKey(float degrees)
        {
            State.TurnKey(degrees);
            int next = Mathf.FloorToInt(State.KeyAngle / 15);
            if (next != detent) { detent = next;Play(click); }
        }

        public void TurnWheel(float degrees)
        {
            if (!State.Armed && Mathf.Abs(degrees) > .1f)
            {
                if (Time.time >= nextFeedback) { Play(boltSound);nextFeedback = Time.time + .7f; }
                return;
            }
            State.TurnWheel(degrees);
            int next = 10 + Mathf.FloorToInt(State.WheelAngle / 30);
            if (next != detent) { detent = next;Play(boltSound); }
        }

        void Refresh()
        {
            cylinder.localRotation = Quaternion.Euler(0, 0, -State.KeyAngle);
            socket.localRotation = cylinder.localRotation;
            if (wheelVisual) wheelVisual.localRotation = Quaternion.Euler(0, 0, -State.WheelAngle);
            if (boltRest != null)
                for (int i = 0; i < bolts.Length; i++)
                    bolts[i].localPosition = boltRest[i] + Vector3.left * (.18f * State.WheelAngle / VaultLockLogic.WheelTravel);
            indicator.sharedMaterial = State.IsOpen ? openMaterial : State.Armed ? armedMaterial : lockedMaterial;
        }

        void Play(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip, .55f); }
        static AudioClip Sound(string name, float duration, float hz, float noise)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)];var random = new System.Random(73);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, envelope = Mathf.Exp(-t * 10 / duration);
                samples[i] = envelope * (Mathf.Sin(t * hz * Mathf.PI * 2) * .4f + ((float)random.NextDouble() * 2 - 1) * noise);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);clip.SetData(samples, 0);return clip;
        }
    }
}
