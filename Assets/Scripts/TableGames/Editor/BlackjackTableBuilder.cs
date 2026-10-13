using System;
using HighStakes.Environment;
using HighStakes.TableGames;
using HighStakes.TableGames.Blackjack;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static TableKit;

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

    /// <summary>Enough for two split hands and the dealer on a long night; more than a hand ever needs in practice.</summary>
    const int CardsOnTable = 32;

    const float PlayerZ = -0.02f;
    const float DealerZ = 0.22f;
    const float MovesZ = -0.17f;
    const float WagerZ = -0.33f;

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
        var kit = Kit(out var interactableLayer);
        if (kit == null) return null;

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

        SetRef(presenter, "hologram", Hologram(kit, t, 1.6f, 0.43f, "BLACKJACK", "PRESS DEAL TO PLAY")); // full table width, on the dealer edge

        var cards = new GameObject("Cards").transform; // stays at the root's origin: the presenter mixes card and shoe local positions
        cards.SetParent(t, false);
        var pool = new Object[CardsOnTable];
        for (var i = 0; i < CardsOnTable; i++) pool[i] = Card(kit, cards, i);
        SetRefs(presenter, "cardPool", pool);

        Probes(t, new[] { -0.75f, 0f, 0.75f }, new[] { -0.45f, 0f, 0.45f });

        Set(presenter, "feltHeight", p => p.floatValue = Felt);
        Set(presenter, "playerZ", p => p.floatValue = PlayerZ);
        Set(presenter, "dealerZ", p => p.floatValue = DealerZ);
        Set(presenter, "cardStep", p => p.floatValue = 0.045f);
        Set(presenter, "splitOffset", p => p.floatValue = 0.18f);
        Set(presenter, "dealSeconds", p => p.floatValue = 0.25f);
        Set(presenter, "unavailable", p => p.colorValue = new Color(1f, 1f, 1f, 0.2f));

        return SavePrefab(root, PrefabPath);
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
}
