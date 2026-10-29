using HighStakes.Environment;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Textured materials for Slot C's props: leather (the notebook covers) and paper (its pages and the staff puzzle's
/// shift note). Both are Material Variants of kit materials (StyleGuide: no new base materials), made with
/// CasinoTextureBuilder's variant helper. Leather reuses the casino's T_Leather (no extra texture memory); paper is one
/// small tileable texture generated here. Menu High Stakes > UI > Apply UI Textures puts them on the existing prefabs
/// without touching any scene. Slot C (Pak) owns this.
/// </summary>
public static class UITextures
{
    const string MaterialFolder = "Assets/Content/UI/Materials/";
    const string TextureFolder = "Assets/Content/UI/Textures/";
    const string CasinoTextureFolder = "Assets/Content/Environment/Casino/Textures/";
    const string PaperPath = TextureFolder + "T_Paper_Albedo.png";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string NotebookPath = "Assets/Content/UI/UI_Notebook.prefab";
    const string ShiftNotePath = "Assets/Content/UI/Puzzles/Puzzle_ShiftNote.prefab";

    const int PaperSize = 512;

    /// <summary>Burgundy leather. Tiling keeps the casino leather's grain to scale on a 15 x 21 cm cover.</summary>
    public static Material Leather(SharedStyleKit kit)
    {
        EnsureFolders();
        return CasinoTextureBuilder.Variant(MaterialFolder, CasinoTextureFolder, "M_UI_Leather", kit.fabricBurgundy, "T_Leather",
            new Vector2(0.3f, 0.3f), Color.white, 0.35f);
    }

    /// <summary>Cream paper with faint fibres.</summary>
    public static Material Paper(SharedStyleKit kit)
    {
        EnsureFolders();
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PaperPath) == null)
            WritePaperTexture();
        return CasinoTextureBuilder.Variant(MaterialFolder, TextureFolder, "M_UI_Paper", kit.plasterCream, "T_Paper",
            Vector2.one, Color.white, 0.05f);
    }

    [MenuItem("High Stakes/UI/Apply UI Textures")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[UITextures] Stop Play mode first, then run Apply UI Textures again.");
            return;
        }
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null)
        {
            Debug.LogError("[UITextures] Style kit not found at " + KitPath + ".");
            return;
        }

        var leather = Leather(kit);
        var paper = Paper(kit);
        AssetDatabase.SaveAssets();

        Patch(NotebookPath, (name, renderer) =>
        {
            if (name == "BackCover" || name == "FrontCover" || name == "Spine") renderer.sharedMaterial = leather;
            else if (name == "Pages") renderer.sharedMaterial = paper;
        });
        Patch(ShiftNotePath, (name, renderer) =>
        {
            if (name == "Paper") renderer.sharedMaterial = paper;
        });
        AssetDatabase.SaveAssets();
        Debug.Log("[UITextures] Leather and paper applied to UI_Notebook and Puzzle_ShiftNote (prefabs only; scenes update from them).");
    }

    // Swaps materials on one prefab's parts in place, so scene instances pick it up without editing any scene.
    static void Patch(string prefabPath, System.Action<string, Renderer> swap)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning("[UITextures] " + prefabPath + " not found; build it first.");
            return;
        }
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            swap(renderer.gameObject.name, renderer);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Tileable cream paper: soft blotches, fine grain and a few hundred faint fibres. Fixed seed, so reruns write the
    // same file.
    static void WritePaperTexture()
    {
        var rng = new System.Random(1207);
        var lattice = new float[64 * 64];
        for (int i = 0; i < lattice.Length; i++) lattice[i] = (float)rng.NextDouble();

        var cream = new Color(0.93f, 0.90f, 0.82f);
        var shade = new float[PaperSize * PaperSize];
        for (int y = 0; y < PaperSize; y++)
            for (int x = 0; x < PaperSize; x++)
            {
                float u = (float)x / PaperSize, v = (float)y / PaperSize;
                float blotch = Noise(lattice, u, v, 4) * 0.5f + Noise(lattice, u, v, 8) * 0.3f + Noise(lattice, u, v, 16) * 0.2f;
                float grain = Noise(lattice, u, v, 64);
                shade[y * PaperSize + x] = (blotch - 0.5f) * 0.08f + (grain - 0.5f) * 0.04f;
            }

        // Fibres: short, slightly darker or lighter strokes, wrapped at the edges so the texture still tiles
        for (int f = 0; f < 600; f++)
        {
            float x = (float)rng.NextDouble() * PaperSize, y = (float)rng.NextDouble() * PaperSize;
            float angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            int length = 6 + rng.Next(18);
            float tone = rng.NextDouble() < 0.6 ? -0.035f : 0.025f;
            for (int s = 0; s < length; s++)
            {
                int px = Wrap((int)(x + Mathf.Cos(angle) * s)), py = Wrap((int)(y + Mathf.Sin(angle) * s));
                shade[py * PaperSize + px] += tone;
            }
        }

        var texture = new Texture2D(PaperSize, PaperSize, TextureFormat.RGB24, false);
        var pixels = new Color[PaperSize * PaperSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            float d = shade[i];
            pixels[i] = new Color(cream.r + d, cream.g + d, cream.b + d * 1.1f);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        System.IO.File.WriteAllBytes(PaperPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(PaperPath); // UITextureImporter sets mipmaps, repeat and Android ASTC
    }

    // Smooth value noise with <paramref name="cells"/> cells across, periodic so it tiles.
    static float Noise(float[] lattice, float u, float v, int cells)
    {
        float x = u * cells, y = v * cells;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = Smooth(x - x0), ty = Smooth(y - y0);
        float a = Lattice(lattice, x0, y0, cells), b = Lattice(lattice, x0 + 1, y0, cells);
        float c = Lattice(lattice, x0, y0 + 1, cells), d = Lattice(lattice, x0 + 1, y0 + 1, cells);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static float Lattice(float[] lattice, int x, int y, int cells)
    {
        x = ((x % cells) + cells) % cells;
        y = ((y % cells) + cells) % cells;
        return lattice[(y * 64 + x) % lattice.Length];
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    static int Wrap(int p) => ((p % PaperSize) + PaperSize) % PaperSize;

    static void EnsureFolders()
    {
        WristDisplayBuilder.EnsureFolder(MaterialFolder.TrimEnd('/'));
        WristDisplayBuilder.EnsureFolder(TextureFolder.TrimEnd('/'));
    }
}
