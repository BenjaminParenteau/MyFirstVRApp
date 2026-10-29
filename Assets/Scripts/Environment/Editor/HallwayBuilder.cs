using System.Collections.Generic;
using HighStakes.Environment;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates the security area: the hub MVP_SecurityHall (entered from the casino's STAFF ONLY door) and three wings,
/// one scene each (MVP_SecurityOffice, MVP_CameraRoom, MVP_Vault): a wide hallway that leads from the hub to a security
/// room, built only from the shared style kit. Hallway and room sit on separate islands of the same scene, joined by a
/// <see cref="DoorPortal"/> pair (hallway end -> room door, and back). The hub's SECURITY / CAMERAS / VAULT doors lead to
/// each wing's start door, and back; SceneFlow loads the target scene in place of the current one.
///
/// The hallways are 4 m wide and 3 m tall so NPCs can walk alongside the player. They use the casino look: the same red
/// carpet floor, Ashton's casino wall panel (cream wall, wood wainscot, brass rail and crown), framed pictures and wall
/// sconces, and chandeliers. The security rooms at the ends are cool (concrete, steel, glowing screens), textured
/// afterwards by <see cref="SecurityTextureBuilder"/>.
///
/// Re-running overwrites the scenes, so after the first run edit them by hand.
/// </summary>
public static class HallwayBuilder
{
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string LightingPath = "Assets/Content/StyleKit/Lighting/StyleKit_Lighting.lighting";
    const string PrefabFolder = "Assets/Content/Environment/Prefabs";
    public const string CasinoScene = "MVP_Casino";
    public const string HubScene = "MVP_SecurityHall";
    public const string HubScenePath = "Assets/Scenes/MVP/" + HubScene + ".unity";
    /// <summary>portalId of the casino's staff door, which the hub's entrance leads back to.</summary>
    public const string CasinoStaffDoor = "Casino_Staff";
    static readonly string[] Letters = { "A", "B", "C" };
    static readonly string[] WingLabels = { "SECURITY", "CAMERAS", "VAULT" };

    const float T = StyleScale.TileSize; // 2 m grid cell
    const int Block = 2;                 // hallway "blocks" are 2x2 cells = 4 m wide, 4 m long

    enum Side { PlusZ, MinusZ, PlusX, MinusX }

    // A door sits on one side of two neighbouring cells (a, b) and spans both, so it is centred on the 4 m wall.
    struct DoorSpec
    {
        public Vector2Int a, b; public Side side; public string id; public string target; public string scene;
        public DoorSpec(Vector2Int a, Vector2Int b, Side side, string id, string target, string scene = null)
        { this.a = a; this.b = b; this.side = side; this.id = id; this.target = target; this.scene = scene; }
    }

    enum Room { Office, Cameras, Vault }

    // Hallway paths are lists of 4 m blocks, (x, z). One path each, no branches. All start at block (0, 0).
    // Room size is in 2 m cells: the security & count room and the camera room are 12 x 12 m; the vault stays 8 x 8 m
    // because Security/VaultDressing is laid out for that.
    static readonly (string letter, string roomName, Room kind, int cells, (int, int)[] path, Side endSide)[] Wings =
    {
        ("A", "Security Office", Room.Office, 6, new[] { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, Side.PlusZ),
        ("B", "Camera Room", Room.Cameras, 6, new[] { (0, 0), (0, 1), (0, 2), (1, 2), (2, 2) }, Side.PlusX),
        ("C", "Vault Antechamber", Room.Vault, 4, new[] { (0, 0), (0, 1), (-1, 1), (-2, 1), (-2, 2), (-2, 3) }, Side.PlusZ),
    };

    /// <summary>Scene paths of the three wings, in hub door A/B/C order (SECURITY, CAMERAS, VAULT).</summary>
    public static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MVP/MVP_SecurityOffice.unity",
        "Assets/Scenes/MVP/MVP_CameraRoom.unity",
        "Assets/Scenes/MVP/MVP_Vault.unity",
    };

    [MenuItem("High Stakes/Build Security Wing Scenes")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null) { Debug.LogError("[Hallways] Style kit not found. Run High Stakes > Build Style Kit first."); return; }
        if (kit.casinoWallPanel == null || kit.chandelier == null)
        {
            Debug.LogError("[Hallways] The casino furnishings are missing from the style kit. Pull the latest main (they are committed prefabs) and do not regenerate them: that rewrites the casino prefabs.");
            return;
        }
        if (!Application.isBatchMode)
        {
            if (!EditorUtility.DisplayDialog("Rebuild security wings?", "The three wing scenes will be overwritten, including any hand edits.", "Overwrite", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }

        EnsureFolder(PrefabFolder);
        var door = PortalDoorPrefab(kit, true);
        var doorCool = PortalDoorPrefab(kit, false);

        for (int i = 0; i < ScenePaths.Length; i++) BuildWing(kit, door, doorCool, i);
        BuildHub(kit, door);
        // Cinder block, concrete, tread plate and drop ceiling in the rooms (skipped with a warning if the textures have
        // not been generated yet).
        SecurityTextureBuilder.BuildAndApplyToWings();

        AssetDatabase.SaveAssets();
        Debug.Log("[Hallways] Built the security hub and wings. Open one and press Play to walk it alone, or play MVP_Main and walk through the casino's STAFF ONLY door.");
    }

    /// <summary>
    /// Regenerates only the given wings (indices into <see cref="ScenePaths"/>) and re-textures them, leaving the hub,
    /// the other wings and their baked lighting alone. The regenerated wings come back empty and unbaked: furnish them
    /// with BackOfHouseBuilder and have them baked again.
    /// </summary>
    public static bool RebuildWings(params int[] wings)
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null || kit.casinoWallPanel == null) { Debug.LogError("[Hallways] Style kit or casino furnishings missing."); return false; }
        EnsureFolder(PrefabFolder);
        var door = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Env_PortalDoor_Warm.prefab");
        var doorCool = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Env_PortalDoor_Cool.prefab");
        if (door == null) door = PortalDoorPrefab(kit, true);
        if (doorCool == null) doorCool = PortalDoorPrefab(kit, false);
        foreach (int i in wings) BuildWing(kit, door, doorCool, i);
        SecurityTextureBuilder.BuildAndApplyToWings();
        return true;
    }

    /// <summary>
    /// Builds only the hub scene and points the existing wings' start doors at it, without rebuilding the wings, so
    /// their baked lighting is kept. Returns false if the style kit is missing.
    /// </summary>
    public static bool BuildHubAndRelinkWings()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null || kit.casinoWallPanel == null) { Debug.LogError("[Hallways] Style kit or casino furnishings missing."); return false; }
        EnsureFolder(PrefabFolder);
        var door = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Env_PortalDoor_Warm.prefab");
        BuildHub(kit, door != null ? door : PortalDoorPrefab(kit, true));

        for (int i = 0; i < ScenePaths.Length; i++)
        {
            var scene = EditorSceneManager.OpenScene(ScenePaths[i], OpenSceneMode.Single);
            bool found = false;
            foreach (var portal in Object.FindObjectsByType<DoorPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (portal.portalId != $"Hall{Letters[i]}_Start") continue;
                portal.targetId = $"Hub_{Letters[i]}";
                portal.targetScene = HubScene;
                PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                found = true;
            }
            if (!found) Debug.LogWarning($"[Hallways] No door Hall{Letters[i]}_Start in {ScenePaths[i]}.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return true;
    }

    // The hub: a straight warm hallway, 4 x 12 m. Entrance at the near end (back to the casino), SECURITY on the left,
    // VAULT on the right, CAMERAS at the far end. Not baked here (see the Progress open question): until it is, only the
    // flat ambient lights it.
    static void BuildHub(SharedStyleKit kit, GameObject door)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x3A, 0x30, 0x26, 0xFF);
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        BuildSoloTest(kit, new Vector3(1f, 0f, 1.2f));
        var content = new GameObject("Hub_Content").transform;

        var blocks = Blocks((0, 0), (0, 1), (0, 2));
        var cells = new List<Vector2Int>();
        foreach (var bl in blocks)
            for (int dx = 0; dx < Block; dx++)
                for (int dz = 0; dz < Block; dz++)
                    cells.Add(new Vector2Int(bl.x * Block + dx, bl.y * Block + dz));

        var entrance = EdgePair(blocks[0], Side.MinusZ);
        var left = EdgePair(blocks[1], Side.MinusX);
        var far = EdgePair(blocks[2], Side.PlusZ);
        var right = EdgePair(blocks[1], Side.PlusX);
        var hub = BuildArea(kit, door, door, content, "SecurityHall", Vector3.zero, cells, new HashSet<Vector2Int>(cells),
            new[]
            {
                new DoorSpec(entrance.a, entrance.b, Side.MinusZ, "Hub_Start", CasinoStaffDoor, CasinoScene),
                new DoorSpec(left.a, left.b, Side.MinusX, "Hub_A", "HallA_Start", SceneName(ScenePaths[0])),
                new DoorSpec(far.a, far.b, Side.PlusZ, "Hub_B", "HallB_Start", SceneName(ScenePaths[1])),
                new DoorSpec(right.a, right.b, Side.PlusX, "Hub_C", "HallC_Start", SceneName(ScenePaths[2])),
            });

        // Signs over the doors, on the wall above the 2.1 m door leaf.
        var signPos = new Vector3(0f, 2.5f, -0.1f);
        Sign(kit, hub.Find("Door_Hub_Start"), "CASINO", signPos, 1.4f);
        for (int i = 0; i < Letters.Length; i++)
            Sign(kit, hub.Find($"Door_Hub_{Letters[i]}"), WingLabels[i], signPos, 1.4f);

        foreach (var bl in blocks)
        {
            Vector3 centre = (CellLocal(new Vector2Int(bl.x * Block, bl.y * Block)) + CellLocal(new Vector2Int(bl.x * Block + 1, bl.y * Block + 1))) * 0.5f;
            Place(kit.chandelier, hub, centre + Vector3.up * 2.38f, 0);
            Place(kit.lightWarmFloor, hub, centre + Vector3.up * 2.6f, 0);
        }

        EditorSceneManager.SaveScene(scene, HubScenePath);
    }

    static string SceneName(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

    /// <summary>A black plate with brass text on the front (local -Z) of <paramref name="parent"/>. Kit materials only.</summary>
    public static void Sign(SharedStyleKit kit, Transform parent, string label, Vector3 localPos, float width)
    {
        if (parent == null) { Debug.LogWarning("[Hallways] No door to put the '" + label + "' sign on."); return; }
        Detail(kit.chipBlack, parent, "SignPlate", PrimitiveType.Cube, localPos, new Vector3(width, 0.22f, 0.03f));

        var sign = new GameObject("SignText").AddComponent<TextMeshPro>();
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = localPos + new Vector3(0f, 0f, -0.02f);
        sign.rectTransform.sizeDelta = new Vector2(width - 0.05f, 0.18f);
        sign.text = label;
        sign.fontStyle = FontStyles.Bold;
        sign.alignment = TextAlignmentOptions.Center;
        sign.enableAutoSizing = true;
        sign.fontSizeMin = 0.1f;
        sign.fontSizeMax = 2f;
        sign.color = kit.brassGold.color;
    }

    static void BuildWing(SharedStyleKit kit, GameObject door, GameObject doorCool, int index)
    {
        var (letter, roomName, roomKind, roomSize, path, endSide) = Wings[index];
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x3A, 0x30, 0x26, 0xFF); // same flat ambient as the casino (MVP_Template)
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        // Solo spawn: just past the start door's arrival point, at the hallway's first block facing in (+Z).
        BuildSoloTest(kit, new Vector3(1f, 0f, 1.2f));
        var content = new GameObject($"Wing{letter}_Content").transform;
        BuildHallway(kit, door, doorCool, content, letter, roomName, roomKind, roomSize, Blocks(path), endSide);

        EditorSceneManager.SaveScene(scene, ScenePaths[index]);
    }

    // ------------------------------------------------------------------ test rig

    static GameObject BuildSoloTest(SharedStyleKit kit, Vector3 spawn)
    {
        var solo = new GameObject("_SoloTest");
        solo.AddComponent<SoloTestRoot>();

        var volGo = new GameObject("Volume_Global");
        volGo.transform.SetParent(solo.transform);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = kit.volumeProfile;

        if (kit.playerRig != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig, solo.transform);
            rig.transform.position = spawn;
        }
        StyleKitBuilder.AddSimulator(solo.transform);
        return solo;
    }

    // ------------------------------------------------------------------ hallways and rooms

    static void BuildHallway(SharedStyleKit kit, GameObject door, GameObject doorCool, Transform parent,
        string letter, string roomName, Room roomKind, int roomSize, List<Vector2Int> blocks, Side endSide)
    {
        // The hallway and its room sit on their own islands so nothing is ever physically connected.
        var hallOrigin = Vector3.zero;
        var roomOrigin = new Vector3(40f, 0f, 0f);

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
                new DoorSpec(start.a, start.b, Side.MinusZ, $"Hall{letter}_Start", $"Hub_{letter}", HubScene),
                new DoorSpec(end.a, end.b, endSide, $"Hall{letter}_End", $"Room{letter}_Door"),
            });

        // One chandelier and one baked warm light per 4 m block, like the casino floor.
        foreach (var b in blocks)
        {
            Vector3 centre = (CellLocal(new Vector2Int(b.x * Block, b.y * Block)) + CellLocal(new Vector2Int(b.x * Block + 1, b.y * Block + 1))) * 0.5f;
            Place(kit.chandelier, hall, centre + Vector3.up * 2.38f, 0);
            Place(kit.lightWarmFloor, hall, centre + Vector3.up * 2.6f, 0);
        }

        // Security room: roomSize x roomSize cells, cool, entered through its own door in the middle of the near (-Z) wall.
        var roomCells = new List<Vector2Int>();
        for (int x = 0; x < roomSize; x++)
            for (int z = 0; z < roomSize; z++)
                roomCells.Add(new Vector2Int(x, z));
        int doorCell = roomSize / 2 - 1;
        var room = BuildArea(kit, doorCool, doorCool, parent, $"Room{letter} ({roomName})", roomOrigin, roomCells, new HashSet<Vector2Int>(),
            new[] { new DoorSpec(new Vector2Int(doorCell, 0), new Vector2Int(doorCell + 1, 0), Side.MinusZ, $"Room{letter}_Door", $"Hall{letter}_End") });
        // Baked cool lights: two on the diagonal of an 8 m room, four in the corners' quarter-points of a bigger one.
        float mid = (roomSize - 1) * T / 2f, spread = roomSize > 4 ? 3f : 1f;
        Place(kit.lightCoolCorridor, room, new Vector3(mid - spread, 2.7f, mid - spread), 0);
        Place(kit.lightCoolCorridor, room, new Vector3(mid + spread, 2.7f, mid + spread), 0);
        if (roomSize > 4)
        {
            Place(kit.lightCoolCorridor, room, new Vector3(mid - spread, 2.7f, mid + spread), 0);
            Place(kit.lightCoolCorridor, room, new Vector3(mid + spread, 2.7f, mid - spread), 0);
        }
        Furnish(kit, room, roomKind, mid);
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
                    portal.targetScene = d.scene;
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

    // Placeholder furniture, replaced by the furnishing builders (Security/Editor/SecurityOfficeBuilder, CountRoomBuilder).
    static void Furnish(SharedStyleKit kit, Transform room, Room kind, float mid)
    {
        // Cells are 2 m centred on even numbers from 0, so the middle is (mid, mid) and the +Z wall's inner face is at
        // z = 2 * mid + 0.925 (6.925 for an 8 m room). Offsets below are from the middle of the back wall.
        var back = new Vector3(mid, 0f, 2f * mid + 0.9f);
        switch (kind)
        {
            case Room.Office:
                Place(kit.tableRound, room, new Vector3(mid, 0, mid + 0.2f), 0);
                Place(kit.chair, room, new Vector3(mid, 0, mid - 0.8f), 0);
                Place(kit.chair, room, new Vector3(mid, 0, mid + 1.2f), 180);
                for (int i = 0; i < 3; i++) Screen(kit, room, back + new Vector3(-1.5f + i * 1.5f, 1.7f, 0f), new Vector3(1.3f, 0.75f, 0.03f));
                break;
            case Room.Cameras:
                Place(kit.chair, room, new Vector3(mid, 0, mid + 1.4f), 180);
                for (int row = 0; row < 2; row++)
                    for (int i = 0; i < 5; i++)
                        Screen(kit, room, back + new Vector3(-2.4f + i * 1.45f, 1.3f + row * 0.8f, 0f), new Vector3(1.3f, 0.7f, 0.03f));
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
