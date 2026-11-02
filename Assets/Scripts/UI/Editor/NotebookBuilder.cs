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

    // Notebook size (metres): an A5 notebook; open, the two-page spread is 2 x Width across.
    const float Width = 0.15f;   // one page, along X
    const float Length = 0.21f;  // along Z (top of the page is -Z)
    const float Thickness = 0.015f;
    const float CoverThickness = 0.003f;

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

    // Heights above a page's surface (metres), so layers never fight: pencil boxes, ink, text, raised bits.
    const float FaintY = 0.0001f, InkY = 0.00015f, TextY = 0.0003f, RaisedY = 0.0008f;
    const float RowWidth = 0.12f, RowHeight = 0.011f, RowPitch = 0.0135f, FirstRowV = -0.058f;

    static void BuildPrefab(SharedStyleKit kit)
    {
        // Notebook space: origin on the spine, +Y out of the page (NotebookOpen turns it toward the eyes), -Z toward
        // the top of the page, +X toward the reader's left. Closed, the book lies on the right (-X) of the spine.
        var root = new GameObject("UI_Notebook");

        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(root.transform, false);
        float pagesTop = Thickness - CoverThickness; // pages end where the closed front cover begins
        float rightX = -Width / 2f;
        var leather = UITextures.Leather(kit);
        var paper = UITextures.Paper(kit);
        var page = UITextures.Page(kit);
        var ink = UITextures.Ink(kit);
        var pencil = UITextures.Rule(kit);
        var m = new Mats { Kit = kit, Ink = ink, Pencil = pencil };

        // Leather back cover and the block of paper pages (UITextures): the right page of the open spread
        WristDisplayBuilder.Prim("BackCover", visuals, PrimitiveType.Cube, new Vector3(rightX, CoverThickness / 2f, 0),
            new Vector3(Width, CoverThickness, Length), leather);
        float blockWidth = Width - 0.008f, blockLength = Length - 0.008f, blockHeight = pagesTop - CoverThickness;
        var block = WristDisplayBuilder.Prim("Pages", visuals, PrimitiveType.Cube, new Vector3(rightX, (CoverThickness + pagesTop) / 2f, 0),
            new Vector3(blockWidth, blockHeight, blockLength), paper);
        PageEdges(kit, visuals, block.transform.localPosition, blockWidth, blockHeight, blockLength);
        WristDisplayBuilder.Prim("Spine", visuals, PrimitiveType.Cube, new Vector3(0, Thickness / 2f, 0),
            new Vector3(0.006f, Thickness, Length), leather);

        // Page space for each page: origin on the page's surface at its centre, +Y out of the page, -Z its top, -X the
        // reader's right (At(u, v) below takes u to the right and v down). The right page sits on the paper block; the
        // left page is the cover's underside, turned so that with the cover open it lines up with the right page.
        var rightFace = Child("RightFace", visuals);
        rightFace.transform.localPosition = new Vector3(rightX, pagesTop + 0.0001f, 0);
        PageSurface(rightFace, blockWidth, blockLength, page);

        var lid = new GameObject("Lid");
        lid.transform.SetParent(visuals.transform, false);
        lid.transform.localPosition = new Vector3(0, pagesTop, 0);
        WristDisplayBuilder.Prim("FrontCover", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, CoverThickness / 2f, 0),
            new Vector3(Width, CoverThickness, Length), leather);
        WristDisplayBuilder.Prim("Clasp", lid, PrimitiveType.Cube, new Vector3(-Width + 0.008f, CoverThickness, 0),
            new Vector3(0.016f, 0.003f, 0.024f), kit.brassGold);
        WristDisplayBuilder.Prim("Ribbon", lid, PrimitiveType.Cube, new Vector3(-Width / 2f, CoverThickness, 0),
            new Vector3(0.006f, 0.001f, Length), kit.carpetRed);
        var leftFace = Child("LeftFace", lid);
        leftFace.transform.localPosition = new Vector3(-Width / 2f, -0.0005f, 0); // the cover's underside is lid -Y
        leftFace.transform.localRotation = Quaternion.Inverse(CoverOpen);
        WristDisplayBuilder.Prim("LeftSheet", leftFace, PrimitiveType.Cube, new Vector3(0, -0.0003f, 0),
            new Vector3(blockWidth, 0.0004f, blockLength), paper);
        PageSurface(leftFace, blockWidth, blockLength, page);

        // Spreads: 0 = items, 1 = case file (map + leads). Their content only shows once the cover is mostly open.
        var rightPage = Child("RightPage", rightFace);
        var rightItems = Child("Items", rightPage);
        var rightCase = Child("CaseFile", rightPage);
        var leftPage = Child("LeftPage", leftFace);
        var leftItems = Child("Items", leftPage);
        var leftCase = Child("CaseFile", leftPage);

        // ITEMS (right): boxed rows, then the card case
        Title(rightItems, "ITEMS");
        var itemRows = Rows(rightItems, NotebookLogic.MaxVisible, m);
        var noItems = Note(rightItems, "NoItems", 0f, FirstRowV, RowWidth, RowHeight, NotebookLogic.EmptyPage, 0.06f);
        Divider(rightItems, "CARD CASE", 0.008f, m);
        const float SlotV = 0.065f;
        var cardSlot = Child("CardSlot", rightItems);
        WristDisplayBuilder.Prim("SlotFrame", cardSlot, PrimitiveType.Cube, At(0, SlotV, RaisedY), new Vector3(0.1f, 0.0016f, 0.05f), kit.brassGold);
        WristDisplayBuilder.Prim("SlotPocket", cardSlot, PrimitiveType.Cube, At(0, SlotV + 0.002f, RaisedY + 0.0001f),
            new Vector3(0.092f, 0.0018f, 0.042f), kit.chipBlack);
        var slotCard = WristDisplayBuilder.Prim("SlotCard", cardSlot, PrimitiveType.Cube, At(0, SlotV - 0.02f, RaisedY - 0.0003f),
            new Vector3(0.085f, 0.001f, 0.054f), kit.keycardPlastic);
        slotCard.SetActive(false);

        // ITEM (left): portrait box with an icon, name and kind, then the note in an info box, then control hints
        Title(leftItems, "ITEM");
        Outline(leftItems, "Portrait", At(-0.03f, -0.04f, InkY), 0.032f, 0.032f, 0.0008f, ink);
        var iconCard = IconCard(leftItems, At(-0.03f, -0.04f, RaisedY), 0.022f, kit);
        var iconNote = IconNote(leftItems, At(-0.03f, -0.04f, RaisedY), 0.022f, m);
        var itemName = Note(leftItems, "ItemName", 0.025f, -0.046f, 0.066f, 0.012f, NotebookLogic.EmptyScreen, 0.08f);
        var itemKind = Note(leftItems, "ItemKind", 0.025f, -0.033f, 0.066f, 0.008f, "", 0.05f);
        itemKind.color = Faded;
        Divider(leftItems, "NOTE", -0.012f, m);
        var itemNote = InfoBox(leftItems, "NoteBox", 0.03f, 0.066f, m);
        var hint = Note(leftItems, "Hint", 0f, 0.078f, RowWidth, 0.01f, "", 0.045f);
        hint.alignment = TextAlignmentOptions.Center;

        // LEADS (right): boxed rows, newest first, then what the Guide said
        Title(rightCase, "LEADS");
        var leadRows = Rows(rightCase, LeadsLogic.Rows, m);
        var noLeads = Note(rightCase, "NoLeads", 0f, FirstRowV, RowWidth, RowHeight, LeadsLogic.NoLeads, 0.06f);
        Divider(rightCase, "GUIDE", 0.012f, m);
        var guideQuote = InfoBox(rightCase, "GuideBox", 0.052f, 0.072f, m);

        // MAP (left): the map in a framed box, then where you are
        Title(leftCase, "MAP");
        var caseFile = root.AddComponent<NotebookCaseFile>();
        BuildMap(leftCase, -0.017f, caseFile, m);
        Divider(leftCase, "YOU ARE HERE", 0.045f, m);
        Outline(leftCase, "HereBox", At(0, 0.07f, InkY), RowWidth, 0.034f, 0.0008f, ink);
        IconPin(leftCase, At(-0.05f, 0.07f, RaisedY), kit);
        var hereName = Note(leftCase, "HereName", 0.006f, 0.062f, 0.096f, 0.01f, "", 0.065f);
        var hereNote = Note(leftCase, "HereNote", 0.006f, 0.076f, 0.096f, 0.012f, MapLogic.OffMap, 0.05f);
        hereNote.alignment = TextAlignmentOptions.TopLeft;
        WristDisplayBuilder.Set(caseFile, "hereName", hereName);
        WristDisplayBuilder.Set(caseFile, "hereNote", hereNote);
        SetArray(caseFile, "leadRows", leadRows);
        WristDisplayBuilder.Set(caseFile, "noLeadsText", noLeads);
        WristDisplayBuilder.Set(caseFile, "guideQuote", guideQuote);

        // Bookmark ribbon hanging from the top of the left page, with the open spread's icon on it
        var ribbonIcons = Ribbon(leftPage, kit, m);

        // Spiral binding: steel coils across the spine, shown with the pages
        var binding = Child("Binding", visuals);
        for (int i = 0; i < 18; i++)
        {
            float z = -Length / 2f + 0.012f + i * (Length - 0.024f) / 17f;
            var coil = WristDisplayBuilder.Prim("Coil", binding, PrimitiveType.Cylinder, new Vector3(0, pagesTop + 0.0015f, z),
                new Vector3(0.0016f, 0.007f, 0.0016f), kit.steelCool); // a cylinder is 2 units tall: 14 mm across the spine
            coil.transform.localRotation = Quaternion.Euler(0, 12f, 90f); // lying across the spine, a little slanted
        }

        var open = root.AddComponent<NotebookOpen>();
        WristDisplayBuilder.Set(open, "visuals", visuals);
        WristDisplayBuilder.Set(open, "lid", lid.transform);
        SetArray(open, "pageContent", rightPage, leftPage, binding);
        var openSo = new SerializedObject(open);
        openSo.FindProperty("openAngle").floatValue = 180f;
        openSo.FindProperty("distance").floatValue = 0.32f; // close enough to read without leaning in
        openSo.FindProperty("drop").floatValue = 0.1f;
        openSo.ApplyModifiedPropertiesWithoutUndo();

        var pages = root.AddComponent<NotebookPages>();
        SetArray(pages, "leftSpreads", leftItems, leftCase);
        SetArray(pages, "rightSpreads", rightItems, rightCase);
        SetArray(pages, "ribbonIcons", ribbonIcons);
        WristDisplayBuilder.Set(pages, "book", open);
        BuildTabs(leftFace, pages, m);

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
        SetArray(display, "rows", itemRows);
        WristDisplayBuilder.Set(display, "emptyText", noItems);
        WristDisplayBuilder.Set(display, "itemName", itemName);
        WristDisplayBuilder.Set(display, "itemKind", itemKind);
        WristDisplayBuilder.Set(display, "itemNote", itemNote);
        WristDisplayBuilder.Set(display, "hintText", hint);
        WristDisplayBuilder.Set(display, "iconCard", iconCard);
        WristDisplayBuilder.Set(display, "iconNote", iconNote);
        WristDisplayBuilder.Set(display, "slotCard", slotCard);
        WristDisplayBuilder.Set(display, "book", open);
        WristDisplayBuilder.Set(display, "pickups", pickups);
        WristDisplayBuilder.Set(display, "pages", pages);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    // The materials most page pieces need, passed around together.
    struct Mats
    {
        public SharedStyleKit Kit;
        public Material Ink, Pencil;
    }

    static readonly Color Faded = new Color(UITextures.InkColor.r, UITextures.InkColor.g, UITextures.InkColor.b, 0.6f);

    // A point in page space: u to the reader's right, v down the page, y up off the page.
    static Vector3 At(float u, float v, float y) => new Vector3(-u, y, v);

    // The printed page (UITextures.Page: kraft, dashed border, corner dots) as a quad lying on the page.
    static void PageSurface(GameObject face, float w, float l, Material page)
    {
        var quad = WristDisplayBuilder.Prim("PageSurface", face, PrimitiveType.Quad, Vector3.zero, new Vector3(w, l, 1f), page);
        quad.transform.localRotation = PageTextRotation; // texture top toward the top of the page, readable from +Y
    }

    // A page's title, big and centred at the top ("ITEMS", "MAP").
    static void Title(GameObject page, string text)
    {
        var title = PageText("Title", page, At(0, -0.078f, TextY), PageTextRotation, new Vector2(0.1f, 0.018f), UITextures.InkColor, text);
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = false;
        title.fontSize = 0.13f;
    }

    // A section label between two short ink lines with a small diamond at each outer end: "--- NOTE ---".
    static void Divider(GameObject page, string label, float v, Mats m)
    {
        var divider = Child("Divider_" + label, page);
        var text = PageText("Label", divider, At(0, v, TextY), PageTextRotation, new Vector2(0.05f, 0.007f), UITextures.InkColor, label);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.fontSize = 0.045f;
        float gap = 0.006f + label.Length * 0.0018f; // clear of the label's ends
        foreach (float side in new[] { -1f, 1f })
        {
            float from = side * gap, to = side * 0.055f;
            WristDisplayBuilder.Prim("Line", divider, PrimitiveType.Cube, At((from + to) / 2f, v, InkY),
                new Vector3(Mathf.Abs(to - from), 0.0001f, 0.0004f), m.Ink);
            WristDisplayBuilder.Prim("End", divider, PrimitiveType.Cube, At(to, v, InkY), new Vector3(0.0018f, 0.0001f, 0.0018f), m.Ink)
                .transform.localRotation = Quaternion.Euler(0, 45f, 0);
        }
    }

    // `count` boxed list rows from FirstRowV down, each a NotebookRow: pencil box (always), ink box (highlighted),
    // a circle on the left with an ink dot (ticked) and the row's text.
    static Object[] Rows(GameObject page, int count, Mats m)
    {
        var rows = new Object[count];
        for (int r = 0; r < count; r++)
        {
            float v = FirstRowV + r * RowPitch;
            var row = Child("Row" + r, page);
            Outline(row, "Pencil", At(0, v, FaintY), RowWidth, RowHeight, 0.0005f, m.Pencil);
            var inkBox = Outline(row, "Ink", At(0, v, InkY), RowWidth, RowHeight, 0.0008f, m.Ink);
            float circleU = -RowWidth / 2f + 0.0065f;
            Disc(row, "Ring", At(circleU, v, InkY), 0.0065f, m.Ink);
            Disc(row, "Hole", At(circleU, v, InkY + 0.00005f), 0.0047f, m.Kit.plasterCream);
            var tick = Disc(row, "Tick", At(circleU, v, InkY + 0.0001f), 0.003f, m.Ink);
            var label = Note(row, "Text", 0.006f, v, RowWidth - 0.018f, RowHeight - 0.003f, "", 0.065f);

            var component = row.AddComponent<NotebookRow>();
            WristDisplayBuilder.Set(component, "label", label);
            WristDisplayBuilder.Set(component, "inkBox", inkBox);
            WristDisplayBuilder.Set(component, "tick", tick);
            rows[r] = component;
        }
        return rows;
    }

    // A framed box with wrapping text inside; returns the text. `v` is its centre, `height` its height.
    static TextMeshPro InfoBox(GameObject page, string name, float v, float height, Mats m)
    {
        Outline(page, name, At(0, v, InkY), RowWidth, height, 0.0008f, m.Ink);
        var body = PageText(name + "Text", page, At(0, v, TextY), PageTextRotation,
            new Vector2(RowWidth - 0.01f, height - 0.008f), UITextures.InkColor, "");
        body.fontSizeMax = 0.06f;
        return body;
    }

    // One line of text, left-aligned and vertically centred in a box centred at (u, v), shrinking to fit up to `maxSize`.
    static TextMeshPro Note(GameObject parent, string name, float u, float v, float width, float height, string text, float maxSize)
    {
        var note = PageText(name, parent, At(u, v, TextY), PageTextRotation, new Vector2(width, height), UITextures.InkColor, text);
        note.alignment = TextAlignmentOptions.MidlineLeft;
        note.fontSizeMax = maxSize;
        return note;
    }

    // A flat disc lying on the page (a squashed cylinder; Unity's is 1 wide and 2 tall).
    static GameObject Disc(GameObject parent, string name, Vector3 pos, float diameter, Material material) =>
        WristDisplayBuilder.Prim(name, parent, PrimitiveType.Cylinder, pos, new Vector3(diameter, 0.00005f, diameter), material);

    // Icons built from kit pieces, `size` across: a keycard (white card, brass stripe), a note (cream sheet with ink
    // lines), a map pin (red disc with an ink dot).
    static GameObject IconCard(GameObject parent, Vector3 centre, float size, SharedStyleKit kit)
    {
        var icon = Child("IconCard", parent);
        icon.transform.localPosition = centre;
        WristDisplayBuilder.Prim("Card", icon, PrimitiveType.Cube, Vector3.zero, new Vector3(size, 0.0006f, size * 0.63f), kit.keycardPlastic);
        WristDisplayBuilder.Prim("Stripe", icon, PrimitiveType.Cube, new Vector3(0, 0.0002f, -size * 0.12f),
            new Vector3(size, 0.0006f, size * 0.12f), kit.brassGold);
        return icon;
    }

    static GameObject IconNote(GameObject parent, Vector3 centre, float size, Mats m)
    {
        var icon = Child("IconNote", parent);
        icon.transform.localPosition = centre;
        WristDisplayBuilder.Prim("Sheet", icon, PrimitiveType.Cube, Vector3.zero, new Vector3(size * 0.75f, 0.0006f, size), m.Kit.plasterCream);
        for (int i = 0; i < 4; i++)
            WristDisplayBuilder.Prim("Line", icon, PrimitiveType.Cube, new Vector3(0, 0.0004f, -size * 0.3f + i * size * 0.2f),
                new Vector3(size * 0.5f, 0.0002f, size * 0.04f), m.Ink);
        return icon;
    }

    static GameObject IconPin(GameObject parent, Vector3 centre, SharedStyleKit kit)
    {
        var icon = Child("IconPin", parent);
        icon.transform.localPosition = centre;
        Disc(icon, "Head", Vector3.zero, 0.007f, kit.carpetRed);
        Disc(icon, "Dot", new Vector3(0, 0.0001f, 0), 0.0025f, kit.chipWhite);
        return icon;
    }

    // The bookmark ribbon: a red strip from just above the left page's top edge, near its outer side, ending in a
    // forked tail, with one icon per spread on it (NotebookPages shows the open one). Returns the icons.
    static Object[] Ribbon(GameObject leftPage, SharedStyleKit kit, Mats m)
    {
        var ribbon = Child("Bookmark", leftPage);
        const float U = -0.048f, Top = -0.108f, Bottom = -0.072f, W = 0.011f;
        WristDisplayBuilder.Prim("Strip", ribbon, PrimitiveType.Cube, At(U, (Top + Bottom) / 2f, RaisedY),
            new Vector3(W, 0.0004f, Bottom - Top), kit.carpetRed);
        foreach (float side in new[] { -1f, 1f })
            WristDisplayBuilder.Prim("Tail", ribbon, PrimitiveType.Cube, At(U + side * W * 0.25f, Bottom + 0.004f, RaisedY),
                new Vector3(W * 0.5f, 0.0004f, 0.009f), kit.carpetRed).transform.localRotation = Quaternion.Euler(0, side * 18f, 0);
        var card = IconCard(ribbon, At(U, -0.082f, RaisedY + 0.0004f), 0.008f, kit);
        var pin = IconPin(ribbon, At(U, -0.082f, RaisedY + 0.0004f), kit);
        pin.transform.localScale = Vector3.one * 0.8f;
        return new Object[] { card, pin };
    }

    // The map page's framed map (MapLogic), centred at `v`: a pencil outline for every room, an ink one once visited,
    // labels, doors between visited rooms and the red pin. Map U (right) and V (down) are page u and v around the box.
    static void BuildMap(GameObject page, float v, NotebookCaseFile caseFile, Mats m)
    {
        const float Line = 0.0009f; // ink line width
        Outline(page, "MapBox", At(0, v, InkY), 0.124f, 0.1f, 0.0008f, m.Ink);
        var map = Child("Map", page);
        map.transform.localPosition = At(0, v, 0);

        var areas = MapLogic.Areas;
        var labels = new Object[areas.Length];
        var outlines = new Object[areas.Length];
        for (int i = 0; i < areas.Length; i++)
        {
            var a = areas[i];
            Outline(map, "Pencil_" + a.Label, At(a.U, a.V, FaintY), a.Width, a.Height, Line * 0.6f, m.Pencil);
            var outline = Outline(map, "Room_" + a.Label, At(a.U, a.V, InkY), a.Width, a.Height, Line, m.Ink);
            outline.SetActive(false);
            outlines[i] = outline;

            var label = PageText("Label_" + a.Label, map, At(a.U, a.V, TextY), PageTextRotation,
                new Vector2(a.Width - 0.005f, Mathf.Min(a.Height - 0.005f, 0.01f)), UITextures.InkColor, MapLogic.Unknown);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSizeMax = 0.06f; // same size in every room unless a name has to shrink to fit
            labels[i] = label;
        }

        // Doors: a line from one room's edge to the other's, through the middle of where the rooms face each other
        var paths = new Object[MapLogic.Paths.Length];
        for (int p = 0; p < MapLogic.Paths.Length; p++)
        {
            MapArea a = areas[MapLogic.Paths[p][0]], b = areas[MapLogic.Paths[p][1]];
            float uLow = Mathf.Max(a.U - a.Width / 2f, b.U - b.Width / 2f), uHigh = Mathf.Min(a.U + a.Width / 2f, b.U + b.Width / 2f);
            Vector3 pos, size;
            if (uLow < uHigh) // one above the other: a vertical line
            {
                var (upper, lower) = a.V < b.V ? (a, b) : (b, a);
                float from = upper.V + upper.Height / 2f, to = lower.V - lower.Height / 2f;
                pos = At((uLow + uHigh) / 2f, (from + to) / 2f, InkY);
                size = new Vector3(Line, 0.0001f, to - from);
            }
            else // side by side: a horizontal line
            {
                var (left, right) = a.U < b.U ? (a, b) : (b, a);
                float vLow = Mathf.Max(a.V - a.Height / 2f, b.V - b.Height / 2f), vHigh = Mathf.Min(a.V + a.Height / 2f, b.V + b.Height / 2f);
                float from = left.U + left.Width / 2f, to = right.U - right.Width / 2f;
                pos = At((from + to) / 2f, (vLow + vHigh) / 2f, InkY);
                size = new Vector3(to - from, 0.0001f, Line);
            }
            var path = WristDisplayBuilder.Prim("Path_" + a.Label + "_" + b.Label, map, PrimitiveType.Cube, pos, size, m.Ink);
            path.SetActive(false);
            paths[p] = path;
        }

        var pin = WristDisplayBuilder.Prim("Pin", map, PrimitiveType.Cube, At(0, 0, RaisedY), new Vector3(0.004f, 0.0006f, 0.004f), m.Kit.carpetRed);
        pin.SetActive(false);

        SetArray(caseFile, "roomLabels", labels);
        SetArray(caseFile, "roomOutlines", outlines);
        SetArray(caseFile, "paths", paths);
        WristDisplayBuilder.Set(caseFile, "pin", pin.transform);
    }

    // Square side tabs down the left page's outer edge (the cover's free edge), one per spread from the top, tucked
    // into the cover and sticking out past it, with the spread's icon on the face. Each is a PressableButton
    // (XRSimpleInteractable: poke, or point and pull the trigger) wired to NotebookPages.ShowSpread. Burgundy fabric
    // when closed, brass for the open spread, which also sticks out further (NotebookPages).
    static void BuildTabs(GameObject leftFace, NotebookPages pages, Mats m)
    {
        const float Across = 0.018f, Along = 0.02f, TabThickness = 0.0012f;
        float edgeU = -(Width / 2f); // the reader's left edge of the left page
        var tabs = new Object[2];
        var faces = new Object[2];
        for (int i = 0; i < 2; i++)
        {
            var tab = Child(i == 0 ? "Tab_Items" : "Tab_Map", leftFace);
            tab.transform.localPosition = At(edgeU - Across / 2f + 0.005f, -0.07f + i * 0.026f, -0.002f); // inside the cover
            var face = WristDisplayBuilder.Prim("Face", tab, PrimitiveType.Cube, Vector3.zero, new Vector3(Across, TabThickness, Along), m.Kit.fabricBurgundy);
            var iconAt = At(-0.003f, 0, TabThickness / 2f + 0.0003f); // on the part that sticks out
            if (i == 0) IconCard(tab, iconAt, 0.01f, m.Kit);
            else IconPin(tab, iconAt, m.Kit);

            var collider = tab.AddComponent<BoxCollider>();
            collider.size = new Vector3(Across, 0.01f, Along); // thicker than the tab so it's easy to poke
            tab.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var button = tab.AddComponent<HighStakes.TableGames.PressableButton>();
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(button.Pressed, pages.ShowSpread, i);

            tabs[i] = tab.transform;
            faces[i] = face.GetComponent<Renderer>();
        }
        SetArray(pages, "tabs", tabs);
        SetArray(pages, "tabFaces", faces);
        WristDisplayBuilder.Set(pages, "tabMaterial", m.Kit.fabricBurgundy);
        WristDisplayBuilder.Set(pages, "activeTabMaterial", m.Kit.brassGold);
        var so = new SerializedObject(pages);
        so.FindProperty("activeTabOffset").vector3Value = At(-0.005f, 0, 0); // further out past the edge
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Four thin lines around a w x h rectangle centred on `centre`, flat on the page.
    static GameObject Outline(GameObject parent, string name, Vector3 centre, float w, float h, float line, Material material)
    {
        var rect = Child(name, parent);
        rect.transform.localPosition = centre;
        WristDisplayBuilder.Prim("Top", rect, PrimitiveType.Cube, new Vector3(0, 0, -h / 2f), new Vector3(w + line, 0.0001f, line), material);
        WristDisplayBuilder.Prim("Bottom", rect, PrimitiveType.Cube, new Vector3(0, 0, h / 2f), new Vector3(w + line, 0.0001f, line), material);
        WristDisplayBuilder.Prim("Left", rect, PrimitiveType.Cube, new Vector3(w / 2f, 0, 0), new Vector3(line, 0.0001f, h), material);
        WristDisplayBuilder.Prim("Right", rect, PrimitiveType.Cube, new Vector3(-w / 2f, 0, 0), new Vector3(line, 0.0001f, h), material);
        return rect;
    }

    // Striped quads on the three outer sides of the paper block (not the spine side), so its edge reads as stacked
    // sheets. A quad is seen from its -Z side; each is turned to face outward, a hair off the block.
    static void PageEdges(SharedStyleKit kit, GameObject parent, Vector3 blockCentre, float w, float h, float l)
    {
        var edge = UITextures.PageEdge(kit);
        const float Off = 0.0002f;
        WristDisplayBuilder.Prim("PageEdgeOuter", parent, PrimitiveType.Quad, blockCentre + new Vector3(-w / 2f - Off, 0, 0),
            new Vector3(l, h, 1f), edge).transform.localRotation = Quaternion.Euler(0, 90f, 0);
        WristDisplayBuilder.Prim("PageEdgeTop", parent, PrimitiveType.Quad, blockCentre + new Vector3(0, 0, -l / 2f - Off),
            new Vector3(w, h, 1f), edge);
        WristDisplayBuilder.Prim("PageEdgeBottom", parent, PrimitiveType.Quad, blockCentre + new Vector3(0, 0, l / 2f + Off),
            new Vector3(w, h, 1f), edge).transform.localRotation = Quaternion.Euler(0, 180f, 0);
    }

    static GameObject Child(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static void SetArray(Object target, string field, params Object[] values)
    {
        var so = new SerializedObject(target);
        var array = so.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
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
        tmp.fontStyle = FontStyles.Bold; // reads on the kraft paper texture
        tmp.enableAutoSizing = true; // grows the page's lines to fill the box, whatever the font's units
        tmp.fontSizeMin = 0.001f;
        tmp.fontSizeMax = 0.3f;
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
