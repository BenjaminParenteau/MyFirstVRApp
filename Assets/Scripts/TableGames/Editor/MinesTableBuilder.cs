using HighStakes.TableGames.Mines;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static TableKit;

/// <summary>
/// Builds the Mines rules asset (only if missing, so tuned rules survive) and the table prefab (always rebuilt) from
/// primitives and the shared style kit: the 5 by 5 grid toward the dealer, the mine count on its left, CASH OUT on its
/// right, the multiplier readouts under it, and the wager row nearest the player. The player stands on the -Z side
/// facing +Z. Re-running overwrites the prefab, so change the table here, not in the prefab.
/// </summary>
public static class MinesTableBuilder
{
    public const string PrefabPath = "Assets/Content/Tables/Prefabs/Tables_MinesTable.prefab";
    const string RulesPath = "Assets/Content/Tables/Data/MinesRules.asset";

    const int Side = 5;
    const float TileSize = 0.075f;
    const float TilePitch = 0.085f;  // leaves a 1 cm gap, so a ray never lands between two tiles' meaning
    const float GridZ = 0.17f;       // centre row; the far row ends just inside the felt
    const float SideX = 0.45f;       // mine count to the left of the grid, CASH OUT to the right
    const float WagerZ = -0.33f;

    [MenuItem("High Stakes/Tables/Build Mines Table")]
    public static void Build()
    {
        var prefab = BuildPrefab();
        if (prefab == null) return;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Mines] Built " + PrefabPath + ".");
    }

    /// <summary>Builds the prefab in the open scene, saves it and removes the scene copy. Null if the style kit is missing.</summary>
    public static GameObject BuildPrefab()
    {
        var kit = Kit(out var layer);
        if (kit == null) return null;

        var rules = GetOrCreateRules();
        var root = new GameObject("Tables_MinesTable");
        var t = root.transform;
        var presenter = root.AddComponent<MinesPresenter>();
        SetRef(presenter, "rules", rules);

        Body(kit, t, 1.6f, 0.9f);
        var white = Color.white;
        var black = kit.chipBlack.color;

        // The grid. Tile 0 is the far left corner, reading left to right and toward the player.
        var tiles = new Object[Side * Side];
        var bodies = new Object[tiles.Length];
        var labels = new Object[tiles.Length];
        for (var i = 0; i < tiles.Length; i++)
        {
            var x = (i % Side - (Side - 1) / 2f) * TilePitch;
            var z = GridZ + ((Side - 1) / 2f - i / Side) * TilePitch;
            var tile = Button(kit.chipBlack, t, "Tile" + i, x, z, TileSize, "", white, layer, out var label, TileSize);
            tiles[i] = tile;
            bodies[i] = tile.transform.Find("Body").GetComponent<Renderer>();
            labels[i] = label;
        }
        SetRefs(presenter, "tiles", tiles);
        SetRefs(presenter, "tileBodies", bodies);
        SetRefs(presenter, "tileLabels", labels);
        SetRef(presenter, "hiddenMaterial", kit.chipBlack);
        SetRef(presenter, "gemMaterial", kit.screenGlow);
        SetRef(presenter, "mineMaterial", kit.chipRed);
        Set(presenter, "gemTextColor", p => p.colorValue = black);
        Set(presenter, "mineTextColor", p => p.colorValue = white);

        // Left of the grid: how many mines, with fewer and more (hold to step quickly).
        SetRef(presenter, "minesText", FlatText(t, "MinesText", -SideX, GridZ + 0.08f, new Vector2(0.24f, 0.06f), 0.6f));
        var fewer = Button(kit.chipBlack, t, "FewerMines", -SideX - 0.07f, GridZ - 0.04f, 0.1f, "-", white, layer, out _);
        var more = Button(kit.chipBlack, t, "MoreMines", -SideX + 0.07f, GridZ - 0.04f, 0.1f, "+", white, layer, out _);
        RepeatWhileHeld(fewer);
        RepeatWhileHeld(more);
        SetRef(presenter, "fewerButton", fewer);
        SetRef(presenter, "moreButton", more);

        // Right of the grid: CASH OUT, within reach of the hand that just turned a tile.
        SetRef(presenter, "cashOutButton", Button(kit.brassGold, t, "CashOut", SideX, GridZ, 0.26f, "CASH OUT", black, layer, out var cashOutLabel, 0.1f));
        SetRef(presenter, "cashOutLabel", cashOutLabel);

        // Under the grid, nearest readable: what cashing out pays now and what the next safe tile would.
        SetRef(presenter, "multiplierText", FlatText(t, "MultiplierText", 0f, -0.1f, new Vector2(0.5f, 0.05f), 0.45f));
        SetRef(presenter, "nextText", FlatText(t, "NextText", 0f, -0.18f, new Vector2(0.6f, 0.045f), 0.4f));

        // Wager row nearest the player: halve, amount, double, chip balance, START.
        SetRef(presenter, "halveButton", Button(kit.chipBlack, t, "HalveWager", -0.5f, WagerZ, 0.1f, "1/2", white, layer, out _));
        SetRef(presenter, "wagerText", FlatText(t, "WagerText", -0.36f, WagerZ, new Vector2(0.16f, 0.06f), 0.6f));
        SetRef(presenter, "doubleButton", Button(kit.chipBlack, t, "DoubleWager", -0.22f, WagerZ, 0.1f, "2x", white, layer, out _));
        SetRef(presenter, "balanceText", FlatText(t, "BalanceText", 0f, WagerZ, new Vector2(0.3f, 0.05f), 0.45f));
        SetRef(presenter, "startButton", Button(kit.brassGold, t, "Start", 0.36f, WagerZ, 0.24f, "START", black, layer, out var startLabel));
        SetRef(presenter, "startLabel", startLabel);
        Set(presenter, "unavailable", p => p.colorValue = new Color(black.r, black.g, black.b, 0.25f));

        SetRef(presenter, "hologram", Hologram(kit, t, 1.6f, 0.43f, "MINES", "PICK YOUR MINES, THEN START")); // full table width, on the dealer edge
        Probes(t, new[] { -0.75f, 0f, 0.75f }, new[] { -0.45f, 0f, 0.45f });
        return SavePrefab(root, PrefabPath);
    }

    static MinesRules GetOrCreateRules()
    {
        var rules = AssetDatabase.LoadAssetAtPath<MinesRules>(RulesPath);
        if (rules != null) return rules;
        EnsureFolder(System.IO.Path.GetDirectoryName(RulesPath).Replace('\\', '/'));
        rules = ScriptableObject.CreateInstance<MinesRules>();
        AssetDatabase.CreateAsset(rules, RulesPath);
        return rules;
    }
}
