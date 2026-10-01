using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates MVP_Hallways.unity: three hallways that lead from the casino to three security rooms, built only from the
/// shared style kit. The pieces are NOT connected in the scene: each end has a <see cref="DoorPortal"/> that carries the
/// player to the matching door (casino door -> hallway start, hallway end -> security room, and back).
/// Each hallway opens in the casino's warm colours and turns cool toward the security side.
///
/// Slot D owns this. Re-running overwrites the scene, so after the first run edit the scene by hand.
/// </summary>
public static class HallwayBuilder
{
    const string ScenePath = "Assets/Scenes/MVP/MVP_Hallways.unity";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string LightingPath = "Assets/Content/StyleKit/Lighting/StyleKit_Lighting.lighting";
    const string PrefabFolder = "Assets/Content/Environment/Prefabs";

    const float T = StyleScale.TileSize; // 2 m grid cell
    const int WarmLeadIn = 3;            // hallway cells that keep the casino's warm look before turning cool

    enum Side { PlusZ, MinusZ, PlusX, MinusX }

    struct DoorSpec
    {
        public Vector2Int cell; public Side side; public string id; public string target;
        public DoorSpec(Vector2Int cell, Side side, string id, string target)
        { this.cell = cell; this.side = side; this.id = id; this.target = target; }
    }

    [MenuItem("High Stakes/Build Hallways Scene")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null) { Debug.LogError("[Hallways] Style kit not found. Run High Stakes > Build Style Kit first."); return; }
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Rebuild hallways?", ScenePath + " exists and will be overwritten, including any hand edits.", "Overwrite", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabFolder);
        var doorWarm = PortalDoorPrefab(kit, true);
        var doorCool = PortalDoorPrefab(kit, false);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x33, 0x2E, 0x2E, 0xFF); // between the warm and cool template ambients
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        // ---- test-only stand-in for the casino (switches off with _SoloTest when MVP_Main loads the real casino)
        var solo = BuildSoloTest(kit);
        BuildCasinoStandIn(kit, doorWarm, doorCool, solo.transform);

        // ---- the real content
        var content = new GameObject("Hallways_Content").transform;

        // Three hallways, each a different shape (not a maze: one path each), with a security room at the end.
        // Cells are (x, z) on the 2 m grid. The first WarmLeadIn cells are warm.
        BuildHallway(kit, doorWarm, doorCool, content, "A", 0, "Security Office", Room.Office,
            Cells((0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (0, 5), (0, 6)), Side.PlusZ);
        BuildHallway(kit, doorWarm, doorCool, content, "B", 1, "Camera Room", Room.Cameras,
            Cells((0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (1, 4), (2, 4), (3, 4)), Side.PlusX);
        BuildHallway(kit, doorWarm, doorCool, content, "C", 2, "Vault Antechamber", Room.Vault,
            Cells((0, 0), (0, 1), (0, 2), (-1, 2), (-2, 2), (-2, 3), (-2, 4), (-2, 5)), Side.PlusZ);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Hallways] Built " + ScenePath + ". Press Play: you start in the stand-in casino; walk up to a door (WASD in the simulator) to be carried into a hallway. Then run High Stakes > Validate Open Scene Against Style Kit.");
    }

    // ------------------------------------------------------------------ test rig

    static GameObject BuildSoloTest(SharedStyleKit kit)
    {
        var solo = new GameObject("_SoloTest");
        solo.AddComponent<SoloTestRoot>();

        var volGo = new GameObject("Volume_Global");
        volGo.transform.SetParent(solo.transform);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = kit.volumeProfile;

        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(solo.transform);
        spawn.transform.position = new Vector3(4f, 0f, 0f); // middle of the stand-in casino, facing the doors (+Z)

        if (kit.playerRig != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig, solo.transform);
            rig.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }
        StyleKitBuilder.AddSimulator(solo.transform);
        return solo;
    }

    // A small warm room with the three doors that, in the real game, are doors in Ashton's casino. They only use the
    // ids Casino_A/B/C, so the real casino just needs doors with the same ids.
    static void BuildCasinoStandIn(SharedStyleKit kit, GameObject doorWarm, GameObject doorCool, Transform solo)
    {
        var cells = new List<Vector2Int>();
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < 4; z++)
                cells.Add(new Vector2Int(x, z));
        var area = BuildArea(kit, doorWarm, doorCool, solo, "CasinoStandIn (test only)", Vector3.zero, cells, new HashSet<Vector2Int>(cells),
            new[]
            {
                new DoorSpec(new Vector2Int(0, 3), Side.PlusZ, "Casino_A", "HallA_Start"),
                new DoorSpec(new Vector2Int(2, 3), Side.PlusZ, "Casino_B", "HallB_Start"),
                new DoorSpec(new Vector2Int(4, 3), Side.PlusZ, "Casino_C", "HallC_Start"),
            });
        Place(kit.tableRound, area, new Vector3(4f, 0f, 2f), 0);
    }

    // ------------------------------------------------------------------ hallways and rooms

    enum Room { Office, Cameras, Vault }

    static void BuildHallway(SharedStyleKit kit, GameObject doorWarm, GameObject doorCool, Transform parent,
        string letter, int index, string roomName, Room roomKind, List<Vector2Int> path, Side endSide)
    {
        // Each hallway and its room sit on their own island so nothing is ever physically connected.
        var hallOrigin = new Vector3(100f, 0f, 40f * index);
        var roomOrigin = new Vector3(140f, 0f, 40f * index);

        var warm = new HashSet<Vector2Int>();
        for (int i = 0; i < Mathf.Min(WarmLeadIn, path.Count); i++) warm.Add(path[i]);

        var hall = BuildArea(kit, doorWarm, doorCool, parent, $"Hall{letter}", hallOrigin, path, warm,
            new[]
            {
                new DoorSpec(path[0], Side.MinusZ, $"Hall{letter}_Start", $"Casino_{letter}"),
                new DoorSpec(path[path.Count - 1], endSide, $"Hall{letter}_End", $"Room{letter}_Door"),
            });

        // Where the warm casino colours give way to the cool back-of-house: a narrow brass-framed doorway.
        for (int i = 0; i + 1 < path.Count; i++)
        {
            if (warm.Contains(path[i]) == warm.Contains(path[i + 1])) continue;
            Vector3 mid = (CellLocal(path[i]) + CellLocal(path[i + 1])) * 0.5f;
            float yaw = path[i].x != path[i + 1].x ? 90f : 0f;
            Place(kit.doorwayWarm, hall, mid, yaw);
        }

        var roomCells = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
            for (int z = 0; z < 3; z++)
                roomCells.Add(new Vector2Int(x, z));
        var room = BuildArea(kit, doorWarm, doorCool, parent, $"Room{letter} ({roomName})", roomOrigin, roomCells, new HashSet<Vector2Int>(),
            new[] { new DoorSpec(new Vector2Int(1, 0), Side.MinusZ, $"Room{letter}_Door", $"Hall{letter}_End") });
        Furnish(kit, room, roomKind);
    }

    // Builds floor, ceiling, walls and lights for a set of grid cells. A wall goes on every side that has no neighbouring
    // cell; the sides named in `doors` get a portal door instead. Cells in `warm` use the warm kit pieces, the rest cool.
    static Transform BuildArea(SharedStyleKit kit, GameObject doorWarm, GameObject doorCool, Transform parent, string name,
        Vector3 origin, List<Vector2Int> cells, HashSet<Vector2Int> warm, DoorSpec[] doors)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.position = origin;
        var set = new HashSet<Vector2Int>(cells);

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            bool w = warm.Contains(c);
            Vector3 centre = CellLocal(c);
            Place(w ? kit.floorTileWarm : kit.floorTileCool, g, centre, 0);
            Place(kit.ceilingTile, g, centre, 0);
            if (i % 3 == 1) Place(w ? kit.lightWarmFloor : kit.lightCoolCorridor, g, centre + Vector3.up * 2.7f, 0);

            foreach (Side s in new[] { Side.PlusZ, Side.MinusZ, Side.PlusX, Side.MinusX })
            {
                Vector3 edge = centre + new Vector3(Step(s).x, 0, Step(s).y);
                bool placedDoor = false;
                foreach (var d in doors)
                {
                    if (d.cell != c || d.side != s) continue;
                    var door = Place(w ? doorWarm : doorCool, g, edge, Yaw(s));
                    var portal = door.GetComponent<DoorPortal>();
                    portal.portalId = d.id;
                    portal.targetId = d.target;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                    door.name = "Door_" + d.id;
                    placedDoor = true;
                }
                if (!placedDoor && !set.Contains(c + Step(s)))
                    Place(w ? kit.wallWarm : kit.wallCool, g, edge, Yaw(s));
            }
        }
        return g;
    }

    static void Furnish(SharedStyleKit kit, Transform room, Room kind)
    {
        // Room cells are (0..2, 0..2), so its middle is (2, 2) and the +Z wall's inner face is at z = 4.925.
        switch (kind)
        {
            case Room.Office:
                Place(kit.tableRound, room, new Vector3(2f, 0, 2.2f), 0);
                Place(kit.chair, room, new Vector3(2f, 0, 1.2f), 0);
                Place(kit.chair, room, new Vector3(2f, 0, 3.2f), 180);
                for (int i = 0; i < 3; i++) Screen(kit, room, new Vector3(0.9f + i * 1.1f, 1.6f, 4.9f), new Vector3(1f, 0.6f, 0.03f));
                break;
            case Room.Cameras:
                Place(kit.chair, room, new Vector3(2f, 0, 2.6f), 180);
                for (int row = 0; row < 2; row++)
                    for (int i = 0; i < 4; i++)
                        Screen(kit, room, new Vector3(-0.3f + i * 1.3f, 1.2f + row * 0.7f, 4.9f), new Vector3(1.1f, 0.6f, 0.03f));
                break;
            case Room.Vault:
                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "VaultDoor_Placeholder";
                door.transform.SetParent(room, false);
                door.transform.localPosition = new Vector3(2f, 1.2f, 4.85f);
                door.transform.localScale = new Vector3(1.8f, 2.4f, 0.1f);
                door.GetComponent<Renderer>().sharedMaterial = kit.steelCool;
                door.isStatic = true;
                // Reader faces -Z, toward the player. Slot B wires its keycard behaviour.
                Place(kit.doorReader, room, new Vector3(3.4f, 1.3f, 4.9f), 0);
                break;
        }
    }

    static void Screen(SharedStyleKit kit, Transform parent, Vector3 pos, Vector3 size)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = "Screen";
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos;
        s.transform.localScale = size;
        s.GetComponent<Renderer>().sharedMaterial = kit.screenGlow;
        Object.DestroyImmediate(s.GetComponent<Collider>());
        s.isStatic = true;
    }

    // ------------------------------------------------------------------ portal door prefab

    // Kit doorway + a closed door leaf (so the opening never shows the empty space behind it) + a card reader for Slot B
    // + the DoorPortal. The door's front (the side the player stands on) faces local -Z.
    static GameObject PortalDoorPrefab(SharedStyleKit kit, bool warm)
    {
        string name = warm ? "Env_PortalDoor_Warm" : "Env_PortalDoor_Cool";
        var root = new GameObject(name);

        var frame = (GameObject)PrefabUtility.InstantiatePrefab(warm ? kit.doorwayWarm : kit.doorwayCool, root.transform);
        frame.name = "Frame";

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Leaf";
        leaf.transform.SetParent(root.transform, false);
        leaf.transform.localPosition = new Vector3(0f, StyleScale.DoorHeight / 2f, 0f);
        leaf.transform.localScale = new Vector3(StyleScale.DoorWidth, StyleScale.DoorHeight, 0.05f);
        leaf.GetComponent<Renderer>().sharedMaterial = warm ? kit.woodDark : kit.steelCool;
        leaf.isStatic = true;

        var reader = (GameObject)PrefabUtility.InstantiatePrefab(kit.doorReader, root.transform);
        reader.name = "Reader";
        reader.transform.localPosition = new Vector3(0.8f, 1.3f, -(StyleScale.WallThickness / 2f + 0.011f));

        var arrival = new GameObject("Arrival").transform;
        arrival.SetParent(root.transform, false);
        arrival.localPosition = new Vector3(0f, 0f, -1.6f);        // in front of the door
        arrival.localRotation = Quaternion.Euler(0f, 180f, 0f);    // facing away from it

        var portal = root.AddComponent<DoorPortal>();
        portal.arrival = arrival;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------ helpers

    static List<Vector2Int> Cells(params (int x, int z)[] c)
    {
        var l = new List<Vector2Int>();
        foreach (var p in c) l.Add(new Vector2Int(p.x, p.z));
        return l;
    }

    static Vector3 CellLocal(Vector2Int c) => new Vector3(c.x * T, 0f, c.y * T);

    static Vector2Int Step(Side s)
    {
        switch (s)
        {
            case Side.PlusZ: return new Vector2Int(0, 1);
            case Side.MinusZ: return new Vector2Int(0, -1);
            case Side.PlusX: return new Vector2Int(1, 0);
            default: return new Vector2Int(-1, 0);
        }
    }

    // Yaw that makes a door's front (-Z) face into the cell the door sits on the edge of.
    static float Yaw(Side s)
    {
        switch (s)
        {
            case Side.PlusZ: return 0f;
            case Side.MinusZ: return 180f;
            case Side.PlusX: return 90f;
            default: return 270f;
        }
    }

    static GameObject Place(GameObject prefab, Transform parent, Vector3 localPos, float yaw)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return go;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
