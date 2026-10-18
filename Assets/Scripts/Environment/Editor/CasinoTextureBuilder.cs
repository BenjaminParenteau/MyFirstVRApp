using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Textured casino floors and walls, built from the CC0 textures and FBX in Assets/Content/Environment/Casino/
/// (sources in that folder's README).
///
/// "Build Casino Textures" makes Material Variants of the shared kit materials (StyleGuide: no new base materials)
/// and Prefab Variants of the kit floor tile and casino wall panel, so they keep the kit's sizes and colliders.
/// "Apply Casino Textures To Open Scene" swaps those into the open scene: kit warm floor tiles and casino wall panels
/// become the textured variants, large plain floor/wall cubes with a kit material (how MVP_Casino is built) get our
/// material tiled to their size (variants in Materials/Tiled/), and the primitive picture frames become the FBX frame.
///
/// Slot D owns this. It never writes to Assets/Content/StyleKit/. Re-running Build updates the same assets in place
/// (GUIDs are kept), so scenes that use them stay linked.
/// </summary>
public static class CasinoTextureBuilder
{
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string Root = "Assets/Content/Environment/Casino";
    const string Tex = Root + "/Textures/";
    const string Mat = Root + "/Materials/";
    const string Pre = Root + "/Prefabs/";
    const string FrameModel = Root + "/Models/fancy_picture_frame_01.fbx";

    // How many metres one copy of each texture covers. Change these to make a pattern bigger or smaller.
    static readonly Vector2 CarpetRepeat = new Vector2(1f, 1f);       // tools/generate_casino_carpet.py tile
    static readonly Vector2 MarbleRepeat = new Vector2(1.5f, 1.5f);    // the slabs' real size
    static readonly Vector2 WallpaperRepeat = new Vector2(0.5f, 0.75f);
    static readonly Vector2 WoodRepeat = new Vector2(1f, 1f);

    // Texture repeats per face of the kit pieces (cube UVs are 0..1 per face): floor tile 2 x 2 m, wall panel upper
    // wall 2 x 3 m, wainscot 2 x 1.3 m.
    static readonly Vector2 CarpetTiling = Tiling(new Vector2(2f, 2f), CarpetRepeat);
    static readonly Vector2 MarbleTiling = Tiling(new Vector2(2f, 2f), MarbleRepeat);
    static readonly Vector2 WallpaperTiling = Tiling(new Vector2(2f, 3f), WallpaperRepeat);
    static readonly Vector2 WainscotTiling = Tiling(new Vector2(2f, 1.3f), WoodRepeat);

    // Loose cubes (scenes not built from kit prefabs, e.g. MVP_Casino) are only re-textured when their visible face is
    // at least this big, so small props that share the kit material (lamps, table tops) are left alone.
    const float MinSurfaceArea = 2f;
    const string TiledFolder = Mat + "Tiled";
    // Kit picture frame size (HallwayBuilder.DecorateWarmWall) that the FBX frame is scaled to.
    static readonly Vector2 FrameSize = new Vector2(1.1f, 0.7f);

    [MenuItem("High Stakes/Casino Textures/Build Casino Textures")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null) { Debug.LogError("[CasinoTextures] Style kit not found at " + KitPath + "."); return; }

        var carpet = Variant("M_CasinoCarpet", kit.carpetRed, "T_CasinoCarpet", CarpetTiling, Color.white, 0.05f);
        var marble = Variant("M_MarbleFloor", kit.plasterCream, "T_MarbleFloor", MarbleTiling, Color.white, 0.45f);
        // Cream-and-gold damask (tools/generate_casino_wallpaper.py, already in the kit palette) on the embossed relief.
        var wallpaper = Variant("M_Wallpaper", kit.plasterCream, "T_Wallpaper", WallpaperTiling, Color.white, null);
        var wood = Variant("M_WoodPanel", kit.woodDark, "T_WoodPanel", WainscotTiling, Color.white, 0.35f);
        var frame = Variant("M_PictureFrame", kit.brassGold, "T_PictureFrame", Vector2.one, Color.white, null);
        var canvas = Variant("M_PictureCanvas", kit.plasterCream, "T_PictureCanvas", Vector2.one, Color.white, 0.1f);

        FloorVariant("Env_CasinoFloor_Carpet", kit.floorTileWarm, carpet);
        FloorVariant("Env_CasinoFloor_Marble", kit.floorTileWarm, marble);
        WallPanelVariant(kit.casinoWallPanel, wallpaper, wood);
        PictureFramePrefab(frame, canvas);
        CasinoDetailTextures.Build(kit);   // ceiling, beams, furniture, tables, metalwork, lamps
        RefreshTiledVariants(TiledFolder); // the parts already in scenes use these

        AssetDatabase.SaveAssets();
        Debug.Log("[CasinoTextures] Built materials in " + Mat + " and prefabs in " + Pre + ". Open a scene you own and run High Stakes > Casino Textures > Apply Casino Textures To Open Scene.");
    }

    // The area scenes MVP_Main loads that use the warm casino look. MVP_VIP is Ben's (Slot A): he OK'd texture-only changes
    // (2026-10-17, Progress_Vol1.md Decisions).
    static string[] AreaScenes()
    {
        var list = new List<string> { "Assets/Scenes/MVP/MVP_Casino.unity", "Assets/Scenes/MVP/MVP_VIP.unity", HallwayBuilder.HubScenePath };
        list.AddRange(HallwayBuilder.ScenePaths);
        return list.ToArray();
    }

    /// <summary>
    /// Textures and saves the given scenes without dialogs (WorldBuilder runs it after regenerating scenes, so rebuilt
    /// areas keep their textures). Skips, with a warning, if the casino materials have not been built.
    /// </summary>
    public static void ApplyToScenes(params string[] paths)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_CasinoCarpet.mat") == null)
        {
            Debug.LogWarning("[CasinoTextures] Casino materials not built; run High Stakes > Casino Textures > Build Casino Textures, then Apply Casino Textures To All Area Scenes.");
            return;
        }
        foreach (var path in paths)
        {
            if (!System.IO.File.Exists(path)) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!ApplyTo(scene)) return;
            EditorSceneManager.SaveScene(scene);
        }
    }

    const string ApplyDescription = "- warm kit floor tiles and casino wall panels get the carpet, wallpaper and wood panel materials\n- large plain floor/wall boxes using the kit carpet, cream or dark wood get the textured material, tiled to their size\n- primitive picture frames become the FBX frame\n- details get textured materials: coffered ceiling (also on warm kit ceiling tiles), wood beams and pilasters, table felt and wood, leather seats, brushed brass and steel, slot cabinets, bar top, glowing lamps, the staff door\nCool back-of-house rooms are left as they are (see Security Textures).";

    [MenuItem("High Stakes/Casino Textures/Apply Casino Textures To Open Scene")]
    public static void ApplyToOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!EditorUtility.DisplayDialog("Apply casino textures?",
                "This edits " + scene.path + ":\n" + ApplyDescription + "\n\nOnly do this in a scene you own (ask the owner first otherwise). Undo works.",
                "Apply", "Cancel")) return;
        if (ApplyTo(scene)) Debug.Log("[CasinoTextures] Save the scene, then run High Stakes > Validate Open Scene Against Style Kit.");
    }

    [MenuItem("High Stakes/Casino Textures/Apply Casino Textures To All Area Scenes")]
    public static void ApplyToAllAreaScenes()
    {
        var scenes = AreaScenes();
        if (!EditorUtility.DisplayDialog("Apply casino textures to every area?",
                "Opens, edits and saves:\n" + string.Join("\n", scenes) + "\n\n" + ApplyDescription + "\n\nMVP_Casino is Ashton's scene and MVP_VIP is Ben's (texture-only changes OK'd): tell them before committing.",
                "Apply and save", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var path in scenes)
        {
            if (!System.IO.File.Exists(path)) { Debug.LogWarning("[CasinoTextures] Skipped missing " + path); continue; }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!ApplyTo(scene)) return;
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[CasinoTextures] All area scenes textured and saved. Open MVP_Main and press Play.");
    }

    static bool ApplyTo(UnityEngine.SceneManagement.Scene scene)
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        var carpet = AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_CasinoCarpet.mat");
        var wallpaper = AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_Wallpaper.mat");
        var wood = AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_WoodPanel.mat");
        var picture = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Env_PictureFrame.prefab");
        if (kit == null || carpet == null || wallpaper == null || wood == null || picture == null)
        {
            Debug.LogError("[CasinoTextures] Run High Stakes > Casino Textures > Build Casino Textures first.");
            return false;
        }

        int floors = 0, walls = 0, pictures = 0;
        int surfaces = TextureLooseSurfaces(kit);
        int details = CasinoDetailTextures.ApplyTo(kit);
        // Kit warm floor tiles and casino wall panels: override the materials on the existing instance (the same ones
        // as the Env_ prefab variants; the pieces are kit sizes, so the base materials' tiling fits). Swapping the prefab
        // instead would give the pieces new identities and lose their baked lightmaps.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            bool isFloor = source == kit.floorTileWarm;
            if (!isFloor && source != kit.casinoWallPanel) continue;
            bool changed = false;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = isFloor ? carpet : r.name.EndsWith("SideWall") ? wallpaper : r.name.EndsWith("Wainscot") ? wood : null;
                if (m == null || r.sharedMaterial == m) continue;
                Undo.RecordObject(r, "Apply casino textures");
                r.sharedMaterial = m;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                changed = true;
            }
            if (changed) { if (isFloor) floors++; else walls++; }
        }

        // Primitive frames added by HallwayBuilder: a "PictureFrame" cube with a "PictureCanvas" cube next to it.
        var oldFrames = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "PictureFrame" && t.GetComponent<MeshFilter>() != null) oldFrames.Add(t.gameObject);
        foreach (var old in oldFrames)
        {
            var parent = old.transform.parent;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(picture, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Apply casino textures");
            instance.transform.localPosition = old.transform.localPosition;
            instance.transform.localRotation = old.transform.localRotation;
            var oldCanvas = parent != null ? parent.Find("PictureCanvas") : null;
            if (oldCanvas != null) Undo.DestroyObjectImmediate(oldCanvas.gameObject);
            Undo.DestroyObjectImmediate(old);
            pictures++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[CasinoTextures] {scene.path}: {floors} floor tiles, {walls} wall panels, {surfaces} large floor/wall surfaces, {pictures} picture frames, {details} details textured.");
        return true;
    }

    // Plain Unity cubes carrying a kit material directly: carpet on floors (horizontal), cream and dark wood on walls
    // (vertical). Each gets a Material Variant of ours whose tiling matches its size, shared by every surface of the
    // same size, so the pattern keeps its real-world scale and the materials stay batchable (no per-object script).
    static int TextureLooseSurfaces(SharedStyleKit kit)
    {
        var rules = new[]
        {
            (kit: kit.carpetRed, ours: AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_CasinoCarpet.mat"), repeat: CarpetRepeat, floor: true),
            (kit: kit.plasterCream, ours: AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_Wallpaper.mat"), repeat: WallpaperRepeat, floor: false),
            (kit: kit.woodDark, ours: AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_WoodPanel.mat"), repeat: WoodRepeat, floor: false),
        };

        int count = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var filter = r.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.name != "Cube" ||
                AssetDatabase.GetAssetPath(filter.sharedMesh) != "Library/unity default resources") continue;
            if (r.sharedMaterials.Length != 1) continue;

            foreach (var rule in rules)
            {
                // The kit material, or ours from an earlier Apply (so re-running picks up new tiling or textures).
                // A large floor with a missing material (a deleted tiled variant, shows magenta) is treated as carpet.
                var current = r.sharedMaterial;
                if (rule.ours == null) continue;
                if (current == null ? !rule.floor :
                    current != rule.kit && current != rule.ours && current.parent != rule.ours) continue;

                // The thinnest local axis is the one the visible face looks along.
                var s = r.transform.lossyScale;
                s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                int thin = s.x <= s.y && s.x <= s.z ? 0 : (s.y <= s.z ? 1 : 2);
                var axis = thin == 0 ? r.transform.right : thin == 1 ? r.transform.up : r.transform.forward;
                bool horizontal = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.7f;
                if (horizontal != rule.floor) break;

                // Unity's cube UVs: X faces run (z, y), Y faces (x, z), Z faces (x, y).
                var face = thin == 0 ? new Vector2(s.z, s.y) : thin == 1 ? new Vector2(s.x, s.z) : new Vector2(s.x, s.y);
                if (face.x * face.y < MinSurfaceArea) break;

                Undo.RecordObject(r, "Apply casino textures");
                r.sharedMaterial = TiledVariant(rule.ours, Tiling(face, rule.repeat));
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                count++;
                break;
            }
        }
        return count;
    }

    // A variant of one of our materials with a different tiling, reused for every surface of the same size.
    static Material TiledVariant(Material parent, Vector2 tiling) => TiledVariant(parent, tiling, TiledFolder);

    /// <summary>A Material Variant of <paramref name="parent"/> with only the tiling changed, saved in (and reused from) <paramref name="folder"/>.</summary>
    internal static Material TiledVariant(Material parent, Vector2 tiling, string folder)
    {
        tiling = new Vector2(Mathf.Round(tiling.x * 10f) / 10f, Mathf.Round(tiling.y * 10f) / 10f);
        EnsureFolder(folder);
        string path = $"{folder}/{parent.name}_{tiling.x:0.#}x{tiling.y:0.#}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(parent);
            AssetDatabase.CreateAsset(m, path);
            m.parent = parent;
        }
        SyncFromParent(m, tiling);
        return m;
    }

    /// <summary>
    /// Re-syncs every tiled variant in <paramref name="folder"/> with its parent. A tiled variant overrides _BaseMap for
    /// its tiling, and texture + tiling are one property, so a new albedo (or colour) on the parent would not reach it
    /// on its own. Run after rebuilding the parent materials.
    /// </summary>
    internal static void RefreshTiledVariants(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null && m.parent != null) SyncFromParent(m, m.GetTextureScale("_BaseMap"));
        }
    }

    static void SyncFromParent(Material m, Vector2 tiling)
    {
        var parent = m.parent;
        m.SetTexture("_BaseMap", parent.GetTexture("_BaseMap"));
        m.SetTextureScale("_BaseMap", tiling);
        m.SetTexture("_BumpMap", parent.GetTexture("_BumpMap"));
        if (parent.IsKeywordEnabled("_NORMALMAP")) m.EnableKeyword("_NORMALMAP"); else m.DisableKeyword("_NORMALMAP");
        m.SetColor("_BaseColor", parent.GetColor("_BaseColor"));
        m.SetFloat("_Smoothness", parent.GetFloat("_Smoothness"));
        EditorUtility.SetDirty(m);
    }

    /// <summary>
    /// <paramref name="material"/> tiled to the renderer's visible face (a scaled Unity cube: the thinnest axis is the one
    /// the face looks along), as a tiled variant in <paramref name="folder"/>. Returns the material itself when its own
    /// tiling already fits, e.g. on a standard kit piece.
    /// </summary>
    internal static Material FitToFace(Material material, Renderer r, Vector2 repeat, string folder)
    {
        var s = r.transform.lossyScale;
        s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        int thin = s.x <= s.y && s.x <= s.z ? 0 : (s.y <= s.z ? 1 : 2);
        // Unity's cube UVs: X faces run (z, y), Y faces (x, z), Z faces (x, y).
        var face = thin == 0 ? new Vector2(s.z, s.y) : thin == 1 ? new Vector2(s.x, s.z) : new Vector2(s.x, s.y);
        var tiling = Tiling(face, repeat);
        if ((material.GetTextureScale("_BaseMap") - tiling).sqrMagnitude < 0.01f) return material;
        return TiledVariant(material, tiling, folder);
    }

    internal static Vector2 Tiling(Vector2 size, Vector2 repeat) => new Vector2(size.x / repeat.x, size.y / repeat.y);

    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    // ------------------------------------------------------------------ materials

    // A Material Variant of a kit material that overrides only the textures, tiling and (optionally) colour/smoothness.
    // Pass color null to keep the kit tint. The kit uses URP Simple Lit: _BaseMap, _BumpMap (+ _NORMALMAP keyword).
    static Material Variant(string name, Material parent, string texturePrefix, Vector2 tiling, Color? color, float? smoothness)
        => Variant(Mat, Tex, name, parent, texturePrefix, tiling, color, smoothness);

    /// <summary>
    /// The same, saved as <paramref name="materialFolder"/>/name.mat with textures (prefix_Albedo / prefix_Normal, .jpg or
    /// .png) from <paramref name="textureFolder"/>. Both folders end in '/'.
    /// </summary>
    internal static Material Variant(string materialFolder, string textureFolder, string name, Material parent, string texturePrefix,
        Vector2 tiling, Color? color, float? smoothness)
    {
        string path = materialFolder + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(parent);
            AssetDatabase.CreateAsset(m, path);
        }
        m.parent = parent;

        var albedo = LoadTexture(textureFolder, texturePrefix + "_Albedo");
        var normal = LoadTexture(textureFolder, texturePrefix + "_Normal");
        if (albedo != null)
        {
            m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", tiling);
        }
        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
            // Simple Lit samples every map with the _BaseMap tiling; set it even when there is no albedo.
            m.SetTextureScale("_BaseMap", tiling);
        }
        else
        {
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
        }
        if (color.HasValue) m.SetColor("_BaseColor", color.Value);
        if (smoothness.HasValue) m.SetFloat("_Smoothness", smoothness.Value);

        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D LoadTexture(string folder, string baseName)
    {
        foreach (var ext in new[] { ".jpg", ".png" })
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + baseName + ext);
            if (t != null) return t;
        }
        return null;
    }

    // ------------------------------------------------------------------ prefabs

    static void FloorVariant(string name, GameObject kitFloor, Material material)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitFloor);
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = material;
        SaveVariant(instance, Pre, name);
    }

    static void WallPanelVariant(GameObject kitPanel, Material wallpaper, Material wood)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitPanel);
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>())
        {
            // The kit panel's parts are Casino_SideWall and Casino_Wainscot.
            if (r.name.EndsWith("SideWall")) r.sharedMaterial = wallpaper;
            else if (r.name.EndsWith("Wainscot")) r.sharedMaterial = wood;
        }
        SaveVariant(instance, Pre, "Env_CasinoWallPanel");
    }

    // The FBX frame, remapped to our materials, oriented to the kit convention (flat on the wall, front facing local
    // -Z) and scaled to the kit picture size. Visual only: no collider, like the kit wall details.
    static void PictureFramePrefab(Material frame, Material canvas)
    {
        var importer = AssetImporter.GetAtPath(FrameModel) as ModelImporter;
        if (importer == null) { Debug.LogError("[CasinoTextures] Missing " + FrameModel); return; }

        bool changed = false;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(FrameModel))
        {
            if (!(asset is Material embedded)) continue;
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name);
            importer.AddRemap(id, embedded.name.ToLower().Contains("canvas") ? canvas : frame);
            changed = true;
        }
        if (changed) importer.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FrameModel);
        var root = new GameObject("Env_PictureFrame");
        var mesh = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        mesh.name = "Model";

        // Put the thinnest axis along Z (the model may come in lying flat), then fit width/height to FrameSize.
        var b = LocalBounds(mesh);
        if (b.size.y < b.size.x && b.size.y < b.size.z) mesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * mesh.transform.localRotation;
        else if (b.size.x < b.size.y && b.size.x < b.size.z) mesh.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * mesh.transform.localRotation;
        b = LocalBounds(mesh);
        float scale = Mathf.Min(FrameSize.x / b.size.x, FrameSize.y / b.size.y);
        mesh.transform.localScale *= scale;
        b = LocalBounds(mesh);
        // Centre it on the root and sit its back against the wall face (root origin = old primitive frame centre).
        mesh.transform.localPosition -= new Vector3(b.center.x, b.center.y, b.center.z);

        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;

        PrefabUtility.SaveAsPrefabAsset(root, Pre + "Env_PictureFrame.prefab");
        Object.DestroyImmediate(root);
    }

    static Bounds LocalBounds(GameObject go)
    {
        var parent = go.transform.parent;
        bool any = false;
        var bounds = new Bounds();
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var w = r.bounds;
            var local = new Bounds(parent.InverseTransformPoint(w.center), parent.InverseTransformVector(w.size));
            local.size = new Vector3(Mathf.Abs(local.size.x), Mathf.Abs(local.size.y), Mathf.Abs(local.size.z));
            if (!any) { bounds = local; any = true; } else bounds.Encapsulate(local);
        }
        return bounds;
    }

    /// <summary>Saves a kit prefab instance (with its overrides) as a static Prefab Variant at <paramref name="folder"/>/name.prefab, then destroys the instance.</summary>
    internal static void SaveVariant(GameObject instance, string folder, string name)
    {
        instance.name = name;
        foreach (var t in instance.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        PrefabUtility.SaveAsPrefabAsset(instance, folder + name + ".prefab");
        Object.DestroyImmediate(instance);
    }
}
