using HighStakes.Environment;
using HighStakes.Environment.TheReturn;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Furnishes <c>Assets/Scenes/TheReturn.unity</c> with the restitution act: a wall rack of eight named pay envelopes on
/// the far wall, a duffel bag of eight grabbable cash bundles by the door, and the lighting rig
/// (<see cref="RestitutionManager"/>) that warms the room from vault blue to sunrise as the envelopes are filled.
///
/// Everything goes under one root, <c>Return_Restitution</c>, which is deleted and rebuilt on each run, so this is an
/// additive pass that leaves the room shell, the baked lightmaps and the solo rig alone (unlike
/// <see cref="TheReturnBuilder"/>, which regenerates the whole scene from MVP_Vault).
///
/// Style kit only: the envelopes, cash, duffel and window glass are small Material Variants of kit materials in this
/// feature's own folder (StyleGuide: no new base materials).
/// </summary>
public static class TheReturnContentBuilder
{
    const string ContentRoot = "Assets/Content/Environment/TheReturn";
    const string MaterialRoot = ContentRoot + "/Materials";
    const string PrefabRoot = ContentRoot + "/Prefabs";
    const string RootName = "Return_Restitution";
    const string CashTag = "CashBundle";
    const string LedgerTag = "BlackLedger";

    // The people the syndicate skimmed. Names, not numbers: each envelope is one person made whole.
    static readonly (string name, string role)[] Staff =
    {
        ("Rosa Delgado", "Cleaning, Night Shift"),
        ("Marcus Hale", "Dealer, Blackjack"),
        ("Imani Brooks", "Cashier Cage"),
        ("Tomas Varga", "Porter, Back of House"),
        ("Grace Okafor", "Dealer, Roulette"),
        ("Danny Pike", "Kitchen, Staff Canteen"),
        ("Leila Haddad", "Housekeeping"),
        ("Walter Boyd", "Security, Day Shift"),
    };

    [MenuItem("High Stakes/Build The Return Content")]
    public static void BuildMenu()
    {
        if (!Application.isBatchMode)
        {
            if (!EditorUtility.DisplayDialog("Build The Return content?",
                    "Rebuilds the envelope rack, the duffel of cash and the lighting rig in TheReturn.unity. The room "
                    + "shell, its baked lighting and the solo rig are left alone.",
                    "Build", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }
        Build();
    }

    /// <summary>Batch entry point (-executeMethod TheReturnContentBuilder.Build).</summary>
    public static void Build()
    {
        var kit = TableKit.Kit(out int layer);
        if (kit == null) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TheReturnBuilder.ScenePath) == null)
        {
            Debug.LogError($"[Return] {TheReturnBuilder.ScenePath} not found. Run High Stakes > Build The Return Scene first.");
            return;
        }

        EnsureTag(CashTag);
        EnsureTag(LedgerTag);
        TableKit.EnsureFolder(MaterialRoot);
        TableKit.EnsureFolder(PrefabRoot);

        var mats = Materials(kit);
        var bundlePrefab = CashBundlePrefab(kit, mats, layer);
        var ledgerPrefab = BlackLedgerPrefab(kit, mats, layer);

        var scene = EditorSceneManager.OpenScene(TheReturnBuilder.ScenePath, OpenSceneMode.Single);

        var room = FindRoom();
        if (room == null)
        {
            Debug.LogError("[Return] No 'BreakRoom' object in the scene. Re-run High Stakes > Build The Return Scene.");
            return;
        }

        var old = room.Find(RootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var root = new GameObject(RootName).transform;
        root.SetParent(room, false);

        var envelopes = BuildRack(kit, mats, root, layer);
        BuildDuffel(kit, mats, root, bundlePrefab);
        var dawn = BuildLighting(kit, mats, root, envelopes);
        BuildEndings(kit, mats, root, layer, ledgerPrefab, dawn);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Return] Built {Staff.Length} envelopes, {Staff.Length} cash bundles, the investigator's desk, "
                  + "the ledger and the dawn lighting rig. Open TheReturn.unity and press Play: carry a bundle from "
                  + "the duffel to an envelope, or walk out of the door to take the other ending.");
    }

    // ---------------------------------------------------------------- the room's far wall: the pay rack

    /// <summary>
    /// The rack on the far wall, where the guard-schedule board used to hang: eight envelopes in a row at a height a
    /// standing player can reach, each printed with one person's name and role.
    /// </summary>
    static RestitutionEnvelope[] BuildRack(SharedStyleKit kit, Mats mats, Transform parent, int layer)
    {
        // Room is 8 x 8 m with its far wall at local z = 7 and its door centred at x = 3.
        var rack = new GameObject("PayRack").transform;
        rack.SetParent(parent, false);
        rack.localPosition = new Vector3(3f, 0f, 6.9f);

        Part(kit.woodDark, rack, "Board", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.02f), new Vector3(3.9f, 0.75f, 0.05f));
        Part(kit.brassGold, rack, "RailTop", PrimitiveType.Cube, new Vector3(0f, 1.68f, -0.01f), new Vector3(3.9f, 0.04f, 0.06f));
        Part(kit.brassGold, rack, "RailBottom", PrimitiveType.Cube, new Vector3(0f, 0.92f, -0.01f), new Vector3(3.9f, 0.04f, 0.06f));

        var heading = TableKit.Text(rack, "Heading", new Vector3(0f, 1.62f, -0.05f), Quaternion.identity,
            new Vector2(2.4f, 0.12f), 0.05f, 0.3f, kit.brassGold.color);
        heading.text = "STAFF PAY  ·  WAGES WITHHELD";
        heading.fontStyle = FontStyles.Bold;

        var envelopes = new RestitutionEnvelope[Staff.Length];
        float spacing = 0.46f;
        float x0 = -(Staff.Length - 1) * spacing / 2f;
        for (int i = 0; i < Staff.Length; i++)
            envelopes[i] = BuildEnvelope(kit, mats, rack, layer, i, new Vector3(x0 + i * spacing, 1.25f, -0.04f));
        return envelopes;
    }

    static RestitutionEnvelope BuildEnvelope(SharedStyleKit kit, Mats mats, Transform rack, int layer, int index, Vector3 pos)
    {
        var (name, role) = Staff[index];

        var go = new GameObject($"Envelope_{index + 1}_{name.Split(' ')[0]}");
        go.transform.SetParent(rack, false);
        go.transform.localPosition = pos;
        // Tipped back against the board so the mouth of the envelope faces up and out, toward the player.
        go.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

        var body = Part(mats.envelope, go.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.2f, 0.135f, 0.01f));
        Part(kit.brassGold, go.transform, "Clip", PrimitiveType.Cube, new Vector3(0f, 0.072f, -0.004f), new Vector3(0.05f, 0.012f, 0.014f));

        // The name printed on the empty envelope, so the player knows whose wages these are before filling it.
        var printed = TableKit.Text(go.transform, "PrintedName", new Vector3(0f, -0.005f, -0.007f), Quaternion.identity,
            new Vector2(0.18f, 0.1f), 0.01f, 0.055f, new Color(0.25f, 0.22f, 0.2f));
        printed.alignment = TextAlignmentOptions.Center;
        printed.textWrappingMode = TextWrappingModes.Normal;

        // The same name again, lit gold once the envelope is sealed.
        var lit = TableKit.Text(go.transform, "LitName", new Vector3(0f, -0.005f, -0.012f), Quaternion.identity,
            new Vector2(0.18f, 0.1f), 0.01f, 0.055f, new Color32(0xFF, 0xC2, 0x7A, 0xFF));
        lit.alignment = TextAlignmentOptions.Center;
        lit.textWrappingMode = TextWrappingModes.Normal;

        // The socket sits at the mouth of the envelope: push a bundle in from the front.
        var socketGo = new GameObject("Socket") { layer = layer };
        socketGo.transform.SetParent(go.transform, false);
        socketGo.transform.localPosition = new Vector3(0f, 0.03f, -0.05f);
        var trigger = socketGo.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.09f;
        var socket = socketGo.AddComponent<XRSocketInteractor>();

        var envelope = go.AddComponent<RestitutionEnvelope>();
        envelope.staffName = name;
        envelope.staffRole = role;
        TableKit.SetRef(envelope, "socket", socket);
        TableKit.SetRef(envelope, "seat", socketGo.transform);
        TableKit.SetRef(envelope, "envelopeRenderer", body.GetComponent<Renderer>());
        TableKit.SetRef(envelope, "sealedMaterial", mats.envelopeSealed);
        TableKit.SetRef(envelope, "nameLabel", lit);
        TableKit.SetRef(envelope, "printedLabel", printed);

        // Its own chime, so eight seals do not fight over one audio source.
        var chime = go.AddComponent<AudioSource>();
        chime.playOnAwake = false;
        chime.spatialBlend = 1f;
        chime.minDistance = 0.5f;
        chime.maxDistance = 8f;
        TableKit.SetRef(envelope, "chime", chime);

        envelope.RefreshLabels();
        lit.gameObject.SetActive(false);
        return envelope;
    }

    // ---------------------------------------------------------------- by the door: the duffel of cash

    /// <summary>The recovered cash, dumped by the door the player comes in through: eight bundles to carry across the room.</summary>
    static void BuildDuffel(SharedStyleKit kit, Mats mats, Transform parent, GameObject bundlePrefab)
    {
        var duffel = new GameObject("Duffel").transform;
        duffel.SetParent(parent, false);
        // Just inside the door (centred at x = 3, z = -1), off to one side so the player does not walk into it.
        duffel.localPosition = new Vector3(1.6f, 0f, 0.4f);
        duffel.localRotation = Quaternion.Euler(0f, 24f, 0f);

        Part(mats.duffel, duffel, "Bag", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.78f, 0.32f, 0.4f));
        Part(mats.duffel, duffel, "EndLeft", PrimitiveType.Cylinder, new Vector3(-0.39f, 0.16f, 0f), new Vector3(0.32f, 0.2f, 0.4f))
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Part(mats.duffel, duffel, "EndRight", PrimitiveType.Cylinder, new Vector3(0.39f, 0.16f, 0f), new Vector3(0.32f, 0.2f, 0.4f))
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Part(kit.steelCool, duffel, "Zip", PrimitiveType.Cube, new Vector3(0f, 0.315f, 0.1f), new Vector3(0.7f, 0.01f, 0.02f));
        Part(kit.chipBlack, duffel, "Strap", PrimitiveType.Cube, new Vector3(0f, 0.33f, -0.08f), new Vector3(0.52f, 0.02f, 0.05f));
        // The open mouth of the bag, so the bundles read as sitting inside it.
        Part(kit.chipBlack, duffel, "Mouth", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.02f, 0.26f));

        // Eight bundles in two rows inside the bag, one per envelope.
        for (int i = 0; i < Staff.Length; i++)
        {
            var bundle = (GameObject)PrefabUtility.InstantiatePrefab(bundlePrefab, duffel);
            bundle.name = $"CashBundle_{i + 1}";
            float bx = -0.24f + (i % 4) * 0.16f;
            float bz = i < 4 ? -0.06f : 0.06f;
            bundle.transform.localPosition = new Vector3(bx, 0.34f + (i < 4 ? 0f : 0.055f), bz);
            bundle.transform.localRotation = Quaternion.Euler(0f, i * 7f, 0f);
        }

        var sign = TableKit.Text(duffel, "Tag", new Vector3(0f, 0.17f, -0.21f), Quaternion.identity,
            new Vector2(0.5f, 0.08f), 0.02f, 0.1f, kit.brassGold.color);
        sign.text = "RECOVERED  ·  THE MERIDIAN";
        sign.fontStyle = FontStyles.Bold;
    }

    /// <summary>One strapped bundle of banknotes: grabbable, tagged so only the envelopes will take it.</summary>
    static GameObject CashBundlePrefab(SharedStyleKit kit, Mats mats, int layer)
    {
        var root = new GameObject("Return_CashBundle") { layer = layer, tag = CashTag };

        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(0.14f, 0.055f, 0.07f);

        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        var grab = root.AddComponent<XRGrabInteractable>();
        TableKit.Set(grab, "m_MovementType", p => p.enumValueIndex = (int)XRBaseInteractable.MovementType.Kinematic);

        Part(mats.cash, root.transform, "Notes", PrimitiveType.Cube, Vector3.zero, new Vector3(0.14f, 0.055f, 0.07f));
        Part(mats.cashBand, root.transform, "Band", PrimitiveType.Cube, Vector3.zero, new Vector3(0.145f, 0.02f, 0.072f));

        var prefab = TableKit.SavePrefab(root, $"{PrefabRoot}/Return_CashBundle.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------------------------------------------------------------- the dawn rig

    /// <summary>
    /// The window and the two realtime lights the <see cref="RestitutionManager"/> drives. The room's lightmaps are
    /// baked cold and cannot be re-baked here (no GPU, StyleGuide §9), so dawn is realtime on top of that bake.
    /// </summary>
    struct Dawn
    {
        public RestitutionManager manager;
        public Transform window;
    }

    static Dawn BuildLighting(SharedStyleKit kit, Mats mats, Transform parent, RestitutionEnvelope[] envelopes)
    {
        var rig = new GameObject("DawnRig").transform;
        rig.SetParent(parent, false);

        // A window on the side wall (local x = 7), the only way daylight can reach a back-of-house room.
        var window = new GameObject("Window").transform;
        window.SetParent(rig, false);
        window.localPosition = new Vector3(6.88f, 1.75f, 3f);
        window.localRotation = Quaternion.Euler(0f, -90f, 0f);
        Part(kit.steelCool, window, "Frame", PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 1.1f, 0.08f));
        Part(mats.sky, window, "Glass", PrimitiveType.Cube, new Vector3(0f, 0f, 0.03f), new Vector3(1.44f, 0.94f, 0.02f));
        Part(kit.steelCool, window, "MullionV", PrimitiveType.Cube, Vector3.zero, new Vector3(0.05f, 1.02f, 0.1f));
        Part(kit.steelCool, window, "MullionH", PrimitiveType.Cube, Vector3.zero, new Vector3(1.5f, 0.05f, 0.1f));

        var mainLight = new GameObject("Light_Main").AddComponent<Light>();
        mainLight.transform.SetParent(rig, false);
        mainLight.transform.localPosition = new Vector3(3f, 2.8f, 3f);
        mainLight.transform.localRotation = Quaternion.Euler(42f, -120f, 0f);
        mainLight.type = LightType.Directional;
        mainLight.shadows = LightShadows.None; // Quest budget: the bake already carries the room's shadowing.
        mainLight.lightmapBakeType = LightmapBakeType.Realtime;

        var windowLight = new GameObject("Light_Window").AddComponent<Light>();
        windowLight.transform.SetParent(rig, false);
        windowLight.transform.localPosition = new Vector3(6.4f, 1.75f, 3f);
        windowLight.transform.localRotation = Quaternion.Euler(12f, -90f, 0f);
        windowLight.type = LightType.Spot;
        windowLight.spotAngle = 92f;
        windowLight.range = 11f;
        windowLight.shadows = LightShadows.None;
        windowLight.lightmapBakeType = LightmapBakeType.Realtime;

        // Switched on only when every envelope is filled.
        var shafts = new GameObject("LightShafts").transform;
        shafts.SetParent(rig, false);
        shafts.localPosition = new Vector3(6.6f, 1.75f, 3f);
        for (int i = 0; i < 3; i++)
        {
            var shaft = Part(mats.shaft, shafts, "Shaft_" + (i + 1), PrimitiveType.Cube,
                new Vector3(-1.5f, -0.5f, -0.45f + i * 0.45f), new Vector3(3.4f, 0.42f, 0.34f));
            shaft.transform.localRotation = Quaternion.Euler(0f, 0f, -17f);
        }
        shafts.gameObject.SetActive(false);

        var dust = new GameObject("DustMotes").AddComponent<ParticleSystem>();
        dust.transform.SetParent(rig, false);
        dust.transform.localPosition = new Vector3(5.2f, 1.6f, 3f);
        ConfigureDust(dust, mats.dust);
        dust.gameObject.SetActive(false);

        // The volume that tints the room with the lights: a global volume with Color Adjustments, its own profile so
        // the style kit's shared profile is never written to.
        var volumeGo = new GameObject("Volume_Dawn");
        volumeGo.transform.SetParent(rig, false);
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = DawnProfile();

        var manager = rig.gameObject.AddComponent<RestitutionManager>();
        TableKit.SetRef(manager, "mainLight", mainLight);
        TableKit.SetRef(manager, "windowLight", windowLight);
        TableKit.SetRef(manager, "volume", volume);
        TableKit.SetRef(manager, "dustMotes", dust);
        TableKit.SetRef(manager, "lightShafts", shafts.gameObject);
        TableKit.Set(manager, "totalEnvelopes", p => p.intValue = envelopes.Length);

        return new Dawn { manager = manager, window = window };
    }

    static void ConfigureDust(ParticleSystem dust, Material material)
    {
        var main = dust.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 9f;
        main.startSpeed = 0.045f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
        main.startColor = new Color(1f, 0.88f, 0.7f, 0.5f);
        main.maxParticles = 140; // Quest budget: a few motes, not a dust storm.
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = -0.004f;

        var emission = dust.emission;
        emission.rateOverTime = 14f;

        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3f, 1.6f, 1.6f);

        var noise = dust.noise;
        noise.enabled = true;
        noise.strength = 0.06f;
        noise.frequency = 0.22f;

        var renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>A volume profile of this feature's own with a Color Adjustments override for the manager to tint.</summary>
    static VolumeProfile DawnProfile()
    {
        string path = $"{ContentRoot}/Return_DawnVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        if (!profile.Has<UnityEngine.Rendering.Universal.ColorAdjustments>())
            profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        if (profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments adjust))
        {
            adjust.colorFilter.overrideState = true;
            adjust.colorFilter.value = new Color32(0x3A, 0x5A, 0x8C, 0xFF);
        }
        EditorUtility.SetDirty(profile);
        return profile;
    }

    // ---------------------------------------------------------------- the two endings

    /// <summary>
    /// The choice at the end of the room: the investigator's desk with its evidence tray, the Black Ledger on a side
    /// table where the player starts, the night skyline behind the window, the ending card above the door, and the
    /// volume in the doorway that reads walking out as the decision. Nothing loads a scene and nothing touches the
    /// door's <see cref="DoorPortal"/> — it stays inert.
    /// </summary>
    static void BuildEndings(SharedStyleKit kit, Mats mats, Transform parent, int layer, GameObject ledgerPrefab, Dawn dawn)
    {
        var endings = new GameObject("Endings").transform;
        endings.SetParent(parent, false);

        var handoff = BuildInvestigatorDesk(kit, mats, endings, layer);
        BuildLedgerTable(kit, endings, ledgerPrefab);
        var skyline = BuildNightSkyline(kit, mats, dawn.window);
        var (panel, headline, body) = BuildEndingCard(kit, mats, endings);

        var ending = endings.gameObject.AddComponent<ReturnEnding>();
        TableKit.SetRef(ending, "panel", panel);
        TableKit.SetRef(ending, "headline", headline);
        TableKit.SetRef(ending, "body", body);
        TableKit.SetRef(ending, "lighting", dawn.manager);
        TableKit.SetRef(ending, "nightSkyline", skyline);
        TableKit.SetRef(handoff, "ending", ending);

        BuildExitZone(endings, ending, dawn.manager, handoff);
        BuildGuideQuestion(kit, endings, ending, dawn.manager);
    }

    // ---------------------------------------------------------------- the Guide's last question

    /// <summary>
    /// The Guide's closing question on a world-space canvas, pointed at and clicked with the rig's UI ray. Hooked to
    /// <see cref="RestitutionManager.OnAllReturned"/>, so it is asked the moment the eighth envelope is sealed.
    ///
    /// The scene gets its own EventSystem with XRI's input module, because the UI ray needs one and this scene is
    /// played on its own (MVP_Main is never loaded alongside it).
    /// </summary>
    static void BuildGuideQuestion(SharedStyleKit kit, Transform parent, ReturnEnding ending, RestitutionManager manager)
    {
        int uiLayer = LayerMask.NameToLayer("UI");

        var events = new GameObject("EventSystem");
        events.transform.SetParent(parent, false);
        events.AddComponent<UnityEngine.EventSystems.EventSystem>();
        events.AddComponent<XRUIInputModule>();

        var root = new GameObject("GuideQuestion");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(3f, 1.6f, 3f); // moved in front of the player when it is asked

        var canvasGo = new GameObject("Canvas") { layer = uiLayer };
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 560f);
        canvasGo.transform.localScale = Vector3.one * 0.001f; // 0.9 x 0.56 m, comfortable at 1 m
        var group = canvasGo.AddComponent<CanvasGroup>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        Panel(canvasGo.transform, "Background", Vector2.zero, new Vector2(900f, 560f), new Color(0.04f, 0.07f, 0.11f, 0.92f), uiLayer);
        Panel(canvasGo.transform, "Edge", new Vector2(0f, -278f), new Vector2(900f, 5f), kit.brassGold.color, uiLayer);

        var speaker = Label(canvasGo.transform, "Speaker", new Vector2(0f, 212f), new Vector2(820f, 52f), 34f,
            kit.brassGold.color, uiLayer);
        speaker.fontStyle = FontStyles.Bold;

        var line = Label(canvasGo.transform, "Line", new Vector2(0f, 70f), new Vector2(820f, 210f), 44f,
            new Color(0.94f, 0.95f, 0.96f), uiLayer);

        var row = new GameObject("AnswerRow") { layer = uiLayer };
        row.transform.SetParent(canvasGo.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        Stretch(rowRect, new Vector2(0f, -150f), new Vector2(860f, 200f));

        var money = AnswerButton(kit, row.transform, "Button_Money", new Vector2(-215f, 0f), "For the money", uiLayer);
        var people = AnswerButton(kit, row.transform, "Button_People", new Vector2(215f, 0f), "Because people were being hurt", uiLayer);

        var voice = root.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f; // the Guide is in the player's earpiece, not in the room

        var question = root.AddComponent<GuideQuestion>();
        TableKit.SetRef(question, "panel", canvasGo);
        TableKit.SetRef(question, "fade", group);
        TableKit.SetRef(question, "speakerLabel", speaker);
        TableKit.SetRef(question, "lineLabel", line);
        TableKit.SetRef(question, "answerRow", row);
        TableKit.SetRef(question, "moneyButton", money);
        TableKit.SetRef(question, "peopleButton", people);
        TableKit.SetRef(question, "voice", voice);
        TableKit.SetRef(question, "ending", ending);

        canvasGo.SetActive(false);

        // Asked the moment the last envelope is sealed.
        if (manager != null)
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                manager.OnAllReturned, new UnityEngine.Events.UnityAction(question.Ask));
    }

    static Button AnswerButton(SharedStyleKit kit, Transform parent, string name, Vector2 at, string label, int uiLayer)
    {
        var go = new GameObject(name) { layer = uiLayer };
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        Stretch(rect, at, new Vector2(410f, 130f));

        var image = go.AddComponent<Image>();
        image.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.22f, 0.3f, 0.38f, 1f);
        colors.pressedColor = new Color(1f, 0.76f, 0.48f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var text = Label(go.transform, "Label", Vector2.zero, new Vector2(380f, 110f), 30f, new Color(0.95f, 0.95f, 0.96f), uiLayer);
        text.text = label;
        return button;
    }

    static Image Panel(Transform parent, string name, Vector2 at, Vector2 size, Color color, int uiLayer)
    {
        var go = new GameObject(name) { layer = uiLayer };
        go.transform.SetParent(parent, false);
        Stretch(go.AddComponent<RectTransform>(), at, size);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false; // never swallow the ray aimed at a button
        return image;
    }

    static TextMeshProUGUI Label(Transform parent, string name, Vector2 at, Vector2 size, float fontSize, Color color, int uiLayer)
    {
        var go = new GameObject(name) { layer = uiLayer };
        go.transform.SetParent(parent, false);
        Stretch(go.AddComponent<RectTransform>(), at, size);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 at, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
    }

    /// <summary>The desk by the far corner: a lawful destination for the ledger rather than leverage to keep.</summary>
    static LedgerHandoff BuildInvestigatorDesk(SharedStyleKit kit, Mats mats, Transform parent, int layer)
    {
        var desk = new GameObject("InvestigatorDesk").transform;
        desk.SetParent(parent, false);
        desk.localPosition = new Vector3(0.9f, 0f, 4.8f);
        desk.localRotation = Quaternion.Euler(0f, 38f, 0f);

        Part(kit.woodDark, desk, "Top", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(1.5f, 0.06f, 0.75f));
        Part(kit.steelCool, desk, "LegLeft", PrimitiveType.Cube, new Vector3(-0.66f, 0.37f, 0f), new Vector3(0.07f, 0.74f, 0.7f));
        Part(kit.steelCool, desk, "LegRight", PrimitiveType.Cube, new Vector3(0.66f, 0.37f, 0f), new Vector3(0.07f, 0.74f, 0.7f));
        Part(kit.chipBlack, desk, "Blotter", PrimitiveType.Cube, new Vector3(-0.35f, 0.775f, 0f), new Vector3(0.6f, 0.01f, 0.45f));

        // The evidence tray: a shallow steel tray with a brass lip, open at the front.
        var tray = new GameObject("EvidenceTray").transform;
        tray.SetParent(desk, false);
        tray.localPosition = new Vector3(0.38f, 0.77f, 0f);
        Part(kit.steelCool, tray, "Base", PrimitiveType.Cube, Vector3.zero, new Vector3(0.44f, 0.015f, 0.34f));
        Part(kit.brassGold, tray, "LipBack", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0.165f), new Vector3(0.44f, 0.05f, 0.015f));
        Part(kit.brassGold, tray, "LipLeft", PrimitiveType.Cube, new Vector3(-0.215f, 0.025f, 0f), new Vector3(0.015f, 0.05f, 0.34f));
        Part(kit.brassGold, tray, "LipRight", PrimitiveType.Cube, new Vector3(0.215f, 0.025f, 0f), new Vector3(0.015f, 0.05f, 0.34f));

        var label = TableKit.Text(tray, "TrayLabel", new Vector3(0f, 0.052f, 0.172f), Quaternion.Euler(0f, 180f, 0f),
            new Vector2(0.4f, 0.04f), 0.01f, 0.045f, kit.brassGold.color);
        label.text = "EVIDENCE  ·  OFFICE OF THE GAMING INVESTIGATOR";
        label.fontStyle = FontStyles.Bold;

        var deskSign = TableKit.Text(desk, "DeskSign", new Vector3(-0.35f, 0.79f, -0.2f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(0.55f, 0.08f), 0.02f, 0.07f, new Color(0.8f, 0.8f, 0.82f));
        deskSign.text = "Hand over what you found.\nWe will take it from here.";

        var socketGo = new GameObject("TraySocket") { layer = layer };
        socketGo.transform.SetParent(tray, false);
        socketGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var trigger = socketGo.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.14f;
        var socket = socketGo.AddComponent<XRSocketInteractor>();

        var handoff = socketGo.AddComponent<LedgerHandoff>();
        TableKit.SetRef(handoff, "tray", socket);
        TableKit.SetRef(handoff, "seat", socketGo.transform);

        var chime = socketGo.AddComponent<AudioSource>();
        chime.playOnAwake = false;
        chime.spatialBlend = 1f;
        chime.minDistance = 0.5f;
        chime.maxDistance = 8f;
        TableKit.SetRef(handoff, "chime", chime);

        return handoff;
    }

    /// <summary>A side table by where the player arrives, with the ledger they carried out of the vault on it.</summary>
    static void BuildLedgerTable(SharedStyleKit kit, Transform parent, GameObject ledgerPrefab)
    {
        var table = new GameObject("LedgerTable").transform;
        table.SetParent(parent, false);
        // The solo rig spawns at about (3, 0, 2) in room space; this sits within arm's reach to its right.
        table.localPosition = new Vector3(4.5f, 0f, 1.9f);
        table.localRotation = Quaternion.Euler(0f, -18f, 0f);

        Part(kit.woodDark, table, "Top", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(0.8f, 0.05f, 0.6f));
        Part(kit.steelCool, table, "Post", PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), new Vector3(0.08f, 0.37f, 0.08f));
        Part(kit.steelCool, table, "Foot", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.02f, 0.42f));

        var ledger = (GameObject)PrefabUtility.InstantiatePrefab(ledgerPrefab, table);
        ledger.name = "BlackLedger";
        ledger.transform.localPosition = new Vector3(0f, 0.79f, 0f);
        ledger.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
    }

    /// <summary>The ledger: a black book with its title in red, the record of who was skimmed and by how much.</summary>
    static GameObject BlackLedgerPrefab(SharedStyleKit kit, Mats mats, int layer)
    {
        var root = new GameObject("Return_BlackLedger") { layer = layer, tag = LedgerTag };

        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(0.22f, 0.05f, 0.3f);

        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        var grab = root.AddComponent<XRGrabInteractable>();
        TableKit.Set(grab, "m_MovementType", p => p.enumValueIndex = (int)XRBaseInteractable.MovementType.Kinematic);

        Part(kit.chipBlack, root.transform, "Cover", PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.05f, 0.3f));
        Part(mats.ledgerPages, root.transform, "Pages", PrimitiveType.Cube, new Vector3(0.008f, 0f, 0f), new Vector3(0.208f, 0.042f, 0.288f));
        Part(kit.chipBlack, root.transform, "Spine", PrimitiveType.Cube, new Vector3(-0.108f, 0f, 0f), new Vector3(0.015f, 0.052f, 0.302f));

        // The title on the front cover, red while the ledger is still being held over people.
        var title = TableKit.Text(root.transform, "Title", new Vector3(0f, 0.026f, 0.04f), Quaternion.Euler(90f, 0f, 0f),
            new Vector2(0.19f, 0.12f), 0.01f, 0.055f, new Color32(0xC4, 0x2B, 0x22, 0xFF));
        title.text = "THE BLACK\nLEDGER";
        title.fontStyle = FontStyles.Bold;
        title.textWrappingMode = TextWrappingModes.Normal;

        var ledger = root.AddComponent<BlackLedger>();
        TableKit.SetRef(ledger, "title", title);

        var prefab = TableKit.SavePrefab(root, $"{PrefabRoot}/Return_BlackLedger.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>City lights behind the window: what the player sees if they walk out with the money.</summary>
    static GameObject BuildNightSkyline(SharedStyleKit kit, Mats mats, Transform window)
    {
        if (window == null) return null;

        var skyline = new GameObject("NightSkyline").transform;
        skyline.SetParent(window, false);
        skyline.localPosition = new Vector3(0f, 0f, 0.06f);

        Part(mats.night, skyline, "Sky", PrimitiveType.Cube, new Vector3(0f, 0f, 0.04f), new Vector3(1.44f, 0.94f, 0.02f));

        // A ragged row of towers, with a few lit windows each.
        float[] heights = { 0.34f, 0.52f, 0.26f, 0.62f, 0.4f, 0.3f };
        float[] widths = { 0.2f, 0.16f, 0.22f, 0.14f, 0.18f, 0.24f };
        float x = -0.62f;
        for (int i = 0; i < heights.Length; i++)
        {
            float w = widths[i];
            Part(kit.chipBlack, skyline, "Tower_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x + w / 2f, -0.47f + heights[i] / 2f, 0.02f), new Vector3(w, heights[i], 0.02f));
            for (int lit = 0; lit < 3; lit++)
                Part(mats.cityLight, skyline, $"Lights_{i + 1}_{lit + 1}", PrimitiveType.Cube,
                    new Vector3(x + w / 2f, -0.44f + heights[i] * (0.25f + lit * 0.3f), 0.008f),
                    new Vector3(w * 0.55f, 0.018f, 0.01f));
            x += w + 0.015f;
        }

        skyline.gameObject.SetActive(false);
        return skyline.gameObject;
    }

    /// <summary>The ending card, above the door and facing back into the room so it reads from anywhere inside.</summary>
    static (GameObject panel, TextMeshPro headline, TextMeshPro body) BuildEndingCard(SharedStyleKit kit, Mats mats, Transform parent)
    {
        var card = new GameObject("EndingCard").transform;
        card.SetParent(parent, false);
        card.localPosition = new Vector3(3f, 2.15f, -0.88f);
        card.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Part(mats.cardGlass, card, "Panel", PrimitiveType.Cube, Vector3.zero, new Vector3(2.3f, 0.78f, 0.02f));
        Part(kit.brassGold, card, "Edge", PrimitiveType.Cube, new Vector3(0f, -0.4f, 0f), new Vector3(2.3f, 0.025f, 0.03f));

        var headline = TableKit.Text(card, "Headline", new Vector3(0f, 0.22f, -0.02f), Quaternion.identity,
            new Vector2(2.1f, 0.26f), 0.05f, 0.5f, kit.brassGold.color);
        headline.fontStyle = FontStyles.Bold;

        var body = TableKit.Text(card, "Body", new Vector3(0f, -0.1f, -0.02f), Quaternion.identity,
            new Vector2(2.1f, 0.42f), 0.03f, 0.16f, new Color(0.92f, 0.93f, 0.95f));
        body.textWrappingMode = TextWrappingModes.Normal;

        card.gameObject.SetActive(false);
        return (card.gameObject, headline, body);
    }

    /// <summary>
    /// The volume in the doorway. Stepping into it is leaving: everyone paid and the ledger handed over keeps the
    /// sunrise, anything less takes the Kept ending. Sized to the doorway so reaching into the duffel cannot trip it.
    /// </summary>
    static void BuildExitZone(Transform parent, ReturnEnding ending, RestitutionManager manager, LedgerHandoff handoff)
    {
        var zone = new GameObject("ExitZone");
        zone.transform.SetParent(parent, false);
        // The room's door is centred at x = 3 on the near wall (z = -1); this sits in the opening itself.
        zone.transform.localPosition = new Vector3(3f, 0f, -0.3f);

        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 1.1f, 0f);
        box.size = new Vector3(1.6f, 2.2f, 0.7f);

        var trigger = zone.AddComponent<ReturnExitTrigger>();
        TableKit.SetRef(trigger, "ending", ending);
        TableKit.SetRef(trigger, "lighting", manager);
        TableKit.SetRef(trigger, "handoff", handoff);
    }

    // ---------------------------------------------------------------- materials, tag, helpers

    struct Mats
    {
        public Material envelope, envelopeSealed, cash, cashBand, duffel, sky, shaft, dust;
        public Material ledgerPages, night, cityLight, cardGlass;
    }

    static Mats Materials(SharedStyleKit kit)
    {
        return new Mats
        {
            // Manila paper and the warmer, gold-tinted paper of a sealed one.
            envelope = Variant("M_Return_Envelope", kit.plasterCream, new Color(0.79f, 0.71f, 0.55f)),
            envelopeSealed = Variant("M_Return_EnvelopeSealed", kit.plasterCream, new Color(0.95f, 0.78f, 0.49f), 0.35f),
            cash = Variant("M_Return_Cash", kit.feltGreen, new Color(0.55f, 0.62f, 0.45f)),
            cashBand = Variant("M_Return_CashBand", kit.plasterCream, new Color(0.88f, 0.84f, 0.76f)),
            duffel = Variant("M_Return_Duffel", kit.fabricBurgundy, new Color(0.14f, 0.15f, 0.17f)),
            // The window starts as night sky and is tinted to sunrise with the lights.
            sky = Variant("M_Return_Sky", kit.screenGlow, new Color(0.09f, 0.13f, 0.22f), 0.6f),
            shaft = Variant("M_Return_Shaft", kit.screenGlow, new Color(1f, 0.78f, 0.5f), 0.45f),
            dust = Variant("M_Return_Dust", kit.screenGlow, new Color(1f, 0.9f, 0.74f), 0.8f),
            ledgerPages = Variant("M_Return_LedgerPages", kit.plasterCream, new Color(0.84f, 0.81f, 0.74f)),
            // The Kept ending's window: a cold night sky with a few lit windows across the city.
            night = Variant("M_Return_Night", kit.chipBlack, new Color(0.04f, 0.05f, 0.09f)),
            cityLight = Variant("M_Return_CityLight", kit.screenGlow, new Color(0.95f, 0.84f, 0.55f), 0.9f),
            cardGlass = Variant("M_Return_CardGlass", kit.chipBlack, new Color(0.05f, 0.07f, 0.1f)),
        };
    }

    static Material Variant(string name, Material parent, Color color, float emission = 0f)
    {
        string path = $"{MaterialRoot}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(parent); AssetDatabase.CreateAsset(material, path); }
        material.parent = parent;
        material.SetColor("_BaseColor", color);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// The sockets only accept objects by tag, so those tags have to exist in the project. Tags live in a shared file
    /// (ProjectSettings/TagManager.asset) that Slot D owns; adding one is additive and safe for the team to pull.
    /// </summary>
    static void EnsureTag(string tag)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) { Debug.LogWarning($"[Return] Could not open TagManager.asset; add the '{tag}' tag by hand."); return; }

        var tagManager = new SerializedObject(asset[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[Return] Added the '{tag}' tag to the project.");
    }

    static Transform FindRoom()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("BreakRoom")) return t;
        return null;
    }

    static GameObject Part(Material material, Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale)
        => TableKit.Part(material, parent, name, type, position, scale, false, false);
}
