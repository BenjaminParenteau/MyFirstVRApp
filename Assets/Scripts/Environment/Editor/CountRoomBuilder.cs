using HighStakes.Environment;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The count floor and supervisor booth in the right half and back of the 12 x 12 m security & count room
/// (MVP_SecurityOffice; Security/Editor/SecurityOfficeBuilder furnishes the staff side on the left). Two count tables
/// with cash bricks, bill counters and count sheets on a rubber mat, a cash cart by the door, a drop safe and the
/// house-rules sign on the right wall, and a glass-fronted supervisor booth in the back-right corner overlooking the
/// tables. The booth's chair has an empty <c>NPC_Supervisor</c> anchor (standing spot, facing the window) for the
/// count supervisor NPC later; nothing else needs to move when it arrives.
///
/// Room coordinates as in HallwayBuilder: the room sits at (40,0,0), inner walls at -0.925 and 10.925, door centred on
/// x 5 in the near (-Z) wall. Kit materials only, plus a glass Material Variant of the UI hologram (the kit has no
/// transparent material) and the vault's banknote material (Security/VaultDressing) so the cash matches the vault.
/// </summary>
public static class CountRoomBuilder
{
    const string ScenePath = SecurityOfficeBuilder.ScenePath;
    const string ContentRoot = "Assets/Content/Environment/Security";
    const string PrefabPath = ContentRoot + "/Prefabs/Env_CountRoom.prefab";
    const string GlassPath = ContentRoot + "/Materials/M_BoothGlass.mat";
    const string HologramPath = "Assets/Content/UI/UI_Hologram.mat";
    const string BanknotePath = "Assets/Content/Progression/VaultDressing/Materials/Engraved banknote.mat";
    const string BandPath = "Assets/Content/Progression/VaultDressing/Materials/Cash bands.mat";
    const string RootName = "CountRoom";
    static readonly Vector3 RoomOrigin = new Vector3(40f, 0f, 0f);

    // Booth: back-right corner, front glass at z 7.4, side glass at x 7.4, door in the side wall near the back.
    const float BoothX = 7.4f, BoothZ = 7.4f, Wall = 10.925f, DoorZ0 = 9.6f, DoorZ1 = 10.5f;
    const float TableTop = 0.9f;

    static SharedStyleKit kit;
    static Material glass, cash, band;

    [MenuItem("High Stakes/Build Count Room")]
    public static void BuildMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
    }

    public static void Build()
    {
        kit = TableKit.Kit(out _);
        if (kit == null) return;
        glass = Glass();
        cash = AssetDatabase.LoadAssetAtPath<Material>(BanknotePath) ?? kit.chipGreen;
        band = AssetDatabase.LoadAssetAtPath<Material>(BandPath) ?? kit.chipWhite;

        var root = new GameObject(RootName);
        CountFloor(root.transform);
        Booth(root.transform);
        var prefab = TableKit.SavePrefab(root, PrefabPath);

        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        foreach (var old in scene.GetRootGameObjects())
            if (old.name == RootName) Object.DestroyImmediate(old);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = RootName;
        instance.transform.position = RoomOrigin;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        Debug.Log("[CountRoom] Count floor and supervisor booth placed in MVP_SecurityOffice. NPC_Supervisor marks the booth seat.");
    }

    // ---------------------------------------------------------------- count floor

    static void CountFloor(Transform root)
    {
        var floor = Group(root, "CountFloor", Vector3.zero, 0f);
        Box(kit.chipBlack, floor, "Count mat", new Vector3(7.5f, 0.008f, 4.2f), new Vector3(5.6f, 0.01f, 4.6f));
        CountTable(floor, "Count table 1", new Vector3(7.4f, 0f, 3f));
        CountTable(floor, "Count table 2", new Vector3(7.4f, 0f, 5.4f));
        CashCart(floor, new Vector3(10f, 0f, 1.3f));
        DropSafe(floor, new Vector3(10.5f, 0f, 5.4f));

        // House rules on the right wall, facing into the room.
        var sign = Group(floor, "Count room sign", new Vector3(10.9f, 2.1f, 3.4f), 90f);
        Box(kit.chipBlack, sign, "Plate", Vector3.zero, new Vector3(2.2f, 0.7f, 0.03f));
        Box(kit.brassGold, sign, "Trim", new Vector3(0f, 0.36f, 0f), new Vector3(2.2f, 0.025f, 0.035f));
        var heading = TableKit.Text(sign, "Heading", new Vector3(0f, 0.18f, -0.02f), Quaternion.identity, new Vector2(2f, 0.22f), 0.1f, 1.5f, kit.brassGold.color);
        heading.fontStyle = FontStyles.Bold;
        heading.text = "COUNT ROOM";
        var rules = TableKit.Text(sign, "Rules", new Vector3(0f, -0.13f, -0.02f), Quaternion.identity, new Vector2(2f, 0.32f), 0.05f, 0.8f, Color.white);
        rules.textWrappingMode = TextWrappingModes.Normal;
        rules.text = "TWO-PERSON RULE  ·  ALL BILLS FACE UP\nNO BAGS  ·  NO PHONES  ·  SUPERVISOR SIGNS EVERY STRAP";
    }

    // A standing count table (top at 0.9 m), long side along X, worked from the -Z side.
    static void CountTable(Transform parent, string name, Vector3 position)
    {
        var t = Group(parent, name, position, 0f);
        Box(kit.steelCool, t, "Top", new Vector3(0f, TableTop - 0.025f, 0f), new Vector3(2.4f, 0.05f, 1.1f), true);
        Box(kit.feltGreen, t, "Count mat", new Vector3(0f, TableTop + 0.002f, -0.05f), new Vector3(2.2f, 0.004f, 0.85f));
        foreach (float x in new[] { -1.12f, 1.12f })
            foreach (float z in new[] { -0.48f, 0.48f })
                Box(kit.chipBlack, t, "Leg", new Vector3(x, (TableTop - 0.05f) / 2f, z), new Vector3(0.06f, TableTop - 0.05f, 0.06f));
        Box(kit.chipBlack, t, "Undershelf", new Vector3(0f, 0.25f, 0f), new Vector3(2.3f, 0.03f, 1f));

        // Bill counters at the back, display toward the worker.
        foreach (float x in new[] { -0.65f, 0.65f })
        {
            var counter = new Vector3(x, TableTop + 0.11f, 0.3f);
            Box(kit.chipBlack, t, "Bill counter", counter, new Vector3(0.32f, 0.22f, 0.3f));
            Box(kit.steelCool, t, "Bill hopper", counter + new Vector3(0f, 0.12f, 0.05f), new Vector3(0.24f, 0.02f, 0.14f));
            Box(kit.screenGlow, t, "Counter display", counter + new Vector3(0f, 0.05f, -0.151f), new Vector3(0.2f, 0.05f, 0.004f));
            var readout = TableKit.Text(t, "Counter readout", counter + new Vector3(0f, 0.05f, -0.155f), Quaternion.identity, new Vector2(0.18f, 0.04f), 0.05f, 0.3f, kit.chipBlack.color);
            readout.text = "$10,000";
        }

        // Strapped bricks in front of the counters, a few loose ones, and a count sheet.
        for (int i = 0; i < 5; i++)
            Brick(t, new Vector3(-0.9f + i * 0.2f, TableTop, -0.05f), 2 + i % 3);
        for (int i = 0; i < 3; i++)
            Brick(t, new Vector3(0.35f + i * 0.2f, TableTop, -0.05f), 1 + i % 2);
        Box(kit.chipBlack, t, "Clipboard", new Vector3(0.95f, TableTop + 0.006f, -0.25f), new Vector3(0.24f, 0.008f, 0.32f));
        Box(kit.chipWhite, t, "Count sheet", new Vector3(0.95f, TableTop + 0.011f, -0.26f), new Vector3(0.21f, 0.002f, 0.28f));
        var sheet = TableKit.Text(t, "Count sheet text", new Vector3(0.95f, TableTop + 0.013f, -0.26f), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.19f, 0.26f), 0.05f, 0.2f, Color.black);
        sheet.textWrappingMode = TextWrappingModes.Normal;
        sheet.text = "<b>NIGHT COUNT</b>\n100s  ____\n50s   ____\n20s   ____\n\nCOUNTER 1 ____\nCOUNTER 2 ____\nSUP. SIGN ____";
    }

    // A stack of strapped $10,000 bundles (banknote face on top, paper band across the middle).
    static void Brick(Transform parent, Vector3 position, int bundles)
    {
        const float h = 0.012f;
        Box(cash, parent, "Cash bundles", position + new Vector3(0f, bundles * h / 2f, 0f), new Vector3(0.156f, bundles * h, 0.066f));
        Box(band, parent, "Strap", position + new Vector3(0f, bundles * h / 2f, 0f), new Vector3(0.035f, bundles * h + 0.002f, 0.068f));
    }

    static void CashCart(Transform parent, Vector3 position)
    {
        var c = Group(parent, "Cash cart", position, 0f);
        Box(kit.steelCool, c, "Top tray", new Vector3(0f, 0.85f, 0f), new Vector3(1f, 0.03f, 0.6f), true);
        Box(kit.steelCool, c, "Bottom tray", new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.03f, 0.6f));
        foreach (float x in new[] { -0.47f, 0.47f })
            foreach (float z in new[] { -0.27f, 0.27f })
            {
                Box(kit.steelCool, c, "Post", new Vector3(x, 0.5f, z), new Vector3(0.03f, 0.9f, 0.03f));
                Box(kit.chipBlack, c, "Caster", new Vector3(x, 0.04f, z), new Vector3(0.06f, 0.08f, 0.06f));
            }
        Box(kit.steelCool, c, "Push handle", new Vector3(-0.53f, 1f, 0f), new Vector3(0.03f, 0.03f, 0.5f));
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
                Brick(c, new Vector3(-0.33f + i * 0.21f, 0.865f, -0.18f + j * 0.18f), 3 + (i + j) % 3);
        Box(kit.chipBlack, c, "Cash bag", new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.28f, 0.4f));
    }

    // Floor safe against the right wall, door and dial facing into the room (-X).
    static void DropSafe(Transform parent, Vector3 position)
    {
        var s = Group(parent, "Drop safe", position, 90f);
        Box(kit.chipBlack, s, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 1f, 0.7f), true);
        Box(kit.steelCool, s, "Door", new Vector3(0f, 0.5f, -0.355f), new Vector3(0.62f, 0.86f, 0.02f));
        var dial = Box(kit.brassGold, s, "Dial", new Vector3(0.12f, 0.62f, -0.375f), new Vector3(0.12f, 0.02f, 0.12f), false, PrimitiveType.Cylinder);
        dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Box(kit.brassGold, s, "Handle", new Vector3(-0.15f, 0.5f, -0.38f), new Vector3(0.04f, 0.22f, 0.04f));
        Box(kit.steelCool, s, "Drop slot", new Vector3(0f, 0.92f, -0.36f), new Vector3(0.36f, 0.03f, 0.02f));
        var label = TableKit.Text(s, "Label", new Vector3(0f, 0.33f, -0.367f), Quaternion.identity, new Vector2(0.5f, 0.06f), 0.05f, 0.4f, kit.brassGold.color);
        label.text = "DROP SAFE";
    }

    // ---------------------------------------------------------------- supervisor booth

    static void Booth(Transform root)
    {
        var b = Group(root, "SupervisorBooth", Vector3.zero, 0f);
        float frontLen = Wall - BoothX, sideLen = Wall - BoothZ;

        // Front: steel to 1 m, glass to 2.3 m, header to 2.6 m, mullions.
        GlassWall(b, "Front", new Vector3(BoothX + frontLen / 2f, 0f, BoothZ), frontLen, 0f);
        foreach (float x in new[] { BoothX, BoothX + frontLen / 3f, BoothX + frontLen * 2f / 3f })
            Box(kit.chipBlack, b, "Mullion", new Vector3(x, 1.3f, BoothZ), new Vector3(0.07f, 2.6f, 0.1f), true);

        // Side toward the room: glass wall up to the door, the door opening, a short solid piece against the back wall.
        float glassLen = DoorZ0 - BoothZ;
        GlassWall(b, "Side", new Vector3(BoothX, 0f, BoothZ + glassLen / 2f), glassLen, 90f);
        Box(kit.chipBlack, b, "Mullion", new Vector3(BoothX, 1.3f, BoothZ + glassLen / 2f), new Vector3(0.1f, 2.6f, 0.07f), true);
        Box(kit.chipBlack, b, "Door post", new Vector3(BoothX, 1.3f, DoorZ0), new Vector3(0.1f, 2.6f, 0.08f), true);
        Box(kit.chipBlack, b, "Door post", new Vector3(BoothX, 1.3f, DoorZ1), new Vector3(0.1f, 2.6f, 0.08f), true);
        Box(kit.steelCool, b, "Door header", new Vector3(BoothX, 2.35f, (DoorZ0 + DoorZ1) / 2f), new Vector3(0.08f, 0.5f, DoorZ1 - DoorZ0), true);
        Box(kit.steelCool, b, "Side panel", new Vector3(BoothX, 1.3f, (DoorZ1 + Wall) / 2f), new Vector3(0.08f, 2.6f, Wall - DoorZ1), true);

        // Roof, an emissive panel under it (no extra realtime light) and the booth's floor.
        Box(kit.steelCool, b, "Roof", new Vector3(BoothX + frontLen / 2f, 2.63f, BoothZ + sideLen / 2f), new Vector3(frontLen + 0.08f, 0.06f, sideLen + 0.08f));
        Box(kit.screenGlow, b, "Ceiling panel", new Vector3(BoothX + frontLen / 2f, 2.595f, BoothZ + sideLen / 2f), new Vector3(1.2f, 0.01f, 0.6f));
        Box(kit.chipBlack, b, "Booth floor", new Vector3(BoothX + frontLen / 2f, 0.008f, BoothZ + sideLen / 2f), new Vector3(frontLen, 0.01f, sideLen));

        // Sign on the front header, facing the count floor.
        var sign = TableKit.Text(b, "Sign", new Vector3(BoothX + frontLen / 2f, 2.45f, BoothZ - 0.051f), Quaternion.identity, new Vector2(frontLen - 0.3f, 0.2f), 0.1f, 1.2f, kit.brassGold.color);
        sign.fontStyle = FontStyles.Bold;
        sign.text = "COUNT SUPERVISOR";

        // Desk along the front glass, looking out over the tables.
        var desk = Group(b, "Supervisor desk", new Vector3(9.15f, 0f, 7.85f), 0f);
        Box(kit.woodDark, desk, "Desktop", new Vector3(0f, 0.75f, 0f), new Vector3(2f, 0.05f, 0.65f), true);
        Box(kit.chipBlack, desk, "Modesty panel", new Vector3(0f, 0.4f, -0.3f), new Vector3(1.9f, 0.7f, 0.03f));
        foreach (float x in new[] { -0.95f, 0.95f })
            Box(kit.chipBlack, desk, "Desk end", new Vector3(x, 0.37f, 0f), new Vector3(0.05f, 0.74f, 0.6f));
        // Monitor turned to the seat (+Z), a closed logbook, phone, and a brass desk lamp.
        Box(kit.chipBlack, desk, "Monitor bezel", new Vector3(0.45f, 1.05f, -0.15f), new Vector3(0.55f, 0.35f, 0.04f));
        Box(kit.screenGlow, desk, "Monitor screen", new Vector3(0.45f, 1.05f, -0.128f), new Vector3(0.5f, 0.3f, 0.004f));
        Box(kit.chipBlack, desk, "Monitor stand", new Vector3(0.45f, 0.83f, -0.17f), new Vector3(0.06f, 0.12f, 0.06f));
        Box(kit.fabricBurgundy, desk, "Count logbook", new Vector3(-0.3f, 0.79f, 0.05f), new Vector3(0.3f, 0.035f, 0.22f));
        Box(kit.chipBlack, desk, "Phone", new Vector3(-0.75f, 0.81f, -0.1f), new Vector3(0.22f, 0.07f, 0.18f));
        Box(kit.brassGold, desk, "Lamp stem", new Vector3(0.85f, 0.95f, -0.2f), new Vector3(0.025f, 0.35f, 0.025f));
        Box(kit.brassGold, desk, "Lamp shade", new Vector3(0.85f, 1.12f, -0.12f), new Vector3(0.16f, 0.06f, 0.18f));

        // Chair facing the window, and where the supervisor NPC will stand or sit later.
        var chair = Group(b, "Supervisor chair", new Vector3(9f, 0f, 8.75f), 0f); // backrest on the +Z side, away from the desk
        Box(kit.chipBlack, chair, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.52f, 0.1f, 0.5f), true);
        Box(kit.chipBlack, chair, "Backrest", new Vector3(0f, 0.85f, 0.23f), new Vector3(0.5f, 0.65f, 0.08f), true);
        Box(kit.steelCool, chair, "Stem", new Vector3(0f, 0.22f, 0f), new Vector3(0.06f, 0.4f, 0.06f));
        Box(kit.steelCool, chair, "Base", new Vector3(0f, 0.04f, 0f), new Vector3(0.6f, 0.05f, 0.6f));
        var npc = new GameObject("NPC_Supervisor").transform;
        npc.SetParent(b, false);
        npc.localPosition = new Vector3(9f, 0f, 8.75f);
        npc.localRotation = Quaternion.Euler(0f, 180f, 0f); // facing the window and the count tables (-Z)

        // Back corner: filing cabinet and a key cabinet on the back wall.
        Box(kit.steelCool, b, "Filing cabinet", new Vector3(10.5f, 0.65f, 10.55f), new Vector3(0.6f, 1.3f, 0.6f), true);
        for (int d = 0; d < 3; d++)
            Box(kit.chipBlack, b, "Cabinet pull", new Vector3(10.5f, 0.3f + d * 0.4f, 10.24f), new Vector3(0.18f, 0.03f, 0.03f));
        Box(kit.chipBlack, b, "Key cabinet", new Vector3(8.7f, 1.6f, 10.86f), new Vector3(0.6f, 0.7f, 0.1f));
        Box(kit.brassGold, b, "Key cabinet label", new Vector3(8.7f, 1.88f, 10.805f), new Vector3(0.3f, 0.05f, 0.005f));
    }

    // Steel lower panel, glass pane and steel header, centred on position, running along local X (rotated by yaw).
    static void GlassWall(Transform parent, string name, Vector3 position, float length, float yaw)
    {
        var w = Group(parent, name + " wall", position, yaw);
        Box(kit.steelCool, w, "Lower panel", new Vector3(0f, 0.5f, 0f), new Vector3(length, 1f, 0.08f), true);
        Box(kit.chipBlack, w, "Sill", new Vector3(0f, 1.02f, 0f), new Vector3(length, 0.04f, 0.12f));
        Box(glass, w, "Glass", new Vector3(0f, 1.65f, 0f), new Vector3(length, 1.26f, 0.02f), true);
        Box(kit.steelCool, w, "Header", new Vector3(0f, 2.45f, 0f), new Vector3(length, 0.3f, 0.08f), true);
    }

    // ---------------------------------------------------------------- helpers

    // The kit has no transparent material: a smoky-blue variant of the UI hologram (already transparent, URP-friendly).
    static Material Glass()
    {
        var parent = AssetDatabase.LoadAssetAtPath<Material>(HologramPath);
        if (parent == null) { Debug.LogWarning("[CountRoom] UI_Hologram.mat missing; the booth glass falls back to steel."); return kit.steelCool; }
        TableKit.EnsureFolder(System.IO.Path.GetDirectoryName(GlassPath).Replace('\\', '/'));
        var m = AssetDatabase.LoadAssetAtPath<Material>(GlassPath);
        if (m == null) { m = new Material(parent) { name = "M_BoothGlass", parent = parent }; AssetDatabase.CreateAsset(m, GlassPath); }
        m.SetColor("_BaseColor", new Color(0.55f, 0.72f, 0.8f, 0.16f));
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Group(Transform parent, string name, Vector3 position, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    static GameObject Box(Material material, Transform parent, string name, Vector3 position, Vector3 size, bool solid = false,
        PrimitiveType type = PrimitiveType.Cube) =>
        TableKit.Part(material, parent, name, type, position, size, true, solid);
}
