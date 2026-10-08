using System;
using System.Collections.Generic;
using HighStakes.Environment;
using HighStakes.TableGames;
using HighStakes.TableGames.Blackjack;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Blackjack rules asset (only if missing, so tuned rules survive) and the table prefab (always rebuilt)
/// from primitives and the shared style kit. The player stands on the -Z side facing +Z; every height is measured from
/// the felt top so the presenter's layout and the builder's agree. Cards are white boxes with text, no card art.
///
/// Slot A owns this. Re-running overwrites the prefab, so change the table here, not in the prefab.
/// </summary>
// ponytail: primitive boxes and no seat; swap the body for a model and add seating when the casino gets them.
public static class BlackjackTableBuilder
{
    public const string PrefabPath = "Assets/Content/Tables/Prefabs/Tables_BlackjackTable.prefab";
    const string RulesPath = "Assets/Content/Tables/Data/BlackjackRules.asset";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";

    /// <summary>Enough for two split hands and the dealer on a long night; more than a hand ever needs in practice.</summary>
    const int CardsOnTable = 32;

    const float Felt = 0.76f;
    const float TextY = Felt + 0.002f;
    const float PlayerZ = -0.02f;
    const float DealerZ = 0.22f;
    const float MovesZ = -0.17f;
    const float WagerZ = -0.33f;
    const float ButtonHeight = 0.025f;

    [MenuItem("High Stakes/Tables/Build Blackjack Table")]
    public static void Build()
    {
        var prefab = BuildPrefab();
        if (prefab == null) return;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Blackjack] Built " + PrefabPath + ".");
    }

    /// <summary>Builds the prefab in the open scene, saves it and removes the scene copy. Null if the style kit is missing.</summary>
    public static GameObject BuildPrefab()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null || kit.feltGreen == null)
        {
            Debug.LogError("[Blackjack] Style kit is missing. Run High Stakes > Build Style Kit or pull the latest dev.");
            return null;
        }
        var interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer < 0)
        {
            Debug.LogError("[Blackjack] The Interactable layer is missing from the project's tags and layers.");
            return null;
        }

        var rules = GetOrCreateRules();
        var root = new GameObject("Tables_BlackjackTable");
        var t = root.transform;
        var presenter = root.AddComponent<BlackjackPresenter>();
        SetRef(presenter, "rules", rules);

        // Body: static, and only the top keeps a collider, so the table stops the player without snagging hands.
        Part(kit.woodDark, t, "Pedestal", PrimitiveType.Cylinder, new Vector3(0f, 0.36f, 0f), new Vector3(0.5f, 0.36f, 0.5f), true, false);
        Part(kit.woodDark, t, "Top", PrimitiveType.Cube, new Vector3(0f, 0.735f, 0f), new Vector3(1.6f, 0.04f, 0.9f), true, true);
        Part(kit.feltGreen, t, "Felt", PrimitiveType.Cube, new Vector3(0f, Felt - 0.0025f, 0f), new Vector3(1.5f, 0.005f, 0.8f), true, false);
        Part(kit.brassGold, t, "Rail", PrimitiveType.Cube, new Vector3(0f, 0.77f, -0.45f), new Vector3(1.6f, 0.03f, 0.04f), true, false);
        var shoe = Part(kit.woodDark, t, "Shoe", PrimitiveType.Cube, new Vector3(0.55f, 0.80f, DealerZ), new Vector3(0.1f, 0.08f, 0.14f), true, false);
        SetRef(presenter, "shoe", shoe.transform);

        // Moves, in the order a player reaches for them.
        var white = Color.white;
        var moveLabels = new Object[4];
        SetRef(presenter, "hitButton", Button(kit.chipBlack, t, "Hit", -0.36f, MovesZ, 0.2f, "HIT", white, interactableLayer, out var label));
        moveLabels[0] = label;
        SetRef(presenter, "standButton", Button(kit.chipBlack, t, "Stand", -0.12f, MovesZ, 0.2f, "STAND", white, interactableLayer, out label));
        moveLabels[1] = label;
        SetRef(presenter, "doubleButton", Button(kit.chipBlack, t, "DoubleDown", 0.12f, MovesZ, 0.2f, "DOUBLE", white, interactableLayer, out label));
        moveLabels[2] = label;
        SetRef(presenter, "splitButton", Button(kit.chipBlack, t, "Split", 0.36f, MovesZ, 0.2f, "SPLIT", white, interactableLayer, out label));
        moveLabels[3] = label;
        SetRefs(presenter, "moveLabels", moveLabels);

        // Wager row nearest the player: halve, amount, double, chip balance, DEAL.
        SetRef(presenter, "halveButton", Button(kit.chipBlack, t, "HalveWager", -0.5f, WagerZ, 0.1f, "1/2", white, interactableLayer, out _));
        SetRef(presenter, "doubleWagerButton", Button(kit.chipBlack, t, "DoubleWager", -0.22f, WagerZ, 0.1f, "2x", white, interactableLayer, out _));
        SetRef(presenter, "dealButton", Button(kit.brassGold, t, "Deal", 0.36f, WagerZ, 0.24f, "DEAL", kit.chipBlack.color, interactableLayer, out _));
        SetRef(presenter, "wagerText", FlatText(t, "WagerText", -0.36f, WagerZ, new Vector2(0.16f, 0.06f), 0.6f));
        SetRef(presenter, "balanceText", FlatText(t, "BalanceText", 0f, WagerZ, new Vector2(0.3f, 0.05f), 0.45f));

        // Totals just beyond each row of cards; the presenter slides the hand totals sideways on a split.
        var handTexts = new Object[2];
        for (var h = 0; h < handTexts.Length; h++)
            handTexts[h] = FlatText(t, "HandTotal" + h, 0f, PlayerZ + 0.085f, new Vector2(0.3f, 0.05f), 0.45f);
        SetRefs(presenter, "handTexts", handTexts);
        SetRef(presenter, "dealerText", FlatText(t, "DealerTotal", 0f, DealerZ + 0.085f, new Vector2(0.3f, 0.05f), 0.45f));

        var marker = Part(kit.brassGold, t, "ActiveHand", PrimitiveType.Cube, new Vector3(0f, Felt + 0.001f, PlayerZ - 0.065f), new Vector3(0.12f, 0.003f, 0.008f), false, false);
        SetRef(presenter, "activeMarker", marker.transform);

        SetRef(presenter, "hologram", BuildHologram(kit, t));

        var cards = new GameObject("Cards").transform; // stays at the root's origin: the presenter mixes card and shoe local positions
        cards.SetParent(t, false);
        var pool = new Object[CardsOnTable];
        for (var i = 0; i < CardsOnTable; i++) pool[i] = Card(kit, cards, i);
        SetRefs(presenter, "cardPool", pool);

        // Cards, buttons and the marker move, so they cannot be lightmapped and would get only the dim flat ambient.
        // The table carries baked probes over its top so they pick up the room's light in whatever scene it sits in.
        var probes = new GameObject("LightProbes").AddComponent<LightProbeGroup>();
        probes.transform.SetParent(t, false);
        var points = new List<Vector3>();
        foreach (var y in new[] { Felt + 0.04f, Felt + 0.4f })
            foreach (var x in new[] { -0.75f, 0f, 0.75f })
                foreach (var z in new[] { -0.45f, 0f, 0.45f })
                    points.Add(new Vector3(x, y, z));
        probes.probePositions = points.ToArray();

        Set(presenter, "feltHeight", p => p.floatValue = Felt);
        Set(presenter, "playerZ", p => p.floatValue = PlayerZ);
        Set(presenter, "dealerZ", p => p.floatValue = DealerZ);
        Set(presenter, "cardStep", p => p.floatValue = 0.045f);
        Set(presenter, "splitOffset", p => p.floatValue = 0.18f);
        Set(presenter, "dealSeconds", p => p.floatValue = 0.25f);
        Set(presenter, "unavailable", p => p.colorValue = new Color(1f, 1f, 1f, 0.2f));

        EnsureFolder(System.IO.Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var saved);
        Object.DestroyImmediate(root);
        if (!saved) throw new InvalidOperationException("Could not save " + PrefabPath);
        return prefab;
    }

    static BlackjackRules GetOrCreateRules()
    {
        var rules = AssetDatabase.LoadAssetAtPath<BlackjackRules>(RulesPath);
        if (rules != null) return rules;
        EnsureFolder(System.IO.Path.GetDirectoryName(RulesPath).Replace('\\', '/'));
        rules = ScriptableObject.CreateInstance<BlackjackRules>();
        AssetDatabase.CreateAsset(rules, RulesPath);
        return rules;
    }

    /// <summary>
    /// A pressable block on the felt with its label lying on top. The collider and interactable sit on an unscaled
    /// parent so the label is not stretched by the block's scale, and the label dips with the press.
    /// </summary>
    static PressableButton Button(Material material, Transform parent, string name, float x, float z, float width,
        string text, Color textColor, int layer, out TextMeshPro label)
    {
        const float depth = 0.08f;
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, Felt + ButtonHeight / 2f, z);
        go.AddComponent<BoxCollider>().size = new Vector3(width, ButtonHeight, depth);
        var button = go.AddComponent<PressableButton>(); // brings its XRSimpleInteractable, which finds the collider
        var body = Part(material, go.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(width, ButtonHeight, depth), false, false);
        body.layer = layer;
        label = Text(go.transform, "Label", new Vector3(0f, ButtonHeight / 2f + 0.001f, 0f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(width - 0.02f, depth - 0.02f), 0.1f, 0.5f, textColor);
        label.text = text;
        label.fontStyle = FontStyles.Bold;
        label.gameObject.layer = layer;
        return button;
    }

    /// <summary>A face-down card: white body, burgundy back on top, and the face text between them.</summary>
    static CardView Card(SharedStyleKit kit, Transform parent, int index)
    {
        var card = new GameObject("Card" + index);
        card.transform.SetParent(parent, false);
        Part(kit.chipWhite, card.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.07f, 0.002f, 0.1f), false, false);
        var back = Part(kit.fabricBurgundy, card.transform, "Back", PrimitiveType.Cube, new Vector3(0f, 0.0015f, 0f), new Vector3(0.066f, 0.001f, 0.096f), false, false);
        var face = Text(card.transform, "Face", new Vector3(0f, 0.0012f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.06f, 0.09f), 0.01f, 0.5f, Color.black);
        face.fontStyle = FontStyles.Bold;
        face.textWrappingMode = TextWrappingModes.Normal; // rank and suit are two lines

        var view = card.AddComponent<CardView>();
        SetRef(view, "face", face);
        SetRef(view, "back", back);
        card.SetActive(false);
        return view;
    }

    /// <summary>Text lying flat on the felt, readable from the player's side.</summary>
    // Hologram materials are shared with the watch HUD (made by High Stakes > UI > Build Wrist Display).
    const string HologramMaterialPath = "Assets/Content/UI/UI_Hologram.mat";
    const string HologramEdgeMaterialPath = "Assets/Content/UI/UI_HologramEdge.mat";
    const string HologramBeamMaterialPath = "Assets/Content/UI/UI_HologramBeam.mat";
    const float BoardWidth = 0.72f;
    const float BoardHeight = 0.38f;

    /// <summary>
    /// A brass projector puck on the dealer's edge and the board it projects where the dealer would stand, facing the
    /// player, centred at eye height for a standing player. TableHologram opens it when the player walks up.
    /// </summary>
    static TableHologram BuildHologram(SharedStyleKit kit, Transform table)
    {
        var glass = AssetDatabase.LoadAssetAtPath<Material>(HologramMaterialPath);
        var edgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(HologramEdgeMaterialPath);
        var beamMaterial = AssetDatabase.LoadAssetAtPath<Material>(HologramBeamMaterialPath);
        if (glass == null || edgeMaterial == null || beamMaterial == null)
            throw new InvalidOperationException("[Blackjack] Hologram materials missing; run High Stakes > UI > Build Wrist Display first.");

        var root = new GameObject("Hologram").transform;
        root.SetParent(table, false);
        var puckPosition = new Vector3(0f, Felt + 0.012f, 0.4f);
        Part(kit.brassGold, root, "Projector", PrimitiveType.Cylinder, puckPosition, new Vector3(0.1f, 0.012f, 0.1f), true, false);
        Part(edgeMaterial, root, "ProjectorLens", PrimitiveType.Cylinder, puckPosition + Vector3.up * 0.0125f, new Vector3(0.06f, 0.001f, 0.06f), false, false);
        var emitter = new GameObject("Emitter").transform;
        emitter.SetParent(root, false);
        emitter.localPosition = puckPosition + Vector3.up * 0.013f;

        var board = new GameObject("Board").transform;
        board.SetParent(root, false);
        board.localPosition = new Vector3(0f, 1.5f, 0.75f);
        Part(glass, board, "Glass", PrimitiveType.Quad, Vector3.zero, new Vector3(BoardWidth, BoardHeight, 1f), false, false);
        const float edge = 0.005f;
        foreach (float y in new[] { BoardHeight / 2f, -BoardHeight / 2f })
            Part(edgeMaterial, board, "Edge", PrimitiveType.Quad, new Vector3(0f, y, -0.001f), new Vector3(BoardWidth, edge, 1f), false, false);
        foreach (float x in new[] { BoardWidth / 2f, -BoardWidth / 2f })
            Part(edgeMaterial, board, "Edge", PrimitiveType.Quad, new Vector3(x, 0f, -0.001f), new Vector3(edge, BoardHeight, 1f), false, false);
        Part(edgeMaterial, board, "Rule", PrimitiveType.Quad, new Vector3(0f, 0.09f, -0.001f), new Vector3(BoardWidth - 0.08f, 0.002f, 1f), false, false);

        // Upright text facing the player (-Z): title, the amount (counts up on a win), and the hands.
        var title = Text(board, "Title", new Vector3(0f, 0.13f, -0.002f), Quaternion.identity, new Vector2(BoardWidth - 0.08f, 0.06f), 0.1f, 2f, kit.accentCyan);
        var amount = Text(board, "Amount", new Vector3(0f, 0.005f, -0.002f), Quaternion.identity, new Vector2(BoardWidth - 0.08f, 0.15f), 0.1f, 4f, Color.white);
        var detail = Text(board, "Detail", new Vector3(0f, -0.13f, -0.002f), Quaternion.identity, new Vector2(BoardWidth - 0.08f, 0.05f), 0.1f, 1.5f, Color.white);
        foreach (var text in new[] { title, amount, detail }) text.fontStyle = FontStyles.Bold;

        var beam = Part(beamMaterial, root, "Beam", PrimitiveType.Cylinder, Vector3.zero, Vector3.one * 0.01f, false, false);
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var hologram = table.gameObject.AddComponent<TableHologram>();
        SetRef(hologram, "emitter", emitter);
        SetRef(hologram, "panel", board);
        SetRef(hologram, "beam", beam.transform);
        SetRef(hologram, "titleText", title);
        SetRef(hologram, "amountText", amount);
        SetRef(hologram, "detailText", detail);
        Set(hologram, "idleColor", p => p.colorValue = Color.white);
        Set(hologram, "winColor", p => p.colorValue = kit.safeGreen);
        Set(hologram, "loseColor", p => p.colorValue = kit.alertRed);
        Set(hologram, "panelHalfHeight", p => p.floatValue = BoardHeight / 2f);
        return hologram;
    }

    static TextMeshPro FlatText(Transform parent, string name, float x, float z, Vector2 size, float maxSize) =>
        Text(parent, name, new Vector3(x, TextY, z), Quaternion.Euler(90f, 0f, 0f), size, 0.1f, maxSize, Color.white);

    /// <summary>Auto-sized, centred, single-line world text. Sizes are TMP points, roughly 10 per metre of line height.</summary>
    static TextMeshPro Text(Transform parent, string name, Vector3 position, Quaternion rotation, Vector2 size,
        float minSize, float maxSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "";
        tmp.rectTransform.sizeDelta = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.fontSize = maxSize;
        tmp.color = color;
        return tmp;
    }

    static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position,
        Vector3 scale, bool isStatic, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = isStatic;
        return go;
    }

    static void SetRef(Object target, string field, Object value) => Set(target, field, p => p.objectReferenceValue = value);

    static void SetRefs(Object target, string field, Object[] values) => Set(target, field, p =>
    {
        p.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    });

    // Throws on a renamed field so the builder fails loudly instead of saving a prefab with a missing reference.
    static void Set(Object target, string field, Action<SerializedProperty> edit)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field)
            ?? throw new ArgumentException($"{target.GetType().Name} has no serialized field {field}");
        edit(property);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
