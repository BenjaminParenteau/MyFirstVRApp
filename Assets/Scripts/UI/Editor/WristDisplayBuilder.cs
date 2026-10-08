using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.UI;
using HighStakes.UI.Mocks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds Slot C's wrist display: the UI_WristDisplay prefab (kit materials only), three mock tier assets, and the
/// MVP_UI scene (copied from MVP_Template the first time). Re-running rebuilds the prefab and tiers and only adds
/// what's missing to the scene. Slot C (Pak) owns this.
/// </summary>
public static class WristDisplayBuilder
{
    const string ContentRoot = "Assets/Content/UI";
    const string MockFolder = ContentRoot + "/Mocks";
    const string PrefabPath = ContentRoot + "/UI_WristDisplay.prefab";
    const string TemplateScenePath = "Assets/Scenes/MVP/MVP_Template.unity";
    const string ScenePath = "Assets/Scenes/MVP/MVP_UI.unity";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string HologramMaterialPath = ContentRoot + "/UI_Hologram.mat";
    const string HologramEdgeMaterialPath = ContentRoot + "/UI_HologramEdge.mat";
    const string HologramBeamMaterialPath = ContentRoot + "/UI_HologramBeam.mat";

    // Hologram panel (metres, panel space: +Y up, viewed from -Z).
    const float PanelWidth = 0.15f;
    const float PanelHeight = 0.08f;
    static readonly Color HoloBlue = new Color(0.25f, 0.65f, 1f);

    // Watch size (metres): a chunky round digital watch, as in docs/reference/VisualReference.md
    const float CaseDiameter = 0.046f;
    const float CaseHeight = 0.011f;
    const float FaceDiameter = 0.038f;
    // The wrist the strap wraps around: an ellipse in the watch's YZ plane, centred under the case.
    const float WristHalfDepth = 0.022f; // along Y, back of the wrist to the palm side
    const float WristHalfWidth = 0.03f;  // along Z, 12 o'clock to 6 o'clock
    const float StrapWidth = 0.022f;     // along X, the forearm

    [MenuItem("High Stakes/UI/Build Wrist Display")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WristDisplay] Stop Play mode first, then run Build Wrist Display again.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // opening MVP_UI replaces the open scene

        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null)
        {
            Debug.LogError("[WristDisplay] Style kit not found at " + KitPath + ". Pull main (day-zero) first.");
            return;
        }
        if (TMP_Settings.defaultFontAsset == null)
            Debug.LogWarning("[WristDisplay] No default TextMeshPro font. Import it via Window > TextMeshPro > Import TMP Essential Resources, then re-run.");

        EnsureFolder(MockFolder);
        BuildPrefab(kit);
        BuildMockTiers();
        AssetDatabase.SaveAssets();

        BuildScene();
        Debug.Log("[WristDisplay] Built " + PrefabPath + " and " + ScenePath + ". Press Play to test.");
    }

    // ---------------------------------------------------------------- prefab

    static GameObject BuildPrefab(SharedStyleKit kit)
    {
        // Watch space, as worn: +Y out of the face, +Z toward 12 o'clock (top of the text), +X toward 3 o'clock and the
        // hand. The case bottom sits on the back of the wrist at y = 0.
        var root = new GameObject("UI_WristDisplay");

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(root.transform, false);
        // Tier looks on the watch itself (StyleGuide colours only): steel -> gold -> gold with a white VIP band
        var caseSteel = WatchCase(kit.steelCool, visuals, "Case");
        var caseGold = WatchCase(kit.brassGold, visuals, "Case_Gold");
        var stripe = Prim("Stripe_VIP", visuals, PrimitiveType.Cube, new Vector3(0, -0.0005f, -(CaseDiameter / 2f + 0.006f)),
            new Vector3(StrapWidth + 0.002f, 0.0045f, 0.004f), kit.keycardPlastic);
        Strap(kit.chipBlack, visuals);

        var face = new GameObject("Face");
        face.transform.SetParent(visuals.transform, false);
        float faceTop = CaseHeight + 0.0004f;
        Prim("Screen", face, PrimitiveType.Cylinder, new Vector3(0, faceTop - 0.0004f, 0), new Vector3(FaceDiameter, 0.0004f, FaceDiameter), kit.chipBlack);

        float textY = faceTop + 0.0003f;
        // Three rows inside the round face. Tier names are long ("VALUED ASSOCIATE"), so that row wraps onto two
        // lines instead of shrinking to an unreadable single line.
        Label("Caption", face, new Vector3(0, textY, 0.0105f), new Vector2(0.024f, 0.005f), "BANKROLL", kit.accentCyan);
        var bankroll = Label("Bankroll", face, new Vector3(0, textY, 0.002f), new Vector2(0.032f, 0.011f), "$0", kit.accentCyan);
        var tier = Label("Tier", face, new Vector3(0, textY, -0.0095f), new Vector2(0.026f, 0.009f), WristDisplayLogic.NoValue, kit.accentCyan, wrap: true);

        var attach = root.AddComponent<WristAttach>();
        Set(attach, "visuals", visuals);

        var display = root.AddComponent<WristDisplay>();
        Set(display, "bankrollText", bankroll);
        Set(display, "tierText", tier);

        // Steel watch at the start; gold from tier 1; VIP band from tier 2
        TierRange(root, caseSteel, 0, 0);
        TierRange(root, caseGold, 1, -1);
        TierRange(root, stripe, 2, -1);

        var visibility = root.AddComponent<WristRaiseVisibility>();
        Set(visibility, "face", face);
        Set(visibility, "faceNormal", root.transform);

        BuildHologram(kit, root, visuals, display, visibility, faceTop);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // Round case (its rim frames the face like a bezel), a crown at 3 o'clock and lugs at 12 and 6 o'clock.
    static GameObject WatchCase(Material metal, GameObject parent, string name)
    {
        var watchCase = new GameObject(name);
        watchCase.transform.SetParent(parent.transform, false);
        // Unity's cylinder is 2 units tall, so a Y scale of h/2 makes it h tall; centred at half its height.
        Prim("Body", watchCase, PrimitiveType.Cylinder, new Vector3(0, CaseHeight / 2f, 0),
            new Vector3(CaseDiameter, CaseHeight / 2f, CaseDiameter), metal);
        var crown = Prim("Crown", watchCase, PrimitiveType.Cylinder, new Vector3(CaseDiameter / 2f + 0.002f, CaseHeight * 0.55f, 0),
            new Vector3(0.006f, 0.0025f, 0.006f), metal);
        crown.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        foreach (float side in new[] { 1f, -1f })
            Prim("Lugs", watchCase, PrimitiveType.Cube, new Vector3(0, CaseHeight * 0.4f, side * (CaseDiameter / 2f + 0.002f)),
                new Vector3(StrapWidth + 0.003f, CaseHeight * 0.6f, 0.007f), metal);
        return watchCase;
    }

    // A band of short flat segments around the wrist ellipse, from one lug, under the wrist, to the other.
    static void Strap(Material material, GameObject parent)
    {
        var strap = new GameObject("Strap");
        strap.transform.SetParent(parent.transform, false);
        var centre = new Vector3(0f, -WristHalfDepth, 0f);
        const int segments = 28;
        for (int i = 0; i < segments; i++)
        {
            float a0 = 2f * Mathf.PI * i / segments, a1 = 2f * Mathf.PI * (i + 1) / segments;
            // Angle 0 is the top of the wrist (under the case): leave out the arc the case covers.
            var p0 = centre + new Vector3(0f, WristHalfDepth * Mathf.Cos(a0), WristHalfWidth * Mathf.Sin(a0));
            var p1 = centre + new Vector3(0f, WristHalfDepth * Mathf.Cos(a1), WristHalfWidth * Mathf.Sin(a1));
            var mid = (p0 + p1) / 2f;
            if (Mathf.Abs(mid.z) < CaseDiameter / 2f && mid.y > -0.004f) continue;
            var seg = Prim("Band", strap, PrimitiveType.Cube, mid,
                new Vector3(StrapWidth, 0.0025f, Vector3.Distance(p0, p1) + 0.0008f), material);
            seg.transform.localRotation = Quaternion.LookRotation(p1 - p0, mid - centre);
        }
    }

    // The HUD the watch projects while looked at. Panel and beam are placed in world space by WatchHologram each frame.
    static void BuildHologram(SharedStyleKit kit, GameObject root, GameObject visuals, WristDisplay display,
        WristRaiseVisibility visibility, float faceTop)
    {
        var panelMaterial = HologramMaterial(HologramMaterialPath, new Color(HoloBlue.r, HoloBlue.g, HoloBlue.b, 0.22f));
        var edgeMaterial = HologramMaterial(HologramEdgeMaterialPath, new Color(HoloBlue.r, HoloBlue.g, HoloBlue.b, 0.85f));
        var beamMaterial = HologramMaterial(HologramBeamMaterialPath, new Color(HoloBlue.r, HoloBlue.g, HoloBlue.b, 0.12f));

        var hologram = new GameObject("Hologram");
        hologram.transform.SetParent(visuals.transform, false);
        var emitter = new GameObject("Emitter");
        emitter.transform.SetParent(hologram.transform, false);
        emitter.transform.localPosition = new Vector3(0f, faceTop, 0f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(hologram.transform, false);
        Prim("Glass", panel, PrimitiveType.Quad, Vector3.zero, new Vector3(PanelWidth, PanelHeight, 1f), panelMaterial);
        const float edge = 0.0015f;
        foreach (float y in new[] { PanelHeight / 2f, -PanelHeight / 2f })
            Prim("Edge", panel, PrimitiveType.Quad, new Vector3(0f, y, -0.0005f), new Vector3(PanelWidth, edge, 1f), edgeMaterial);
        foreach (float x in new[] { PanelWidth / 2f, -PanelWidth / 2f })
            Prim("Edge", panel, PrimitiveType.Quad, new Vector3(x, 0f, -0.0005f), new Vector3(edge, PanelHeight, 1f), edgeMaterial);
        // Rule line under the header.
        Prim("Rule", panel, PrimitiveType.Quad, new Vector3(0f, 0.022f, -0.0005f), new Vector3(PanelWidth - 0.02f, 0.0008f, 1f), edgeMaterial);

        HudText("Header", panel, new Vector3(0f, 0.03f), new Vector2(PanelWidth - 0.02f, 0.01f), "HIGH STAKES", TextAlignmentOptions.Center, kit.accentCyan);
        var rows = new[] { ("BANKROLL", 0.011f), ("TIER", -0.008f), ("SESSION", -0.027f) };
        var values = new TextMeshPro[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var (caption, y) = rows[i];
            HudText(caption + "_Caption", panel, new Vector3(-0.037f, y), new Vector2(0.06f, 0.011f), caption, TextAlignmentOptions.Left, HoloBlue);
            values[i] = HudText(caption + "_Value", panel, new Vector3(0.025f, y), new Vector2(0.085f, 0.014f), WristDisplayLogic.NoValue, TextAlignmentOptions.Right, kit.accentCyan);
        }

        var beam = Prim("Beam", hologram, PrimitiveType.Cylinder, Vector3.zero, Vector3.one * 0.01f, beamMaterial);

        var holo = root.AddComponent<WatchHologram>();
        Set(holo, "visibility", visibility);
        Set(holo, "display", display);
        Set(holo, "emitter", emitter.transform);
        Set(holo, "panel", panel.transform);
        Set(holo, "beam", beam.transform);
        Set(holo, "bankrollText", values[0]);
        Set(holo, "tierText", values[1]);
        Set(holo, "sessionText", values[2]);
        var so = new SerializedObject(holo);
        so.FindProperty("panelHalfHeight").floatValue = PanelHeight / 2f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Upright text on the panel, readable from -Z (the side WatchHologram turns toward the eyes).
    static TextMeshPro HudText(string name, GameObject panel, Vector2 pos, Vector2 size, string text, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(panel.transform, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, -0.001f);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 1f;
        tmp.margin = Vector4.zero;
        return tmp;
    }

    // Transparent, unlit, both sides: holograms glow the same in any light and never cast shadows.
    static Material HologramMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetFloat("_Surface", 1f); // transparent
        material.SetFloat("_Blend", 0f);   // alpha
        material.SetFloat("_Cull", 0f);    // both sides
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static TextMeshPro Label(string name, GameObject parent, Vector3 pos, Vector2 size, string text, Color color, bool wrap = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // readable from +Y (the face side)
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true; // fits the text to the box, whatever the font's units
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 1f;
        tmp.margin = Vector4.zero;
        return tmp;
    }

    static void TierRange(GameObject root, GameObject target, int minTier, int maxTier)
    {
        var range = root.AddComponent<TierVisibility>();
        var so = new SerializedObject(range);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("minTier").intValue = minTier;
        so.FindProperty("maxTier").intValue = maxTier;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject Prim(string name, GameObject parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // display only; must not block hands or rays
        var renderer = go.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // ---------------------------------------------------------------- mock tiers

    static TierDefinition[] BuildMockTiers()
    {
        // Placeholder names and thresholds from Progress_Vol1.md. Slot B's real tiers live in Content/Progression.
        return new[]
        {
            Tier("SO_MockTier_0_OrdinaryNight", "Ordinary Night", 0, 0),
            Tier("SO_MockTier_1_KnownHighRoller", "Known High Roller", 1, 1000),
            Tier("SO_MockTier_2_ValuedAssociate", "Valued Associate", 2, 5000),
        };
    }

    static TierDefinition Tier(string file, string displayName, int index, int threshold)
    {
        string path = $"{MockFolder}/{file}.asset";
        var tier = AssetDatabase.LoadAssetAtPath<TierDefinition>(path);
        if (tier == null)
        {
            tier = ScriptableObject.CreateInstance<TierDefinition>();
            AssetDatabase.CreateAsset(tier, path);
        }
        tier.displayName = displayName;
        tier.index = index;
        tier.chipThreshold = threshold;
        EditorUtility.SetDirty(tier);
        return tier;
    }

    // ---------------------------------------------------------------- scene

    static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null && !AssetDatabase.CopyAsset(TemplateScenePath, ScenePath))
        {
            Debug.LogError("[WristDisplay] Could not copy " + TemplateScenePath + " to " + ScenePath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Load the assets only now: opening a scene unloads unused assets, which turned earlier references into
        // empty slots (the mock tiers ended up as {fileID: 0} and the watch showed "--").
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var tiers = BuildMockTiers();
        GameObject content = null, slice = null, solo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "UI_Content") content = root;
            else if (root.name.StartsWith("SliceContent")) slice = root;
            else if (root.name == "_SoloTest") solo = root;
        }
        if (solo == null)
        {
            Debug.LogError("[WristDisplay] " + ScenePath + " has no _SoloTest. It must start as a copy of MVP_Template.");
            return;
        }

        // StyleGuide §5d: rename the template's slice root to <Area>_Content and build only inside it
        if (content == null)
        {
            content = slice != null ? slice : new GameObject();
            content.name = "UI_Content";
        }
        if (content.GetComponentInChildren<WristDisplay>(true) == null)
            PrefabUtility.InstantiatePrefab(prefab, content.transform);

        // Mocks go under _SoloTest so they switch off when MVP_Main loads this scene (the real wallet/tier take over)
        var mocksTransform = solo.transform.Find("UI_Mocks");
        var mocks = mocksTransform != null ? mocksTransform.gameObject : new GameObject("UI_Mocks");
        mocks.transform.SetParent(solo.transform, false);

        var wallet = mocks.GetComponent<MockChipWallet>();
        if (wallet == null) wallet = mocks.AddComponent<MockChipWallet>();
        var tier = mocks.GetComponent<MockCharacterTier>();
        if (tier == null) tier = mocks.AddComponent<MockCharacterTier>();

        var so = new SerializedObject(tier);
        so.FindProperty("walletSource").objectReferenceValue = wallet;
        var tierArray = so.FindProperty("tiers");
        tierArray.arraySize = tiers.Length;
        for (int i = 0; i < tiers.Length; i++)
            tierArray.GetArrayElementAtIndex(i).objectReferenceValue = tiers[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < tiers.Length; i++)
        {
            if (tierArray.GetArrayElementAtIndex(i).objectReferenceValue == null)
                Debug.LogError("[WristDisplay] Mock tier " + i + " didn't wire up; the wrist will show '--' for the tier.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = mocks; // the +/- chip buttons are right there in the Inspector
    }

    // ---------------------------------------------------------------- helpers

    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
