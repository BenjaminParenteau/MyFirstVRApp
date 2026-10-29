using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Security
{
    public static class VaultKeySession
    {
        public static VaultLockLogic Lock { get; private set; } = new VaultLockLogic();
        public static bool HasKey { get; internal set; }
        public static VaultKeyItem Key { get; internal set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Lock = new VaultLockLogic(); HasKey = false; Key = null;
            SceneManager.sceneLoaded -= Loaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded += Loaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) Loaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
        }

        static void Loaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name != "MVP_SecurityOffice" || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects()) if (root.name == "Vault key custody station") return;
            var prefab = Resources.Load<GameObject>("VaultKeyCustodyStation");
            if (!prefab) return;
            var station = Object.Instantiate(prefab);
            station.name = "Vault key custody station";
            station.transform.position = new Vector3(44.65f, 0, 2.45f);
            station.transform.rotation = Quaternion.Euler(0, -25, 0);
            SceneManager.MoveGameObjectToScene(station, scene);
            if (HasKey || Key || Lock.KeySeated) return;
            var keyPrefab = Resources.Load<GameObject>("VaultCustodianKey");
            if (!keyPrefab) return;
            var key = Object.Instantiate(keyPrefab);
            key.transform.SetPositionAndRotation(station.transform.TransformPoint(new Vector3(0, 1.08f, -.08f)), station.transform.rotation);
            SceneManager.MoveGameObjectToScene(key, scene);
        }
    }
}
