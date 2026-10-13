using System;
using System.Collections.Generic;
using HighStakes.Environment;
using HighStakes.TableGames;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The pieces every casino table is built from, shared by the table builders: kit-material boxes, flat and upright
/// text, pressable buttons on the felt, the dealer-side hologram board, light probes, and SerializedObject wiring.
/// Every table's felt top is at <see cref="Felt"/>; the player stands on the -Z side facing +Z.
/// </summary>
public static class TableKit
{
    public const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    public const float Felt = 0.76f;
    public const float TextY = Felt + 0.002f;
    public const float ButtonHeight = 0.025f;

    // Hologram materials are shared with the watch HUD (made by High Stakes > UI > Build Wrist Display).
    const string HologramMaterialPath = "Assets/Content/UI/UI_Hologram.mat";
    const string HologramEdgeMaterialPath = "Assets/Content/UI/UI_HologramEdge.mat";
    const float BoardHeight = 0.42f;

    /// <summary>The style kit and the Interactable layer, or null with an error logged if either is missing.</summary>
    public static SharedStyleKit Kit(out int interactableLayer)
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null || kit.feltGreen == null)
        {
            Debug.LogError("[Tables] Style kit is missing. Run High Stakes > Build Style Kit or pull the latest dev.");
            return null;
        }
        if (interactableLayer < 0)
        {
            Debug.LogError("[Tables] The Interactable layer is missing from the project's tags and layers.");
            return null;
        }
        return kit;
    }

    /// <summary>
    /// A pressable block on the felt with its label lying on top. The collider and interactable sit on an unscaled
    /// parent so the label is not stretched by the block's scale, and the label dips with the press.
    /// </summary>
    public static PressableButton Button(Material material, Transform parent, string name, float x, float z, float width,
        string text, Color textColor, int layer, out TextMeshPro label, float depth = 0.08f)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, Felt + ButtonHeight / 2f, z);
        go.AddComponent<BoxCollider>().size = new Vector3(width, ButtonHeight, depth);
        var button = go.AddComponent<PressableButton>(); // brings its XRSimpleInteractable, which finds the collider
        var body = Part(material, go.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(width, ButtonHeight, depth), false, false);
        body.layer = layer;
        label = Text(go.transform, "Label", new Vector3(0f, ButtonHeight / 2f + 0.001f, 0f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(width - 0.01f, depth - 0.01f), 0.05f, 0.5f, textColor);
        label.text = text;
        label.fontStyle = FontStyles.Bold;
        label.gameObject.layer = layer;
        return button;
    }

    /// <summary>
    /// A brass projector strip along the dealer's edge and the board it projects, <paramref name="width"/> wide and
    /// standing on the table where the dealer would be, facing the player. TableHologram raises it when the player
    /// walks up and shows <paramref name="tableName"/> between rounds.
    /// </summary>
    public static TableHologram Hologram(SharedStyleKit kit, Transform table, float width, float dealerEdgeZ, string tableName, string prompt)
    {
        var glass = AssetDatabase.LoadAssetAtPath<Material>(HologramMaterialPath);
        var edgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(HologramEdgeMaterialPath);
        if (glass == null || edgeMaterial == null)
            throw new InvalidOperationException("[Tables] Hologram materials missing; run High Stakes > UI > Build Wrist Display first.");

        var root = new GameObject("Hologram").transform;
        root.SetParent(table, false);
        const float stripHeight = 0.012f;
        const float tableTop = Felt - 0.005f;
        Part(kit.brassGold, root, "Projector", PrimitiveType.Cube, new Vector3(0f, tableTop + stripHeight / 2f, dealerEdgeZ),
            new Vector3(width, stripHeight, 0.03f), true, false);
        Part(edgeMaterial, root, "ProjectorLens", PrimitiveType.Cube, new Vector3(0f, tableTop + stripHeight + 0.0005f, dealerEdgeZ),
            new Vector3(width - 0.04f, 0.001f, 0.012f), false, false);

        // Pivot at the middle of the bottom edge, which sits on the strip; the parts sit above it.
        var board = new GameObject("Board").transform;
        board.SetParent(root, false);
        board.localPosition = new Vector3(0f, tableTop + stripHeight, dealerEdgeZ);
        float mid = BoardHeight / 2f;
        Part(glass, board, "Glass", PrimitiveType.Quad, new Vector3(0f, mid, 0f), new Vector3(width, BoardHeight, 1f), false, false);
        const float edge = 0.006f;
        foreach (float y in new[] { BoardHeight, 0f })
            Part(edgeMaterial, board, "Edge", PrimitiveType.Quad, new Vector3(0f, y, -0.001f), new Vector3(width, edge, 1f), false, false);
        foreach (float x in new[] { width / 2f, -width / 2f })
            Part(edgeMaterial, board, "Edge", PrimitiveType.Quad, new Vector3(x, mid, -0.001f), new Vector3(edge, BoardHeight, 1f), false, false);
        Part(edgeMaterial, board, "Rule", PrimitiveType.Quad, new Vector3(0f, mid + 0.105f, -0.001f), new Vector3(width - 0.2f, 0.003f, 1f), false, false);

        // Upright text facing the player (-Z): title, the amount (counts up on a win), and a detail line.
        float textWidth = Mathf.Min(width - 0.2f, 1.4f);
        var title = Text(board, "Title", new Vector3(0f, mid + 0.15f, -0.002f), Quaternion.identity, new Vector2(textWidth, 0.07f), 0.1f, 3f, kit.accentCyan);
        var amount = Text(board, "Amount", new Vector3(0f, mid + 0.005f, -0.002f), Quaternion.identity, new Vector2(textWidth, 0.17f), 0.1f, 6f, Color.white);
        var detail = Text(board, "Detail", new Vector3(0f, mid - 0.145f, -0.002f), Quaternion.identity, new Vector2(textWidth, 0.06f), 0.1f, 2.5f, Color.white);
        foreach (var text in new[] { title, amount, detail }) text.fontStyle = FontStyles.Bold;

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var hologram = table.gameObject.AddComponent<TableHologram>();
        SetRef(hologram, "panel", board);
        SetRef(hologram, "titleText", title);
        SetRef(hologram, "amountText", amount);
        SetRef(hologram, "detailText", detail);
        Set(hologram, "tableName", p => p.stringValue = tableName);
        Set(hologram, "prompt", p => p.stringValue = prompt);
        Set(hologram, "idleColor", p => p.colorValue = Color.white);
        Set(hologram, "winColor", p => p.colorValue = kit.safeGreen);
        Set(hologram, "loseColor", p => p.colorValue = kit.alertRed);
        return hologram;
    }

    /// <summary>
    /// Baked light probes over the table top. Cards, chips, buttons and wheels move, so they cannot be lightmapped and
    /// would get only the dim flat ambient; probes let them pick up the room's light in whatever scene the table sits in.
    /// </summary>
    public static void Probes(Transform table, float[] xs, float[] zs)
    {
        var probes = new GameObject("LightProbes").AddComponent<LightProbeGroup>();
        probes.transform.SetParent(table, false);
        var points = new List<Vector3>();
        foreach (var y in new[] { Felt + 0.04f, Felt + 0.4f })
            foreach (var x in xs)
                foreach (var z in zs)
                    points.Add(new Vector3(x, y, z));
        probes.probePositions = points.ToArray();
    }

    /// <summary>Text lying flat on the felt, readable from the player's side.</summary>
    public static TextMeshPro FlatText(Transform parent, string name, float x, float z, Vector2 size, float maxSize) =>
        Text(parent, name, new Vector3(x, TextY, z), Quaternion.Euler(90f, 0f, 0f), size, 0.1f, maxSize, Color.white);

    /// <summary>Auto-sized, centred, single-line world text. Sizes are TMP points, roughly 10 per metre of line height.</summary>
    public static TextMeshPro Text(Transform parent, string name, Vector3 position, Quaternion rotation, Vector2 size,
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

    public static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position,
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

    public static void SetRef(Object target, string field, Object value) => Set(target, field, p => p.objectReferenceValue = value);

    public static void SetRefs(Object target, string field, Object[] values) => Set(target, field, p =>
    {
        p.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    });

    // Throws on a renamed field so the builder fails loudly instead of saving a prefab with a missing reference.
    public static void Set(Object target, string field, Action<SerializedProperty> edit)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field)
            ?? throw new ArgumentException($"{target.GetType().Name} has no serialized field {field}");
        edit(property);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static GameObject SavePrefab(GameObject root, string path)
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out var saved);
        Object.DestroyImmediate(root);
        if (!saved) throw new InvalidOperationException("Could not save " + path);
        return prefab;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
