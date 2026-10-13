using System;
using HighStakes.Environment;
using HighStakes.TableGames.Plinko;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static TableKit;

/// <summary>
/// Builds the Plinko rules asset (only if missing, so tuned paytables survive) and the table prefab (always rebuilt)
/// from primitives and the shared style kit. The game is virtual: the peg board is drawn inside a tall dealer hologram,
/// under its text, and the felt only holds the controls. The player stands on the -Z side facing +Z.
/// </summary>
public static class PlinkoTableBuilder
{
    public const string PrefabPath = "Assets/Content/Tables/Prefabs/Tables_PlinkoTable.prefab";
    const string RulesPath = "Assets/Content/Tables/Data/PlinkoRules.asset";
    const string HologramEdgePath = "Assets/Content/UI/UI_HologramEdge.mat";

    const float Width = 1.6f;
    const float Depth = 0.9f;
    const float WagerZ = -0.33f;
    const float BoardZ = -0.17f;
    /// <summary>The hologram's text takes the top TableKit.BoardHeight of this; the pegs fill the rest.</summary>
    const float HologramHeight = 1.15f;

    [MenuItem("High Stakes/Tables/Build Plinko Table")]
    public static void Build()
    {
        var prefab = BuildPrefab();
        if (prefab == null) return;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Plinko] Built " + PrefabPath + ".");
    }

    /// <summary>Builds the prefab in the open scene, saves it and removes the scene copy. Null if the style kit is missing.</summary>
    public static GameObject BuildPrefab()
    {
        var kit = Kit(out var layer);
        if (kit == null) return null;
        var pegMaterial = AssetDatabase.LoadAssetAtPath<Material>(HologramEdgePath);
        if (pegMaterial == null)
            throw new InvalidOperationException("[Plinko] " + HologramEdgePath + " is missing; run High Stakes > UI > Build Wrist Display first.");

        var rules = GetOrCreateRules();
        var root = new GameObject("Tables_PlinkoTable");
        var t = root.transform;
        var presenter = root.AddComponent<PlinkoPresenter>();
        SetRef(presenter, "rules", rules);

        Body(kit, t, Width, Depth);
        var white = Color.white;

        // Board choice: rows down and up around the count, then the three risks.
        SetRef(presenter, "fewerRowsButton", Button(kit.chipBlack, t, "FewerRows", -0.5f, BoardZ, 0.1f, "-", white, layer, out var fewer));
        SetRef(presenter, "fewerRowsLabel", fewer);
        SetRef(presenter, "rowsText", FlatText(t, "RowsText", -0.36f, BoardZ, new Vector2(0.16f, 0.06f), 0.5f));
        SetRef(presenter, "moreRowsButton", Button(kit.chipBlack, t, "MoreRows", -0.22f, BoardZ, 0.1f, "+", white, layer, out var more));
        SetRef(presenter, "moreRowsLabel", more);
        var riskNames = new[] { "LOW", "MED", "HIGH" };
        var riskButtons = new Object[riskNames.Length];
        var riskLabels = new Object[riskNames.Length];
        for (var i = 0; i < riskNames.Length; i++)
        {
            riskButtons[i] = Button(kit.chipBlack, t, "Risk" + (PlinkoRisk)i, 0.12f + i * 0.24f, BoardZ, 0.2f, riskNames[i], white, layer, out var label);
            riskLabels[i] = label;
        }
        SetRefs(presenter, "riskButtons", riskButtons);
        SetRefs(presenter, "riskLabels", riskLabels);
        Set(presenter, "unselected", p => p.colorValue = new Color(1f, 1f, 1f, 0.3f));

        // Wager row nearest the player: halve, amount, double, chip balance, DROP.
        SetRef(presenter, "halveButton", Button(kit.chipBlack, t, "HalveWager", -0.5f, WagerZ, 0.1f, "1/2", white, layer, out _));
        SetRef(presenter, "wagerText", FlatText(t, "WagerText", -0.36f, WagerZ, new Vector2(0.16f, 0.06f), 0.6f));
        SetRef(presenter, "doubleButton", Button(kit.chipBlack, t, "DoubleWager", -0.22f, WagerZ, 0.1f, "2x", white, layer, out _));
        SetRef(presenter, "balanceText", FlatText(t, "BalanceText", 0f, WagerZ, new Vector2(0.3f, 0.05f), 0.45f));
        SetRef(presenter, "dropButton", Button(kit.brassGold, t, "Drop", 0.36f, WagerZ, 0.24f, "DROP", kit.chipBlack.color, layer, out _));

        var hologram = Hologram(kit, t, Width, Depth / 2f - 0.02f, "PLINKO", "PICK ROWS AND RISK, THEN DROP", HologramHeight);
        SetRef(presenter, "hologram", hologram);
        BuildBoard(kit, t.Find("Hologram/Board"), presenter, pegMaterial);

        Probes(t, new[] { -0.75f, 0f, 0.75f }, new[] { -0.45f, 0f, 0.45f });
        return SavePrefab(root, PrefabPath);
    }

    // Everything the presenter lays out at runtime, parented under the hologram's board so it rises and sinks with it:
    // an empty peg mesh, a multiplier label per bucket of the largest board, and the ball it copies for each drop.
    static void BuildBoard(SharedStyleKit kit, Transform board, PlinkoPresenter presenter, Material pegMaterial)
    {
        if (board == null) throw new InvalidOperationException("[Plinko] TableKit.Hologram no longer makes Hologram/Board.");
        SetRef(presenter, "board", board);

        var pegs = new GameObject("Pegs");
        pegs.transform.SetParent(board, false);
        var renderer = pegs.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = pegMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        SetRef(presenter, "pegs", pegs.AddComponent<MeshFilter>());

        var labels = new Object[PlinkoPaytable.MaxRows + 1];
        for (var k = 0; k < labels.Length; k++)
        {
            var label = Text(board, "Bucket" + k, Vector3.zero, Quaternion.identity, new Vector2(0.06f, 0.03f), 0.05f, 0.35f, Color.white);
            label.enableAutoSizing = false; // one size for every bucket, set by the presenter from the spacing
            label.fontStyle = FontStyles.Bold;
            labels[k] = label;
        }
        SetRefs(presenter, "bucketLabels", labels);

        var ball = Part(kit.chipWhite, board, "Ball", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.03f, false, false);
        ball.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetRef(presenter, "ballTemplate", ball.transform);

        float pegTop = HologramHeight - BoardHeight - 0.01f;
        Set(presenter, "boardWidth", p => p.floatValue = 1.1f);
        Set(presenter, "pegTop", p => p.floatValue = pegTop);
        Set(presenter, "bucketY", p => p.floatValue = 0.06f);
        Set(presenter, "highColor", p => p.colorValue = kit.brassGold.color);
        Set(presenter, "winColor", p => p.colorValue = kit.safeGreen);
        Set(presenter, "loseColor", p => p.colorValue = kit.alertRed);
    }

    static PlinkoRules GetOrCreateRules()
    {
        var rules = AssetDatabase.LoadAssetAtPath<PlinkoRules>(RulesPath);
        if (rules != null) return rules;
        EnsureFolder(System.IO.Path.GetDirectoryName(RulesPath).Replace('\\', '/'));
        rules = ScriptableObject.CreateInstance<PlinkoRules>();
        AssetDatabase.CreateAsset(rules, RulesPath);
        return rules;
    }
}
