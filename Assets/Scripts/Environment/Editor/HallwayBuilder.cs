using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates MVP_Hallways.unity: three wide hallways that lead from the casino to three security rooms, built only from
/// the shared style kit. The pieces are NOT connected in the scene: each end has a <see cref="DoorPortal"/> that carries
/// the player to the matching door (casino door -> hallway start, hallway end -> security room, and back).
///
/// The hallways are 4 m wide and 3 m tall so NPCs can walk alongside the player. They use the casino look: the same red
/// carpet floor, Ashton's casino wall panel (cream wall, wood wainscot, brass rail and crown), framed pictures and wall
/// sconces, and chandeliers. The security rooms at the ends are cool (concrete, steel, glowing screens).
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
    const int Block = 2;                 // hallway "blocks" are 2x2 cells = 4 m wide, 4 m long

    enum Side { PlusZ, MinusZ, PlusX, MinusX }

    // A door sits on one side of two neighbouring cells (a, b) and spans both, so it is centred on the 4 m wall.
    struct DoorSpec
    {
        public Vector2Int a, b; public Side side; public string id; public string target;
        public DoorSpec(Vector2Int a, Vector2Int b, Side side, string id, string target)
        { this.a = a; this.b = b; this.side = side; this.id = id; this.target = target; }
    }

    enum Room { Office, Cameras, Vault }

    [MenuItem("High Stakes/Build Hallways Scene")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null) { Debug.LogError("[Hallways] Style kit not found. Run High Stakes > Build Style Kit first."); return; }
        if (kit.casinoWallPanel == null || kit.chandelier == null)
        {
            Debug.LogError("[Hallways] The casino furnishings are missing from the style kit. Run High Stakes > Build Casino Furnishings first.");
            return;
        }
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Rebuild hallways?", ScenePath + " exists and will be overwritten, including any hand edits.", "Overwrite", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabFolder);
        var door = PortalDoorPrefab(kit, true);
        var doorCool = PortalDoorPrefab(kit, false);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x3A, 0x30, 0x26, 0xFF); // same flat ambient as the casino (MVP_Template)
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        // Test-only stand-in for the casino; switches off with _SoloTest when MVP_Main loads the real casino.
        var solo = BuildSoloTest(kit);
        BuildCasinoStandIn(kit, door, solo.transform);

        var content = new GameObject("Hallways_Content").transform;

        // Hallway paths are lists of 4 m blocks, (x, z). One path each, no branches.
        BuildHallway(kit, door, doorCool, content, "A", 0, "Security Office", Room.Office,
            Blocks((0, 0), (0, 1), (0, 2), (0, 3), (0, 4)), Side.PlusZ);
        BuildHallway(kit, door, doorCool, content, "B", 1, "Camera Room", Room.Cameras,
            Blocks((0, 0), (0, 1), (0, 2), (1, 2), (2, 2)), Side.PlusX);
        BuildHallway(kit, door, doorCool, content, "C", 2, "Vault Antechamber", Room.Vault,
            Blocks((0, 0), (0, 1), (-1, 1), (-2, 1), (-2, 2), (-2, 3)), Side.PlusZ);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Hallways] Built " + ScenePath + ". Press Play: you start in the stand-in casino; walk up to a door (WASD in the simulator) to be carried into a hallway. Then run High Stakes > Validate Open Scene Against Style Kit.");
    }

    // ------------------------------------------------------------------ test rig and stand-in casino

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
        spawn.transform.position = new Vector3(5f, 0f, 0f); // middle of the stand-in casino, facing the doors (+Z)

        if (kit.playerRig != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig, solo.transform);
            rig.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }
        StyleKitBuilder.AddSimulator(solo.transform);
        return solo;
    }

    // A warm room with the three doors that, in the real game, are doors in Ashton's casino. They only use the ids
    // Casino_A/B/C, so the real casino just needs doors with the same ids (the Env_PortalDoor_Warm prefab).
    static void BuildCasinoStandIn(SharedStyleKit kit, GameObject door, Transform solo)
    {
        var cells = new List<Vector2Int>();
        for (int x = 0; x < 6; x++)
            for (int z = 0; z < 4; z++)
                cells.Add(new Vector2Int(x, z));
        var area = BuildArea(kit, door, door, solo, "CasinoStandIn (test only)", Vector3.zero, cells, new HashSet<Vector2Int>(cells),
            new[]
            {
                new DoorSpec(new Vector2Int(0, 3), new Vector2Int(1, 3), Side.PlusZ, "Casino_A", "HallA_Start"),
                new DoorSpec(new Vector2Int(2, 3), new Vector2Int(3, 3), Side.PlusZ, "Casino_B", "HallB_Start"),
                new DoorSpec(new Vector2Int(4, 3), new Vector2Int(5, 3), Side.PlusZ, "Casino_C", "HallC_Start"),
            });
        Place(kit.casinoTableSet, area, new Vector3(5f, 0f, 2f), 0);
        Place(kit.chandelier, area, new Vector3(5f, 2.38f, 2f), 0);
        Place(kit.lightWarmFloor, area, new Vector3(5f, 2.6f, 2f), 0);
    }

    // ------------------------------------------------------------------ hallways and rooms

    static void BuildHallway(SharedStyleKit kit, GameObject door, GameObject doorCool, Transform parent,
        string letter, int index, string roomName, Room roomKind, List<Vector2Int> blocks, Side endSide)
    {
        // Each hallway and its room sit on their own island so nothing is ever physically connected.
        var hallOrigin = new Vector3(100f, 0f, 40f * index);
        var roomOrigin = new Vector3(140f, 0f, 40f * index);

        // Expand each 4 m block into its 2x2 cells.
        var cells = new List<Vector2Int>();
        foreach (var b in blocks)
            for (int dx = 0; dx < Block; dx++)
                for (int dz = 0; dz < Block; dz++)
                    cells.Add(new Vector2Int(b.x * Block + dx, b.y * Block + dz));

        var first = blocks[0];
        var last = blocks[blocks.Count - 1];
        var start = EdgePair(first, Side.MinusZ);
        var end = EdgePair(last, endSide);

        // The whole hallway is casino-warm (all cells in the warm set).
        var hall = BuildArea(kit, door, door, parent, $"Hall{letter}", hallOrigin, cells, new HashSet<Vector2Int>(cells),
            new[]
            {
                new DoorSpec(start.a, start.b, Side.MinusZ, $"Hall{letter}_Start", $"Casino_{letter}"),
                new DoorSpec(end.a, end.b, endSide, $"Hall{letter}_End", $"Room{letter}_Door"),
            });

        // One chandelier and one baked warm light per 4 m block, like the casino floor.
        foreach (var b in blocks)
        {
            Vector3 centre = (CellLocal(new Vector2Int(b.x * Block, b.y * Block)) + CellLocal(new Vector2Int(b.x * Block + 1, b.y * Block + 1))) * 0.5f;
            Place(kit.chandelier, hall, centre + Vector3.up * 2.38f, 0);
            Place(kit.lightWarmFloor, hall, centre + Vector3.up * 2.6f, 0);
        }

        // Security room: 4x4 cells (8 x 8 m), cool, entered through its own door.
        var roomCells = new List<Vector2Int>();
        for (int x = 0; x < 4; x++)
            for (int z = 0; z < 4; z++)
                roomCells.Add(new Vector2Int(x, z));
        var room = BuildArea(kit, doorCool, doorCool, parent, $"Room{letter} ({roomName})", roomOrigin, roomCells, new HashSet<Vector2Int>(),
            new[] { new DoorSpec(new Vector2Int(1, 0), new Vector2Int(2, 0), Side.MinusZ, $"Room{letter}_Door", $"Hall{letter}_End") });
        Place(kit.lightCoolCorridor, room, new Vector3(2f, 2.7f, 2f), 0);
        Place(kit.lightCoolCorridor, room, new Vector3(4f, 2.7f, 4f), 0);
        Furnish(kit, room, roomKind);
    }

    // Builds floor, ceiling and walls for a set of grid cells. A wall goes on every side with no neighbouring cell; a
    // DoorSpec replaces the wall across its two cells with a (double-width) portal door. Cells in `warm` get the casino
    // look (carpet, panelled walls with pictures and sconces); the rest are the cool back-of-house look.
    static Transform BuildArea(SharedStyleKit kit, GameObject doorWarm, GameObject doorCool, Transform parent, string name,
        Vector3 origin, List<Vector2Int> cells, HashSet<Vector2Int> warm, DoorSpec[] doors)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.position = origin;
        var set = new HashSet<Vector2Int>(cells);
        int wallCount = 0;

        foreach (var c in cells)
        {
            bool w = warm.Contains(c);
            Vector3 centre = CellLocal(c);
            Place(w ? kit.floorTileWarm : kit.floorTileCool, g, centre, 0);
            Place(kit.ceilingTile, g, centre, 0);

            foreach (Side s in new[] { Side.PlusZ, Side.MinusZ, Side.PlusX, Side.MinusX })
            {
                bool covered = false;
                foreach (var d in doors)
                {
                    if (d.side != s || (d.a != c && d.b != c)) continue;
                    covered = true;
                    if (d.a != c) continue; // placed once, when processing cell a
                    Vector3 mid = (EdgeLocal(d.a, s) + EdgeLocal(d.b, s)) * 0.5f;
                    var instance = Place(w ? doorWarm : doorCool, g, mid, Yaw(s));
                    var portal = instance.GetComponent<DoorPortal>();
                    portal.portalId = d.id;
                    portal.targetId = d.target;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                    instance.name = "Door_" + d.id;
                }
                if (covered || set.Contains(c + Step(s))) continue;

                var wall = Place(w ? kit.casinoWallPanel : kit.wallCool, g, EdgeLocal(c, s), Yaw(s));
                if (w) DecorateWarmWall(kit, wall.transform, wallCount);
                else DecorateCoolWall(kit, wall.transform);
                wallCount++;
            }
        }
        return g;
    }

    // Wall "texture": the kit has no image textures (no new base materials allowed), so the walls get relief instead.
    // The casino wall panel already has the wood wainscot, brass chair rail and crown moulding; every wall segment also
    // gets either a framed picture or a brass wall sconce, alternating. Fronts face local -Z.
    static void DecorateWarmWall(SharedStyleKit kit, Transform wall, int n)
    {
        if (n % 2 == 0)
        {
            Detail(kit.brassGold, wall, "PictureFrame", PrimitiveType.Cube, new Vector3(0f, 1.95f, -0.10f), new Vector3(1.1f, 0.7f, 0.04f));
            Detail(kit.fabricBurgundy, wall, "PictureCanvas", PrimitiveType.Cube, new Vector3(0f, 1.95f, -0.108f), new Vector3(0.95f, 0.55f, 0.03f));
        }
        else
        {
            Detail(kit.brassGold, wall, "SconceBracket", PrimitiveType.Cube, new Vector3(0f, 2.0f, -0.11f), new Vector3(0.08f, 0.3f, 0.06f));
            Detail(kit.plasterCream, wall, "SconceLamp", PrimitiveType.Sphere, new Vector3(0f, 2.25f, -0.15f), new Vector3(0.2f, 0.28f, 0.2f));
        }
    }

    static void DecorateCoolWall(SharedStyleKit kit, Transform wall)
    {
        Detail(kit.steelCool, wall, "SteelBand", PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.09f), new Vector3(2f, 0.08f, 0.03f));
        Detail(kit.steelCool, wall, "SteelBandTop", PrimitiveType.Cube, new Vector3(0f, 2.6f, -0.09f), new Vector3(2f, 0.04f, 0.03f));
    }

    static void Furnish(SharedStyleKit kit, Transform room, Room kind)
    {
        // Room cells are (0..3, 0..3): the middle is (3, 3) and the +Z wall's inner face is at z = 6.925.
        switch (kind)
        {
            case Room.Office:
                Place(kit.tableRound, room, new Vector3(3f, 0, 3.2f), 0);
                Place(kit.chair, room, new Vector3(3f, 0, 2.2f), 0);
                Place(kit.chair, room, new Vector3(3f, 0, 4.2f), 180);
                for (int i = 0; i < 3; i++) Screen(kit, room, new Vector3(1.5f + i * 1.5f, 1.7f, 6.9f), new Vector3(1.3f, 0.75f, 0.03f));
                break;
            case Room.Cameras:
                Place(kit.chair, room, new Vector3(3f, 0, 4.4f), 180);
                for (int row = 0; row < 2; row++)
                    for (int i = 0; i < 5; i++)
                        Screen(kit, room, new Vector3(0.6f + i * 1.45f, 1.3f + row * 0.8f, 6.9f), new Vector3(1.3f, 0.7f, 0.03f));
                break;
            case Room.Vault:
                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "VaultDoor_Placeholder";
                door.transform.SetParent(room, false);
                door.transform.localPosition = new Vector3(3f, 1.3f, 6.85f);
                door.transform.localScale = new Vector3(2.2f, 2.6f, 0.1f);
                door.GetComponent<Renderer>().sharedMaterial = kit.steelCool;
                door.isStatic = true;
                // Reader faces -Z, toward the player. Slot B wires its keycard behaviour.
                Place(kit.doorReader, room, new Vector3(4.6f, 1.3f, 6.9f), 0);
                break;
        }
    }

    static void Screen(SharedStyleKit kit, Transform parent, Vector3 pos, Vector3 size)
        => Detail(kit.screenGlow, parent, "Screen", PrimitiveType.Cube, pos, size);

    // Visual-only piece (no collider) so it can never snag the player.
    static void Detail(Material m, Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
    }

    // ------------------------------------------------------------------ portal door prefab

    // A double-width (2 m opening) kit doorway + a closed door leaf (so the opening never shows the empty space behind it)
    // + a card reader for Slot B + the DoorPortal. The door's front (the side the player stands on) faces local -Z.
    static GameObject PortalDoorPrefab(SharedStyleKit kit, bool warm)
    {
        string name = warm ? "Env_PortalDoor_Warm" : "Env_PortalDoor_Cool";
        var root = new GameObject(name);

        var frame = (GameObject)PrefabUtility.InstantiatePrefab(warm ? kit.doorwayWarm : kit.doorwayCool, root.transform);
        frame.name = "Frame";
        frame.transform.localScale = new Vector3(2f, 1f, 1f); // 4 m wide, 2 m opening

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Leaf";
        leaf.transform.SetParent(root.transform, false);
        leaf.transform.localPosition = new Vector3(0f, StyleScale.DoorHeight / 2f, 0f);
        leaf.transform.localScale = new Vector3(StyleScale.DoorWidth * 2f, StyleScale.DoorHeight, 0.05f);
        leaf.GetComponent<Renderer>().sharedMaterial = warm ? kit.woodDark : kit.steelCool;
        leaf.isStatic = true;

        var reader = (GameObject)PrefabUtility.InstantiatePrefab(kit.doorReader, root.transform);
        reader.name = "Reader";
        reader.transform.localPosition = new Vector3(1.6f, 1.3f, -(StyleScale.WallThickness / 2f + 0.011f));

        var arrival = new GameObject("Arrival").transform;
        arrival.SetParent(root.transform, false);
        arrival.localPosition = new Vector3(0f, 0f, -1.8f);        // in front of the door
        arrival.localRotation = Quaternion.Euler(0f, 180f, 0f);    // facing away from it

        var portal = root.AddComponent<DoorPortal>();
        portal.arrival = arrival;
        portal.triggerWidth = StyleScale.DoorWidth * 2f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------ helpers

    static List<Vector2Int> Blocks(params (int x, int z)[] c)
    {
        var l = new List<Vector2Int>();
        foreach (var p in c) l.Add(new Vector2Int(p.x, p.z));
        return l;
    }

    // The two cells along one side of a 2x2 block.
    static (Vector2Int a, Vector2Int b) EdgePair(Vector2Int block, Side side)
    {
        int x0 = block.x * Block, z0 = block.y * Block;
        switch (side)
        {
            case Side.MinusZ: return (new Vector2Int(x0, z0), new Vector2Int(x0 + 1, z0));
            case Side.PlusZ: return (new Vector2Int(x0, z0 + 1), new Vector2Int(x0 + 1, z0 + 1));
            case Side.MinusX: return (new Vector2Int(x0, z0), new Vector2Int(x0, z0 + 1));
            default: return (new Vector2Int(x0 + 1, z0), new Vector2Int(x0 + 1, z0 + 1));
        }
    }

    static Vector3 CellLocal(Vector2Int c) => new Vector3(c.x * T, 0f, c.y * T);

    static Vector3 EdgeLocal(Vector2Int c, Side s)
    {
        var step = Step(s);
        return CellLocal(c) + new Vector3(step.x, 0f, step.y) * (T / 2f);
    }

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

    // Yaw that makes a wall's or door's front (-Z) face into the cell it sits on the edge of.
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
