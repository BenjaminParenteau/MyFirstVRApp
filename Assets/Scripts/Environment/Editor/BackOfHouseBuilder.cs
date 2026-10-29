using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One place that puts the back of house together after HallwayBuilder generates the wing scenes, which come back
/// empty: the security & count room (Ashton's staff side + the count floor and supervisor booth), the camera room
/// (Ashton's live CCTV wall + the tape archive), and the puzzle chain through the hub to the vault keypad.
/// Build MVP World runs <see cref="FurnishAll"/> too, so a full rebuild no longer wipes the furniture and puzzles.
/// </summary>
public static class BackOfHouseBuilder
{
    [MenuItem("High Stakes/Build Security & Camera Rooms (12 x 12)")]
    public static void RebuildRoomsMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorUtility.DisplayDialog("Rebuild security & camera rooms?",
                "Regenerates MVP_SecurityOffice and MVP_CameraRoom at 12 x 12 m and furnishes them again (count room, supervisor booth, CCTV wall, puzzles). " +
                "Hand edits in those two scenes are lost and their baked lighting has to be re-baked (Ben or Ashton). The hub and vault keep theirs.",
                "Rebuild", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        RebuildRooms();
    }

    /// <summary>Batch entry point (-executeMethod BackOfHouseBuilder.RebuildRooms).</summary>
    public static void RebuildRooms()
    {
        var rooms = new[] { HallwayBuilder.ScenePaths[0], HallwayBuilder.ScenePaths[1] };
        if (!HallwayBuilder.RebuildWings(0, 1)) return;
        CasinoTextureBuilder.ApplyToScenes(rooms); // the warm hallways
        FurnishAll();
        RenderingOptimizer.OptimizeScenes(rooms);
        AssetDatabase.SaveAssets();
        Debug.Log("[BackOfHouse] Security & count room and camera room rebuilt at 12 x 12 m and furnished. Ask Ben or Ashton to bake both scenes.");
    }

    public static void FurnishAll()
    {
        SecurityOfficeBuilder.BuildOffice();
        SecurityOfficeBuilder.BuildCameraRoom();
        CountRoomBuilder.Build();
        BackRoomPuzzlesBuilder.BuildAll(); // tape archive, breaker panel, hub clearance, vault keypad
    }
}
