using HighStakes.Environment.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighStakes.Security
{
    /// <summary>Installs audio on existing generated rooms and on subsequent rebuilds.</summary>
    internal static class SecurityAudioRoom
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => SceneManager.sceneLoaded -= Loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() => SceneManager.sceneLoaded += Loaded;

        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (SecurityMonitorWall.LoadingRemote) return;
            // The shared Soundscape builder can supply a scene instance later. Make this branch
            // playable from the checked-in MVP_Main in the meantime, without saving that scene.
            if (scene.name == "MVP_Main" && !Soundscape.Instance)
            {
                var holder = new GameObject("Soundscape (runtime)");
                SceneManager.MoveGameObjectToScene(holder, scene);
                holder.AddComponent<Soundscape>();
            }

            bool hall = scene.name == "MVP_SecurityHall";
            if (!hall && scene.name != "MVP_SecurityOffice" && scene.name != "MVP_CameraRoom") return;
            bool foundLight = false;
            bool foundRadio = false;
            string radioAnchor = hall ? "SconceLamp" : "Dispatch panel";
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var anchor in root.GetComponentsInChildren<Transform>())
                {
                    if (!foundLight && anchor.name == "Fluorescent housing")
                    {
                        SecurityLoopEmitter.Attach(anchor, "sec.fluorescent");
                        foundLight = true;
                    }
                    if (!foundRadio && anchor.name == radioAnchor)
                    {
                        SecurityLoopEmitter.Attach(anchor, "sec.radio.chatter");
                        foundRadio = true;
                    }
                    if (foundLight && foundRadio) return;
                }
            }
        }
    }
}
