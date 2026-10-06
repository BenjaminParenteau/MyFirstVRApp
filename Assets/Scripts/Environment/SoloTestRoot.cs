using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;

namespace HighStakes.Environment
{
    /// <summary>
    /// Sits on the "_SoloTest" object of every slice scene (rig, simulator, test floor, light, volume), and on MVP_Main's
    /// "_EditorSimulator". When the scene is opened on its own it stays on, so the owner can test. When the scene is
    /// loaded additively into MVP_Main (which is then the active scene) it switches itself off, so there is only ever
    /// one player rig.
    /// </summary>
    [DefaultExecutionOrder(-10000)] // run before the rig's own Awake so the duplicate rig never starts
    public class SoloTestRoot : MonoBehaviour
    {
        [Tooltip("Walking speed (m/s) with WASD in the XR Interaction Simulator. The simulator's own default is 1 m/s.")]
        [SerializeField] float simulatorWalkSpeed = 3f;

        XRInteractionSimulator simulator;

        /// <summary>
        /// The keyboard/mouse simulator is for the Editor without a headset only. With a headset (Quest Link) it would
        /// add a fake HMD next to the real one and the camera could follow the fake one: view on the floor, head
        /// tracking wrong. A running XR loader means a headset; isDeviceActive alone can still be false at startup.
        /// </summary>
        public static bool SimulatorAllowed()
        {
            if (!Application.isEditor || XRSettings.isDeviceActive) return false;
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            return manager == null || manager.activeLoader == null;
        }

        void Awake()
        {
            if (gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                gameObject.SetActive(false);
                return;
            }

            simulator = GetComponentInChildren<XRInteractionSimulator>(true);
            if (simulator == null) return;
            if (!SimulatorAllowed())
                simulator.gameObject.SetActive(false);
            else if (simulator.translateZSpeed > 0f)
                simulator.bodyTranslateMultiplier = simulatorWalkSpeed / simulator.translateZSpeed; // speed = key value x translate speed x multiplier
        }

        // A headset session can come up after the first frame; switch the simulator off as soon as it does.
        void Update()
        {
            if (simulator != null && simulator.gameObject.activeSelf && !SimulatorAllowed())
                simulator.gameObject.SetActive(false);
        }
    }
}
