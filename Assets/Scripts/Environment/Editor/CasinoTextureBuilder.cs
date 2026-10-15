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
        // Wallpaper keeps the kit cream colour and only adds the embossed floral relief.
        var wallpaper = Variant("M_Wallpaper", kit.plasterCream, "T_Wallpaper", WallpaperTiling, null, null);
        var wood = Variant("M_WoodPanel", kit.woodDark, "T_WoodPanel", WainscotTiling, Color.white, 0.35f);
        var frame = Variant("M_PictureFrame", kit.brassGold, "T_PictureFrame", Vector2.one, Color.white, null);
        var canvas = Variant("M_PictureCanvas", kit.plasterCream, "T_PictureCanvas", Vector2.one, Color.white, 0.1f);

        FloorVariant("Env_CasinoFloor_Carpet", kit.floorTileWarm, carpet);
        FloorVariant("Env_CasinoFloor_Marble", kit.floorTileWarm, marble);
        WallPanelVariant(kit.casinoWallPanel, wallpaper, wood);
        PictureFramePrefab(frame, canvas);

        AssetDatabase.SaveAssets();
        Debug.Log("[CasinoTextures] Built materials in " + Mat + " and prefabs in " + Pre + ". Open a scene you own and run High Stakes > Casino Textures > Apply Casino Textures To Open Scene.");
    }

    // The area scenes MVP_Main loads that use the warm casino look. MVP_VIP (Slot A) is left to its owner.
    static string[] AreaScenes()
    {
        var list = new List<string> { "Assets/Scenes/MVP/MVP_Casino.unity", HallwayBuilder.HubScenePath };
        list.AddRange(HallwayBuilder.ScenePaths);
        return list.ToArray();
    }

    const string ApplyDescription = "- warm kit floor tiles and casino wall panels become the textured variants\n- large plain floor/wall boxes using the kit carpet, cream or dark wood get the textured material, tiled to their size\n- primitive picture frames become the FBX frame\nCool back-of-house rooms are left as they are.";

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
                "Opens, edits and saves:\n" + string.Join("\n", scenes) + "\n\n" + ApplyDescription + "\n\nMVP_Casino is Ashton's scene: tell him before committing it.",
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
        var floor = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Env_CasinoFloor_Carpet.prefab");
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Env_CasinoWallPanel.prefab");
        var picture = AssetDatabase.LoadAssetAtPath<GameObject>(Pre + "Env_PictureFrame.prefab");
        if (kit == null || floor == null || wall == null || picture == null)
        {
            Debug.LogError("[CasinoTextures] Run High Stakes > Casino Textures > Build Casino Textures first.");
            return false;
        }

        int floors = 0, walls = 0, pictures = 0;
        int surfaces = TextureLooseSurfaces(kit);
        var swaps = new List<(GameObject instance, GameObject target)>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source == kit.floorTileWarm) swaps.Add((go, floor));
            else if (source == kit.casinoWallPanel) swaps.Add((go, wall));
        }
        foreach (var (instance, target) in swaps)
        {
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instance, target, InteractionMode.UserAction);
            if (target == floor) floors++; else walls++;
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
        Debug.Log($"[CasinoTextures] {scene.path}: {floors} floor tiles, {walls} wall panels, {surfaces} large floor/wall surfaces, {pictures} picture frames textured.");
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
    static Material TiledVariant(Material parent, Vector2 tiling)
    {
        tiling = new Vector2(Mathf.Round(tiling.x * 10f) / 10f, Mathf.Round(tiling.y * 10f) / 10f);
        EnsureFolder(TiledFolder);
        string path = $"{TiledFolder}/{parent.name}_{tiling.x:0.#}x{tiling.y:0.#}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(parent);
        AssetDatabase.CreateAsset(m, path);
        m.parent = parent;
        m.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Vector2 Tiling(Vector2 size, Vector2 repeat) => new Vector2(size.x / repeat.x, size.y / repeat.y);

    static void EnsureFolder(string path)
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
    {
        string path = Mat + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(parent);
            AssetDatabase.CreateAsset(m, path);
        }
        m.parent = parent;

        var albedo = LoadTexture(texturePrefix + "_Albedo");
        var normal = LoadTexture(texturePrefix + "_Normal");
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

    static Texture2D LoadTexture(string baseName)
    {
        foreach (var ext in new[] { ".jpg", ".png" })
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + baseName + ext);
            if (t != null) return t;
        }
        return null;
    }

    // ------------------------------------------------------------------ prefabs

    static void FloorVariant(string name, GameObject kitFloor, Material material)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitFloor);
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = material;
        SaveVariant(instance, name);
    }

    static void WallPanelVariant(GameObject kitPanel, Material wallpaper, Material wood)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitPanel);
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.name == "SideWall") r.sharedMaterial = wallpaper;
            else if (r.name == "Wainscot") r.sharedMaterial = wood;
        }
        SaveVariant(instance, "Env_CasinoWallPanel");
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

    static void SaveVariant(GameObject instance, string name)
    {
        instance.name = name;
        foreach (var t in instance.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        PrefabUtility.SaveAsPrefabAsset(instance, Pre + name + ".prefab");
        Object.DestroyImmediate(instance);
    }
}
