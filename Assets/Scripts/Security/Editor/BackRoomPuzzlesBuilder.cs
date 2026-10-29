using HighStakes.Core;
using HighStakes.Dialogue;
using HighStakes.Environment;
using HighStakes.Security;
using HighStakes.TableGames;
using HighStakes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Builds the back-of-house puzzle chain off the security hub, ending at the vault:
///
/// 1. Camera room, tape archive (<see cref="TapeDeckPuzzle"/>): a duty log on the archive table says the vault keypad
///    was reset 20 minutes after the 02:20 rounds, on the vault approach camera; the deck's monitor lists the camera
///    numbers. The CAM 04 · 02:40 tape plays the keypad footage and prints the CAM 04 still (first key 5, last key 3).
/// 2. Security office, breaker panel (WirePanelBuilder, re-run here): its Breaker Log now also says the keypad's worn
///    keys are 1 3 5 8 and the 8 is pressed right after the first key.
/// 3. Hub, VAULT door (<see cref="VaultClearance"/>): locked until the Breaker Log and the CAM 04 still are both in
///    the notebook, so the vault is always the last room.
/// 4. Vault, the final lock (<see cref="VaultKeypad"/>): a keypad console in front of the treasury door. Code 5813,
///    which only both clues together give (VaultCodeLogicTests checks that). Three wrong codes lock it down; the
///    right one opens a drawer holding the Black Ledger.
///
/// Everything is placed per HallwayBuilder's room offset (the room sits at (40,0,0) in each wing scene) and clear of
/// the vault's runtime treasury dressing (Security/VaultDressing). HallwayBuilder.Build / Build Security Hub /
/// Build MVP World regenerate these scenes and wipe the placements; re-run this menu after they do.
/// </summary>
public static class BackRoomPuzzlesBuilder
{
    const string ContentRoot = "Assets/Content/Environment/Security/Puzzles";
    const string ArchivePath = ContentRoot + "/Puzzle_TapeArchive.prefab";
    const string DutyLogPath = ContentRoot + "/Puzzle_DutyLog.prefab";
    const string StillPath = ContentRoot + "/Puzzle_Cam04Still.prefab";
    const string ConsolePath = ContentRoot + "/Puzzle_VaultConsole.prefab";
    const string LedgerPath = ContentRoot + "/Puzzle_BlackLedger.prefab";
    const string ClearancePath = ContentRoot + "/Puzzle_VaultClearance.prefab";
    const string DutyLogItemPath = ContentRoot + "/SO_Item_DutyLog.asset";
    const string StillItemPath = ContentRoot + "/SO_Item_Cam04Still.asset";
    const string LedgerItemPath = ContentRoot + "/SO_Item_BlackLedger.asset";
    const string BreakerItemPath = ContentRoot + "/SO_Item_BreakerLog.asset"; // made by WirePanelBuilder

    static readonly Vector3 RoomOrigin = new Vector3(40f, 0f, 0f);
    // Camera room: console desk in front of the monitor wall (screens at z 6.9), behind the room's chair (3, 0, 4.4).
    static readonly Vector3 DeskLocal = new Vector3(3f, 0f, 5.9f);
    // Archive table along the left wall, relative to the desk: room (0.2, 0, 3.6), long side along Z, facing +X.
    static readonly Vector3 ArchiveFromDesk = new Vector3(-2.8f, 0f, -2.3f);
    // Vault: right of the centre aisle, in front of the treasury door (3, 0, 6.5); clear of the bullion pallet
    // (5.25, 0, 5.25) and the cash trolley (5.1, 0, 2.5).
    static readonly Vector3 ConsoleLocal = new Vector3(3.9f, 0f, 5.3f);

    const string Answer = "CAM 04 · 02:40";
    const string VaultCode = "5813";
    const int TapeReward = 300;
    const int VaultReward = 5000;

    // Archive order (front row left to right, then back row); the answer is not first.
    static readonly (string label, string footage)[] Tapes =
    {
        ("CAM 04 · 02:20", "VAULT APPROACH\nGuard rounds pass the vault door.\nNobody stops at the keypad."),
        ("CAM 02 · 02:40", "CASHIER CAGE\nThe night float is counted and bagged."),
        ("CAM 04 · 01:10", "VAULT APPROACH\nEmpty corridor. A ceiling light flickers."),
        ("CAM 01 · 02:40", "CASINO FLOOR\nA high roller is up big at blackjack."),
        (Answer, "VAULT APPROACH\nThe night manager punches the VAULT 03 keypad.\nFirst key 5 ... last key 3.\nHis hand hides the middle two.\n\n<i>Printing still...</i>"),
        ("CAM 03 · 00:30", "VIP LOUNGE\nThe Pit Boss pockets a stack of chips."),
    };

    const string CameraIndex =
        "<b>TAPE PLAYBACK</b>  ·  INSERT TAPE\n\nCAM 01   CASINO FLOOR\nCAM 02   CASHIER CAGE\nCAM 03   VIP LOUNGE\nCAM 04   VAULT APPROACH";

    [MenuItem("High Stakes/Puzzles/Build Back Room Puzzles + Vault")]
    public static void BuildMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[BackRoom] Stop Play mode first, then run Build Back Room Puzzles + Vault again.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildAll();
    }

    /// <summary>Batch entry point (-executeMethod BackRoomPuzzlesBuilder.BuildAll).</summary>
    public static void BuildAll()
    {
        WirePanelBuilder.Build(); // the Breaker Log carries one of the two vault clues
        var kit = TableKit.Kit(out int layer);
        if (kit == null) return;
        TableKit.EnsureFolder(ContentRoot);

        var dutyItem = NotebookBuilder.ItemAt(DutyLogItemPath, "duty_log", "Night Duty Log",
            "Camera room archive. 02:20 rounds, all quiet. Twenty minutes later the night manager reset the VAULT 03 keypad. Vault approach footage pulled and archived.");
        var stillItem = NotebookBuilder.ItemAt(StillItemPath, "cam04_still", "CAM 04 Still",
            "Printed from the CAM 04 · 02:40 tape: the night manager at the VAULT 03 keypad. First key 5, last key 3. His hand hides the middle two.");
        var ledgerItem = NotebookBuilder.ItemAt(LedgerItemPath, "black_ledger", "Black Ledger",
            "The casino's record of laundered money, from VAULT 03. The proof the Guide wanted. Get it out of the building.");
        var breakerItem = AssetDatabase.LoadAssetAtPath<ItemData>(BreakerItemPath);
        AssetDatabase.SaveAssets();

        var dutyLog = Note(kit, dutyItem, DutyLogPath, "NIGHT DUTY LOG",
            "02:20  Rounds. All quiet.\n\n20 min later: night mgr\nreset VAULT 03 keypad.\n\nVault approach footage\npulled and archived.");
        var still = Note(kit, stillItem, StillPath, "CAM 04 · 02:40",
            "Night manager at the\nVAULT 03 keypad.\n\nFirst key: 5\nLast key: 3\nMiddle keys hidden\nby his hand.");
        var archive = BuildArchive(kit, layer, dutyLog, still);
        var ledger = BuildLedger(kit, ledgerItem);
        var console = BuildConsole(kit, layer, ledger);
        var clearance = BuildClearance(kit, breakerItem, stillItem);

        EditScene(HallwayBuilder.ScenePaths[1], scene =>
            PlaceRoot(scene, archive, "Puzzle_TapeArchive", RoomOrigin + DeskLocal));
        EditScene(HallwayBuilder.ScenePaths[2], scene =>
        {
            Vector3 position=RoomOrigin+ConsoleLocal;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name=="RoomC (Vault Antechamber)")position=t.TransformPoint(ConsoleLocal);
            PlaceRoot(scene,console,"Puzzle_VaultConsole",position);
        });
        EditScene(HallwayBuilder.HubScenePath, scene => PlaceClearance(scene, clearance));

        AssetDatabase.SaveAssets();
        Debug.Log("[BackRoom] Built the tape archive (MVP_CameraRoom), the vault clearance (MVP_SecurityHall) and the vault keypad (MVP_Vault). Re-run after HallwayBuilder regenerates the wings.");
    }

    // ---------------------------------------------------------------- camera room

    // Desk space: front on -Z (the player's side), floor at y 0.
    static GameObject BuildArchive(SharedStyleKit kit, int layer, GameObject dutyLog, GameObject still)
    {
        var root = new GameObject("Puzzle_TapeArchive");
        Desk(kit, root.transform, 2.6f, 0.6f);
        const float top = 0.76f;

        // Deck, left of the monitor: a socket on its top takes one tape, lying flat, label up.
        var deckBody = new Vector3(-0.75f, top + 0.05f, -0.05f);
        Part(kit.chipBlack, root.transform, "Deck", PrimitiveType.Cube, deckBody, new Vector3(0.36f, 0.1f, 0.26f));
        Part(kit.steelCool, root.transform, "DeckSlot", PrimitiveType.Cube, deckBody + new Vector3(0f, 0.051f, 0f), new Vector3(0.22f, 0.002f, 0.13f));
        var deckLabel = TableKit.Text(root.transform, "DeckLabel", deckBody + new Vector3(0f, 0f, -0.131f), Quaternion.identity, new Vector2(0.3f, 0.04f), 0.02f, 0.2f, kit.accentCyan);
        deckLabel.text = "PLAYBACK";
        var socketGo = new GameObject("TapeSocket") { layer = layer };
        socketGo.transform.SetParent(root.transform, false);
        socketGo.transform.localPosition = deckBody + new Vector3(0f, 0.065f, 0f);
        var trigger = socketGo.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.1f;
        var deck = socketGo.AddComponent<XRSocketInteractor>();

        // Monitor standing on the desk, facing the player.
        Part(kit.steelCool, root.transform, "MonitorStand", PrimitiveType.Cube, new Vector3(0f, top + 0.1f, 0.12f), new Vector3(0.08f, 0.2f, 0.06f));
        Part(kit.chipBlack, root.transform, "MonitorBezel", PrimitiveType.Cube, new Vector3(0f, top + 0.42f, 0.1f), new Vector3(0.78f, 0.5f, 0.05f));
        Part(kit.screenGlow, root.transform, "MonitorScreen", PrimitiveType.Cube, new Vector3(0f, top + 0.42f, 0.074f), new Vector3(0.72f, 0.44f, 0.002f));
        var screen = TableKit.Text(root.transform, "MonitorText", new Vector3(0f, top + 0.42f, 0.07f), Quaternion.identity, new Vector2(0.66f, 0.4f), 0.05f, 0.3f, Color.white);
        screen.textWrappingMode = TextWrappingModes.Normal;
        screen.text = CameraIndex;

        // Printer right of the monitor; the still lands in its out-tray.
        var printer = new Vector3(0.75f, top + 0.07f, 0.02f);
        Part(kit.chipBlack, root.transform, "Printer", PrimitiveType.Cube, printer, new Vector3(0.32f, 0.14f, 0.26f));
        Part(kit.steelCool, root.transform, "PrinterTray", PrimitiveType.Cube, new Vector3(0.75f, top + 0.005f, -0.2f), new Vector3(0.24f, 0.01f, 0.2f));
        var printSeat = new GameObject("PrintSeat").transform;
        printSeat.SetParent(root.transform, false);
        printSeat.localPosition = new Vector3(0.75f, top + 0.011f, -0.2f);

        // Archive table along the left wall: six tapes and the duty log, all readable from its front (local -Z).
        var table = new GameObject("ArchiveTable").transform;
        table.SetParent(root.transform, false);
        table.localPosition = ArchiveFromDesk;
        table.localRotation = Quaternion.Euler(0f, -90f, 0f); // front (-Z) faces +X, into the room
        Desk(kit, table, 2f, 0.7f);
        var plaque = TableKit.Text(table, "Plaque", new Vector3(0f, top - 0.05f, -0.352f), Quaternion.identity, new Vector2(0.8f, 0.05f), 0.02f, 0.25f, kit.brassGold.color);
        plaque.text = "TAPE ARCHIVE";
        for (int i = 0; i < Tapes.Length; i++)
        {
            var pos = new Vector3(-0.7f + (i % 3) * 0.3f, top + 0.0125f, i < 3 ? -0.14f : 0.14f);
            Tape(kit, layer, table, Tapes[i].label, Tapes[i].footage, pos);
        }
        var log = (GameObject)PrefabUtility.InstantiatePrefab(dutyLog, table);
        log.transform.localPosition = new Vector3(0.55f, top + 0.001f, -0.05f);

        var card = CompleteCard(root.transform, new Vector3(0f, 1.75f, -0.1f));

        var puzzle = root.AddComponent<TapeDeckPuzzle>();
        TableKit.SetRef(puzzle, "deck", deck);
        TableKit.Set(puzzle, "answerLabel", p => p.stringValue = Answer);
        TableKit.SetRef(puzzle, "screen", screen);
        TableKit.Set(puzzle, "idleText", p => p.stringValue = CameraIndex);
        TableKit.SetRef(puzzle, "stillPrefab", still);
        TableKit.SetRef(puzzle, "printSeat", printSeat);
        TableKit.Set(puzzle, "reward", p => p.intValue = TapeReward);
        TableKit.SetRef(puzzle, "completeCard", card);
        return TableKit.SavePrefab(root, ArchivePath);
    }

    // A VHS-sized tape lying flat, its label on top readable from the table's front.
    static void Tape(SharedStyleKit kit, int layer, Transform parent, string label, string footage, Vector3 position)
    {
        var go = new GameObject("Tape " + label) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.AddComponent<BoxCollider>().size = new Vector3(0.19f, 0.025f, 0.105f);
        var body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var grab = go.AddComponent<XRGrabInteractable>();
        TableKit.Set(grab, "m_MovementType", p => p.enumValueIndex = (int)XRBaseInteractable.MovementType.Kinematic);
        var tape = go.AddComponent<VideoTape>();
        TableKit.Set(tape, "label", p => p.stringValue = label);
        TableKit.Set(tape, "footage", p => p.stringValue = footage);
        Part(kit.chipBlack, go.transform, "Shell", PrimitiveType.Cube, Vector3.zero, new Vector3(0.19f, 0.025f, 0.105f));
        Part(kit.chipWhite, go.transform, "Sticker", PrimitiveType.Cube, new Vector3(0f, 0.0128f, 0f), new Vector3(0.16f, 0.001f, 0.075f));
        var text = TableKit.Text(go.transform, "Label", new Vector3(0f, 0.014f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.15f, 0.065f), 0.05f, 0.3f, Color.black);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.fontStyle = FontStyles.Bold;
        text.text = label.Replace(" · ", "\n");
    }

    // ---------------------------------------------------------------- vault

    // Console space: front on -Z (the player's side, facing the treasury door behind it), floor at y 0.
    static GameObject BuildConsole(SharedStyleKit kit, int layer, GameObject ledger)
    {
        var root = new GameObject("Puzzle_VaultConsole");
        var frame = root.AddComponent<BoxCollider>();
        frame.size = new Vector3(0.5f, 1f, 0.4f);
        frame.center = new Vector3(0f, 0.5f, 0f);
        Part(kit.steelCool, root.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1f, 0.4f));
        Part(kit.chipBlack, root.transform, "Fascia", PrimitiveType.Cube, new Vector3(0f, 0.82f, -0.201f), new Vector3(0.44f, 0.2f, 0.004f));
        var plate = TableKit.Text(root.transform, "Plate", new Vector3(0f, 0.82f, -0.204f), Quaternion.identity, new Vector2(0.4f, 0.16f), 0.02f, 0.2f, kit.brassGold.color);
        plate.textWrappingMode = TextWrappingModes.Normal;
        plate.text = "<b>VAULT 03</b>\n<size=60%>MERIDIAN RESERVE\nAUTHORIZED ACCESS ONLY</size>";

        // Keypad tilted back on top, facing up toward the player, like the housekeeping cart's.
        var panel = new GameObject("Keypad").transform;
        panel.SetParent(root.transform, false);
        panel.localPosition = new Vector3(0f, 0.97f, -0.1f);
        panel.localRotation = Quaternion.Euler(35f, 0f, 0f);
        var keys = Keypad(kit, layer, panel, "VAULT 03", out var display, out var status);
        display.text = LockboxLogic.Display("", VaultCode.Length);
        status.text = "ENTER VAULT CODE";
        var lamps = new Renderer[2];
        for (int i = 0; i < 2; i++)
            lamps[i] = Part(kit.chipRed, panel, "Lamp", PrimitiveType.Cube, new Vector3(i == 0 ? -0.12f : 0.12f, 0.4f, -0.02f), new Vector3(0.04f, 0.03f, 0.03f)).GetComponent<Renderer>();

        // Ledger drawer on the front, outside the body's collider so the ledger can be looked at and picked up. Its lid
        // lies flat over it, hinged at the console face, and swings up when the vault is cracked.
        Part(kit.chipBlack, root.transform, "DrawerInside", PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.38f), new Vector3(0.46f, 0.02f, 0.34f));
        Part(kit.steelCool, root.transform, "DrawerLip", PrimitiveType.Cube, new Vector3(0f, 0.53f, -0.555f), new Vector3(0.46f, 0.06f, 0.01f));
        foreach (float x in new[] { -0.235f, 0.235f })
            Part(kit.steelCool, root.transform, "DrawerSide", PrimitiveType.Cube, new Vector3(x, 0.53f, -0.38f), new Vector3(0.01f, 0.06f, 0.36f));
        var lid = new GameObject("DrawerLid").transform;
        lid.SetParent(root.transform, false);
        lid.localPosition = new Vector3(0f, 0.57f, -0.2f);
        Part(kit.steelCool, lid, "DrawerLidPlate", PrimitiveType.Cube, new Vector3(0f, 0f, -0.18f), new Vector3(0.48f, 0.02f, 0.37f));
        var seat = new GameObject("LedgerSeat").transform;
        seat.SetParent(root.transform, false);
        seat.localPosition = new Vector3(0f, 0.511f, -0.38f);

        var card = CompleteCard(root.transform, new Vector3(0f, 1.85f, -0.2f));

        var keypad = root.AddComponent<VaultKeypad>();
        TableKit.SetRefs(keypad, "keys", keys);
        TableKit.SetRef(keypad, "display", display);
        TableKit.SetRef(keypad, "status", status);
        TableKit.Set(keypad, "code", p => p.stringValue = VaultCode);
        TableKit.SetRefs(keypad, "lamps", lamps);
        TableKit.SetRef(keypad, "lockedMaterial", kit.chipRed);
        TableKit.SetRef(keypad, "openMaterial", kit.chipGreen);
        TableKit.SetRef(keypad, "offMaterial", kit.chipBlack);
        TableKit.SetRef(keypad, "drawerLid", lid);
        TableKit.SetRef(keypad, "ledgerPrefab", ledger);
        TableKit.SetRef(keypad, "ledgerSeat", seat);
        TableKit.Set(keypad, "reward", p => p.intValue = VaultReward);
        TableKit.SetRef(keypad, "completeCard", card);
        return TableKit.SavePrefab(root, ConsolePath);
    }

    // A black, gold-titled logbook lying flat, title readable from the front. A notebook pickup.
    static GameObject BuildLedger(SharedStyleKit kit, ItemData item)
    {
        var root = new GameObject("Puzzle_BlackLedger");
        var collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.27f, 0.045f, 0.2f);
        collider.center = new Vector3(0f, 0.0225f, 0f);
        Part(kit.chipWhite, root.transform, "Pages", PrimitiveType.Cube, new Vector3(0.005f, 0.0225f, 0f), new Vector3(0.255f, 0.035f, 0.19f));
        Part(kit.chipBlack, root.transform, "CoverBottom", PrimitiveType.Cube, new Vector3(0f, 0.0025f, 0f), new Vector3(0.27f, 0.005f, 0.2f));
        Part(kit.chipBlack, root.transform, "CoverTop", PrimitiveType.Cube, new Vector3(0f, 0.0425f, 0f), new Vector3(0.27f, 0.005f, 0.2f));
        Part(kit.chipBlack, root.transform, "Spine", PrimitiveType.Cube, new Vector3(-0.132f, 0.0225f, 0f), new Vector3(0.008f, 0.045f, 0.2f));
        var title = TableKit.Text(root.transform, "Title", new Vector3(0f, 0.046f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.22f, 0.06f), 0.05f, 0.3f, kit.brassGold.color);
        title.fontStyle = FontStyles.Bold;
        title.text = "BLACK LEDGER";
        TableKit.SetRef(root.AddComponent<PickupItem>(), "item", item);
        return TableKit.SavePrefab(root, LedgerPath);
    }

    // ---------------------------------------------------------------- hub

    // A checklist plate on the VAULT door leaf (door space: front on -Z, floor at y 0).
    static GameObject BuildClearance(SharedStyleKit kit, ItemData breaker, ItemData still)
    {
        var root = new GameObject("VaultClearance");
        Part(kit.chipBlack, root.transform, "Plate", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(1.1f, 0.6f, 0.02f));
        Part(kit.brassGold, root.transform, "Trim", PrimitiveType.Cube, new Vector3(0f, 1.71f, 0f), new Vector3(1.1f, 0.02f, 0.024f));
        var heading = TableKit.Text(root.transform, "Heading", new Vector3(0f, 1.63f, -0.012f), Quaternion.identity, new Vector2(1f, 0.08f), 0.05f, 0.5f, kit.brassGold.color);
        heading.fontStyle = FontStyles.Bold;
        heading.text = "VAULT WING  ·  CLEARANCE";
        var checklist = TableKit.Text(root.transform, "Checklist", new Vector3(0f, 1.43f, -0.012f), Quaternion.identity, new Vector2(1f, 0.26f), 0.05f, 0.4f, Color.white);
        checklist.textWrappingMode = TextWrappingModes.Normal;
        checklist.alignment = TextAlignmentOptions.Left;
        var status = TableKit.Text(root.transform, "Status", new Vector3(0.05f, 1.18f, -0.012f), Quaternion.identity, new Vector2(0.9f, 0.08f), 0.05f, 0.4f, kit.accentCyan);
        var lamp = Part(kit.chipRed, root.transform, "Lamp", PrimitiveType.Cube, new Vector3(-0.47f, 1.18f, -0.015f), new Vector3(0.05f, 0.05f, 0.02f));

        var gate = root.AddComponent<VaultClearance>();
        TableKit.SetRefs(gate, "required", new Object[] { breaker, still });
        TableKit.Set(gate, "foundIn", p =>
        {
            p.arraySize = 2;
            p.GetArrayElementAtIndex(0).stringValue = "SECURITY";
            p.GetArrayElementAtIndex(1).stringValue = "CAMERAS";
        });
        TableKit.SetRef(gate, "checklist", checklist);
        TableKit.SetRef(gate, "status", status);
        TableKit.SetRef(gate, "statusLight", lamp.GetComponent<Renderer>());
        TableKit.SetRef(gate, "lockedMaterial", kit.chipRed);
        TableKit.SetRef(gate, "openMaterial", kit.chipGreen);
        return TableKit.SavePrefab(root, ClearancePath);
    }

    static void PlaceClearance(Scene scene, GameObject prefab)
    {
        var door = FindInScene(scene, "Door_Hub_C");
        var portal = door != null ? door.GetComponent<DoorPortal>() : null;
        if (portal == null)
        {
            Debug.LogWarning("[BackRoom] Door_Hub_C not found in MVP_SecurityHall; run High Stakes > Build Security Hub first. The vault door is not gated.");
            return;
        }
        var old = door.Find("VaultClearance");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, door);
        instance.name = "VaultClearance";
        // On the leaf, in front of the frame (its face is at -WallThickness / 2), like the staff door's sign.
        instance.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        instance.transform.localRotation = Quaternion.identity;
        TableKit.SetRef(instance.GetComponent<VaultClearance>(), "door", portal);
    }

    // ---------------------------------------------------------------- shared pieces

    // A steel work table: top at 0.76 m, four legs. Width along X, depth along Z.
    static void Desk(SharedStyleKit kit, Transform parent, float width, float depth)
    {
        Part(kit.steelCool, parent, "DeskTop", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(width, 0.04f, depth), true);
        foreach (float x in new[] { -width / 2f + 0.05f, width / 2f - 0.05f })
            foreach (float z in new[] { -depth / 2f + 0.05f, depth / 2f - 0.05f })
                Part(kit.chipBlack, parent, "DeskLeg", PrimitiveType.Cube, new Vector3(x, 0.36f, z), new Vector3(0.05f, 0.72f, 0.05f));
    }

    // 12-key pad (1-9, CLR, 0, OK) on a brass case, built in the panel's XY plane facing -Z. Same layout as the
    // housekeeping cart's lockbox (UI/Puzzles/Editor/StaffDoorPuzzleBuilder), so every keypad in the game reads alike.
    static PressableButton[] Keypad(SharedStyleKit kit, int layer, Transform panel, string headingText, out TextMeshPro display, out TextMeshPro status)
    {
        Part(kit.brassGold, panel, "Case", PrimitiveType.Cube, new Vector3(0f, 0.19f, 0f), new Vector3(0.3f, 0.38f, 0.03f));
        Part(kit.chipBlack, panel, "Face", PrimitiveType.Cube, new Vector3(0f, 0.19f, -0.018f), new Vector3(0.28f, 0.36f, 0.006f));
        Part(kit.chipBlack, panel, "Screen", PrimitiveType.Cube, new Vector3(0f, 0.315f, -0.023f), new Vector3(0.24f, 0.07f, 0.006f));
        var heading = TableKit.Text(panel, "Heading", new Vector3(0f, 0.362f, -0.022f), Quaternion.identity, new Vector2(0.24f, 0.014f), 0.02f, 0.14f, kit.brassGold.color);
        heading.text = headingText;
        display = TableKit.Text(panel, "Code", new Vector3(0f, 0.325f, -0.027f), Quaternion.identity, new Vector2(0.2f, 0.04f), 0.05f, 0.4f, kit.accentCyan);
        status = TableKit.Text(panel, "Status", new Vector3(0f, 0.292f, -0.027f), Quaternion.identity, new Vector2(0.2f, 0.014f), 0.02f, 0.14f, kit.accentCyan);

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
        return keys;
    }

    // A sheet of paper lying flat, readable from the front (-Z). A notebook pickup. Same layout as the Breaker Log.
    static GameObject Note(SharedStyleKit kit, ItemData item, string path, string titleText, string bodyText)
    {
        var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        root.AddComponent<BoxCollider>().size = new Vector3(0.12f, 0.01f, 0.16f);
        Part(kit.chipWhite, root.transform, "Paper", PrimitiveType.Cube, new Vector3(0f, 0.001f, 0f), new Vector3(0.12f, 0.002f, 0.16f));
        var flat = Quaternion.Euler(90f, 0f, 0f);
        var title = TableKit.Text(root.transform, "Title", new Vector3(0f, 0.0025f, 0.055f), flat, new Vector2(0.1f, 0.02f), 0.02f, 0.16f, Color.black);
        title.text = titleText;
        title.fontStyle = FontStyles.Bold;
        var body = TableKit.Text(root.transform, "Body", new Vector3(0f, 0.0025f, -0.015f), flat, new Vector2(0.11f, 0.115f), 0.02f, 0.1f, Color.black);
        body.text = bodyText;
        TableKit.SetRef(root.AddComponent<PickupItem>(), "item", item);
        return TableKit.SavePrefab(root, path);
    }

    // "Task complete" card that floats over a puzzle for a few seconds, like the breaker panel's (SpeechBubble).
    static SpeechBubble CompleteCard(Transform parent, Vector3 position)
    {
        var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Content/UI/UI_Hologram.mat");
        var card = new GameObject("CompleteCard").transform;
        card.SetParent(parent, false);
        var cardPanel = new GameObject("Panel").transform;
        cardPanel.SetParent(card, false);
        cardPanel.localPosition = position;
        var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backing.name = "Backing";
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        backing.transform.SetParent(cardPanel, false);
        backing.transform.localScale = new Vector3(0.7f, 0.36f, 1f);
        backing.GetComponent<Renderer>().sharedMaterial = glass;
        var cardText = TableKit.Text(cardPanel, "Line", new Vector3(0f, 0f, -0.002f), Quaternion.identity, new Vector2(0.64f, 0.32f), 0.05f, 0.5f, Color.white);
        cardText.textWrappingMode = TextWrappingModes.Normal;
        var bubble = card.gameObject.AddComponent<SpeechBubble>();
        TableKit.SetRef(bubble, "bubble", cardPanel.gameObject);
        TableKit.SetRef(bubble, "text", cardText);
        return bubble;
    }

    static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, bool keepCollider = false) =>
        TableKit.Part(material, parent, name, type, position, scale, false, keepCollider);

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

    static void PlaceRoot(Scene scene, GameObject prefab, string name, Vector3 position)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) Object.DestroyImmediate(root);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = name;
        instance.transform.position = position;
    }

    static Transform FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
