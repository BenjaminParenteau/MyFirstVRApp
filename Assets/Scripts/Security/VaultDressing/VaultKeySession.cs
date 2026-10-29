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
            if (HasKey || Key || Lock.KeySeated) return;
            var keyPrefab = Resources.Load<GameObject>("VaultCustodianKey");
            if (!keyPrefab) return;
            var key = Object.Instantiate(keyPrefab);
            // On the existing right operator counter, clear of its keyboard and radio.
            key.transform.SetPositionAndRotation(new Vector3(44.55f, .766f, 5.64f), Quaternion.Euler(0, 25, 0));
            SceneManager.MoveGameObjectToScene(key, scene);
        }
    }
}
