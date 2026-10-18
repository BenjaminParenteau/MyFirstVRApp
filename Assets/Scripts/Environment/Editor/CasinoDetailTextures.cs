using System.Collections.Generic;
using HighStakes.Environment;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The casino's details, textured: carved wood coffer ceiling (the casino's ceiling and every warm kit ceiling tile), rosewood ceiling beams, dark wood pilasters and table wood,
/// wool felt, burgundy leather seats and padded table rails, brushed brass trim (chandeliers, slot machines, rails, frames), brushed steel chair
/// bases, black brushed slot cabinets and sign backing, a lacquered bar top, glowing chandelier and sconce lamps, and a wood staff door.
/// Textures come from tools/generate_casino_detail_textures.py (sources in Assets/Content/Environment/Casino/README.md).
///
/// Run by <see cref="CasinoTextureBuilder"/>: Build makes the Material Variants, ApplyTo picks the parts by their name
/// and kit material (the part names are the same in the kit prefabs and in MVP_Casino's loose cubes, e.g.
/// Casino_SlotCabinet) and swaps in a variant tiled to each part's size. It only overrides materials, so baked lightmaps
/// are kept. It touches loose scene objects and instances of the style kit and Environment prefabs; Slot A's table
/// prefabs only get their felt, wood and rail overridden in the scene (their prefab files are not changed); other slots'
/// prefabs (NPCs, puzzles) are skipped.
/// </summary>
static class CasinoDetailTextures
{
    const string Root = "Assets/Content/Environment/Casino";
    const string Tex = Root + "/Textures/";
    const string Mat = Root + "/Materials/";
    const string TiledFolder = Mat + "Tiled";

    // How many metres one copy of each texture covers (tools/generate_casino_detail_textures.py).
    // 2 m (not the photo's 1.4 m) so each 2 m kit ceiling tile holds whole coffers and the grid runs on across tiles.
    static readonly Vector2 CofferRepeat = new Vector2(2f, 2f);
    static readonly Vector2 RosewoodRepeat = new Vector2(2.4f, 2.4f);
    static readonly Vector2 DarkWoodRepeat = new Vector2(1f, 1f);
    static readonly Vector2 FeltRepeat = new Vector2(1f, 1f);
    static readonly Vector2 LeatherRepeat = new Vector2(0.6f, 0.6f);
    static readonly Vector2 MetalRepeat = new Vector2(1f, 1f);
    static readonly Vector2 WoodPanelRepeat = new Vector2(1f, 1f); // CasinoTextureBuilder.WoodRepeat

    // Parts of Slot A's table prefabs that may be overridden in the scene (cards, buttons and layouts are left alone).
    static readonly HashSet<string> TableParts = new HashSet<string> { "Felt", "Top", "Pedestal", "Shoe", "Rail", "Bar" };

    static readonly Color LampGlow = new Color(1f, 0.78f, 0.5f) * 1.6f; // warm, close to Light_WarmFloor #FFC88A

    /// <summary>Builds (or updates) the detail Material Variants. Returns false, with a warning, if the textures are missing.</summary>
    public static bool Build(SharedStyleKit kit)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "T_BrushedMetal_Albedo.jpg") == null)
        {
            Debug.LogWarning("[CasinoTextures] No detail textures (T_BrushedMetal_Albedo etc.) in " + Tex + ". Run `python tools/generate_casino_detail_textures.py` from the repository root to texture the ceiling, furniture and metalwork too.");
            return false;
        }
        var white = Color.white; // wood, felt and leather albedos are already in the kit palette
        CasinoTextureBuilder.Variant(Mat, Tex, "M_CeilingCoffer", kit.woodDark, "T_CeilingCoffer", Vector2.one, white, 0.25f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_Rosewood", kit.woodDark, "T_Rosewood", Vector2.one, white, 0.45f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_DarkWood", kit.woodDark, "T_DarkWood", Vector2.one, white, 0.45f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_BarTop", kit.woodDark, "T_DarkWood", Vector2.one, white, 0.75f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_TableFelt", kit.feltGreen, "T_Felt", Vector2.one, white, 0.05f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_Leather", kit.fabricBurgundy, "T_Leather", Vector2.one, white, 0.35f);
        // The brushed metal is neutral grey; these keep the kit colour as the tint.
        CasinoTextureBuilder.Variant(Mat, Tex, "M_BrushedBrass", kit.brassGold, "T_BrushedMetal", Vector2.one, null, 0.7f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_BrushedSteel", kit.steelCool, "T_BrushedMetal", Vector2.one, null, 0.7f);
        CasinoTextureBuilder.Variant(Mat, Tex, "M_SlotCabinet", kit.chipBlack, "T_BrushedMetal", Vector2.one, null, 0.55f);

        // Frosted lamp glass that glows. Emission is visual only (EmissiveIsBlack), so baked lighting is unchanged.
        var glass = CasinoTextureBuilder.Variant(Mat, Tex, "M_LampGlass", kit.plasterCream, "-", Vector2.one, new Color(1f, 0.95f, 0.85f), 0.8f);
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", LampGlow);
        glass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        EditorUtility.SetDirty(glass);
        return true;
    }

    /// <summary>Textures the details in the open scene(s). Returns how many parts changed.</summary>
    public static int ApplyTo(SharedStyleKit kit)
    {
        var m = new Dictionary<string, Material>();
        foreach (var name in new[] { "M_CeilingCoffer", "M_Rosewood", "M_DarkWood", "M_BarTop", "M_TableFelt", "M_Leather",
                     "M_BrushedBrass", "M_BrushedSteel", "M_SlotCabinet", "M_LampGlass", "M_WoodPanel" })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Mat + name + ".mat");
            if (mat == null) return 0; // not built (textures missing): Build already warned
            m[name] = mat;
        }

        // Which kit material a part started with, also after an earlier Apply (so re-running updates tiling).
        var kitMaterials = new HashSet<Material> { kit.woodDark, kit.brassGold, kit.chipBlack, kit.fabricBurgundy, kit.feltGreen, kit.steelCool, kit.plasterCream };
        var origin = new Dictionary<Material, Material>
        {
            { m["M_CeilingCoffer"], kit.woodDark }, { m["M_Rosewood"], kit.brassGold }, { m["M_DarkWood"], kit.woodDark },
            { m["M_BarTop"], kit.chipBlack }, { m["M_TableFelt"], kit.feltGreen }, { m["M_Leather"], kit.fabricBurgundy },
            { m["M_BrushedBrass"], kit.brassGold }, { m["M_BrushedSteel"], kit.steelCool }, { m["M_SlotCabinet"], kit.chipBlack },
            { m["M_LampGlass"], kit.plasterCream },
        };

        int count = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.sharedMaterials.Length != 1 || r.sharedMaterial == null) continue;
            if (!Allowed(r, kit, out bool table)) continue;

            var current = r.sharedMaterial;
            Material start = kitMaterials.Contains(current) ? current
                : origin.TryGetValue(current, out var o) ? o
                : current.parent != null && origin.TryGetValue(current.parent, out o) ? o
                : null;
            // The staff door leaf may already carry the wood panel (or its tiled variant).
            bool staffLeaf = r.name == "Leaf" && r.transform.parent != null && r.transform.parent.name == "Door_Casino_Staff";
            // Warm kit ceiling tiles (casino look); the security rooms' tiles ("Room..." parents) are SecurityTextureBuilder's.
            var instance = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            bool ceilingTile = instance != null && PrefabUtility.GetCorrespondingObjectFromSource(instance) == kit.ceilingTile && !InRoom(r.transform);
            if (staffLeaf) start = kit.steelCool;
            if (ceilingTile && start == kit.woodDark && (current == m["M_CeilingCoffer"] || current.parent == m["M_CeilingCoffer"])) start = kit.plasterCream;
            if (start == null) continue;
            if (table && !TableParts.Contains(r.name)) continue;

            var (target, repeat) = ceilingTile && start == kit.plasterCream ? (m["M_CeilingCoffer"], CofferRepeat)
                : Choose(r.name, start, staffLeaf, kit, m);
            if (target == null) continue;
            var fitted = target == m["M_LampGlass"] ? target : CasinoTextureBuilder.FitToFace(target, r, repeat, TiledFolder);
            if (current == fitted) continue;
            Undo.RecordObject(r, "Apply casino textures");
            r.sharedMaterial = fitted;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            count++;
        }
        return count;
    }

    static (Material, Vector2) Choose(string name, Material start, bool staffLeaf, SharedStyleKit kit, Dictionary<string, Material> m)
    {
        if (start == kit.woodDark)
        {
            if (name == "Casino_Ceiling") return (m["M_CeilingCoffer"], CofferRepeat);
            if (name.StartsWith("Casino_WallPilaster") || name == "Top" || name == "Pedestal" || name == "Shoe" || name == "Bar") return (m["M_DarkWood"], DarkWoodRepeat);
        }
        else if (start == kit.brassGold)
        {
            if (name.StartsWith("Casino_CeilingBeam") || name.StartsWith("Casino_CeilingCrossbeam")) return (m["M_Rosewood"], RosewoodRepeat);
            if (name == "Rail") return (m["M_Leather"], LeatherRepeat); // padded armrest round the table edge
            return (m["M_BrushedBrass"], MetalRepeat);
        }
        else if (start == kit.chipBlack)
        {
            if (name.StartsWith("Casino_SlotCabinet") || name.StartsWith("Casino_FeatureSignBacking")) return (m["M_SlotCabinet"], MetalRepeat);
            if (name.StartsWith("Casino_BarTop")) return (m["M_BarTop"], DarkWoodRepeat);
        }
        else if (start == kit.fabricBurgundy)
        {
            if (name != "PictureCanvas") return (m["M_Leather"], LeatherRepeat); // seats, velvet ropes, VIP door inset
        }
        else if (start == kit.feltGreen)
        {
            if (name == "Felt") return (m["M_TableFelt"], FeltRepeat);
        }
        else if (start == kit.steelCool)
        {
            if (staffLeaf) return (m["M_WoodPanel"], WoodPanelRepeat);
            if (name == "Base" || name == "Stem") return (m["M_BrushedSteel"], MetalRepeat);
        }
        else if (start == kit.plasterCream)
        {
            if (name.Contains("ChandelierLamp") || name == "SconceLamp") return (m["M_LampGlass"], Vector2.one);
        }
        return (null, default);
    }

    static bool InRoom(Transform t)
    {
        for (; t != null; t = t.parent)
            if (t.name.StartsWith("Room")) return true;
        return false;
    }

    // Loose scene objects, and parts of style-kit or Environment prefab instances (not the player rig). Slot A's table
    // prefabs are allowed with table = true (only TableParts). Everything else (NPCs, puzzles, samples) is skipped.
    static bool Allowed(Renderer r, SharedStyleKit kit, out bool table)
    {
        table = false;
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
        if (root == null) return true;
        if (PrefabUtility.GetCorrespondingObjectFromSource(root) == kit.playerRig) return false;
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        if (path.StartsWith("Assets/Content/Tables/")) { table = true; return true; }
        return path.StartsWith("Assets/Content/StyleKit/Prefabs/") || path.StartsWith("Assets/Content/Environment/");
    }
}
