using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.Inventory;
using HighStakes.TableGames;
using HighStakes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the "housekeeping cart" puzzle that locks the casino's STAFF ONLY door (see UI/Puzzles/README.md): a shift
/// note on the cashier counter, brass table-number stands by blackjack (4), roulette (7) and the high-roller dice
/// table (2), a cleaning cart whose lockbox opens on 472 to a STAFF keycard, and a STAFF reader on the door.
/// Reuses Slot B's keycard, reader and AccessBuilder and the tables' PressableButton without changing them.
/// Edits MVP_Casino (Ashton) and MVP_VIP and the door from Zach's WorldBuilder: tell them before committing.
/// Re-running replaces only its own objects. Slot C (Pak) prototype.
/// </summary>
public static class StaffDoorPuzzleBuilder
{
    const string ContentRoot = "Assets/Content/UI/Puzzles";
    const string CartPath = ContentRoot + "/Puzzle_CleaningCart.prefab";
    const string NotePath = ContentRoot + "/Puzzle_ShiftNote.prefab";
    const string StandPath = ContentRoot + "/Puzzle_TableStand.prefab";
    const string StaffCardPath = ContentRoot + "/Item_StaffKeycard.prefab";
    const string NoteItemPath = ContentRoot + "/SO_Item_ShiftNote.asset";
    const string CardItemPath = ContentRoot + "/SO_Item_StaffKeycard.asset";
    const string CasinoPath = "Assets/Scenes/MVP/MVP_Casino.unity";
    const string VipPath = "Assets/Scenes/MVP/MVP_VIP.unity";

    const string StaffAccess = "STAFF";
    const string Code = "472";

    // The note sits on the cashier counter (top at y 1.16, front edge z 9.43), between the brass bars.
    static readonly Vector3 NotePosition = new Vector3(5.5f, 1.162f, 9.65f);
    // The cart stands along the right wall just before the STAFF ONLY door (z 8), clear of the slots (z <= 4.35),
    // the pilaster at z 6 and the door's trigger. Yaw 90: its front (-Z) faces into the room.
    static readonly Vector3 CartPosition = new Vector3(7.85f, 0f, 5.3f);
    // Reader beside the 1 m staff door, past its brass frame, in the door's space (front faces -Z).
    static readonly Vector3 ReaderLocal = new Vector3(0.75f, 1.3f, -0.1f);

    [MenuItem("High Stakes/Puzzles/Build Staff Door Puzzle")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[StaffPuzzle] Stop Play mode first, then run Build Staff Door Puzzle again.");
            return;
        }
        var kit = TableKit.Kit(out int layer);
        if (kit == null) return;
        var vipCard = AssetDatabase.LoadAssetAtPath<GameObject>(AccessBuilder.VipKeycardPath);
        if (vipCard == null)
        {
            Debug.LogError("[StaffPuzzle] " + AccessBuilder.VipKeycardPath + " not found. Pull dev (Slot B's VIP keycard) first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TableKit.EnsureFolder(ContentRoot);
        var noteItem = NotebookBuilder.ItemAt(NoteItemPath, "shift_note", "Shift Note",
            "Cart lockbox: clean the tables in order CARDS > WHEEL > DICE. The table numbers are on the brass stands. - M.");
        var cardItem = NotebookBuilder.ItemAt(CardItemPath, "staff_keycard", "Staff Keycard",
            "From the housekeeping cart. Opens the STAFF ONLY door by the cashier.");
        AssetDatabase.SaveAssets();

        var staffCard = BuildStaffCard(kit, vipCard, cardItem);
        var cart = BuildCart(kit, layer, staffCard);
        var note = BuildNote(kit, noteItem);
        var stand = BuildStand(kit);

        EditScene(CasinoPath, scene =>
        {
            Replace(scene, cart, "Puzzle_StaffCart", CartPosition, Quaternion.Euler(0f, 90f, 0f));
            Replace(scene, note, "Puzzle_ShiftNote", NotePosition, Quaternion.Euler(0f, -8f, 0f));
            PlaceStand(scene, stand, "Tables_Blackjack", "Puzzle_TableStand_Blackjack", 4, -1f);
            PlaceStand(scene, stand, "Tables_Roulette", "Puzzle_TableStand_Roulette", 7, 1f);
            AddStaffReader(scene);
        });
        EditScene(VipPath, scene => PlaceStand(scene, stand, "Tables_Dice", "Puzzle_TableStand_Dice", 2, -1f));

        AssetDatabase.SaveAssets();
        Debug.Log("[StaffPuzzle] Built the staff door puzzle in MVP_Casino and MVP_VIP. Code " + Code + ". Tell Ashton and Zach before committing those scenes.");
    }

    // ---------------------------------------------------------------- prefabs

    // A variant of Slot B's VIP card: steel, reads STAFF, opens STAFF readers, and is a notebook pickup.
    static Keycard BuildStaffCard(SharedStyleKit kit, GameObject vipCard, ItemData item)
    {
        var card = (GameObject)PrefabUtility.InstantiatePrefab(vipCard);
        card.name = "Item_StaffKeycard";
        TableKit.Set(card.GetComponent<Keycard>(), "accessId", p => p.stringValue = StaffAccess);
        var label = card.transform.Find("VIP");
        if (label != null) label.GetComponent<TMP_Text>().text = "STAFF";
        var body = card.transform.Find("Card");
        if (body != null) body.GetComponent<Renderer>().sharedMaterial = kit.steelCool;
        TableKit.SetRef(card.AddComponent<PickupItem>(), "item", item);

        var prefab = PrefabUtility.SaveAsPrefabAsset(card, StaffCardPath); // stays a variant of the VIP card
        Object.DestroyImmediate(card);
        return prefab.GetComponent<Keycard>();
    }

    // Cart space: long side along X (0.9 m), front (the player's side) on -Z, floor at y 0.
    static GameObject BuildCart(SharedStyleKit kit, int layer, Keycard staffCard)
    {
        var root = new GameObject("Puzzle_CleaningCart");
        var collider = root.AddComponent<BoxCollider>(); // the frame only, so the card on top can still be looked at
        collider.center = new Vector3(0f, 0.45f, 0f);
        collider.size = new Vector3(0.9f, 0.9f, 0.5f);

        foreach (float x in new[] { -0.42f, 0.42f })
            foreach (float z in new[] { -0.22f, 0.22f })
            {
                Part(kit.steelCool, root.transform, "Post", PrimitiveType.Cube, new Vector3(x, 0.5f, z), new Vector3(0.03f, 0.9f, 0.03f));
                var wheel = Part(kit.chipBlack, root.transform, "Wheel", PrimitiveType.Cylinder, new Vector3(x, 0.04f, z), new Vector3(0.08f, 0.015f, 0.08f));
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        foreach (float y in new[] { 0.12f, 0.5f, 0.9f })
            Part(kit.steelCool, root.transform, "Shelf", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(0.9f, 0.02f, 0.5f));
        Part(kit.steelCool, root.transform, "Handle", PrimitiveType.Cube, new Vector3(-0.5f, 1.0f, 0f), new Vector3(0.03f, 0.03f, 0.46f));
        foreach (float z in new[] { -0.22f, 0.22f })
            Part(kit.steelCool, root.transform, "HandleArm", PrimitiveType.Cube, new Vector3(-0.47f, 0.95f, z), new Vector3(0.06f, 0.03f, 0.03f));
        // Folded towels and a laundry bin
        Part(kit.plasterCream, root.transform, "Towels", PrimitiveType.Cube, new Vector3(-0.22f, 0.57f, 0f), new Vector3(0.3f, 0.12f, 0.34f));
        Part(kit.chipWhite, root.transform, "Towels", PrimitiveType.Cube, new Vector3(0.18f, 0.56f, 0f), new Vector3(0.3f, 0.1f, 0.34f));
        Part(kit.chipBlack, root.transform, "LaundryBin", PrimitiveType.Cube, new Vector3(0.15f, 0.3f, 0f), new Vector3(0.5f, 0.34f, 0.42f));

        // Lockbox on the top shelf, lid hinged at the back
        var box = new Vector3(0.22f, 0.97f, 0.04f);
        Part(kit.steelCool, root.transform, "Lockbox", PrimitiveType.Cube, box, new Vector3(0.3f, 0.12f, 0.24f));
        Part(kit.chipBlack, root.transform, "LockboxInside", PrimitiveType.Cube, box + new Vector3(0f, 0.058f, 0f), new Vector3(0.27f, 0.006f, 0.21f));
        var lid = new GameObject("Lid").transform;
        lid.SetParent(root.transform, false);
        lid.localPosition = box + new Vector3(0f, 0.06f, 0.12f);
        Part(kit.steelCool, lid, "LidPlate", PrimitiveType.Cube, new Vector3(0f, 0.01f, -0.12f), new Vector3(0.3f, 0.02f, 0.24f));
        Part(kit.brassGold, lid, "Latch", PrimitiveType.Cube, new Vector3(0f, 0f, -0.24f), new Vector3(0.05f, 0.03f, 0.01f));
        var seat = new GameObject("CardSeat").transform;
        seat.SetParent(root.transform, false);
        seat.localPosition = box + new Vector3(0f, 0.064f, 0f);
        seat.localRotation = Quaternion.Euler(90f, 0f, 0f); // card face (-Z) up

        // Keypad: a tilted brass panel at the front-left of the top shelf, like the bar's PIN pad but smaller
        var panel = new GameObject("Keypad").transform;
        panel.SetParent(root.transform, false);
        panel.localPosition = new Vector3(-0.2f, 0.91f, -0.12f);
        panel.localRotation = Quaternion.Euler(20f, 0f, 0f);
        Part(kit.brassGold, panel, "Case", PrimitiveType.Cube, new Vector3(0f, 0.19f, 0f), new Vector3(0.3f, 0.38f, 0.03f));
        Part(kit.chipBlack, panel, "Face", PrimitiveType.Cube, new Vector3(0f, 0.19f, -0.018f), new Vector3(0.28f, 0.36f, 0.006f));
        Part(kit.chipBlack, panel, "Screen", PrimitiveType.Cube, new Vector3(0f, 0.315f, -0.023f), new Vector3(0.24f, 0.07f, 0.006f));
        var heading = TableKit.Text(panel, "Heading", new Vector3(0f, 0.362f, -0.022f), Quaternion.identity, new Vector2(0.24f, 0.014f), 0.02f, 0.14f, kit.brassGold.color);
        heading.text = "HOUSEKEEPING";
        var display = TableKit.Text(panel, "Code", new Vector3(0f, 0.325f, -0.027f), Quaternion.identity, new Vector2(0.2f, 0.04f), 0.05f, 0.4f, kit.accentCyan);
        display.text = LockboxLogic.Display("", Code.Length);
        var status = TableKit.Text(panel, "Status", new Vector3(0f, 0.292f, -0.027f), Quaternion.identity, new Vector2(0.2f, 0.014f), 0.02f, 0.14f, kit.accentCyan);
        status.text = "ENTER CODE";

        var keys = new PressableButton[12];
        for (int i = 0; i < 12; i++)
        {
            float x = (i % 3 - 1) * 0.09f, y = 0.235f - (i / 3) * 0.058f;
            string text = i < 9 ? (i + 1).ToString() : i == 9 ? "CLR" : i == 10 ? "0" : "OK";
            var key = new GameObject("Key_" + text) { layer = layer };
            key.transform.SetParent(panel, false);
            key.transform.localPosition = new Vector3(x, y, -0.03f);
            key.AddComponent<BoxCollider>().size = new Vector3(0.08f, 0.048f, 0.02f);
            keys[i] = key.AddComponent<PressableButton>();
            TableKit.Set(keys[i], "pressDip", p => p.floatValue = 0.002f); // upright panel: a short dip
            Part(i == 11 ? kit.brassGold : kit.chipWhite, key.transform, "Keycap", PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.048f, 0.02f));
            var label = TableKit.Text(key.transform, "Label", new Vector3(0f, 0f, -0.011f), Quaternion.identity, new Vector2(0.07f, 0.036f), 0.05f, i < 9 || i == 10 ? 0.36f : 0.22f, Color.black);
            label.text = text;
        }

        var lockbox = root.AddComponent<CartLockbox>();
        TableKit.SetRefs(lockbox, "keys", keys);
        TableKit.SetRef(lockbox, "display", display);
        TableKit.SetRef(lockbox, "status", status);
        TableKit.Set(lockbox, "code", p => p.stringValue = Code);
        TableKit.SetRef(lockbox, "lid", lid);
        TableKit.SetRef(lockbox, "cardPrefab", staffCard);
        TableKit.SetRef(lockbox, "cardSeat", seat);
        return TableKit.SavePrefab(root, CartPath);
    }

    // A sheet of paper lying flat, readable from the counter's front (-Z). A notebook pickup.
    static GameObject BuildNote(SharedStyleKit kit, ItemData item)
    {
        var root = new GameObject("Puzzle_ShiftNote");
        root.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.01f, 0.2f);
        Part(UITextures.Paper(kit), root.transform, "Paper", PrimitiveType.Cube, new Vector3(0f, 0.001f, 0f), new Vector3(0.15f, 0.002f, 0.2f));
        var flat = Quaternion.Euler(90f, 0f, 0f);
        var title = TableKit.Text(root.transform, "Title", new Vector3(0f, 0.0025f, 0.075f), flat, new Vector2(0.13f, 0.02f), 0.02f, 0.18f, Color.black);
        title.text = "HOUSEKEEPING";
        title.fontStyle = FontStyles.Bold;
        var body = TableKit.Text(root.transform, "Body", new Vector3(0f, 0.0025f, -0.01f), flat, new Vector2(0.13f, 0.12f), 0.02f, 0.12f, Color.black);
        body.text = "Cart lockbox:\nclean the tables in order\n\nCARDS  >  WHEEL  >  DICE\n\n- M.";
        TableKit.SetRef(root.AddComponent<PickupItem>(), "item", item);
        return TableKit.SavePrefab(root, NotePath);
    }

    // A brass table-number post with a black plate, readable from both sides. The number is set per instance.
    static GameObject BuildStand(SharedStyleKit kit)
    {
        var root = new GameObject("Puzzle_TableStand");
        Part(kit.chipBlack, root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), new Vector3(0.3f, 0.015f, 0.3f));
        Part(kit.brassGold, root.transform, "Post", PrimitiveType.Cylinder, new Vector3(0f, 0.53f, 0f), new Vector3(0.04f, 0.5f, 0.04f));
        Part(kit.brassGold, root.transform, "PlateRim", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0f), new Vector3(0.34f, 0.2f, 0.015f));
        Part(kit.chipBlack, root.transform, "Plate", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0f), new Vector3(0.31f, 0.17f, 0.02f));
        foreach (var (name, z, yaw) in new[] { ("NumberFront", -0.011f, 0f), ("NumberBack", 0.011f, 180f) })
        {
            var text = TableKit.Text(root.transform, name, new Vector3(0f, 1.12f, z), Quaternion.Euler(0f, yaw, 0f), new Vector2(0.28f, 0.12f), 0.1f, 1f, kit.brassGold.color);
            text.text = "TABLE 0";
            text.fontStyle = FontStyles.Bold;
        }
        return TableKit.SavePrefab(root, StandPath);
    }

    static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale) =>
        TableKit.Part(material, parent, name, type, position, scale, false, false);

    // ---------------------------------------------------------------- scenes

    static void EditScene(string path, System.Action<Scene> edit)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        edit(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    static GameObject Replace(Scene scene, GameObject prefab, string name, Vector3 position, Quaternion rotation)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name)
                Object.DestroyImmediate(root);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    // Next to the table's front corner on the side away from the room's centre line (side -1 = left, +1 = right).
    static void PlaceStand(Scene scene, GameObject stand, string tableName, string name, int number, float side)
    {
        var table = Find(scene, tableName);
        if (table == null)
        {
            Debug.LogWarning("[StaffPuzzle] " + tableName + " not found in " + scene.name + "; its number stand was not placed.");
            return;
        }
        var renderers = table.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float x = side < 0f ? bounds.min.x - 0.3f : bounds.max.x + 0.3f;
        var instance = Replace(scene, stand, name, new Vector3(x, 0f, bounds.min.z - 0.3f), Quaternion.identity);
        foreach (var text in instance.GetComponentsInChildren<TMP_Text>())
        {
            text.text = "TABLE " + number;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }

    // The STAFF ONLY door gets Slot B's reader (built by their AccessBuilder), retargeted to the STAFF card.
    static void AddStaffReader(Scene scene)
    {
        var doorObject = Find(scene, "Door_Casino_Staff");
        var door = doorObject != null ? doorObject.GetComponent<DoorPortal>() : null;
        if (door == null)
        {
            Debug.LogWarning("[StaffPuzzle] Door_Casino_Staff not found in MVP_Casino; run High Stakes > Build Security Hub first. No reader added.");
            return;
        }
        foreach (var old in new[] { "Staff_Reader", "Staff_Sign", "VIP_Reader", "VIP_Sign" })
        {
            var child = door.transform.Find(old);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        AccessBuilder.AddVipReader(door, ReaderLocal);
        var reader = door.transform.Find("VIP_Reader");
        var sign = door.transform.Find("VIP_Sign");
        reader.name = "Staff_Reader";
        sign.name = "Staff_Sign";
        // On the door leaf, under the frame (the VIP door's sign height overlaps this smaller door's frame)
        sign.localPosition = new Vector3(0f, 1.75f, -0.09f);
        sign.GetComponent<TMP_Text>().rectTransform.sizeDelta = new Vector2(0.9f, 0.1f);
        var component = reader.GetComponent<KeycardReader>();
        TableKit.Set(component, "accessId", p => p.stringValue = StaffAccess);
        TableKit.Set(component, "lockedText", p => p.stringValue = "STAFF  ·  KEYCARD REQUIRED");
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t.gameObject;
        return null;
    }
}
