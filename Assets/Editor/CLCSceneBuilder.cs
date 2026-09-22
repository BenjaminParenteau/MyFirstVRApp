using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// One-click scene setup for the "CLC - Re-imagining an Experience in Virtual Reality" assignment.
// Run these from the Unity menu: CLC > ...
public static class CLCSceneBuilder
{
    const string FolderPath = "Assets/CLC - Re-imagining an Experience in Virtual Reality";

    // Same rig BasicScene.unity uses: controller-based locomotion with teleport + smooth move providers already wired up.
    const string XrRigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    [MenuItem("CLC/Build Part 1 - Beach Scene (Life Experience)")]
    public static void BuildBeachScene()
    {
        EnsureFolder();

        // Empty, not DefaultGameObjects: the XR rig brings its own camera, so we don't want Unity's default Main Camera too.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sunGO = new GameObject("Directional Light");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.8f);
        sun.intensity = 1.2f;
        sunGO.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.6f, 0.75f, 0.85f);

        var sand = CreateWalkableGround("Sand", new Vector3(0f, 0f, 0f), new Vector3(5f, 1f, 3f), new Color(0.87f, 0.76f, 0.52f));
        var ocean = CreateWalkableGround("Ocean", new Vector3(0f, -0.05f, 32f), new Vector3(5f, 1f, 6f), new Color(0.16f, 0.45f, 0.66f));

        var interactionManagerGO = new GameObject("XR Interaction Manager");
        interactionManagerGO.AddComponent<XRInteractionManager>();

        SpawnXrRig(new Vector3(0f, 0f, -8f));
        AddDeviceSimulator();

        AddBeachProps();

        SaveScene(scene, "Part1_LifeExperience_Beach.unity");
    }

    // A teleport/walk-enabled ground plane. Interaction layer is set to Everything so it matches
    // whatever layer mask the rig's teleport interactor uses, without having to hand-copy the
    // project's custom interaction layer setup.
    static GameObject CreateWalkableGround(string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        SetColor(go, color);

        var teleportArea = go.AddComponent<TeleportationArea>();
        teleportArea.interactionLayers = -1;

        return go;
    }

    static void SpawnXrRig(Vector3 position)
    {
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPrefabPath);
        if (rigPrefab == null)
        {
            Debug.LogWarning("CLCSceneBuilder: couldn't find XR rig prefab at " + XrRigPrefabPath + " - scene will have no player/movement.");
            return;
        }

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = position;
    }

    // Lets you playtest with keyboard/mouse in the Editor Game view when no headset is connected.
    // Remove this object before building to an actual Quest 3, since real headset/controller input drives the rig there instead.
    static void AddDeviceSimulator()
    {
        var simGO = new GameObject("XR Device Simulator");
        simGO.AddComponent<XRDeviceSimulator>();
    }

    static void AddBeachProps()
    {
        var props = new GameObject("Beach Props");

        var towel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        towel.name = "Towel";
        towel.transform.SetParent(props.transform);
        towel.transform.position = new Vector3(2.5f, 0.02f, -4f);
        towel.transform.localScale = new Vector3(1.5f, 0.02f, 0.8f);
        SetColor(towel, new Color(0.85f, 0.2f, 0.25f));

        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Beach Ball";
        ball.transform.SetParent(props.transform);
        ball.transform.position = new Vector3(3.6f, 0.25f, -3.7f);
        ball.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        SetColor(ball, new Color(0.95f, 0.75f, 0.1f));

        var umbrellaPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        umbrellaPole.name = "Umbrella Pole";
        umbrellaPole.transform.SetParent(props.transform);
        umbrellaPole.transform.position = new Vector3(-2.5f, 1f, -4f);
        umbrellaPole.transform.localScale = new Vector3(0.06f, 1f, 0.06f);
        SetColor(umbrellaPole, new Color(0.4f, 0.3f, 0.2f));

        var umbrellaCanopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        umbrellaCanopy.name = "Umbrella Canopy";
        umbrellaCanopy.transform.SetParent(props.transform);
        umbrellaCanopy.transform.position = new Vector3(-2.5f, 2f, -4f);
        umbrellaCanopy.transform.localScale = new Vector3(1.4f, 0.08f, 1.4f);
        SetColor(umbrellaCanopy, new Color(0.9f, 0.35f, 0.2f));
    }

    [MenuItem("CLC/Build Part 2 Placeholder Scene (Software Application)")]
    public static void BuildPart2Placeholder()
    {
        BuildPlaceholder("Part2_SoftwareApplication.unity");
    }

    [MenuItem("CLC/Build Part 3 Placeholder Scene (Game)")]
    public static void BuildPart3Placeholder()
    {
        BuildPlaceholder("Part3_Game.unity");
    }

    static void BuildPlaceholder(string fileName)
    {
        EnsureFolder();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SaveScene(scene, fileName);
    }

    static void SaveScene(Scene scene, string fileName)
    {
        string path = FolderPath + "/" + fileName;
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        Debug.Log("Saved scene to " + path);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "CLC - Re-imagining an Experience in Virtual Reality");
        }
    }

    static void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;

        var mat = new Material(shader);
        mat.color = color;
        renderer.sharedMaterial = mat;
    }
}
