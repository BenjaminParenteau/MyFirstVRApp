using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Security
{
    /// <summary>Self-contained vault set dressing; no changes to portals, rewards, inventory or source scene.</summary>
    public sealed class VaultDressingModule : MonoBehaviour
    {
        public int cashBundles, goldBars, depositDrawers, cashCarts, triangles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLoader() => SceneManager.sceneLoaded -= Loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded += Loaded;
            for(int i=0;i<SceneManager.sceneCount;i++) Loaded(SceneManager.GetSceneAt(i),LoadSceneMode.Additive);
        }

        static void Loaded(Scene scene,LoadSceneMode _)
        {
            if(!scene.isLoaded || scene.name!="MVP_Vault")return;
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<VaultDressingModule>())return;
            var prefab=Resources.Load<GameObject>("VaultDressingModule");
            if(!prefab)return;
            var instance=Instantiate(prefab);
            instance.name="VaultDressingModule";
            instance.transform.position=new Vector3(40f,0,0);
            SceneManager.MoveGameObjectToScene(instance,scene);
        }
    }
}
