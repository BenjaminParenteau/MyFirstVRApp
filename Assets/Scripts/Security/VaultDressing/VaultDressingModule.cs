using UnityEngine;
using UnityEngine.SceneManagement;
using HighStakes.Environment;

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
            var entrance=Resources.Load<GameObject>("VaultEntranceDoor");
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name=="VaultDoor_Placeholder")t.gameObject.SetActive(false);
                foreach(var portal in root.GetComponentsInChildren<DoorPortal>(true))
                {
                    if(portal.portalId!="RoomC_Door" && portal.portalId!="HallC_End")continue;
                    if(!entrance)continue;
                    // Only replace the plain visual leaf; readers, arrival points and triggers stay intact.
                    var leaf=portal.transform.Find("Leaf");
                    if(leaf && leaf.TryGetComponent<Renderer>(out var renderer))renderer.enabled=false;
                    if(portal.portalId=="HallC_End")
                    {
                        var outside=Instantiate(entrance,portal.transform);
                        outside.name="Treasury entrance - hallway side";
                        outside.transform.localPosition=Vector3.zero;
                        outside.transform.localRotation=Quaternion.identity;
                    }
                }
            }
        }
    }
}
