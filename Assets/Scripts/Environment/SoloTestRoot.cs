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
            if (simulator != null && (!Application.isEditor || XRSettings.isDeviceActive))
                simulator.gameObject.SetActive(false);
        }
    }
}
