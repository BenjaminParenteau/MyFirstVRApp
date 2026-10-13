using HighStakes.Environment;
using HighStakes.TableGames;
using HighStakes.TableGames.Roulette;
using UnityEditor;
using UnityEngine;
using static TableKit;

/// <summary>
/// Builds the European roulette table prefab from primitives and the shared style kit: the wheel bowl on the left,
/// the betting layout (0-36, columns, dozens, even-money bets) on the right, the chip and spin controls nearest the
/// player, and the dealer board behind. The player stands on the -Z side facing +Z.
/// </summary>
// ponytail: straight-up and outside bets only. Splits, streets and corners sit on the lines between numbers, which are
// too fine to aim at with a ray; add them as small targets on the cell edges if players miss them.
public static class RouletteTableBuilder
{
    public const string PrefabPath = "Assets/Content/Tables/Prefabs/Tables_RouletteTable.prefab";

    const float Width = 2.6f;
    const float Depth = 1.2f;
    const float CellW = 0.09f;   // one number's width (x)
    const float CellD = 0.085f;  // one number's depth (z)
    const float GridX = -0.25f;  // centre of the first column of numbers (1, 2, 3)
    const float GridZ = -0.05f;  // centre of the row nearest the player (1, 4, 7, ...)
    const float ControlsZ = -0.4f;
    static readonly Vector3 WheelCentre = new Vector3(-0.85f, Felt, 0.1f);

    [MenuItem("High Stakes/Tables/Build Roulette Table")]
    public static void Build()
    {
        var prefab = BuildPrefab();
        if (prefab != null) Debug.Log("[Roulette] Built " + PrefabPath + ".");
    }

    public static GameObject BuildPrefab()
    {
        var kit = Kit(out var layer);
        if (kit == null) return null;

        var root = new GameObject("Tables_RouletteTable");
        var t = root.transform;
        var presenter = root.AddComponent<RoulettePresenter>();

        // Body: static, and only the top keeps a collider, so the table stops the player without snagging hands.
        foreach (float x in new[] { -0.8f, 0.8f })
            Part(kit.woodDark, t, "Pedestal", PrimitiveType.Cylinder, new Vector3(x, 0.36f, 0f), new Vector3(0.4f, 0.36f, 0.4f), true, false);
        Part(kit.woodDark, t, "Top", PrimitiveType.Cube, new Vector3(0f, 0.735f, 0f), new Vector3(Width, 0.04f, Depth), true, true);
        Part(kit.feltGreen, t, "Felt", PrimitiveType.Cube, new Vector3(0f, Felt - 0.0025f, 0f), new Vector3(Width - 0.1f, 0.005f, Depth - 0.1f), true, false);
        Part(kit.brassGold, t, "Rail", PrimitiveType.Cube, new Vector3(0f, 0.77f, -Depth / 2f), new Vector3(Width, 0.03f, 0.04f), true, false);

        BuildWheel(kit, t, presenter);
        BuildLayout(kit, t, layer);

        // Controls nearest the player: chip size, clear, spin. Totals sit under the wheel.
        var white = Color.white;
        SetRef(presenter, "halveButton", Button(kit.chipBlack, t, "HalveChip", -0.3f, ControlsZ, 0.1f, "1/2", white, layer, out _));
        SetRef(presenter, "chipText", FlatText(t, "ChipText", -0.17f, ControlsZ, new Vector2(0.12f, 0.06f), 0.6f));
        SetRef(presenter, "doubleButton", Button(kit.chipBlack, t, "DoubleChip", -0.04f, ControlsZ, 0.1f, "2x", white, layer, out _));
        SetRef(presenter, "clearButton", Button(kit.chipBlack, t, "Clear", 0.2f, ControlsZ, 0.18f, "CLEAR", white, layer, out _));
        SetRef(presenter, "spinButton", Button(kit.brassGold, t, "Spin", 0.62f, ControlsZ, 0.3f, "SPIN", kit.chipBlack.color, layer, out _));
        SetRef(presenter, "totalText", FlatText(t, "TotalText", WheelCentre.x, -0.33f, new Vector2(0.4f, 0.05f), 0.45f));
        SetRef(presenter, "balanceText", FlatText(t, "BalanceText", WheelCentre.x, -0.43f, new Vector2(0.4f, 0.05f), 0.45f));

        SetRef(presenter, "hologram", Hologram(kit, t, Width, Depth / 2f - 0.02f, "ROULETTE", "PLACE YOUR BETS, THEN SPIN"));
        Probes(t, new[] { -1.2f, -0.6f, 0f, 0.6f, 1.2f }, new[] { -0.5f, 0f, 0.5f });
        return SavePrefab(root, PrefabPath);
    }

    // A static wooden bowl with a brass ball track, and the turning head: 37 coloured pockets clockwise from zero in
    // real wheel order, their numbers, and a brass turret. The ball rides on the base, placed by the presenter.
    static void BuildWheel(SharedStyleKit kit, Transform table, RoulettePresenter presenter)
    {
        var wheel = new GameObject("Wheel").transform;
        wheel.SetParent(table, false);
        wheel.localPosition = WheelCentre;
        Part(kit.woodDark, wheel, "Bowl", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.66f, 0.04f, 0.66f), true, false);
        Part(kit.brassGold, wheel, "Track", PrimitiveType.Cylinder, new Vector3(0f, 0.081f, 0f), new Vector3(0.6f, 0.001f, 0.6f), true, false);

        var head = new GameObject("WheelHead").transform;
        head.SetParent(wheel, false);
        head.localPosition = new Vector3(0f, 0.083f, 0f);
        Part(kit.chipBlack, head, "Disc", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.48f, 0.004f, 0.48f), false, false);
        Part(kit.brassGold, head, "Turret", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.07f, 0.02f, 0.07f), false, false);
        Part(kit.brassGold, head, "TurretTop", PrimitiveType.Sphere, new Vector3(0f, 0.045f, 0f), new Vector3(0.035f, 0.035f, 0.035f), false, false);
        for (int slot = 0; slot < RouletteWheel.Pockets; slot++)
        {
            int pocket = RouletteWheel.Order[slot];
            float angle = slot * 360f / RouletteWheel.Pockets;
            var rotation = Quaternion.Euler(0f, angle, 0f);
            var material = RouletteWheel.IsGreen(pocket) ? kit.chipGreen : RouletteWheel.IsRed(pocket) ? kit.chipRed : kit.chipBlack;
            var cell = Part(material, head, "Pocket" + pocket, PrimitiveType.Cube, rotation * new Vector3(0f, 0.004f, 0.2f), new Vector3(0.03f, 0.004f, 0.05f), false, false);
            cell.transform.localRotation = rotation;
            Part(kit.brassGold, head, "Fret", PrimitiveType.Cube, Quaternion.Euler(0f, angle + 180f / RouletteWheel.Pockets, 0f) * new Vector3(0f, 0.008f, 0.2f),
                new Vector3(0.002f, 0.01f, 0.05f), false, false).transform.localRotation = Quaternion.Euler(0f, angle + 180f / RouletteWheel.Pockets, 0f);
            var label = Text(head, "Number" + pocket, rotation * new Vector3(0f, 0.0065f, 0.2f), rotation * Quaternion.Euler(90f, 180f, 0f),
                new Vector2(0.026f, 0.02f), 0.01f, 0.2f, Color.white);
            label.text = pocket.ToString();
        }

        var ball = Part(kit.chipWhite, wheel, "Ball", PrimitiveType.Sphere, new Vector3(0f, 0.097f, 0.27f), Vector3.one * 0.018f, false, false);
        SetRef(presenter, "wheelHead", head);
        SetRef(presenter, "ball", ball.transform);
    }

    // 0 down the left, 1-36 in 12 columns of 3 (1, 2, 3 nearest the zero; 1, 4, 7... nearest the player), the three
    // 2-to-1 columns at the far end, the dozens under the numbers, and the even-money bets nearest the player.
    static void BuildLayout(SharedStyleKit kit, Transform table, int layer)
    {
        var layout = new GameObject("Layout").transform;
        layout.SetParent(table, false);
        var white = Color.white;

        Spot(kit, layout, layer, kit.chipGreen, "Zero", GridX - CellW, GridZ + CellD, CellW - 0.006f, CellD * 3f - 0.006f, "0", RouletteBetKind.Straight, 0);
        for (int n = 1; n <= 36; n++)
        {
            int column = (n - 1) / 3, row = (n - 1) % 3;
            var material = RouletteWheel.IsRed(n) ? kit.chipRed : kit.chipBlack;
            Spot(kit, layout, layer, material, "Number" + n, GridX + column * CellW, GridZ + row * CellD, CellW - 0.006f, CellD - 0.006f,
                n.ToString(), RouletteBetKind.Straight, n);
        }
        for (int row = 0; row < 3; row++)
            Spot(kit, layout, layer, kit.woodDark, "Column" + (row + 1), GridX + 12 * CellW, GridZ + row * CellD, CellW - 0.006f, CellD - 0.006f,
                "2:1", RouletteBetKind.Column, row + 1);
        for (int d = 0; d < 3; d++)
            Spot(kit, layout, layer, kit.woodDark, "Dozen" + (d + 1), GridX + (1.5f + d * 4f) * CellW, GridZ - CellD, CellW * 4f - 0.006f, CellD - 0.006f,
                RouletteBet.Dozen(d + 1).Label, RouletteBetKind.Dozen, d + 1);
        var evenMoney = new[] { RouletteBetKind.Low, RouletteBetKind.Even, RouletteBetKind.Red, RouletteBetKind.Black, RouletteBetKind.Odd, RouletteBetKind.High };
        for (int i = 0; i < evenMoney.Length; i++)
        {
            var kind = evenMoney[i];
            var material = kind == RouletteBetKind.Red ? kit.chipRed : kind == RouletteBetKind.Black ? kit.chipBlack : kit.woodDark;
            Spot(kit, layout, layer, material, kind.ToString(), GridX + (0.5f + i * 2f) * CellW, GridZ - 2f * CellD, CellW * 2f - 0.006f, CellD - 0.006f,
                RouletteBet.EvenMoney(kind).Label, kind, 0);
        }
    }

    // A pressable cell with its label, plus a hidden chip stack and amount that show what is riding on it.
    static void Spot(SharedStyleKit kit, Transform parent, int layer, Material material, string name, float x, float z, float width, float depth,
        string text, RouletteBetKind kind, int number)
    {
        var button = Button(material, parent, name, x, z, width, text, Color.white, layer, out var label, depth);
        label.rectTransform.sizeDelta = new Vector2(width - 0.012f, Mathf.Min(depth - 0.012f, 0.05f));
        var go = button.gameObject;
        var chips = new GameObject("Chips");
        chips.transform.SetParent(go.transform, false);
        Part(kit.brassGold, chips.transform, "Stack", PrimitiveType.Cylinder, new Vector3(width * 0.28f, ButtonHeight / 2f + 0.006f, -depth * 0.2f),
            new Vector3(0.032f, 0.006f, 0.032f), false, false);
        var amount = Text(chips.transform, "Amount", new Vector3(width * 0.28f, ButtonHeight / 2f + 0.0125f, -depth * 0.2f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(0.03f, 0.016f), 0.01f, 0.2f, kit.chipBlack.color);
        amount.fontStyle = TMPro.FontStyles.Bold;

        var spot = go.AddComponent<RouletteSpot>();
        Set(spot, "kind", p => p.enumValueIndex = (int)kind);
        Set(spot, "number", p => p.intValue = number);
        SetRef(spot, "chips", chips);
        SetRef(spot, "amountText", amount);
    }
}
