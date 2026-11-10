using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace HighStakes.Environment
{
    /// <summary>
    /// Softens teleporting: the player arrives with the view black and it fades up over a tenth of a second, instead of
    /// the world cutting from one place to another. The cut is the part of teleporting testers report as jarring, and
    /// unlike smooth movement a fade costs no vection, so this is on whichever movement mode is chosen.
    /// <br/>
    /// Shares MVP_Main's ScreenFade quad with <see cref="SceneFlow"/> and stays out of its way while it is swapping
    /// areas (walking through a door already fades, and <see cref="DoorPortal"/> moves the rig with the same provider).
    /// </summary>
    public sealed class TeleportBlink : MonoBehaviour
    {
        [Tooltip("The black quad in front of the headset camera, shared with SceneFlow.")]
        [SerializeField] Renderer fade;
        [Tooltip("How long the view takes to come back after arriving (s). Above ~0.2 s it starts to feel sluggish.")]
        [SerializeField] float seconds = 0.12f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock block;
        TeleportationProvider provider;
        float alpha;

        void Awake() => block = new MaterialPropertyBlock();

        void OnDisable()
        {
            if (provider != null) provider.locomotionEnded -= OnArrived;
            provider = null;
            if (alpha > 0f) { alpha = 0f; SetFade(0f); }
        }

        void Update()
        {
            if (provider == null) Find();

            if (alpha <= 0f) return;
            alpha -= seconds > 0f ? Time.unscaledDeltaTime / seconds : 1f;
            SetFade(Mathf.Max(alpha, 0f));
            if (alpha <= 0f) alpha = 0f;
        }

        void Find()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;
            provider = origin.GetComponentInChildren<TeleportationProvider>(true);
            if (provider != null) provider.locomotionEnded += OnArrived;
        }

        void OnArrived(LocomotionProvider _)
        {
            // Travelling through a door is SceneFlow's fade, not ours.
            if (SceneFlow.Instance != null && SceneFlow.Instance.Busy) return;
            alpha = 1f;
            SetFade(1f);
        }

        void SetFade(float value)
        {
            if (fade == null) return;
            fade.enabled = value > 0f;
            block.SetColor(BaseColor, new Color(0f, 0f, 0f, value));
            fade.SetPropertyBlock(block);
        }
    }
}
