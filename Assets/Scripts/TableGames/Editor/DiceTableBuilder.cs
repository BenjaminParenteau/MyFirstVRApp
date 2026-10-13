using HighStakes.Environment;
using HighStakes.TableGames.Dice;
using TMPro;
using UnityEditor;
using UnityEngine;
using static TableKit;

/// <summary>
/// Builds the Dice rules asset (only if missing, so tuned rules survive) and the table prefab (always rebuilt) from
/// primitives and the shared style kit: the wager row nearest the player, the target controls and readouts above it,
/// the 0 to 100 bar beyond them, and the dealer board behind. The player stands on the -Z side facing +Z.
///
/// Re-running overwrites the prefab, so change the table here, not in the prefab.
/// </summary>
public static class DiceTableBuilder
{
    public const string PrefabPath = "Assets/Content/Tables/Prefabs/Tables_DiceTable.prefab";
    const string RulesPath = "Assets/Content/Tables/Data/DiceRules.asset";

    const float WagerZ = -0.33f;
    const float TargetZ = -0.17f;
    const float CaptionZ = -0.105f;
    const float ReadoutX = 0.48f;
    const float BarZ = 0.12f;
    const float BarLength = 0.9f;

    [MenuItem("High Stakes/Tables/Build Dice Table")]
    public static void Build()
    {
        var prefab = BuildPrefab();
        if (prefab == null) return;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Dice] Built " + PrefabPath + ".");
    }

    /// <summary>Builds the prefab in the open scene, saves it and removes the scene copy. Null if the style kit is missing.</summary>
    public static GameObject BuildPrefab()
    {
        var kit = Kit(out var layer);
        if (kit == null) return null;

        var rules = GetOrCreateRules();
        var root = new GameObject("Tables_DiceTable");
        var t = root.transform;
        var presenter = root.AddComponent<DicePresenter>();
        SetRef(presenter, "rules", rules);

        Body(kit, t, 1.6f, 0.9f);

        // Wager row nearest the player: halve, amount, double, chip balance, ROLL.
        var white = Color.white;
        SetRef(presenter, "halveButton", Button(kit.chipBlack, t, "HalveWager", -0.5f, WagerZ, 0.1f, "1/2", white, layer, out _));
        SetRef(presenter, "wagerText", FlatText(t, "WagerText", -0.36f, WagerZ, new Vector2(0.16f, 0.06f), 0.6f));
        SetRef(presenter, "doubleButton", Button(kit.chipBlack, t, "DoubleWager", -0.22f, WagerZ, 0.1f, "2x", white, layer, out _));
        SetRef(presenter, "balanceText", FlatText(t, "BalanceText", 0f, WagerZ, new Vector2(0.3f, 0.05f), 0.45f));
        SetRef(presenter, "rollButton", Button(kit.brassGold, t, "Roll", 0.36f, WagerZ, 0.24f, "ROLL", kit.chipBlack.color, layer, out _));

        // Target row: multiplier, - target +, win chance, each under a brass caption. Only the steps repeat while held.
        var lower = Button(kit.chipBlack, t, "LowerTarget", -0.14f, TargetZ, 0.1f, "-", white, layer, out _);
        var raise = Button(kit.chipBlack, t, "RaiseTarget", 0.14f, TargetZ, 0.1f, "+", white, layer, out _);
        RepeatWhileHeld(lower);
        RepeatWhileHeld(raise);
        SetRef(presenter, "lowerButton", lower);
        SetRef(presenter, "raiseButton", raise);
        SetRef(presenter, "targetText", FlatText(t, "TargetText", 0f, TargetZ, new Vector2(0.14f, 0.06f), 0.6f));
        SetRef(presenter, "multiplierText", FlatText(t, "MultiplierText", -ReadoutX, TargetZ, new Vector2(0.24f, 0.06f), 0.6f));
        SetRef(presenter, "winChanceText", FlatText(t, "WinChanceText", ReadoutX, TargetZ, new Vector2(0.24f, 0.06f), 0.6f));
        Caption(kit, t, "MultiplierCaption", -ReadoutX, "MULTIPLIER");
        Caption(kit, t, "TargetCaption", 0f, "ROLL OVER");
        Caption(kit, t, "WinChanceCaption", ReadoutX, "WIN CHANCE");

        SetRef(presenter, "bar", Bar(kit, t));

        SetRef(presenter, "hologram", Hologram(kit, t, 1.6f, 0.43f, "DICE", "SET A TARGET, THEN ROLL")); // full table width, on the dealer edge
        Probes(t, new[] { -0.75f, 0f, 0.75f }, new[] { -0.45f, 0f, 0.45f });
        return SavePrefab(root, PrefabPath);
    }

    static DiceRules GetOrCreateRules()
    {
        var rules = AssetDatabase.LoadAssetAtPath<DiceRules>(RulesPath);
        if (rules != null) return rules;
        EnsureFolder(System.IO.Path.GetDirectoryName(RulesPath).Replace('\\', '/'));
        rules = ScriptableObject.CreateInstance<DiceRules>();
        AssetDatabase.CreateAsset(rules, RulesPath);
        return rules;
    }

    static void Caption(SharedStyleKit kit, Transform table, string name, float x, string text)
    {
        var caption = FlatText(table, name, x, CaptionZ, new Vector2(0.22f, 0.03f), 0.3f);
        caption.text = text;
        caption.color = kit.brassGold.color;
        caption.fontStyle = FontStyles.Bold;
    }

    /// <summary>
    /// The 0 to 100 bar on a wooden backing: red and green tracks the presenter resizes at the target, ticks every 25
    /// beyond it, and the result marker on the player's side with its number lying in front of it.
    /// </summary>
    static DiceBar Bar(SharedStyleKit kit, Transform table)
    {
        var group = new GameObject("Bar").transform;
        group.SetParent(table, false);
        group.localPosition = new Vector3(0f, Felt, BarZ);
        var bar = group.gameObject.AddComponent<DiceBar>();

        const float backingTop = 0.01f;
        Part(kit.woodDark, group, "Backing", PrimitiveType.Cube, new Vector3(0f, backingTop / 2f, -0.0325f), new Vector3(BarLength + 0.1f, backingTop, 0.215f), true, false);
        var trackSize = new Vector3(BarLength / 2f, 0.012f, 0.05f);
        var lose = Part(kit.chipRed, group, "TrackLose", PrimitiveType.Cube, new Vector3(-BarLength / 4f, backingTop + 0.006f, 0f), trackSize, false, false);
        var win = Part(kit.chipGreen, group, "TrackWin", PrimitiveType.Cube, new Vector3(BarLength / 4f, backingTop + 0.006f, 0f), trackSize, false, false);

        foreach (var tick in new[] { 0, 25, 50, 75, 100 })
        {
            var label = Text(group, "Tick" + tick, new Vector3(-BarLength / 2f + tick / 100f * BarLength, backingTop + 0.001f, 0.045f),
                Quaternion.Euler(90f, 0f, 0f), new Vector2(0.06f, 0.025f), 0.05f, 0.25f, Color.white);
            label.text = tick.ToString();
        }

        var marker = new GameObject("Marker").transform;
        marker.SetParent(group, false);
        marker.localPosition = new Vector3(0f, 0.03f, -0.07f);
        var cube = Part(kit.chipWhite, marker, "Cube", PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.035f, false, false);
        var markerLabel = Text(marker, "Label", new Vector3(0f, backingTop + 0.001f - 0.03f, -0.04f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(0.1f, 0.04f), 0.05f, 0.4f, Color.white);
        markerLabel.fontStyle = FontStyles.Bold;

        SetRef(bar, "trackLose", lose.transform);
        SetRef(bar, "trackWin", win.transform);
        SetRef(bar, "marker", marker);
        SetRef(bar, "markerCube", cube.GetComponent<Renderer>());
        SetRef(bar, "markerLabel", markerLabel);
        Set(bar, "length", p => p.floatValue = BarLength);
        SetRef(bar, "idleMaterial", kit.chipWhite);
        SetRef(bar, "winMaterial", kit.chipGreen);
        SetRef(bar, "loseMaterial", kit.chipRed);
        Set(bar, "idleColor", p => p.colorValue = Color.white);
        Set(bar, "winColor", p => p.colorValue = kit.safeGreen);
        Set(bar, "loseColor", p => p.colorValue = kit.alertRed);
        return bar;
    }
}
