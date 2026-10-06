using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Environment
{
    /// <summary>
    /// Lives in MVP_Main, next to the one player rig, wallet and wrist UI, which stay loaded all game. Exactly one area
    /// scene (casino, VIP, a security wing) is loaded on top at a time: walking into a <see cref="DoorPortal"/> whose
    /// target is in another scene fades to black, loads that scene, unloads the old one, moves the rig to the target
    /// door's arrival point and fades back in. Area scenes are authored at the origin and keep their own _SoloTest rig,
    /// which switches itself off here because MVP_Main is the active scene while they load.
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        [Tooltip("Area scene loaded at start. The rig in MVP_Main already stands at its start position.")]
        [SerializeField] string startScene = "MVP_Casino";
        [Tooltip("Black quad just in front of the headset camera, faded in while scenes swap.")]
        [SerializeField] Renderer fade;
        [SerializeField] float fadeSeconds = 0.25f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static SceneFlow Instance { get; private set; }

        /// <summary>The area scene currently loaded, or null while the first one loads.</summary>
        public string CurrentScene { get; private set; }

        public bool Busy { get; private set; }

        MaterialPropertyBlock block;

        void Awake()
        {
            Instance = this;
            block = new MaterialPropertyBlock();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            Busy = true;
            SetFade(1f);
            yield return Load(startScene);
            CurrentScene = startScene;
            yield return Fade(1f, 0f);
            Busy = false;
        }

        /// <summary>Swaps to <paramref name="scene"/> and puts the rig at door <paramref name="arrivalId"/>. False while a swap is running.</summary>
        public bool Go(string scene, string arrivalId)
        {
            if (Busy) return false;
            StartCoroutine(Swap(scene, arrivalId));
            return true;
        }

        IEnumerator Swap(string scene, string arrivalId)
        {
            Busy = true;
            yield return Fade(0f, 1f);

            var old = CurrentScene;
            if (old != scene)
            {
                yield return Load(scene);
                CurrentScene = scene;
                if (old != null) yield return SceneManager.UnloadSceneAsync(old);
                LightProbes.TetrahedralizeAsync();
            }

            if (DoorPortal.TryGetArrival(arrivalId, out var arrival))
            {
                DoorPortal.MoveRig(arrival);
            }
            else
            {
                Debug.LogError($"[SceneFlow] No door '{arrivalId}' in {scene}.");
                DoorPortal.Unblock();
            }

            // The rig's teleport is queued; let it land before the view comes back.
            yield return null;
            yield return null;
            yield return Fade(1f, 0f);
            Busy = false;
        }

        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            // The area's ambient light and lightmaps apply only while it is the active scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            LightProbes.TetrahedralizeAsync();
        }

        IEnumerator Fade(float from, float to)
        {
            for (var t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / fadeSeconds));
                yield return null;
            }
            SetFade(to);
        }

        void SetFade(float alpha)
        {
            if (fade == null) return;
            fade.enabled = alpha > 0f;
            block.SetColor(BaseColor, new Color(0f, 0f, 0f, alpha));
            fade.SetPropertyBlock(block);
        }
    }
}
