using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace HighStakes.Environment
{
    /// <summary>
    /// Sits on the "_SoloTest" object of every slice scene (rig, simulator, test floor, light, volume).
    /// When the scene is opened on its own it stays on, so the owner can test. When the scene is loaded additively
    /// into MVP_Main (which is then the active scene) it switches itself off, so there is only ever one player rig.
    /// </summary>
    [DefaultExecutionOrder(-10000)] // run before the rig's own Awake so the duplicate rig never starts
    public class SoloTestRoot : MonoBehaviour
    {
        [Tooltip("Walking speed (m/s) with WASD in the XR Interaction Simulator. The simulator's own default is 1 m/s.")]
        [SerializeField] float simulatorWalkSpeed = 3f;

        void Awake()
        {
            if (gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                gameObject.SetActive(false);
                return;
            }

            // The simulator (WASD + mouse) is for the Editor only: in a Quest build, or with a real headset connected
            // (Quest Link), it would add fake devices on top of the real ones.
            var simulator = GetComponentInChildren<XRInteractionSimulator>(true);
            if (simulator == null) return;
            if (!Application.isEditor || XRSettings.isDeviceActive)
                simulator.gameObject.SetActive(false);
            else if (simulator.translateZSpeed > 0f)
                simulator.bodyTranslateMultiplier = simulatorWalkSpeed / simulator.translateZSpeed; // speed = key value x translate speed x multiplier
        }
    }
}
