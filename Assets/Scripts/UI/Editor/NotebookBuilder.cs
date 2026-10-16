using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.UI;
using HighStakes.UI.Mocks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds Slot C's notebook: the UI_Notebook prefab (kit materials only), two mock pickup items, and adds both,
/// plus a MockInventoryEvents, to MVP_UI (run Build Wrist Display first so that scene exists). Re-running rebuilds
/// the prefab and items and only adds what's missing to the scene. Slot C (Pak) owns this.
/// </summary>
public static class NotebookBuilder
{
    const string ContentRoot = "Assets/Content/UI";
    const string MockFolder = ContentRoot + "/Mocks";
    const string ItemFolder = ContentRoot + "/Items";
    const string KeycardItemPath = ItemFolder + "/SO_Item_VIPKeycard.asset";
    const string PrefabPath = ContentRoot + "/UI_Notebook.prefab";
    const string ScenePath = "Assets/Scenes/MVP/MVP_UI.unity";
    const string MainScenePath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    // Made by WristDisplayBuilder (transparent unlit blue); reused as the glass of the left page's screen.
    const string HologramMaterialPath = ContentRoot + "/UI_Hologram.mat";

    // Notebook size (metres): an A5 notebook; open, the two-page spread is 2 x Width across.
    const float Width = 0.15f;   // one page, along X
    const float Length = 0.21f;  // along Z (top of the page is -Z)
    const float Thickness = 0.015f;
    const float CoverThickness = 0.003f;
    const float Margin = 0.012f;

    // Page text lies flat, readable from +Y with its top toward -Z (the 180 about Y spins it on the page so it reads
    // upright the way NotebookOpen turns the notebook; without it the text reads upside down).
    static readonly Quaternion PageTextRotation = Quaternion.Euler(90f, 180f, 0f);
    // The cover's rotation once fully open (NotebookOpen: -openAngle about Z, openAngle 180).
    static readonly Quaternion CoverOpen = Quaternion.Euler(0f, 0f, -180f);

    [MenuItem("High Stakes/UI/Build Notebook")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Notebook] Stop Play mode first, then run Build Notebook again.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("[Notebook] " + ScenePath + " not found. Run High Stakes > UI > Build Wrist Display first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // opening MVP_UI replaces the open scene

        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        if (kit == null)
        {
            Debug.LogError("[Notebook] Style kit not found at " + KitPath + ". Pull main (day-zero) first.");
            return;
        }
        if (TMP_Settings.defaultFontAsset == null)
            Debug.LogWarning("[Notebook] No default TextMeshPro font. Import it via Window > TextMeshPro > Import TMP Essential Resources, then re-run.");

        WristDisplayBuilder.EnsureFolder(MockFolder);
        WristDisplayBuilder.EnsureFolder(ItemFolder);
        BuildPrefab(kit);
        BuildMockItems();
        AssetDatabase.SaveAssets();

        BuildScene();
        AddToMain();
        Debug.Log("[Notebook] Built " + PrefabPath + " and added it to " + ScenePath + " and " + MainScenePath + ". Play: N opens the notebook, F picks up / takes, B next item.");
    }

    // ---------------------------------------------------------------- prefab

    static void BuildPrefab(SharedStyleKit kit)
    {
        // Notebook space: origin on the spine, +Y out of the page (NotebookOpen turns it toward the eyes), -Z toward
        // the top of the page, +X toward the reader's left. Closed, the book lies on the right (-X) of the spine.
        var root = new GameObject("UI_Notebook");

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(root.transform, false);
        float pagesTop = Thickness - CoverThickness; // pages end where the closed front cover begins
        float rightX = -Width / 2f;

        // Back cover and the block of cream pages: the right page of the open spread
        WristDisplayBuilder.Prim("BackCover", visuals, PrimitiveType.Cube, new Vector3(rightX, CoverThickness / 2f, 0),
            new Vector3(Width, CoverThickness, Length), kit.woodDark);
        WristDisplayBuilder.Prim("Pages", visuals, PrimitiveType.Cube, new Vector3(rightX, (CoverThickness + pagesTop) / 2f, 0),
            new Vector3(Width - 0.008f, pagesTop - CoverThickness, Length - 0.008f), kit.plasterCream);
        WristDisplayBuilder.Prim("Spine", visuals, PrimitiveType.Cube, new Vector3(0, Thickness / 2f, 0),
            new Vector3(0.006f, Thickness, Length), kit.woodDark);

        // Card slot near the bottom of the right page: a brass-edged pocket with a keycard tucked into it
        float slotZ = Length / 2f - 0.035f;
        WristDisplayBuilder.Prim("SlotFrame", visuals, PrimitiveType.Cube, new Vector3(rightX, pagesTop + 0.0008f, slotZ),
            new Vector3(0.1f, 0.0016f, 0.05f), kit.brassGold);
        WristDisplayBuilder.Prim("SlotPocket", visuals, PrimitiveType.Cube, new Vector3(rightX, pagesTop + 0.0009f, slotZ + 0.002f),
            new Vector3(0.092f, 0.0018f, 0.042f), kit.chipBlack);
        var slotCard = WristDisplayBuilder.Prim("SlotCard", visuals, PrimitiveType.Cube, new Vector3(rightX, pagesTop + 0.0005f, slotZ - 0.03f),
            new Vector3(0.085f, 0.001f, 0.054f), kit.keycardPlastic);
        slotCard.SetActive(false);

        // Right page text: the notes list, above the card slot
        float textTop = -Length / 2f + Margin, textBottom = slotZ - 0.06f;
        var rightPage = new GameObject("RightPage");
        rightPage.transform.SetParent(visuals.transform, false);
        var pageText = PageText("NotesText", rightPage, new Vector3(rightX, pagesTop + 0.0003f, (textTop + textBottom) / 2f),
            PageTextRotation, new Vector2(Width - 2f * Margin, textBottom - textTop), kit.chipBlack.color, NotebookLogic.EmptyPage);

        // Front cover, hinged on the spine. Its underside becomes the left page once it has swung over: a dark screen
        // with blue glass and cyan text. Everything on the underside is placed and turned so it reads upright when the
        // cover lies open (local = inverse(open cover rotation) x open pose).
        var lid = new GameObject("Lid");
        lid.transform.SetParent(visuals.transform, false);
        lid.transform.localPosition = new Vector3(0, pagesTop, 0);
        WristDisplayBuilder.Prim("FrontCover", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, CoverThickness / 2f, 0),
            new Vector3(Width, CoverThickness, Length), kit.woodDark);
        WristDisplayBuilder.Prim("Clasp", lid, PrimitiveType.Cube, new Vector3(-Width + 0.008f, CoverThickness, 0),
            new Vector3(0.016f, 0.003f, 0.024f), kit.brassGold);
        WristDisplayBuilder.Prim("Ribbon", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, CoverThickness, 0),
            new Vector3(0.006f, 0.001f, Length), kit.carpetRed);

        WristDisplayBuilder.Prim("ScreenBezel", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, -0.0004f, 0),
            new Vector3(Width - 0.016f, 0.0008f, Length - 0.02f), kit.chipBlack);
        var glass = AssetDatabase.LoadAssetAtPath<Material>(HologramMaterialPath);
        if (glass != null)
            WristDisplayBuilder.Prim("ScreenGlass", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, -0.0009f, 0),
                new Vector3(Width - 0.024f, 0.0002f, Length - 0.03f), glass);
        else
            Debug.LogWarning("[Notebook] " + HologramMaterialPath + " not found (run Build Wrist Display); the screen will be plain dark.");
        var leftPage = new GameObject("LeftPage");
        leftPage.transform.SetParent(lid.transform, false);
        var screenText = PageText("ScreenText", leftPage, new Vector3(-Width / 2f, -0.0012f, 0),
            Quaternion.Inverse(CoverOpen) * PageTextRotation, new Vector2(Width - 2f * Margin - 0.008f, Length - 2f * Margin - 0.01f),
            kit.accentCyan, NotebookLogic.EmptyScreen);

        var open = root.AddComponent<NotebookOpen>();
        WristDisplayBuilder.Set(open, "visuals", visuals);
        WristDisplayBuilder.Set(open, "lid", lid.transform);
        var openSo = new SerializedObject(open);
        var content = openSo.FindProperty("pageContent");
        content.arraySize = 2;
        content.GetArrayElementAtIndex(0).objectReferenceValue = rightPage;
        content.GetArrayElementAtIndex(1).objectReferenceValue = leftPage;
        openSo.FindProperty("openAngle").floatValue = 180f;
        openSo.ApplyModifiedPropertiesWithoutUndo();

        // Prototype inventory: look at Slot B's keycard (matched by its component name) and press the button
        var pickups = root.AddComponent<NotebookPickups>();
        var (prompt, promptText) = Prompt(kit, root);
        var pickupsSo = new SerializedObject(pickups);
        var byComponent = pickupsSo.FindProperty("byComponent");
        byComponent.arraySize = 1;
        byComponent.GetArrayElementAtIndex(0).FindPropertyRelative("componentName").stringValue = "Keycard";
        byComponent.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue =
            ItemAt(KeycardItemPath, "vip_keycard", "VIP Keycard", "From the Pit Boss. Opens the VIP lounge door at the back.");
        pickupsSo.FindProperty("book").objectReferenceValue = open;
        pickupsSo.FindProperty("prompt").objectReferenceValue = prompt;
        pickupsSo.FindProperty("promptText").objectReferenceValue = promptText;
        pickupsSo.ApplyModifiedPropertiesWithoutUndo();

        var display = root.AddComponent<NotebookDisplay>();
        WristDisplayBuilder.Set(display, "pageText", pageText);
        WristDisplayBuilder.Set(display, "screenText", screenText);
        WristDisplayBuilder.Set(display, "slotCard", slotCard);
        WristDisplayBuilder.Set(display, "book", open);
        WristDisplayBuilder.Set(display, "pickups", pickups);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    // "F  PICK UP  VIP KEYCARD": cyan text on a dark strip, placed above the looked-at item by NotebookPickups.
    static (Transform, TextMeshPro) Prompt(SharedStyleKit kit, GameObject root)
    {
        var prompt = new GameObject("Prompt");
        prompt.transform.SetParent(root.transform, false);
        const float w = 0.34f, h = 0.06f;
        // A quad faces -Z; the prompt's +Z points away from the eyes, so its front is toward them.
        WristDisplayBuilder.Prim("Backing", prompt, PrimitiveType.Quad, new Vector3(0, 0, 0.001f), new Vector3(w, h, 1f), kit.chipBlack);
        var go = new GameObject("Text");
        go.transform.SetParent(prompt.transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(w - 0.02f, h - 0.012f);
        tmp.text = "F  PICK UP";
        tmp.color = kit.accentCyan;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 1f;
        tmp.margin = Vector4.zero;
        prompt.SetActive(false);
        return (prompt.transform, tmp);
    }

    // Top-aligned wrapping text lying on a page.
    static TextMeshPro PageText(string name, GameObject parent, Vector3 pos, Quaternion rotation, Vector2 size, Color color, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        go.transform.localPosition = pos;
        go.transform.localRotation = rotation;
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = true; // fits the page's lines into the box, whatever the font's units
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 0.05f;
        tmp.margin = Vector4.zero;
        return tmp;
    }

    // ---------------------------------------------------------------- mock items

    static ItemData[] BuildMockItems()
    {
        // Stand-ins for Slot B's real items (Content/Progression). Same ItemData type, so the notebook can't tell.
        return new[]
        {
            Item("SO_MockItem_VIPKeycard", "vip_keycard", "VIP Keycard", "From the Pit Boss. Opens the VIP door."),
            Item("SO_MockItem_BarClue", "bar_clue", "Bar Note", "Four digits scribbled on a napkin behind the bar."),
        };
    }

    static ItemData Item(string file, string id, string displayName, string description) =>
        ItemAt($"{MockFolder}/{file}.asset", id, displayName, description);

    internal static ItemData ItemAt(string path, string id, string displayName, string description)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        EditorUtility.SetDirty(item);
        return item;
    }

    // ---------------------------------------------------------------- scene

    static void BuildScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Load the assets only now: opening a scene unloads unused assets, which turns earlier references into
        // empty slots.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var items = BuildMockItems();
        GameObject content = null, solo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "UI_Content") content = root;
            else if (root.name == "_SoloTest") solo = root;
        }
        if (content == null || solo == null)
        {
            Debug.LogError("[Notebook] " + ScenePath + " needs UI_Content and _SoloTest. Run High Stakes > UI > Build Wrist Display to rebuild it.");
            return;
        }

        if (content.GetComponentInChildren<NotebookDisplay>(true) == null)
            PrefabUtility.InstantiatePrefab(prefab, content.transform);

        // Mocks go under _SoloTest so they switch off when MVP_Main loads this scene (the real inventory takes over)
        var mocksTransform = solo.transform.Find("UI_Mocks");
        var mocks = mocksTransform != null ? mocksTransform.gameObject : new GameObject("UI_Mocks");
        mocks.transform.SetParent(solo.transform, false);

        var inventory = mocks.GetComponent<MockInventoryEvents>();
        if (inventory == null) inventory = mocks.AddComponent<MockInventoryEvents>();

        var so = new SerializedObject(inventory);
        var itemArray = so.FindProperty("sampleItems");
        itemArray.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            itemArray.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < items.Length; i++)
        {
            if (itemArray.GetArrayElementAtIndex(i).objectReferenceValue == null)
                Debug.LogError("[Notebook] Mock item " + i + " didn't wire up; its Pick up button will be missing.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = mocks; // the Pick up buttons are right there in the Inspector
    }

    // MVP_Main is generated by WorldBuilder (Slot D), which only adds the wrist display. Until it instantiates
    // UI_Notebook too, this adds the notebook to MVP_Main; Build MVP World would drop it again.
    static void AddToMain()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
        {
            Debug.LogWarning("[Notebook] " + MainScenePath + " not found; notebook not added to MVP_Main.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); // load after opening, as in BuildScene
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<NotebookDisplay>(true) != null)
                return; // already there
        }

        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
