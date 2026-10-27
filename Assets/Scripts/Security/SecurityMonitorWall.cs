using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HighStakes.Security
{
    /// <summary>One mono CCTV camera, staggered across the wall. Never renders to the headset.</summary>
    public sealed class SecurityMonitorWall : MonoBehaviour
    {
        [Serializable]
        public sealed class Feed
        {
            public string label;
            public string sceneName;
            public Vector3 position;
            public Vector3 lookAt;
            public Renderer screen;
            public TMP_Text caption;
            [NonSerialized] public RenderTexture texture;
            [NonSerialized] public int layer;
            [NonSerialized] public bool offline;
        }

        public Feed[] feeds = Array.Empty<Feed>();
        public Material screenMaterial;
        [Range(1, 10)] public int updatesPerFeed = 4;
        [Range(128, 1024)] public int resolution = 512;
        public float viewingDistance = 12f;
        public bool paused;
        Camera capture;
        readonly List<Scene> remoteScenes = new List<Scene>();
        readonly Dictionary<Camera,int> cameraMasks = new Dictionary<Camera,int>();
        AsyncOperation pendingLoad;
        bool releasing;
        bool ready;
        SphericalHarmonicsL2 captureAmbient;
        static SecurityMonitorWall instance;
        public static bool LoadingRemote { get; private set; }
        int next;
        float due;
        MaterialPropertyBlock block;
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        public int RenderedFrames { get; private set; }
        public int RemoteSceneCount => remoteScenes.Count;

        void OnEnable()
        {
            if (!Application.isPlaying || feeds.Length == 0) return;
            block = new MaterialPropertyBlock();
            instance = this;
            ready = false;
            releasing = false;
            capture = new GameObject("CCTV capture (mono, no audio)").AddComponent<Camera>();
            capture.transform.SetParent(transform, false);
            capture.enabled = false;
            capture.fieldOfView = 75f;
            capture.nearClipPlane = .08f;
            capture.farClipPlane = 65f;
            capture.allowHDR = false;
            capture.allowMSAA = false;
            capture.clearFlags = CameraClearFlags.SolidColor;
            capture.backgroundColor = new Color(.025f, .04f, .055f);
            capture.useOcclusionCulling = false;
            var data = capture.GetUniversalAdditionalCameraData();
            data.allowXRRendering = false;
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorTexture = false;
            data.requiresDepthTexture = false;
            captureAmbient = new SphericalHarmonicsL2();
            captureAmbient.AddAmbientLight(new Color(.4f,.45f,.5f));
            for (int i = 0; i < feeds.Length; i++)
            {
                var f = feeds[i];
                if (!f.screen) continue;
                f.texture = new RenderTexture(resolution, resolution * 9 / 16, 16,
                    RenderTextureFormat.ARGB32) { name = "CCTV " + f.label, antiAliasing = 1 };
                f.texture.Create();
                f.screen.sharedMaterial = screenMaterial;
                Bind(f);
                if (CctvOutage.IsDown(f.sceneName)) BlackOut(f);
            }
            CctvOutage.WentDown += OnOutage;
            due = 0f;
            StartCoroutine(LoadRooms());
        }

        IEnumerator LoadRooms()
        {
            // Let SceneFlow finish unloading the previous playable area before observing it.
            while(HighStakes.Environment.SceneFlow.Instance && HighStakes.Environment.SceneFlow.Instance.Busy) yield return null;
            if(releasing) yield break;
            var layers = new Dictionary<string,int>();
            SceneManager.sceneLoaded += PrepareRoom;
            try
            {
                foreach(var f in feeds)
                {
                    if(releasing) yield break;
                    if(!layers.TryGetValue(f.sceneName,out int layer))
                    {
                        if(!Application.CanStreamedLevelBeLoaded(f.sceneName))
                        {
                            if(f.caption) f.caption.text=f.label+" / OFFLINE";
                            continue;
                        }
                        layer=26+layers.Count;
                        if(layer>29) throw new InvalidOperationException("CCTV supports four distinct remote rooms.");
                        layers.Add(f.sceneName,layer);
                        // Reserve the capture layers on the real player camera before remote renderers activate.
                        foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                        {
                            if(camera==capture || cameraMasks.ContainsKey(camera)) continue;
                            cameraMasks[camera]=camera.cullingMask;
                            camera.cullingMask &= ~(15 << 26);
                        }
                        LoadingRemote=true;
                        pendingLoad=SceneManager.LoadSceneAsync(f.sceneName,
                            new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));
                        yield return pendingLoad;
                        pendingLoad=null;
                        LoadingRemote=false;
                        var scene=SceneManager.GetSceneByName(f.sceneName);
                        remoteScenes.Add(scene);
                        foreach(var root in scene.GetRootGameObjects())
                            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=layer;
                    }
                    f.layer=layer;
                }
                ready=!releasing;
            }
            finally
            {
                LoadingRemote=false;
                SceneManager.sceneLoaded-=PrepareRoom;
            }
        }

        void PrepareRoom(Scene scene, LoadSceneMode mode)
        {
            if(!LoadingRemote || scene==gameObject.scene) return;
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root.name.StartsWith("_SoloTest") || root.name=="Zones") { root.SetActive(false); continue; }
                // Room geometry and actual patrol animation stay live; interactions/economy are dormant.
                foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if(!(behaviour is TMP_Text) && !(behaviour is PatrolWalker)) behaviour.enabled=false;
                foreach(var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
                foreach(var audio in root.GetComponentsInChildren<AudioSource>(true)) { audio.Stop(); audio.enabled=false; }
                foreach(var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled=false;
                foreach(var light in root.GetComponentsInChildren<Light>(true)) light.enabled=false;
                foreach(var body in root.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic=true;
                foreach(var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
                foreach(var animator in root.GetComponentsInChildren<Animator>(true)) animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            }
        }

        /// <summary>SceneFlow waits for capture rooms to unload before entering one as a playable area.</summary>
        public static IEnumerator ReleaseBeforeTravel()
        {
            if(!instance) yield break;
            var wall=instance;
            wall.releasing=true;
            wall.ready=false;
            if(wall.pendingLoad!=null) yield return wall.pendingLoad;
            // Resume LoadRooms so its completed load is tracked before unloading.
            yield return null;
            foreach(var scene in wall.remoteScenes)
                if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            wall.remoteScenes.Clear();
            wall.RestoreMasks();
        }

        void RestoreMasks()
        {
            foreach(var pair in cameraMasks) if(pair.Key) pair.Key.cullingMask=pair.Value;
            cameraMasks.Clear();
        }

        void Bind(Feed f)
        {
            f.screen.GetPropertyBlock(block);
            block.SetTexture(BaseMap, f.texture);
            block.SetTexture(EmissionMap, f.texture);
            f.screen.SetPropertyBlock(block);
        }

        void OnOutage(string sceneName)
        {
            foreach (var f in feeds)
                if (f.screen && f.sceneName == sceneName) BlackOut(f);
        }

        // A cut camera shows a black screen and is no longer rendered.
        void BlackOut(Feed f)
        {
            f.offline = true;
            f.screen.GetPropertyBlock(block);
            block.SetTexture(BaseMap, Texture2D.blackTexture);
            block.SetTexture(EmissionMap, Texture2D.blackTexture);
            f.screen.SetPropertyBlock(block);
            if (f.caption) f.caption.text = f.label + "  /  NO SIGNAL";
        }

        public void TogglePause()
        {
            paused = !paused;
            foreach (var f in feeds)
                if (f.caption && !f.offline) f.caption.text = f.label + (paused ? "  /  HOLD" : "  /  LIVE");
        }

        void LateUpdate()
        {
            if (!capture || !ready || paused || Time.unscaledTime < due) return;
            // No camera work while the player is in the approach hallway or outside the office.
            var player = Camera.main;
            if (player && (player.transform.position - transform.position).sqrMagnitude > viewingDistance * viewingDistance) return;
            due = Time.unscaledTime + 1f / (Mathf.Max(1, updatesPerFeed) * feeds.Length);
            var f = feeds[next];
            next = (next + 1) % feeds.Length;
            if (f.layer==0 || !f.texture || f.offline) return;
            capture.cullingMask=1 << f.layer;
            capture.transform.position = f.position;
            capture.transform.LookAt(f.lookAt);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = f.texture };
            if (RenderPipeline.SupportsRenderRequest(capture, request))
            {
                // CCTV exposure lifts unbaked source rooms without changing the player's lighting.
                var ambient=RenderSettings.ambientProbe;
                try { RenderSettings.ambientProbe=captureAmbient; RenderPipeline.SubmitRenderRequest(capture, request); }
                finally { RenderSettings.ambientProbe=ambient; }
                RenderedFrames++;
            }
        }

        void OnDisable()
        {
            ready=false;
            CctvOutage.WentDown-=OnOutage;
            SceneManager.sceneLoaded-=PrepareRoom;
            if(instance==this) instance=null;
            LoadingRemote=false;
            RestoreMasks();
            foreach(var scene in remoteScenes)
                if(scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            remoteScenes.Clear();
            foreach (var f in feeds)
            {
                if (f.screen) f.screen.SetPropertyBlock(null);
                if (f.texture) { f.texture.Release(); Destroy(f.texture); f.texture = null; }
            }
            if (capture) { Destroy(capture.gameObject); capture = null; }
        }
    }
}
