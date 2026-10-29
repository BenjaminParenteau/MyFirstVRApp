using UnityEngine;
using UnityEngine.SceneManagement;
using HighStakes.Environment;

namespace HighStakes.Security
{
    /// <summary>Installs treasury dressing and one open, outward-swinging door at the actual connected entrance.</summary>
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
            Transform room=null;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name=="RoomC (Vault Antechamber)")room=t;
            if(!room){Debug.LogError("Vault room anchor is missing.");return;}
            var instance=Instantiate(prefab);
            instance.name="VaultDressingModule";
            instance.transform.SetPositionAndRotation(room.position,room.rotation);
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
                    portal.physicalPassage=true;
                    portal.SetLocked(false);
                    // The shared opening has one physical leaf, not two teleport doors.
                    var leaf=portal.transform.Find("Leaf");
                    if(leaf)leaf.gameObject.SetActive(false);
                    if(portal.portalId=="RoomC_Door")
                    {
                        foreach(var renderer in portal.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                        foreach(var collider in portal.GetComponentsInChildren<Collider>())collider.enabled=false;
                    }
                    if(portal.portalId=="HallC_End")
                    {
                        var outside=Instantiate(entrance,portal.transform);
                        outside.name="Treasury entrance - hallway side";
                        outside.transform.localPosition=Vector3.zero;
                        outside.transform.localRotation=Quaternion.identity;
                        outside.AddComponent<VaultDoorMotion>().Prepare();
                    }
                }
            }
            // The office feed watches the actual relocated room, including its real entrance.
            var wall=FindFirstObjectByType<SecurityMonitorWall>();
            if(wall && wall.feeds!=null)
                foreach(var feed in wall.feeds)
                    if(feed.sceneName=="MVP_Vault")
                    {feed.position=room.TransformPoint(new Vector3(.3f,2.65f,-.1f));feed.lookAt=room.TransformPoint(new Vector3(3,1.1f,6));}
        }
    }
}
