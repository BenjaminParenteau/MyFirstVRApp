using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds <c>Assets/Scenes/TheReturn.unity</c>, the Christian-worldview scene ("The Return").
///
/// This is a **standalone scene for the assignment**: it is not part of the MVP world, nothing loads it and nothing
/// links to it, so it cannot disturb the team's door/scene flow, build list or anyone else's area. Open it and press
/// Play to walk it with the XR Interaction Simulator (WASD), or on the headset once it is added to Build Settings by
/// hand. It is deliberately left out of <see cref="WorldBuilder.AreaScenePaths"/> for that reason.
///
/// It is a duplicate of the back-of-house connector room: the scene is copied from MVP_Vault so it keeps the shared
/// style kit, the cool back-of-house look and the vault's baked lightmaps (they cannot be re-baked on a machine
/// without a GPU, StyleGuide §9), then the vault-specific parts are stripped and the room becomes the staff break
/// room. The copy keeps the <c>_SoloTest</c> rig (the one XR Origin, with locomotion and the simulator) and
/// <c>Volume_Global</c>, so the scene plays on its own.
///
/// Re-running overwrites the scene, so make hand edits after the last run.
/// </summary>
public static class TheReturnBuilder
{
    public const string SceneName = "TheReturn";
    public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";

    /// <summary>The connector room this scene is duplicated from (hallway + 8 x 8 m room, cool back-of-house look).</summary>
    const string SourceScenePath = "Assets/Scenes/MVP/MVP_Vault.unity";

    // Doors in the copied scene: the room's own door becomes the room's exit, inert until the endings are built.
    const string SourceRoomDoor = "RoomC_Door";
    const string ReturnDoor = "Return_Door";

    [MenuItem("High Stakes/Build The Return Scene")]
    public static void BuildMenu()
    {
        if (!Application.isBatchMode)
        {
            if (!EditorUtility.DisplayDialog("Build The Return?",
                    "Copies MVP_Vault to Assets/Scenes/TheReturn.unity and strips the vault parts out of it. Nothing "
                    + "else in the project is touched. Hand edits to TheReturn are lost.",
                    "Build", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }
        Build();
    }

    /// <summary>Batch entry point (-executeMethod TheReturnBuilder.Build).</summary>
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
        {
            Debug.LogError($"[Return] {SourceScenePath} not found. Run High Stakes > Build Security Wing Scenes first.");
            return;
        }

        if (!Duplicate()) return;
        Strip();

        AssetDatabase.SaveAssets();
        Debug.Log($"[Return] Built {ScenePath}. Open it and press Play to walk the break room with WASD.");
    }

    // ------------------------------------------------------------------ duplicate

    static bool Duplicate()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError($"[Return] Could not copy {SourceScenePath} to {ScenePath}.");
            return false;
        }
        AssetDatabase.ImportAsset(ScenePath);
        return true;
    }

    // ------------------------------------------------------------------ strip the vault out of the copy

    /// <summary>
    /// Turns the copied vault wing into the break room: drops the hallway and the vault door, renames the room, removes
    /// any suspicion meter, makes the room's door inert and drops the solo rig just inside it.
    /// </summary>
    static void Strip()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var content = GameObject.Find("WingC_Content");
        if (content != null) content.name = "Return_Content";

        // The hallway half of the wing is not part of the break room.
        DestroyByName(content, "HallC");

        var room = FindChildStartingWith(content, "RoomC");
        if (room != null) room.name = "BreakRoom (Staff Break Room)";

        // The treasury door facade belongs to the vault, not to a break room.
        DestroyByName(room, "VaultDoor_Placeholder");

        RemoveSuspicionMeter();
        MakeDoorInert(out var arrival);
        MoveSoloSpawn(arrival);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// The suspicion meter is Slot B's and is not built yet, so normally there is nothing to remove. The scene is
    /// explicitly cleared of it anyway: nothing in The Return is hidden from anyone, so if a suspicion meter is ever
    /// added to the shared rooms this scene keeps coming back without one.
    /// </summary>
    static void RemoveSuspicionMeter()
    {
        int removed = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.name.IndexOf("Suspicion", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Object.DestroyImmediate(t.gameObject);
            removed++;
        }
        Debug.Log(removed == 0
            ? "[Return] No suspicion meter in the scene (Slot B has not built one yet); nothing to remove."
            : $"[Return] Removed {removed} suspicion meter object(s).");
    }

    /// <summary>
    /// The room's door led back to the wing's hallway, which is gone. It stays as the room's exit but does nothing yet:
    /// the endings (hand the ledger over, or walk out with the money) will drive it.
    /// </summary>
    static void MakeDoorInert(out Transform arrival)
    {
        arrival = null;
        foreach (var portal in Object.FindObjectsByType<DoorPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (portal.portalId != SourceRoomDoor) continue;
            portal.portalId = ReturnDoor;
            portal.targetId = string.Empty;
            portal.targetScene = string.Empty;
            portal.physicalPassage = false;
            portal.locked = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            arrival = portal.arrival;
            return;
        }
        Debug.LogWarning($"[Return] No door '{SourceRoomDoor}' in the copied scene; the solo rig keeps the vault spawn.");
    }

    /// <summary>Puts the solo-test rig just inside the room, in front of the door and facing into the room.</summary>
    static void MoveSoloSpawn(Transform arrival)
    {
        if (arrival == null) return;
        var solo = GameObject.Find("_SoloTest");
        if (solo == null) return;
        var origin = solo.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
        if (origin == null) return;
        origin.transform.position = arrival.position + arrival.forward * 1.2f;
        origin.transform.rotation = arrival.rotation;
    }

    // ------------------------------------------------------------------ helpers

    static void DestroyByName(GameObject parent, string name)
    {
        var go = parent == null ? GameObject.Find(name) : FindChild(parent, name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static GameObject FindChild(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
            if (child.name == name) return child.gameObject;
        return null;
    }

    static GameObject FindChildStartingWith(GameObject parent, string prefix)
    {
        if (parent == null) return null;
        foreach (Transform child in parent.transform)
            if (child.name.StartsWith(prefix)) return child.gameObject;
        return null;
    }
}
