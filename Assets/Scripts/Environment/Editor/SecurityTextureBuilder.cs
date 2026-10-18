using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Textured back-of-house rooms (the security office, camera room and vault at the end of each security wing), from the
/// textures in Assets/Content/Environment/Security/Textures/ (made by tools/generate_security_textures.py, sources in
/// that folder's README): painted cinder-block walls, sealed concrete floors, diamond tread plate in the vault, steel
/// panel doors and an acoustic drop ceiling.
///
/// Like <see cref="CasinoTextureBuilder"/> it makes Material Variants of the kit's cool materials (StyleGuide: no new
/// base materials) and Prefab Variants of the kit cool floor tile, cool wall and ceiling tile for new rooms. Applying to a
/// scene only swaps the material on the existing kit pieces (a prefab override), so their baked lightmaps are kept; every
/// surface gets a variant tiled to its size so the pattern keeps its real-world scale. The cool portal door prefab gets a
/// steel leaf and a block frame. Warm (casino-look) hallways are left alone.
///
/// Slot D owns this. It never writes to Assets/Content/StyleKit/. HallwayBuilder.Build runs it after rebuilding the
/// wings, so regenerated wings come back textured.
/// </summary>
public static class SecurityTextureBuilder
{
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string Root = "Assets/Content/Environment/Security";
    const string Tex = Root + "/Textures/";
    const string Mat = Root + "/Materials/";
    const string Pre = Root + "/Prefabs/";
    const string TiledFolder = Mat + "Tiled";
    const string CoolDoorPath = "Assets/Content/Environment/Prefabs/Env_PortalDoor_Cool.prefab";

    // How many metres one copy of each texture covers (tools/generate_security_textures.py).
    static readonly Vector2 WallRepeat = new Vector2(2f, 2f);
    static readonly Vector2 FloorRepeat = new Vector2(2f, 2f);
    static readonly Vector2 PlateRepeat = new Vector2(0.5f, 0.5f);
    static readonly Vector2 SteelRepeat = new Vector2(2f, 2f);
    static readonly Vector2 CeilingRepeat = new Vector2(1f, 1f);

    // Kit piece faces: floor and ceiling tiles 2 x 2 m, cool wall 2 x 3 m.
    static readonly Vector2 TileFace = new Vector2(2f, 2f);
    static readonly Vector2 WallFace = new Vector2(2f, 3f);

    struct Materials { public Material wall, floor, plate, steel, ceiling; }

    [MenuItem("High Stakes/Security Textures/Build And Apply To Security Wings")]
    public static void BuildAndApplyMenu()
    {
        if (!EditorUtility.DisplayDialog("Texture the security rooms?",
                "Builds the security materials and prefabs, then opens, edits and saves:\n" + string.Join("\n", HallwayBuilder.ScenePaths) +
                "\n\nCool floors, walls and ceilings in the rooms get the textured materials (lightmaps are kept). Warm hallways are not touched.",
                "Apply and save", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (BuildAndApplyToWings())
            Debug.Log("[SecurityTextures] Security rooms textured and saved. Open MVP_Main and walk through STAFF ONLY, or open a wing and press Play.");
    }

    [MenuItem("High Stakes/Security Textures/Apply To Open Scene")]
    public static void ApplyToOpenSceneMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!EditorUtility.DisplayDialog("Apply security textures?",
                "This edits " + scene.path + ": kit cool floor tiles and walls, and ceiling tiles inside \"Room...\" objects, get the textured security materials.\n\nOnly do this in a scene you own (ask the owner first otherwise). Undo works.",
                "Apply", "Cancel")) return;
        if (!Build(out var mats)) return;
        Debug.Log($"[SecurityTextures] {scene.path}: {ApplyTo(mats)} surfaces textured. Save the scene.");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    /// <summary>
    /// Builds the materials and prefabs and textures the three wing scenes (opened and saved one by one, no dialogs).
    /// Returns false, with a warning, if the textures have not been generated yet.
    /// </summary>
    public static bool BuildAndApplyToWings()
    {
        if (!Build(out var mats)) return false;
        foreach (var path in HallwayBuilder.ScenePaths)
        {
            if (!System.IO.File.Exists(path)) { Debug.LogWarning("[SecurityTextures] Skipped missing " + path); continue; }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = ApplyTo(mats);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SecurityTextures] {path}: {n} surfaces textured.");
        }
        AssetDatabase.SaveAssets();
        return true;
    }

    // ------------------------------------------------------------------ assets

    static bool Build(out Materials mats)
    {
        mats = default;
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null) { Debug.LogError("[SecurityTextures] Style kit not found at " + KitPath + "."); return false; }
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "T_SecurityWall_Albedo.jpg") == null)
        {
            Debug.LogWarning("[SecurityTextures] No textures in " + Tex + ". Run `python tools/generate_security_textures.py` from the repository root, then try again.");
            return false;
        }

        CasinoTextureBuilder.EnsureFolder(Root + "/Materials");
        CasinoTextureBuilder.EnsureFolder(Root + "/Prefabs");
        var white = Color.white; // the albedos are already recoloured into the kit palette
        mats.wall = CasinoTextureBuilder.Variant(Mat, Tex, "M_SecurityWall", kit.concreteCool, "T_SecurityWall",
            CasinoTextureBuilder.Tiling(WallFace, WallRepeat), white, null);
        mats.floor = CasinoTextureBuilder.Variant(Mat, Tex, "M_SecurityFloor", kit.tileCool, "T_SecurityFloor",
            CasinoTextureBuilder.Tiling(TileFace, FloorRepeat), white, 0.4f);
        mats.plate = CasinoTextureBuilder.Variant(Mat, Tex, "M_VaultFloor", kit.steelCool, "T_VaultFloor",
            CasinoTextureBuilder.Tiling(TileFace, PlateRepeat), white, 0.55f);
        mats.steel = CasinoTextureBuilder.Variant(Mat, Tex, "M_SecuritySteel", kit.steelCool, "T_SecuritySteel",
            Vector2.one, white, 0.6f);
        mats.ceiling = CasinoTextureBuilder.Variant(Mat, Tex, "M_SecurityCeiling", kit.plasterCream, "T_SecurityCeiling",
            CasinoTextureBuilder.Tiling(TileFace, CeilingRepeat), white, 0.05f);

        KitVariant(kit.floorTileCool, mats.floor, "Env_SecurityFloor_Concrete");
        KitVariant(kit.floorTileCool, mats.plate, "Env_SecurityFloor_Plate");
        KitVariant(kit.wallCool, mats.wall, "Env_SecurityWall");
        KitVariant(kit.ceilingTile, mats.ceiling, "Env_SecurityCeiling");
        TextureCoolDoor(mats);
        CasinoTextureBuilder.RefreshTiledVariants(TiledFolder);

        AssetDatabase.SaveAssets();
        return true;
    }

    static void KitVariant(GameObject kitPrefab, Material material, string name)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitPrefab);
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = material;
        CasinoTextureBuilder.SaveVariant(instance, Pre, name);
    }

    // Steel leaf and cinder-block frame on HallwayBuilder's cool portal door (used by every security room door). The card
    // reader keeps its kit look.
    static void TextureCoolDoor(Materials mats)
    {
        if (!System.IO.File.Exists(CoolDoorPath)) return;
        var root = PrefabUtility.LoadPrefabContents(CoolDoorPath);
        var frame = root.transform.Find("Frame");
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.name == "Leaf") r.sharedMaterial = Fit(mats.steel, r, SteelRepeat);
            else if (frame != null && r.transform.IsChildOf(frame)) r.sharedMaterial = Fit(mats.wall, r, WallRepeat);
        }
        PrefabUtility.SaveAsPrefabAsset(root, CoolDoorPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ------------------------------------------------------------------ scenes

    // Kit cool floor tiles (diamond plate in the vault) and cool walls anywhere, ceiling tiles inside a "Room..." object
    // (HallwayBuilder's security rooms; the warm hallways keep the cream ceiling), and the vault door placeholder.
    static int ApplyTo(Materials mats)
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        int count = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.sharedMaterials.Length != 1) continue;
            var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            var source = instanceRoot != null ? PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot) : null;
            var room = RoomOf(r.transform);
            bool vault = room != null && room.name.Contains("Vault");

            Material material; Vector2 repeat;
            if (source == kit.floorTileCool) { material = vault ? mats.plate : mats.floor; repeat = vault ? PlateRepeat : FloorRepeat; }
            else if (source == kit.wallCool) { material = mats.wall; repeat = WallRepeat; }
            else if (source == kit.ceilingTile && room != null) { material = mats.ceiling; repeat = CeilingRepeat; }
            else if (instanceRoot == null && r.name == "VaultDoor_Placeholder") { material = mats.steel; repeat = SteelRepeat; }
            else continue;

            var fitted = Fit(material, r, repeat);
            if (r.sharedMaterial == fitted) continue;
            Undo.RecordObject(r, "Apply security textures");
            r.sharedMaterial = fitted;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            count++;
        }
        return count;
    }

    static Transform RoomOf(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.name.StartsWith("Room")) return t;
        return null;
    }

    static Material Fit(Material material, Renderer r, Vector2 repeat)
        => CasinoTextureBuilder.FitToFace(material, r, repeat, TiledFolder);
}
