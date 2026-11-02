using System;
using HighStakes.UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace HighStakes.Environment
{
    /// <summary>
    /// Lives in MVP_Main and applies the player's comfort choices (<see cref="ComfortChoices"/>) to the one rig: smooth
    /// move or teleport on the left stick, snap or smooth turn on the right, a tunneling vignette while moving, and a
    /// seated mode that lifts the view to standing height. Saved in PlayerPrefs so a headset keeps its choices. The
    /// notebook's SETTINGS spread and the start screen change them through <see cref="Toggle"/>.
    /// </summary>
    public sealed class ComfortSettings : MonoBehaviour
    {
        const string PrefsKey = "HighStakes.Comfort";

        [Tooltip("XRI's TunnelingVignette prefab (Starter Assets), put on the headset camera when the vignette is on.")]
        [SerializeField] GameObject vignettePrefab;
        [Tooltip("How far seated mode lifts the view (metres): roughly standing minus sitting eye height.")]
        [SerializeField] float seatedLift = 0.45f;

        public static ComfortSettings Instance { get; private set; }

        public ComfortChoices Choices { get; private set; }

        public event Action<ComfortChoices> Changed;

        XROrigin origin;
        GameObject vignette;

        void Awake()
        {
            Instance = this;
            Choices = PlayerPrefs.HasKey(PrefsKey) ? SettingsLogic.FromBits(PlayerPrefs.GetInt(PrefsKey)) : SettingsLogic.Default;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start() => Apply();

        /// <summary>Switches one comfort row (<see cref="SettingsLogic.Move"/> etc.), applies and saves it.</summary>
        public void Toggle(int row)
        {
            Choices = SettingsLogic.Toggle(Choices, row);
            PlayerPrefs.SetInt(PrefsKey, SettingsLogic.ToBits(Choices));
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke(Choices);
        }

        void Apply()
        {
            if (origin == null) origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;

            // The starter rig's left hand moves (smooth, or teleport when off); the right hand always teleports and turns.
            foreach (var hand in origin.GetComponentsInChildren<ControllerInputActionManager>(true))
            {
                if (hand.name.Contains("Left")) hand.smoothMotionEnabled = Choices.SmoothMove;
                hand.smoothTurnEnabled = Choices.SmoothTurn;
            }

            if (Choices.Vignette && vignette == null && vignettePrefab != null && origin.Camera != null)
            {
                vignette = Instantiate(vignettePrefab, origin.Camera.transform, false);
                var controller = vignette.GetComponent<TunnelingVignetteController>();
                controller.locomotionVignetteProviders.Clear();
                foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>(true))
                    controller.locomotionVignetteProviders.Add(new LocomotionVignetteProvider { locomotionProvider = provider, enabled = true });
            }
            if (vignette != null) vignette.SetActive(Choices.Vignette);
        }

        // XROrigin resets the camera offset on a recenter, so seated mode re-adds its lift every frame.
        void LateUpdate()
        {
            if (origin == null || origin.CameraFloorOffsetObject == null) return;
            float baseHeight;
            switch (origin.CurrentTrackingOriginMode)
            {
                case UnityEngine.XR.TrackingOriginModeFlags.Floor: baseHeight = 0f; break;
                case UnityEngine.XR.TrackingOriginModeFlags.Device:
                case UnityEngine.XR.TrackingOriginModeFlags.Unbounded: baseHeight = origin.CameraYOffset; break;
                default: return; // tracking not started yet: leave XROrigin's offset alone
            }
            var offset = origin.CameraFloorOffsetObject.transform;
            var position = offset.localPosition;
            position.y = baseHeight + (Choices.Seated ? seatedLift : 0f);
            offset.localPosition = position;
        }
    }
}
