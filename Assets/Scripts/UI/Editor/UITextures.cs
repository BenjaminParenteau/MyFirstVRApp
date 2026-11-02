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
    const string PageEdgePath = TextureFolder + "T_PageEdge_Albedo.png";
    const string PagePath = TextureFolder + "T_NotebookPage_Albedo.png";
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

    /// <summary>The notebook's dark-brown ink, for text colour (lines use <see cref="Ink"/>).</summary>
    public static readonly Color InkColor = new Color(0.24f, 0.16f, 0.09f);

    /// <summary>Dark-brown ink for the notebook's lines: page frames, underlines, the map.</summary>
    public static Material Ink(SharedStyleKit kit)
    {
        EnsureFolders();
        return CasinoTextureBuilder.Variant(MaterialFolder, TextureFolder, "M_UI_Ink", kit.chipBlack, "T_UI_Ink",
            Vector2.one, InkColor, 0.1f);
    }

    /// <summary>Pencil-brown for the notebook's faint boxes and the outlines of rooms not yet visited on the map.</summary>
    public static Material Rule(SharedStyleKit kit)
    {
        EnsureFolders();
        return CasinoTextureBuilder.Variant(MaterialFolder, TextureFolder, "M_UI_Rule", kit.plasterCream, "T_UI_Rule",
            Vector2.one, new Color(0.7f, 0.55f, 0.38f), 0.05f);
    }

    /// <summary>
    /// The notebook's page surface: tan kraft paper with a dashed border and small dots at the corners and edge
    /// midpoints, all in one generated texture (one quad per page instead of dozens of line pieces).
    /// </summary>
    public static Material Page(SharedStyleKit kit)
    {
        EnsureFolders();
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PagePath) == null)
            WritePageTexture();
        return CasinoTextureBuilder.Variant(MaterialFolder, TextureFolder, "M_UI_Page", kit.plasterCream, "T_NotebookPage",
            Vector2.one, Color.white, 0.05f);
    }

    /// <summary>The side of the paper block: thin cream and tan stripes, so it reads as a stack of pages.</summary>
    public static Material PageEdge(SharedStyleKit kit)
    {
        EnsureFolders();
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PageEdgePath) == null)
            WritePageEdgeTexture();
        return CasinoTextureBuilder.Variant(MaterialFolder, TextureFolder, "M_UI_PageEdge", kit.plasterCream, "T_PageEdge",
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

    // One page, 512 x 728 (the page's 14.2 x 20.2 cm): kraft tan with soft blotches and grain, darkening toward the
    // edges, a dashed border 20 px in and 4 px dots at the corners and edge midpoints. Fixed seed, so reruns match.
    static void WritePageTexture()
    {
        const int w = 512, h = 728, inset = 20, dotInset = 9;
        var rng = new System.Random(2718);
        var lattice = new float[64 * 64];
        for (int i = 0; i < lattice.Length; i++) lattice[i] = (float)rng.NextDouble();

        var kraft = new Color(0.86f, 0.69f, 0.5f);
        var dash = new Color(0.56f, 0.39f, 0.24f);
        var dot = new Color(0.42f, 0.28f, 0.17f);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w, v = (float)y / h;
                float blotch = Noise(lattice, u, v, 4) * 0.5f + Noise(lattice, u, v, 8) * 0.3f + Noise(lattice, u, v, 16) * 0.2f;
                float d = (blotch - 0.5f) * 0.07f + (Noise(lattice, u, v, 64) - 0.5f) * 0.035f;
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 16) d -= 0.07f * (1f - edge / 16f);
                pixels[y * w + x] = new Color(kraft.r + d, kraft.g + d, kraft.b + d * 0.8f);
            }

        // Dashed border: 8 px dashes, 6 px gaps, 2 px thick
        for (int i = inset; i < w - inset; i++)
            if ((i - inset) % 14 < 8)
                for (int k = 0; k < 2; k++)
                {
                    pixels[(inset + k) * w + i] = dash;
                    pixels[(h - 1 - inset - k) * w + i] = dash;
                }
        for (int j = inset; j < h - inset; j++)
            if ((j - inset) % 14 < 8)
                for (int k = 0; k < 2; k++)
                {
                    pixels[j * w + inset + k] = dash;
                    pixels[j * w + w - 1 - inset - k] = dash;
                }

        // Dots at the corners and edge midpoints
        int[] xs = { dotInset, w / 2, w - 1 - dotInset }, ys = { dotInset, h / 2, h - 1 - dotInset };
        foreach (int cx in xs)
            foreach (int cy in ys)
            {
                if (cx == w / 2 && cy == h / 2) continue; // not in the middle of the page
                for (int dy = -2; dy < 2; dy++)
                    for (int dx = -2; dx < 2; dx++)
                        pixels[(cy + dy) * w + cx + dx] = dot;
            }

        var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
        texture.SetPixels(pixels);
        texture.Apply();
        System.IO.File.WriteAllBytes(PagePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(PagePath); // UITextureImporter sets mipmaps and Android ASTC (512 max there)
    }

    // 64 x 64 page edges: rows of cream paper with a darker gap every few pixels (rows run along the page, so on a quad
    // standing on the book's side they read as stacked sheets), slightly uneven so it doesn't look printed. Fixed seed.
    static void WritePageEdgeTexture()
    {
        const int size = 64;
        var rng = new System.Random(4471);
        var cream = new Color(0.93f, 0.90f, 0.82f);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float row = y % 3 == 2 ? -0.12f : (float)rng.NextDouble() * 0.04f - 0.02f; // every third row is a gap
            for (int x = 0; x < size; x++)
            {
                float d = row + (float)rng.NextDouble() * 0.02f - 0.01f;
                pixels[y * size + x] = new Color(cream.r + d, cream.g + d, cream.b + d * 1.1f);
            }
        }
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        texture.SetPixels(pixels);
        texture.Apply();
        System.IO.File.WriteAllBytes(PageEdgePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(PageEdgePath); // UITextureImporter sets mipmaps, repeat and Android ASTC
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
