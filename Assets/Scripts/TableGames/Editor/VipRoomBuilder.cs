using System.Collections.Generic;
using HighStakes.Economy;
using HighStakes.Environment;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates MVP_VIP.unity: a 4 m wide hallway that opens through a roped-off "VIP LOUNGE" doorway into a 12 x 12 m
/// VIP room (a playable blackjack table, a high-stakes table set, a bar, slot machines), built only from the shared
/// style kit in the casino look.
///
/// The hallway starts at a portal door with id VIP_HallStart that leads to the casino door Casino_VIP in MVP_Casino
/// (SceneFlow swaps the scenes; same scheme as HallwayBuilder). Played on its own, you spawn at the
/// start of the hallway facing the lounge, with a 1,000-chip test wallet under _SoloTest so the blackjack table plays.
///
/// Slot A owns this. Re-running overwrites the scene, so after the first run edit the scene by hand.
/// </summary>
public static class VipRoomBuilder
{
    const string ScenePath = "Assets/Scenes/MVP/MVP_VIP.unity";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string LightingPath = "Assets/Content/StyleKit/Lighting/StyleKit_Lighting.lighting";
    const string PortalDoorPath = "Assets/Content/Environment/Prefabs/Env_PortalDoor_Warm.prefab";
    const float T = StyleScale.TileSize; // 2 m grid cell

    [MenuItem("High Stakes/Tables/Build VIP Room Scene")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        var portalDoor = AssetDatabase.LoadAssetAtPath<GameObject>(PortalDoorPath);
        if (kit == null || kit.casinoWallPanel == null || portalDoor == null)
        {
            Debug.LogError("[VIPRoom] Style kit, casino furnishings or the portal door prefab are missing. Pull the latest dev.");
            return;
        }
        if (!Application.isBatchMode)
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Rebuild VIP room?", ScenePath + " exists and will be overwritten, including any hand edits.", "Overwrite", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Built in the fresh scene (its scene copy is removed again) so the user's scene is never touched.
        var blackjack = AssetDatabase.LoadAssetAtPath<GameObject>(BlackjackTableBuilder.PrefabPath);
        if (blackjack == null) blackjack = BlackjackTableBuilder.BuildPrefab();
        if (blackjack == null) return;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x3A, 0x30, 0x26, 0xFF); // same flat ambient as the casino
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        BuildSoloTest(kit, new Vector3(1f, 0f, 1f));

        var content = new GameObject("VIP_Content").transform;

        // Hallway: cells x 0..1, z 0..4 (4 m wide, 10 m long). Room: cells x -2..3, z 5..10 (12 x 12 m).
        // The two touch, so the hall opens straight into the room; the doorway frame marks the entrance.
        var cells = new List<Vector2Int>();
        for (int x = 0; x <= 1; x++) for (int z = 0; z <= 4; z++) cells.Add(new Vector2Int(x, z));
        for (int x = -2; x <= 3; x++) for (int z = 5; z <= 10; z++) cells.Add(new Vector2Int(x, z));
        var area = BuildShell(kit, content, cells, skipEdges: new[] { (new Vector2Int(0, 0), Vector2Int.down), (new Vector2Int(1, 0), Vector2Int.down) });

        // Back door to the casino (closed leaf, portal).
        var door = Place(portalDoor, area, new Vector3(1f, 0f, -1f), 180f);
        door.name = "Door_VIP_HallStart";
        var portal = door.GetComponent<DoorPortal>();
        portal.portalId = "VIP_HallStart";
        portal.targetId = "Casino_VIP";
        portal.targetScene = HallwayBuilder.CasinoScene;
        PrefabUtility.RecordPrefabInstancePropertyModifications(portal);

        BuildEntrance(kit, area);

        // Hallway lights, one per 4 m like the casino floor.
        foreach (float z in new[] { 1f, 5f })
        {
            Place(kit.chandelier, area, new Vector3(1f, 2.38f, z), 0);
            Place(kit.lightWarmFloor, area, new Vector3(1f, 2.6f, z), 0);
        }

        // Room: the blackjack table and a high-stakes table set, bar on the back wall, slot machines along the sides.
        Place(blackjack, area, new Vector3(-1.5f, 0f, 15f), 0).name = "Tables_Blackjack"; // player side faces the hallway
        Place(kit.casinoTableSet, area, new Vector3(3.5f, 0f, 15f), 0);
        Place(kit.barCounter, area, new Vector3(1f, 0f, 19.8f), 0);
        foreach (float x in new[] { -0.5f, 1f, 2.5f }) Place(kit.barStool, area, new Vector3(x, 0f, 18.8f), 0);
        foreach (float z in new[] { 12f, 18f })
        {
            Place(kit.slotMachine, area, new Vector3(-4.5f, 0f, z), 0);   // screen faces +X
            Place(kit.slotMachine, area, new Vector3(6.5f, 0f, z), 180f); // screen faces -X
        }
        foreach (var p in new[] { new Vector3(-1.5f, 0, 15f), new Vector3(3.5f, 0, 15f), new Vector3(1f, 0, 11f), new Vector3(1f, 0, 19f) })
        {
            Place(kit.chandelier, area, p + Vector3.up * 2.38f, 0);
            Place(kit.lightWarmFloor, area, p + Vector3.up * 2.6f, 0);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[VIPRoom] Built " + ScenePath + ". Press Play and walk forward (WASD in the simulator) into the lounge.");
    }

    // Batch entry point: rebuild the blackjack prefab, build, then bake the lightmaps (CPU, per the kit's lighting settings).
    public static void BuildAndBake()
    {
        BlackjackTableBuilder.BuildPrefab();
        Build();
        Lightmapping.Bake();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[VIPRoom] Baked lighting.");
    }

    static void BuildSoloTest(SharedStyleKit kit, Vector3 spawn)
    {
        var solo = new GameObject("_SoloTest");
        solo.AddComponent<SoloTestRoot>();
        var vol = new GameObject("Volume_Global").AddComponent<Volume>();
        vol.transform.SetParent(solo.transform);
        vol.isGlobal = true;
        vol.sharedProfile = kit.volumeProfile;
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig, solo.transform);
        rig.transform.position = spawn; // faces +Z, down the hallway
        StyleKitBuilder.AddSimulator(solo.transform);

        // Stand-in for MVP_Main's wallet; under _SoloTest, so it switches off there and the real one is the only one.
        var wallet = new GameObject("SoloTest_ChipWallet").AddComponent<ChipWallet>();
        wallet.transform.SetParent(solo.transform);
        var so = new SerializedObject(wallet);
        so.FindProperty("startingBalance").intValue = 1000;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ponytail: same floor/ceiling/wall-per-open-edge idea as HallwayBuilder.BuildArea (private there), warm look only.
    static Transform BuildShell(SharedStyleKit kit, Transform parent, List<Vector2Int> cells, (Vector2Int cell, Vector2Int side)[] skipEdges)
    {
        var g = new GameObject("VIP_Shell").transform;
        g.SetParent(parent, false);
        var set = new HashSet<Vector2Int>(cells);
        var skip = new HashSet<(Vector2Int, Vector2Int)>(skipEdges);
        int n = 0;
        foreach (var c in cells)
        {
            var centre = new Vector3(c.x * T, 0f, c.y * T);
            Place(kit.floorTileWarm, g, centre, 0);
            Place(kit.ceilingTile, g, centre, 0);
            foreach (var s in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left })
            {
                if (set.Contains(c + s) || skip.Contains((c, s))) continue;
                float yaw = s == Vector2Int.up ? 0f : s == Vector2Int.down ? 180f : s == Vector2Int.right ? 90f : 270f;
                var wall = Place(kit.casinoWallPanel, g, centre + new Vector3(s.x, 0f, s.y) * (T / 2f), yaw).transform;
                // Walls alternate a framed picture and a brass sconce (fronts face local -Z), like the hallways.
                if (n++ % 2 == 0)
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
        }
        return g;
    }

    // Doorway frame where the hall meets the room, a "VIP LOUNGE" sign over it, and a velvet-rope lane leading in.
    static void BuildEntrance(SharedStyleKit kit, Transform parent)
    {
        var frame = Place(kit.doorwayWarm, parent, new Vector3(1f, 0f, 9f), 0);
        frame.name = "VIP_Entrance";
        frame.transform.localScale = new Vector3(2f, 1f, 1f); // 4 m wide, 2 m opening, like the portal doors

        float face = 9f - StyleScale.WallThickness / 2f; // hall-side face of the header
        Detail(kit.chipBlack, parent, "SignPlate", PrimitiveType.Cube, new Vector3(1f, 2.55f, face - 0.02f), new Vector3(1.9f, 0.6f, 0.03f));
        var sign = new GameObject("SignText").AddComponent<TextMeshPro>();
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = new Vector3(1f, 2.55f, face - 0.04f);
        sign.rectTransform.sizeDelta = new Vector2(1.8f, 0.5f);
        sign.text = "VIP LOUNGE";
        sign.fontStyle = FontStyles.Bold;
        sign.alignment = TextAlignmentOptions.Center;
        sign.enableAutoSizing = true;
        sign.fontSizeMin = 0.1f;
        sign.fontSizeMax = 4f;
        sign.color = kit.brassGold.color;
        sign.gameObject.isStatic = true;

        // Brass posts with burgundy ropes on both sides of a 2 m lane in front of the entrance.
        foreach (float x in new[] { -0.1f, 2.1f })
        {
            foreach (float z in new[] { 6.4f, 8.4f })
            {
                Detail(kit.brassGold, parent, "RopePost", PrimitiveType.Cylinder, new Vector3(x, 0.47f, z), new Vector3(0.07f, 0.47f, 0.07f));
                Detail(kit.brassGold, parent, "RopePostTop", PrimitiveType.Sphere, new Vector3(x, 0.96f, z), new Vector3(0.1f, 0.1f, 0.1f));
            }
            var rope = Detail(kit.fabricBurgundy, parent, "VelvetRope", PrimitiveType.Cylinder, new Vector3(x, 0.85f, 7.4f), new Vector3(0.04f, 1f, 0.04f));
            rope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    // Visual-only piece (no collider) so it can never snag the player.
    static GameObject Detail(Material m, Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static GameObject Place(GameObject prefab, Transform parent, Vector3 localPos, float yaw)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return go;
    }
}
