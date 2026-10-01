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

    // Watch size (metres): a chunky digital watch, as in docs/reference/VisualReference.md
    static readonly Vector3 CaseSize = new Vector3(0.06f, 0.012f, 0.046f);
    static readonly Vector3 ScreenSize = new Vector3(0.054f, 0.0005f, 0.04f);

    [MenuItem("High Stakes/UI/Build Wrist Display")]
    public static void Build()
    {
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
        var prefab = BuildPrefab(kit);
        var tiers = BuildMockTiers();
        AssetDatabase.SaveAssets();

        BuildScene(prefab, tiers);
        Debug.Log("[WristDisplay] Built " + PrefabPath + " and " + ScenePath + ". Press Play to test.");
    }

    // ---------------------------------------------------------------- prefab

    static GameObject BuildPrefab(SharedStyleKit kit)
    {
        // Watch space: +Y points out of the face, +Z is the top of the text
        var root = new GameObject("UI_WristDisplay");

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(root.transform, false);
        Prim("Case", visuals, Vector3.zero, CaseSize, kit.steelCool);

        var face = new GameObject("Face");
        face.transform.SetParent(visuals.transform, false);
        float faceY = CaseSize.y / 2f;
        Prim("Screen", face, new Vector3(0, faceY + ScreenSize.y / 2f, 0), ScreenSize, kit.chipBlack);

        float textY = faceY + ScreenSize.y + 0.0003f;
        Label("Caption", face, new Vector3(0, textY, 0.013f), new Vector2(0.05f, 0.007f), "BANKROLL", kit.accentCyan);
        var bankroll = Label("Bankroll", face, new Vector3(0, textY, 0.001f), new Vector2(0.05f, 0.015f), "$0", kit.accentCyan);
        var tier = Label("Tier", face, new Vector3(0, textY, -0.013f), new Vector2(0.05f, 0.007f), WristDisplayLogic.NoValue, kit.accentCyan);

        var attach = root.AddComponent<WristAttach>();
        Set(attach, "visuals", visuals);

        var display = root.AddComponent<WristDisplay>();
        Set(display, "bankrollText", bankroll);
        Set(display, "tierText", tier);

        var visibility = root.AddComponent<WristRaiseVisibility>();
        Set(visibility, "face", face);
        Set(visibility, "faceNormal", root.transform);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static TextMeshPro Label(string name, GameObject parent, Vector3 pos, Vector2 size, string text, Color color)
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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true; // fits the text to the box, whatever the font's units
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 1f;
        tmp.margin = Vector4.zero;
        return tmp;
    }

    static void Prim(string name, GameObject parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // display only; must not block hands or rays
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

    static void BuildScene(GameObject prefab, TierDefinition[] tiers)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null && !AssetDatabase.CopyAsset(TemplateScenePath, ScenePath))
        {
            Debug.LogError("[WristDisplay] Could not copy " + TemplateScenePath + " to " + ScenePath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
