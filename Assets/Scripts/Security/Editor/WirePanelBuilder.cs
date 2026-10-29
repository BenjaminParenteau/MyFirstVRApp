using HighStakes.Core;
using HighStakes.Environment;
using HighStakes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Builds the "breaker panel" wire-matching puzzle for the security office: four loose colour-coded plugs on a
/// shelf, four sockets on the panel above, each needing the plug of its own colour (red, blue, yellow, green, left
/// to right). The panel starts cross-wired (the shelf order doesn't match the sockets). Restoring the circuit pays a
/// chip reward, cuts the monitor wall's vault camera feed, shows a "task complete" card and pops a drawer open with
/// a note inside, to be picked up into the notebook. Shelf, plugs and drawer are on the front (-Z), the player's side.
/// The note (Breaker Log) is one of the two vault-code clues: BackRoomPuzzlesBuilder makes the other, and
/// VaultCodeLogicTests checks that together they leave exactly one code.
///
/// Plugs are discrete tokens, not a simulated flexible cable (no existing pattern for that in the project, and a
/// simple token is what the staff-door lockbox and VIP keycard already use). Wires use a plain XRSocketInteractor
/// per socket and an XRGrabInteractable per plug; <see cref="HighStakes.Security.WirePanelPuzzle"/> reads which
/// plug (if any) is seated in each socket and checks the match.
///
/// The security wing's three rooms are regenerated wholesale by HallwayBuilder.Build / Build Security Hub, which
/// would wipe this placement; re-run this menu after they do. Edits MVP_SecurityOffice only. Placed per the
/// generator's own roomOrigin offset (HallwayBuilder.cs: the office room sits at local (40,0,0) in that scene), not
/// by searching for the generated room object by name, so it doesn't depend on exactly how HallwayBuilder names it.
/// </summary>
public static class WirePanelBuilder
{
    const string ContentRoot = "Assets/Content/Environment/Security/Puzzles";
    const string MaterialRoot = "Assets/Content/Environment/Security/Materials";
    const string PanelPath = ContentRoot + "/Puzzle_WirePanel.prefab";
    const string NotePath = ContentRoot + "/Puzzle_WirePanelNote.prefab";
    const string NoteItemPath = ContentRoot + "/SO_Item_BreakerLog.asset";
    const string ScenePath = "Assets/Scenes/MVP/MVP_SecurityOffice.unity";

    // HallwayBuilder.cs: the office room's content sits at this offset within MVP_SecurityOffice.unity.
    static readonly Vector3 RoomOrigin = new Vector3(40f, 0f, 0f);
    // On the back wall of the 12 x 12 m security & count room's staff side (back wall inner face at z 10.925), facing
    // the door; clear of the noticeboard on the left wall and the supervisor booth in the back-right corner.
    static readonly Vector3 PanelLocal = new Vector3(2f, 0f, 10.6f);

    // Red, blue, yellow, green, in socket (and label) order.
    const int Red = 0, Blue = 1, Yellow = 2, Green = 3;
    // Shelf order the plugs start in: deliberately not the socket order above, so the panel starts cross-wired.
    static readonly int[] ShelfOrder = { Yellow, Green, Red, Blue };
    static readonly string[] ColorNames = { "RED", "BLUE", "YELLOW", "GREEN" };

    const int Reward = 300;

    [MenuItem("High Stakes/Puzzles/Build Security Wire Panel")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WirePanel] Stop Play mode first, then run Build Security Wire Panel again.");
            return;
        }
        var kit = TableKit.Kit(out int layer);
        if (kit == null) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TableKit.EnsureFolder(ContentRoot);
        var colors = WireColors(kit);
        var noteItem = NotebookBuilder.ItemAt(NoteItemPath, "breaker_log", "Breaker Log",
            "Panel re-wired: +" + Reward + " chips. The vault approach camera (feed 04) is on this breaker and is now dark. " +
            "Tech note: the VAULT 03 keypad is on this circuit too. Worn keys: 1, 3, 5, 8. The 8 sticks; it is always pressed right after the first key.");
        AssetDatabase.SaveAssets();

        var note = BuildNote(kit, noteItem);
        var panel = BuildPanel(kit, layer, colors, note);

        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Puzzle_WirePanel") Object.DestroyImmediate(root);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(panel, scene);
        instance.name = "Puzzle_WirePanel";
        instance.transform.position = RoomOrigin + PanelLocal;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);

        AssetDatabase.SaveAssets();
        Debug.Log("[WirePanel] Built the breaker-panel puzzle in MVP_SecurityOffice. Re-run after HallwayBuilder regenerates the wings.");
    }

    // ---------------------------------------------------------------- materials

    // Red and green already exist in the kit; blue and yellow are small Material Variants (StyleGuide: no new base
    // materials), next to the security wing's other generated materials.
    static Material[] WireColors(SharedStyleKit kit)
    {
        TableKit.EnsureFolder(MaterialRoot);
        var blue = Variant("M_WireBlue", kit.steelCool, new Color(0.18f, 0.32f, 0.85f));
        var yellow = Variant("M_WireYellow", kit.steelCool, new Color(0.85f, 0.73f, 0.12f));
        return new[] { kit.chipRed, blue, yellow, kit.chipGreen };
    }

    static Material Variant(string name, Material parent, Color color)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(parent); AssetDatabase.CreateAsset(material, path); }
        material.parent = parent;
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------------------------------------------------------------- prefabs

    // Cabinet space: front on -Z (the player's side), floor at y 0, panel face at chest height.
    static GameObject BuildPanel(SharedStyleKit kit, int layer, Material[] colors, GameObject notePrefab)
    {
        var root = new GameObject("Puzzle_WirePanel");
        var frame = root.AddComponent<BoxCollider>();
        frame.size = new Vector3(0.7f, 1.5f, 0.28f);
        frame.center = new Vector3(0f, 0.75f, 0f);

        Part(kit.steelCool, root.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 1.5f, 0.28f));
        Part(kit.chipBlack, root.transform, "FacePlate", PrimitiveType.Cube, new Vector3(0f, 1.3f, -0.15f), new Vector3(0.56f, 0.46f, 0.02f));
        var heading = TableKit.Text(root.transform, "Heading", new Vector3(0f, 1.49f, -0.161f), Quaternion.identity, new Vector2(0.5f, 0.05f), 0.02f, 0.16f, kit.brassGold.color);
        heading.text = "BREAKER PANEL";
        heading.fontStyle = FontStyles.Bold;
        var status = TableKit.Text(root.transform, "Status", new Vector3(0f, 1.1f, -0.161f), Quaternion.identity, new Vector2(0.5f, 0.05f), 0.02f, 0.14f, kit.accentCyan);

        // Four sockets across the face plate, red/blue/yellow/green left to right.
        var sockets = new XRSocketInteractor[4];
        float[] socketX = { -0.18f, -0.06f, 0.06f, 0.18f };
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector3(socketX[i], 1.3f, -0.17f);
            Part(colors[i], root.transform, "SocketRing_" + ColorNames[i], PrimitiveType.Cylinder, pos, new Vector3(0.045f, 0.01f, 0.045f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var socketGo = new GameObject("Socket_" + ColorNames[i]) { layer = layer };
            socketGo.transform.SetParent(root.transform, false);
            socketGo.transform.localPosition = pos;
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.04f;
            sockets[i] = socketGo.AddComponent<XRSocketInteractor>();
            var label = TableKit.Text(root.transform, "SocketLabel_" + ColorNames[i], pos + new Vector3(0f, -0.05f, 0f), Quaternion.identity, new Vector2(0.11f, 0.02f), 0.01f, 0.08f, Color.white);
            label.text = ColorNames[i];
        }

        // Tray on the front below the panel, outside the body's collider so the note can be looked at and picked up.
        // Its lid lies flat over it, hinged at the cabinet face, and swings up on solve to reveal the note.
        Part(kit.steelCool, root.transform, "DrawerInside", PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.25f), new Vector3(0.46f, 0.02f, 0.2f));
        Part(kit.steelCool, root.transform, "DrawerLip", PrimitiveType.Cube, new Vector3(0f, 0.525f, -0.345f), new Vector3(0.46f, 0.05f, 0.01f));
        var lid = new GameObject("DrawerLid").transform;
        lid.SetParent(root.transform, false);
        lid.localPosition = new Vector3(0f, 0.56f, -0.14f);
        Part(kit.steelCool, lid, "DrawerLidPlate", PrimitiveType.Cube, new Vector3(0f, 0f, -0.11f), new Vector3(0.48f, 0.02f, 0.22f));
        var noteSeat = new GameObject("NoteSeat").transform;
        noteSeat.SetParent(root.transform, false);
        noteSeat.localPosition = new Vector3(0f, 0.515f, -0.25f); // the note prefab already lies flat, text up

        // Shelf on the front holding the four plugs, cross-wired relative to the sockets above.
        Part(kit.steelCool, root.transform, "Shelf", PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.23f), new Vector3(0.6f, 0.03f, 0.18f));
        var plugs = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            int color = ShelfOrder[i];
            var plugGo = new GameObject("Wire_" + ColorNames[color]) { layer = layer };
            plugGo.transform.SetParent(root.transform, false);
            plugGo.transform.localPosition = new Vector3(socketX[i], 0.945f, -0.23f);
            var collider = plugGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.04f, 0.04f, 0.1f);
            var body = plugGo.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var grab = plugGo.AddComponent<XRGrabInteractable>();
            TableKit.Set(grab, "m_MovementType", p => p.enumValueIndex = (int)XRBaseInteractable.MovementType.Kinematic);
            var plug = plugGo.AddComponent<HighStakes.Security.WirePlug>();
            TableKit.Set(plug, "colorIndex", p => p.intValue = color);
            Part(colors[color], plugGo.transform, "PlugBody", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.035f, 0.05f, 0.035f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plugs[i] = plugGo;
        }

        // "Task complete" card that floats above the panel for a few seconds on solve, turned to the player. Same
        // hologram backing and SpeechBubble as the Pit Boss's lines (PitBossBuilder.AddKeycardGiver).
        var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Content/UI/UI_Hologram.mat");
        var card = new GameObject("CompleteCard").transform;
        card.SetParent(root.transform, false);
        var cardPanel = new GameObject("Panel").transform;
        cardPanel.SetParent(card, false);
        cardPanel.localPosition = new Vector3(0f, 1.95f, -0.2f);
        var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backing.name = "Backing";
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        backing.transform.SetParent(cardPanel, false);
        backing.transform.localScale = new Vector3(0.7f, 0.36f, 1f);
        backing.GetComponent<Renderer>().sharedMaterial = glass;
        var cardText = TableKit.Text(cardPanel, "Line", new Vector3(0f, 0f, -0.002f), Quaternion.identity, new Vector2(0.64f, 0.32f), 0.05f, 0.5f, Color.white);
        cardText.textWrappingMode = TextWrappingModes.Normal;
        var completeCard = card.gameObject.AddComponent<HighStakes.Dialogue.SpeechBubble>();
        TableKit.SetRef(completeCard, "bubble", cardPanel.gameObject);
        TableKit.SetRef(completeCard, "text", cardText);

        var puzzle = root.AddComponent<HighStakes.Security.WirePanelPuzzle>();
        TableKit.SetRefs(puzzle, "sockets", sockets);
        TableKit.Set(puzzle, "socketColors", p =>
        {
            p.arraySize = 4;
            for (int i = 0; i < 4; i++) p.GetArrayElementAtIndex(i).intValue = i; // Red, Blue, Yellow, Green left to right
        });
        TableKit.SetRef(puzzle, "status", status);
        TableKit.SetRef(puzzle, "cabinetLid", lid);
        TableKit.Set(puzzle, "cabinetOpenAngle", p => p.floatValue = 80f); // up against the cabinet face
        TableKit.SetRef(puzzle, "notePrefab", notePrefab);
        TableKit.SetRef(puzzle, "noteSeat", noteSeat);
        TableKit.Set(puzzle, "reward", p => p.intValue = Reward);
        TableKit.SetRef(puzzle, "completeCard", completeCard);
        TableKit.Set(puzzle, "cutFeedScene", p => p.stringValue = "MVP_Vault");

        return TableKit.SavePrefab(root, PanelPath);
    }

    // A sheet of paper lying flat in the drawer, readable once it's lifted out. A notebook pickup.
    static GameObject BuildNote(SharedStyleKit kit, ItemData item)
    {
        var root = new GameObject("Puzzle_WirePanelNote");
        root.AddComponent<BoxCollider>().size = new Vector3(0.12f, 0.01f, 0.16f);
        Part(kit.chipWhite, root.transform, "Paper", PrimitiveType.Cube, new Vector3(0f, 0.001f, 0f), new Vector3(0.12f, 0.002f, 0.16f));
        var flat = Quaternion.Euler(90f, 0f, 0f);
        var title = TableKit.Text(root.transform, "Title", new Vector3(0f, 0.0025f, 0.055f), flat, new Vector2(0.1f, 0.02f), 0.02f, 0.16f, Color.black);
        title.text = "BREAKER LOG";
        title.fontStyle = FontStyles.Bold;
        var body = TableKit.Text(root.transform, "Body", new Vector3(0f, 0.0025f, -0.015f), flat, new Vector2(0.11f, 0.115f), 0.02f, 0.1f, Color.black);
        body.text = "Re-wired.  +" + Reward + " chips\nCAM 04 / VAULT APPROACH: DARK\n\nTECH NOTE: VAULT 03 keypad\non this circuit. Worn keys\n1  3  5  8\nThe 8 sticks: always pressed\nright after the first key.";
        TableKit.SetRef(root.AddComponent<PickupItem>(), "item", item);
        return TableKit.SavePrefab(root, NotePath);
    }

    static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale) =>
        TableKit.Part(material, parent, name, type, position, scale, false, false);
}
