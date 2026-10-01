using UnityEngine;

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
                gameObject.SetActive(false);
        }
    }
}
