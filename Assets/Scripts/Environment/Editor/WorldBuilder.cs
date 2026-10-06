using HighStakes.Economy;
using HighStakes.Environment;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts the game together as one scene per area, loaded through doors:
/// MVP_Main (always loaded: the one player rig, chip wallet, wrist display, SceneFlow) and the areas MVP_Casino
/// (start), MVP_VIP and the three security wings. Builds the generated areas, adds the doors to Ashton's casino,
/// builds MVP_Main and sets the build's scene list with MVP_Main first.
/// </summary>
public static class WorldBuilder
{
    const string KitPath = "Assets/Content/StyleKit/SharedStyleKit.asset";
    const string LightingPath = "Assets/Content/StyleKit/Lighting/StyleKit_Lighting.lighting";
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string CasinoPath = "Assets/Scenes/MVP/MVP_Casino.unity";
    const string VipPath = "Assets/Scenes/MVP/MVP_VIP.unity";
    const string WristDisplayPath = "Assets/Content/UI/UI_WristDisplay.prefab";
    const string FadeMaterialPath = "Assets/Content/Environment/Materials/Env_ScreenFade.mat";

    // Ashton's casino spawn (its _SoloTest rig): the entrance end of the centre aisle, facing the tables.
    static readonly Vector3 CasinoStart = new Vector3(0f, 0f, -7.7f);

    [MenuItem("High Stakes/Build MVP World (all scenes)")]
    public static void BuildMenu()
    {
        if (!EditorUtility.DisplayDialog("Build MVP world?",
                "Rebuilds MVP_Main, MVP_VIP and the security wings, re-adds the casino doors and sets the build scene list. Hand edits to the generated scenes are lost. Lightmaps are not re-baked (use the batch entry point for that).",
                "Build", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build(bake: false);
    }

    /// <summary>Batch entry point (-executeMethod WorldBuilder.BuildAndBake): everything, with baked lighting for the generated areas.</summary>
    public static void BuildAndBake() => Build(bake: true);

    static void Build(bool bake)
    {
        BlackjackTableBuilder.BuildPrefab();
        VipRoomBuilder.Build();
        if (bake) Bake(VipPath);
        HallwayBuilder.Build();
        if (bake) foreach (var path in HallwayBuilder.ScenePaths) Bake(path);
        AddCasinoDoors();
        BuildMain();

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainPath, true),
            new EditorBuildSettingsScene(CasinoPath, true),
            new EditorBuildSettingsScene(VipPath, true),
        };
        foreach (var path in HallwayBuilder.ScenePaths) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("[World] Built. Open MVP_Main and press Play: you start in the casino; walk into a door to change area.");
    }

    static void Bake(string path)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Lightmapping.Bake();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    // ------------------------------------------------------------------ casino doors

    /// <summary>
    /// The VIP portal goes on Ashton's existing VIP door; the security wings get staff doors: one on the right wall
    /// near the cashier cage, two on the back wall between the VIP door and the cage. Re-running replaces them.
    /// </summary>
    static void AddCasinoDoors()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        var scene = EditorSceneManager.OpenScene(CasinoPath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Casino_Doors") Object.DestroyImmediate(root);
        var doors = new GameObject("Casino_Doors").transform;

        // Ashton's VIP door: 1.9 m wide, its face at z 12.66, the player in front of it on -Z.
        Portal(doors, "Door_Casino_VIP", new Vector3(-1.8f, 0f, 12.6f), 0f, 1.9f, "Casino_VIP", "VIP_HallStart", "MVP_VIP");

        // Staff doors: right wall (inner face x 8.8) between the pilasters at z 6 and 10; back wall (panel face z 12.775).
        StaffDoor(kit, doors, "Door_Casino_A", new Vector3(8.8f, 0f, 8f), 90f, "SECURITY", "Casino_A", "HallA_Start", HallwayBuilder.ScenePaths[0]);
        StaffDoor(kit, doors, "Door_Casino_B", new Vector3(1.0f, 0f, 12.775f), 0f, "CAMERAS", "Casino_B", "HallB_Start", HallwayBuilder.ScenePaths[1]);
        StaffDoor(kit, doors, "Door_Casino_C", new Vector3(3.6f, 0f, 12.775f), 0f, "VAULT", "Casino_C", "HallC_Start", HallwayBuilder.ScenePaths[2]);

        EditorSceneManager.SaveScene(scene);
    }

    // A door's front (where the player stands) is its local -Z.
    static DoorPortal Portal(Transform parent, string name, Vector3 pos, float yaw, float width, string id, string target, string scenePath)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var arrival = new GameObject("Arrival").transform;
        arrival.SetParent(root, false);
        arrival.localPosition = new Vector3(0f, 0f, -1.8f);   // in front of the door
        arrival.localRotation = Quaternion.Euler(0f, 180f, 0f); // facing away from it

        var portal = root.gameObject.AddComponent<DoorPortal>();
        portal.portalId = id;
        portal.targetId = target;
        portal.targetScene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        portal.arrival = arrival;
        portal.triggerWidth = width;
        return portal;
    }

    // A 1 x 2.1 m steel staff door in a brass frame against the wall, with a small sign over it. Kit materials only.
    static void StaffDoor(SharedStyleKit kit, Transform parent, string name, Vector3 pos, float yaw, string label, string id, string target, string scenePath)
    {
        var root = Portal(parent, name, pos, yaw, StyleScale.DoorWidth, id, target, scenePath).transform;
        float w = StyleScale.DoorWidth, h = StyleScale.DoorHeight;
        Box(kit.steelCool, root, "Leaf", new Vector3(0f, h / 2f, -0.03f), new Vector3(w, h, 0.05f));
        Box(kit.brassGold, root, "FrameLeft", new Vector3(-(w / 2f + 0.06f), (h + 0.12f) / 2f, -0.06f), new Vector3(0.12f, h + 0.12f, 0.1f));
        Box(kit.brassGold, root, "FrameRight", new Vector3(w / 2f + 0.06f, (h + 0.12f) / 2f, -0.06f), new Vector3(0.12f, h + 0.12f, 0.1f));
        Box(kit.brassGold, root, "FrameTop", new Vector3(0f, h + 0.06f, -0.06f), new Vector3(w + 0.24f, 0.12f, 0.1f));
        Box(kit.chipBlack, root, "SignPlate", new Vector3(0f, h + 0.32f, -0.03f), new Vector3(0.9f, 0.22f, 0.03f));

        var sign = new GameObject("SignText").AddComponent<TextMeshPro>();
        sign.transform.SetParent(root, false);
        sign.transform.localPosition = new Vector3(0f, h + 0.32f, -0.05f);
        sign.rectTransform.sizeDelta = new Vector2(0.85f, 0.18f);
        sign.text = label;
        sign.fontStyle = FontStyles.Bold;
        sign.alignment = TextAlignmentOptions.Center;
        sign.enableAutoSizing = true;
        sign.fontSizeMin = 0.1f;
        sign.fontSizeMax = 2f;
        sign.color = kit.brassGold.color;
    }

    static void Box(Material m, Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.isStatic = true;
    }

    // ------------------------------------------------------------------ MVP_Main

    static void BuildMain()
    {
        var kit = AssetDatabase.LoadAssetAtPath<SharedStyleKit>(KitPath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(0x3A, 0x30, 0x26, 0xFF);
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
        if (lighting != null) Lightmapping.lightingSettings = lighting;

        // The one player rig, standing at the casino's start.
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(kit.playerRig);
        rig.transform.position = CasinoStart;
        var fade = BuildFade(rig.GetComponentInChildren<Camera>(true).transform);

        // Editor-only keyboard/mouse simulator; SoloTestRoot switches it off in builds and with a headset.
        var editorSim = new GameObject("_EditorSimulator");
        editorSim.AddComponent<SoloTestRoot>();
        StyleKitBuilder.AddSimulator(editorSim.transform);

        var game = new GameObject("Game");
        var flow = game.AddComponent<SceneFlow>();
        var so = new SerializedObject(flow);
        so.FindProperty("startScene").stringValue = "MVP_Casino";
        so.FindProperty("fade").objectReferenceValue = fade;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ponytail: 1,000 chips to start until the cashier cage hands them out.
        var wallet = new GameObject("ChipWallet").AddComponent<ChipWallet>();
        wallet.transform.SetParent(game.transform);
        var walletSo = new SerializedObject(wallet);
        walletSo.FindProperty("startingBalance").intValue = 1000;
        walletSo.ApplyModifiedPropertiesWithoutUndo();

        var volume = new GameObject("Volume_Global").AddComponent<Volume>();
        volume.transform.SetParent(game.transform);
        volume.isGlobal = true;
        volume.sharedProfile = kit.volumeProfile;

        var wrist = AssetDatabase.LoadAssetAtPath<GameObject>(WristDisplayPath);
        if (wrist != null) PrefabUtility.InstantiatePrefab(wrist);
        else Debug.LogWarning("[World] " + WristDisplayPath + " not found; MVP_Main has no wrist display.");

        EditorSceneManager.SaveScene(scene, MainPath);
    }

    // A black quad just in front of the eyes; SceneFlow fades its alpha. Transparent URP Unlit, drawn last.
    static Renderer BuildFade(Transform camera)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(FadeMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f); // transparent
            material.SetFloat("_Blend", 0f);   // alpha
            material.SetFloat("_Cull", 0f);    // both sides
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", Color.black);
            material.renderQueue = (int)RenderQueue.Overlay;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FadeMaterialPath));
            AssetDatabase.CreateAsset(material, FadeMaterialPath);
        }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ScreenFade";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(camera, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 0.08f);
        quad.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        var renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }
}
