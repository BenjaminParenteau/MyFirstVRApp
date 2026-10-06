using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// Generates the shared style kit (materials, lighting, greybox prefabs, template scene) from the
/// standards in docs/StyleGuide.md. Re-running is safe: existing assets are updated in place.
/// Slot D (Zach) owns this; everyone else only consumes the output.
/// </summary>
public static class StyleKitBuilder
{
    const string Root = "Assets/Content/StyleKit";
    const string TemplateScenePath = "Assets/Scenes/MVP/MVP_Template.unity";
    // The plain XRI starter rig (as in vrstake): the VR Template variant adds a tunneling vignette on every move and turn
    // and floating controller callouts.
    const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    [MenuItem("High Stakes/Build Style Kit")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // the template build replaces the open scene
        foreach (var f in new[] { "Materials", "Prefabs", "Lighting" })
            EnsureFolder($"{Root}/{f}");
        EnsureFolder("Assets/Scenes/MVP");

        var kit = LoadOrCreate<SharedStyleKit>($"{Root}/SharedStyleKit.asset");

        BuildMaterials(kit);
        BuildLighting(kit);
        BuildPrefabs(kit);
        CasinoStyleKitBuilder.Build(kit);
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();

        BuildTemplateScene(kit);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StyleKit] Built. Kit asset: " + Root + "/SharedStyleKit.asset, template scene: " + TemplateScenePath);
    }

    // ---------------------------------------------------------------- materials

    static void BuildMaterials(SharedStyleKit kit)
    {
        // Warm palette (main floor / VIP)
        kit.carpetRed = Mat("CarpetRed", "#5A1420", 0.10f);
        kit.feltGreen = Mat("FeltGreen", "#0F5A3A", 0.05f);
        kit.woodDark = Mat("WoodDark", "#3B2416", 0.35f);
        kit.brassGold = Mat("BrassGold", "#B8893A", 0.65f);
        kit.plasterCream = Mat("PlasterCream", "#CDBB98", 0.10f);
        kit.fabricBurgundy = Mat("FabricBurgundy", "#5A1A2B", 0.05f);
        // Cool palette (back-of-house / vault)
        kit.concreteCool = Mat("ConcreteCool", "#6E7780", 0.10f);
        kit.steelCool = Mat("SteelCool", "#8FA3B0", 0.70f);
        kit.tileCool = Mat("TileCool", "#4E5A63", 0.35f);
        kit.screenGlow = Mat("ScreenGlow", "#38D6E8", 0.50f, emissive: true);
        // Props
        kit.chipWhite = Mat("ChipWhite", "#E8E4D8", 0.30f);
        kit.chipRed = Mat("ChipRed", "#B3202A", 0.30f);
        kit.chipGreen = Mat("ChipGreen", "#1E8A4C", 0.30f);
        kit.chipBlack = Mat("ChipBlack", "#1A1A1A", 0.30f);
        kit.keycardPlastic = Mat("KeycardPlastic", "#E9EDF0", 0.40f);
    }

    static Material Mat(string name, string hex, float smoothness, bool emissive = false)
    {
        string path = $"{Root}/Materials/M_{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Simple Lit is the cheapest URP lit shader and is what we standardise on for Quest 3.
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        var color = Hex(hex);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_SpecColor", new Color(0.2f, 0.2f, 0.2f, 1f));
        mat.SetFloat("_Smoothness", smoothness);
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------------------------------------------------------------- lighting

    static void BuildLighting(SharedStyleKit kit)
    {
        // Shared post-processing profile: tonemapping and a touch of colour grading only (bloom etc. are too costly on Quest).
        string vpPath = $"{Root}/Lighting/StyleKit_Volume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(vpPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, vpPath);
        }
        if (!profile.TryGet<Tonemapping>(out var tone))
            tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);
        if (!profile.TryGet<ColorAdjustments>(out var grade))
            grade = profile.Add<ColorAdjustments>(true);
        grade.postExposure.Override(0f);
        grade.saturation.Override(5f);
        foreach (var c in profile.components)
            if (c != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(c)))
                AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        kit.volumeProfile = profile;

        // Light presets. Baked only; no realtime shadows.
        kit.lightWarmFloor = LightPrefab("Light_WarmFloor", "#FFC88A", 2.0f, 9f);
        kit.lightCoolCorridor = LightPrefab("Light_CoolCorridor", "#8FB8FF", 1.5f, 6f);
    }

    static GameObject LightPrefab(string name, string hex, float intensity, float range)
    {
        var go = new GameObject(name);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Hex(hex);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        l.lightmapBakeType = LightmapBakeType.Baked;
        return SavePrefab(go, name);
    }

    static LightingSettings BuildLightingSettings()
    {
        string path = $"{Root}/Lighting/StyleKit_Lighting.lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
        if (ls == null)
        {
            ls = new LightingSettings();
            AssetDatabase.CreateAsset(ls, path);
        }
        ls.bakedGI = true;
        ls.realtimeGI = false;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        ls.lightmapResolution = 20;
        ls.lightmapMaxSize = 1024;
        ls.directSampleCount = 32;
        ls.indirectSampleCount = 128;
        EditorUtility.SetDirty(ls);
        return ls;
    }

    // ---------------------------------------------------------------- prefabs

    static void BuildPrefabs(SharedStyleKit kit)
    {
        kit.floorTileWarm = FloorTile("Kit_FloorTile_Warm", kit.carpetRed);
        kit.floorTileCool = FloorTile("Kit_FloorTile_Cool", kit.tileCool);
        kit.wallWarm = Wall("Kit_Wall_Warm", kit.plasterCream);
        kit.wallCool = Wall("Kit_Wall_Cool", kit.concreteCool);
        kit.doorwayWarm = Doorway("Kit_Doorway_Warm", kit.plasterCream);
        kit.doorwayCool = Doorway("Kit_Doorway_Cool", kit.concreteCool);
        kit.ceilingTile = CeilingTile("Kit_CeilingTile", kit.plasterCream);
        kit.tableRound = Table("Kit_Table_Round", kit);
        kit.chair = Chair("Kit_Chair", kit);
        kit.chip = Chip("Kit_Chip", kit.chipRed);
        kit.keycard = Keycard("Kit_Keycard", kit.keycardPlastic);
        kit.doorReader = DoorReader("Kit_DoorReader", kit);
        kit.scaleReference = ScaleReference("Kit_ScaleReference", kit);
        kit.playerRig = PlayerRig("Kit_PlayerRig");
    }

    /// <summary>
    /// Rebuilds only Kit_PlayerRig from the starter rig, keeping its GUID so every scene keeps its reference. Instance
    /// overrides made against the old base (e.g. a scene rig's position) are dropped; re-place those rigs.
    /// </summary>
    [MenuItem("High Stakes/Rebuild Player Rig (XRI starter rig)")]
    public static void RebuildPlayerRig()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>($"{Root}/SharedStyleKit.asset");
        var rig = PlayerRig("Kit_PlayerRig", fresh: true);
        if (kit != null && rig != null)
        {
            kit.playerRig = rig;
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("[StyleKit] Rebuilt Kit_PlayerRig on the XRI starter rig.");
    }

    // The one player rig for the whole game: a Prefab Variant of the XRI starter rig carrying our settings, so the
    // vendored prefab stays untouched and a change here reaches every scene at once.
    static GameObject PlayerRig(string name, bool fresh = false)
    {
        string path = $"{Root}/Prefabs/{name}.prefab";
        GameObject contents;
        bool exists = !fresh && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        if (exists)
        {
            contents = PrefabUtility.LoadPrefabContents(path);
        }
        else
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (basePrefab == null)
            {
                Debug.LogWarning("[StyleKit] XR rig prefab not found at " + RigPrefabPath + "; template will have no rig.");
                return null;
            }
            contents = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            contents.name = name;
        }

        // The template rig steps up 0.5 m, enough to walk onto a 0.45 m chair seat. 0.3 m still allows real steps.
        var body = contents.GetComponentInChildren<CharacterController>(true);
        if (body != null) body.stepOffset = StyleScale.MaxStepHeight;

        // Move speed stays at the starter rig's default, which is what vrstake plays with.

        // No skybox (StyleGuide §5): clear to black instead of Unity's default blue, so gaps in a room read as darkness.
        var cam = contents.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
        if (exists) PrefabUtility.UnloadPrefabContents(contents);
        else Object.DestroyImmediate(contents);
        return prefab;
    }

    static GameObject FloorTile(string name, Material m)
    {
        var root = new GameObject(name);
        Prim(PrimitiveType.Cube, "Mesh", root, new Vector3(0, -0.05f, 0), new Vector3(StyleScale.TileSize, 0.1f, StyleScale.TileSize), m);
        root.isStatic = true;
        return SavePrefab(root, name);
    }

    static GameObject Wall(string name, Material m)
    {
        var root = new GameObject(name);
        float h = StyleScale.CeilingHeight;
        Prim(PrimitiveType.Cube, "Mesh", root, new Vector3(0, h / 2f, 0), new Vector3(StyleScale.TileSize, h, StyleScale.WallThickness), m);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject Doorway(string name, Material m)
    {
        var root = new GameObject(name);
        float w = StyleScale.TileSize, t = StyleScale.WallThickness, dh = StyleScale.DoorHeight, h = StyleScale.CeilingHeight;
        float post = (w - StyleScale.DoorWidth) / 2f;
        Prim(PrimitiveType.Cube, "PostLeft", root, new Vector3(-(w - post) / 2f, h / 2f, 0), new Vector3(post, h, t), m);
        Prim(PrimitiveType.Cube, "PostRight", root, new Vector3((w - post) / 2f, h / 2f, 0), new Vector3(post, h, t), m);
        Prim(PrimitiveType.Cube, "Header", root, new Vector3(0, dh + (h - dh) / 2f, 0), new Vector3(StyleScale.DoorWidth, h - dh, t), m);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject CeilingTile(string name, Material m)
    {
        var root = new GameObject(name);
        Prim(PrimitiveType.Cube, "Mesh", root, new Vector3(0, StyleScale.CeilingHeight + 0.05f, 0), new Vector3(StyleScale.TileSize, 0.1f, StyleScale.TileSize), m, collider: false);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject Table(string name, SharedStyleKit kit)
    {
        var root = new GameObject(name);
        float top = StyleScale.TableTopHeight;
        Prim(PrimitiveType.Cylinder, "Top", root, new Vector3(0, top - 0.03f, 0), new Vector3(1.8f, 0.03f, 1.8f), kit.woodDark);
        Prim(PrimitiveType.Cylinder, "Felt", root, new Vector3(0, top + 0.0025f, 0), new Vector3(1.6f, 0.0025f, 1.6f), kit.feltGreen, collider: false);
        Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, (top - 0.06f) / 2f, 0), new Vector3(0.25f, (top - 0.06f) / 2f, 0.25f), kit.woodDark);
        Prim(PrimitiveType.Cylinder, "Base", root, new Vector3(0, 0.01f, 0), new Vector3(0.7f, 0.01f, 0.7f), kit.brassGold);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject Chair(string name, SharedStyleKit kit)
    {
        var root = new GameObject(name);
        float seat = StyleScale.SeatHeight;
        Prim(PrimitiveType.Cube, "Seat", root, new Vector3(0, seat, 0), new Vector3(0.45f, 0.05f, 0.45f), kit.fabricBurgundy);
        Prim(PrimitiveType.Cube, "Back", root, new Vector3(0, seat + 0.25f, -0.2f), new Vector3(0.45f, 0.45f, 0.05f), kit.fabricBurgundy);
        Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0, (seat - 0.025f) / 2f, 0), new Vector3(0.06f, (seat - 0.025f) / 2f, 0.06f), kit.steelCool);
        Prim(PrimitiveType.Cylinder, "Base", root, new Vector3(0, 0.01f, 0), new Vector3(0.4f, 0.01f, 0.4f), kit.steelCool);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject Chip(string name, Material m)
    {
        // Mesh only. Behaviour (grab, stacking, value) is added by Slot A on top of this prefab.
        var root = new GameObject(name);
        Prim(PrimitiveType.Cylinder, "Mesh", root, Vector3.zero, new Vector3(StyleScale.ChipDiameter, 0.00165f, StyleScale.ChipDiameter), m);
        return SavePrefab(root, name);
    }

    static GameObject Keycard(string name, Material m)
    {
        // Credit-card size (85.6 x 54 mm). Behaviour added by Slot B.
        var root = new GameObject(name);
        Prim(PrimitiveType.Cube, "Mesh", root, Vector3.zero, new Vector3(0.0856f, 0.054f, 0.0008f), m);
        return SavePrefab(root, name);
    }

    static GameObject DoorReader(string name, SharedStyleKit kit)
    {
        // Front faces -Z. Behaviour added by Slot B.
        var root = new GameObject(name);
        Prim(PrimitiveType.Cube, "Body", root, Vector3.zero, new Vector3(0.08f, 0.12f, 0.02f), kit.steelCool);
        Prim(PrimitiveType.Cube, "Screen", root, new Vector3(0, 0.03f, -0.0105f), new Vector3(0.05f, 0.02f, 0.003f), kit.screenGlow, collider: false);
        SetStaticRecursive(root);
        return SavePrefab(root, name);
    }

    static GameObject ScaleReference(string name, SharedStyleKit kit)
    {
        var root = new GameObject(name);
        var fig = Prim(PrimitiveType.Capsule, "Figure_1.8m", root, new Vector3(0, StyleScale.FigureHeight / 2f, 0), new Vector3(0.5f, StyleScale.FigureHeight / 2f, 0.5f), kit.steelCool, collider: false);
        fig.name = "Figure_1.8m";
        Prim(PrimitiveType.Cube, "Cube_1m", root, new Vector3(1.5f, 0.5f, 0), Vector3.one, kit.brassGold, collider: false);
        var door = (GameObject)PrefabUtility.InstantiatePrefab(kit.doorwayWarm, root.transform);
        door.transform.localPosition = new Vector3(3f, 0, 0);
        var table = (GameObject)PrefabUtility.InstantiatePrefab(kit.tableRound, root.transform);
        table.transform.localPosition = new Vector3(5.5f, 0, 0);
        var chair = (GameObject)PrefabUtility.InstantiatePrefab(kit.chair, root.transform);
        chair.transform.localPosition = new Vector3(5.5f, 0, -1.3f);
        return SavePrefab(root, name);
    }

    // ---------------------------------------------------------------- template scene

    static void BuildTemplateScene(SharedStyleKit kit)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("#3A3026");

        // Everything only needed to test this scene on its own goes under _SoloTest. SoloTestRoot switches it off
        // when MVP_Main loads the scene additively, so the combined game has exactly one rig, light set and floor.
        var solo = new GameObject("_SoloTest");
        solo.AddComponent<SoloTestRoot>();

        var volGo = new GameObject("Volume_Global");
        volGo.transform.SetParent(solo.transform);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = kit.volumeProfile;

        // 14 x 14 m test floor: covers the spawn (0, -2) and the scale reference (x 0..6.4, z 4.7..7.3) with margin.
        var env = new GameObject("TestFloor");
        env.transform.SetParent(solo.transform);
        for (int x = -2; x <= 4; x++)
            for (int z = -2; z <= 4; z++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(kit.floorTileWarm, env.transform);
                tile.transform.position = new Vector3(x * StyleScale.TileSize, 0, z * StyleScale.TileSize);
            }

        var lights = new GameObject("TestLights");
        lights.transform.SetParent(solo.transform);
        var light = (GameObject)PrefabUtility.InstantiatePrefab(kit.lightWarmFloor, lights.transform);
        light.transform.position = new Vector3(0, 2.7f, 0);

        var refGo = (GameObject)PrefabUtility.InstantiatePrefab(kit.scaleReference, solo.transform);
        refGo.transform.position = new Vector3(0, 0, 6f);

        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(solo.transform);
        spawn.transform.position = new Vector3(0, 0, -2f);

        if (kit.playerRig != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig, solo.transform);
            rig.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }

        AddSimulator(solo.transform);
        AddCasinoExamples(kit, solo.transform);
        AddZones(kit);

        // Empty root for the owner's own content, outside _SoloTest so it survives in MVP_Main.
        new GameObject("SliceContent (rename to <Area>_Content, build inside your zone)");

        Lightmapping.lightingSettings = BuildLightingSettings();
        EditorSceneManager.SaveScene(scene, TemplateScenePath);
    }

    static void AddCasinoExamples(SharedStyleKit kit, Transform solo)
    {
        var examples = new GameObject("CasinoExamples");
        examples.transform.SetParent(solo, false);
        PlaceCasinoExample(kit.casinoTableSet, examples.transform, new Vector3(2.5f, 0, 1.5f));
        PlaceCasinoExample(kit.cashierCounter, examples.transform, new Vector3(-3.5f, 0, 1.5f));
        for (int i = 0; i < 3; i++)
            PlaceCasinoExample(kit.slotMachine, examples.transform, new Vector3(7.5f, 0, -1f + i * 2f), 180f);
        PlaceCasinoExample(kit.barCounter, examples.transform, new Vector3(2.5f, 0, -4f));
        for (int i = 0; i < 3; i++)
            PlaceCasinoExample(kit.barStool, examples.transform, new Vector3(1.5f + i, 0, -3f));
        PlaceCasinoExample(kit.chandelier, examples.transform, new Vector3(2.5f, 2.38f, 1.5f));
        for (int i = 0; i < 3; i++)
            PlaceCasinoExample(kit.casinoWallPanel, examples.transform, new Vector3(-4f + i * 2f, 0, 3.5f));
    }

    static void PlaceCasinoExample(GameObject prefab, Transform parent, Vector3 position, float yaw = 0f)
    {
        if (prefab == null) return;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
    }

    // Where each slice builds, so the scenes line up when MVP_Main loads them together. Values mirror the zone table
    // in docs/StyleGuide.md. Markers are flat glowing outlines with no collider, tagged EditorOnly so builds strip them.
    static void AddZones(SharedStyleKit kit)
    {
        var root = new GameObject("Zones");
        root.tag = "EditorOnly";
        Zone(root, "Zone_A_CashierCage", new Vector3(-3.5f, 0, 1.5f), new Vector2(3f, 3f), kit.screenGlow);
        Zone(root, "Zone_A_Table", new Vector3(2.5f, 0, 1.5f), new Vector2(3f, 3f), kit.screenGlow);
        Zone(root, "Zone_B_VIPDoor", new Vector3(-3.5f, 0, 6.5f), new Vector2(3f, 3f), kit.screenGlow);
    }

    static void Zone(GameObject parent, string name, Vector3 centre, Vector2 size, Material m)
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent.transform, false);
        zone.transform.position = centre;
        // Outline only (4 thin strips) so the marker doesn't flood the view while testing.
        const float w = 0.05f, y = 0.005f, h = 0.01f;
        Prim(PrimitiveType.Cube, "EdgeN", zone, new Vector3(0, y, size.y / 2f), new Vector3(size.x, h, w), m, collider: false);
        Prim(PrimitiveType.Cube, "EdgeS", zone, new Vector3(0, y, -size.y / 2f), new Vector3(size.x, h, w), m, collider: false);
        Prim(PrimitiveType.Cube, "EdgeE", zone, new Vector3(size.x / 2f, y, 0), new Vector3(w, h, size.y), m, collider: false);
        Prim(PrimitiveType.Cube, "EdgeW", zone, new Vector3(-size.x / 2f, y, 0), new Vector3(w, h, size.y), m, collider: false);
    }

    // Two of four team laptops have no GPU (no Quest Link), so every scene must be testable in Play mode
    // with the XR Interaction Simulator. The prefab lives in the XRI sample, which must be imported first.
    internal static void AddSimulator(Transform parent)
    {
        string guid = null;
        foreach (var g in AssetDatabase.FindAssets("\"XR Interaction Simulator\" t:Prefab"))
        {
            if (AssetDatabase.GUIDToAssetPath(g).EndsWith("/XR Interaction Simulator.prefab")) { guid = g; break; }
        }
        if (guid == null)
        {
            Debug.LogWarning("[StyleKit] XR Interaction Simulator prefab not found. Import it (Package Manager > XR Interaction Toolkit > Samples > XR Interaction Simulator), then re-run Build Style Kit.");
            return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        PrefabUtility.InstantiatePrefab(prefab, parent);
    }

    // ---------------------------------------------------------------- validation

    [MenuItem("High Stakes/Validate Open Scene Against Style Kit")]
    public static void ValidateOpenScene()
    {
        var problems = new List<string>();
        int realtimeLights = 0;

        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer || r.gameObject.name.StartsWith("Solo")) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) { problems.Add($"{Path(r)}: missing material"); continue; }
                string p = AssetDatabase.GetAssetPath(m);
                if (!p.StartsWith("Assets/Content/") && !p.StartsWith("Assets/VRTemplateAssets/") && !p.StartsWith("Assets/Samples/") && !p.StartsWith("Packages/"))
                    problems.Add($"{Path(r)}: material '{m.name}' is not from Assets/Content/ (use a kit material or a variant of one)");
                else if (m.shader != null && m.shader.name == "Hidden/InternalErrorShader")
                    problems.Add($"{Path(r)}: broken shader on '{m.name}'");
            }
        }
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) continue;
            if (l.lightmapBakeType != LightmapBakeType.Baked) { realtimeLights++; problems.Add($"{Path(l)}: light is not Baked"); }
            if (l.shadows != LightShadows.None && l.lightmapBakeType != LightmapBakeType.Baked) problems.Add($"{Path(l)}: realtime shadows are not allowed");
        }
        if (realtimeLights > 2) problems.Add($"{realtimeLights} non-baked lights (max 2)");

        if (problems.Count == 0) Debug.Log("[StyleKit] Scene passes the style check.");
        else Debug.LogWarning("[StyleKit] Style check found " + problems.Count + " issue(s):\n- " + string.Join("\n- ", problems));
    }

    // ---------------------------------------------------------------- helpers

    static string Path(Component c)
    {
        var sb = new System.Text.StringBuilder(c.gameObject.name);
        for (var t = c.transform.parent; t != null; t = t.parent) sb.Insert(0, t.name + "/");
        return sb.ToString();
    }

    static GameObject Prim(PrimitiveType type, string name, GameObject parent, Vector3 pos, Vector3 scale, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        else if (type == PrimitiveType.Cylinder)
        {
            // A cylinder primitive ships with a CapsuleCollider, which turns into a sphere on flat shapes
            // (table top, bases, chip). A convex MeshCollider matches the actual cylinder.
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.AddComponent<MeshCollider>().convex = true;
        }
        return go;
    }

    static void SetStaticRecursive(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }

    static GameObject SavePrefab(GameObject root, string name)
    {
        string path = $"{Root}/Prefabs/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
