using System;
using System.IO;
using System.Reflection;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Checks real notebook ownership, physical insertion, portal gating and unload/revisit behavior.</summary>
[InitializeOnLoad]
public static class VaultKeyVerification
{
    const string Pending = "VaultKey.PlayCheck";
    static int stage;
    static double deadline, next;
    static VaultKeyLock mechanism;
    static VaultKeyVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                stage = 0;deadline = EditorApplication.timeSinceStartup + 150;next = 0;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);EditorSceneManager.OpenScene("Assets/Scenes/MVP/MVP_Main.unity");
            }
        };
    }

    [MenuItem("High Stakes/Vault/Run Custodian Key Play Check")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Temp/VaultKeyQA");SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MVP/MVP_Main.unity");EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out at stage " + stage);
            if (EditorApplication.timeSinceStartup < next) return;
            var flow = SceneFlow.Instance;if (!flow || flow.Busy) return;
            if (stage == 0) { Require(flow.Go("MVP_SecurityOffice", "RoomA_Door"), "Office travel failed");stage = 1;return; }
            if (stage == 1)
            {
                var key = VaultKeySession.Key;if (!key) return;
                Require(!VaultKeySession.HasKey && !VaultKeySession.Lock.IsOpen, "Key or vault granted before collection");
                var inventory = Object.FindFirstObjectByType<NotebookPickups>();Require(inventory, "Missing notebook inventory");
                Capture(key.transform.position + new Vector3(0, .42f, -1.4f), key.transform.position + Vector3.up * .18f, "office-key");
                typeof(NotebookPickups).GetMethod("PickUp", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inventory, new object[] { key.gameObject, key.GetComponent<PickupItem>().Item });
                Require(VaultKeySession.HasKey && !key.gameObject.activeSelf, "Notebook did not retain key");
                Require(key.gameObject.scene == inventory.gameObject.scene, "Stored key left in area scene");
                Require(flow.Go("MVP_Vault", "HallC_End"), "Vault travel failed");stage = 2;next = EditorApplication.timeSinceStartup + 2;return;
            }
            if (stage == 2)
            {
                mechanism = Object.FindFirstObjectByType<VaultKeyLock>();if (!mechanism) return;
                Require(mechanism.portal.locked, "Vault entry bypassed key lock");
                mechanism.TurnWheel(270);Require(!VaultKeySession.Lock.IsOpen, "Wheel bypassed key interlock");
                Capture(mechanism.transform.TransformPoint(new Vector3(.2f, 1.6f, -2.6f)), mechanism.transform.TransformPoint(new Vector3(.15f, 1.4f, 0)), "locked-vault");
                var inventory = Object.FindFirstObjectByType<NotebookPickups>();Require(inventory.TakeOut(VaultLockLogic.KeyCode), "Could not take key from notebook");
                var key = VaultKeySession.Key;key.transform.SetPositionAndRotation(mechanism.socket.position, mechanism.socket.rotation);
                typeof(VaultKeyLock).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(mechanism, null);
                Require(VaultKeySession.Lock.KeySeated && key.Seated, "Aligned held key did not seat");
                Require(inventory.HeldKey == null && key.transform.parent == mechanism.socket, "Notebook did not transfer key ownership");
                mechanism.TurnKey(90);VaultKeySession.Lock.Tick(.61f, true);
                Require(VaultKeySession.Lock.Armed, "Key interlock failed");mechanism.TurnWheel(270);
                stage = 3;next = EditorApplication.timeSinceStartup + 3;return;
            }
            if (stage == 3)
            {
                Require(VaultKeySession.Lock.IsOpen && !mechanism.portal.locked, "Door failed to open after bolt withdrawal");
                Require(mechanism.motion.OpenFraction > .9f, "Door did not swing clear");
                int movingDoors = 0;
                foreach (var motion in Object.FindObjectsByType<VaultDoorMotion>(FindObjectsSortMode.None))
                {
                    Require(motion.OpenFraction > .9f, "An entrance side did not swing open");movingDoors++;
                }
                Require(movingDoors == 2, "Expected an opening door on both sides of the portal");
                Capture(mechanism.portal.transform.TransformPoint(new Vector3(.2f, 1.6f, -2.6f)), mechanism.portal.transform.TransformPoint(new Vector3(.15f, 1.4f, 0)), "open-vault");
                Require(flow.Go("MVP_SecurityOffice", "RoomA_Door"), "Office revisit failed");stage = 4;return;
            }
            if (stage == 4)
            {
                Require(!VaultKeySession.Key, "Captive key survived as a duplicate outside its unloaded vault");
                Require(flow.Go("MVP_Vault", "HallC_End"), "Vault revisit failed");stage = 5;next = EditorApplication.timeSinceStartup + 3;return;
            }
            if (stage == 5)
            {
                mechanism = Object.FindFirstObjectByType<VaultKeyLock>();if (!mechanism) return;
                Require(VaultKeySession.Lock.IsOpen && !mechanism.portal.locked, "Revisit reset the unlocked door");
                Require(VaultKeySession.Key && VaultKeySession.Key.Seated, "Revisit lost the captive key");
                foreach (var door in Object.FindObjectsByType<DoorPortal>(FindObjectsSortMode.None))
                    if (door.portalId == "RoomC_Door") Require(!door.locked, "Vault exit trapped the player");
                Finish("PASS: office key -> notebook -> vault; held-key alignment; captive handoff; safety interlock; bolts and opening; office/vault revisit; free exit.");
            }
        }
        catch (Exception error) { Finish("FAIL at stage " + stage + ": " + error); }
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Finish(string result)
    {
        EditorApplication.update -= Tick;File.WriteAllText("Temp/VaultKeyQA/play-check.txt", result);
        Debug.Log("[VaultKey QA] " + result);EditorApplication.isPlaying = false;
    }
    static void Capture(Vector3 position, Vector3 target, string name)
    {
        var go = new GameObject("Vault QA camera");var camera = go.AddComponent<Camera>();
        camera.fieldOfView = 65;camera.nearClipPlane = .04f;go.transform.position = position;go.transform.LookAt(target);
        var rt = new RenderTexture(1200, 1000, 24);camera.targetTexture = rt;camera.Render();
        var previous = RenderTexture.active;RenderTexture.active = rt;
        var image = new Texture2D(1200, 1000, TextureFormat.RGB24, false);image.ReadPixels(new Rect(0, 0, 1200, 1000), 0, 0);image.Apply();
        File.WriteAllBytes("Temp/VaultKeyQA/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = previous;camera.targetTexture = null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
    }
}
