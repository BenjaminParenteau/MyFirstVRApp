using HighStakes.Environment;
using HighStakes.TableGames;
using HighStakes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TableKit;

/// <summary>
/// Adds the start screen and the comfort settings to MVP_Main: a ComfortSettings object, and a small dark lobby at the
/// casino's start (floor, invisible rail, title hologram, console table with MOVE / TURN / START NIGHT / QUIT) whose
/// StartScreen tells SceneFlow when to load the casino. Re-running replaces both. WorldBuilder.BuildMain calls
/// <see cref="AddTo"/> too, so rebuilding MVP_Main keeps them.
/// </summary>
public static class StartScreenBuilder
{
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string VignettePath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";
    const string HologramPath = "Assets/Content/UI/UI_Hologram.mat";
    const string HologramEdgePath = "Assets/Content/UI/UI_HologramEdge.mat";

    [MenuItem("High Stakes/UI/Build Start Screen + Comfort Settings")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        AddTo(scene, WorldBuilder.CasinoStart);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StartScreen] Added the start lobby and comfort settings to MVP_Main. Press Play: choose MOVE / TURN, then START NIGHT.");
    }

    public static void AddTo(Scene scene, Vector3 start)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "StartLobby" || root.name == "ComfortSettings") Object.DestroyImmediate(root);

        var comfort = new GameObject("ComfortSettings").AddComponent<ComfortSettings>();
        SceneManager.MoveGameObjectToScene(comfort.gameObject, scene);
        SetRef(comfort, "vignettePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(VignettePath));

        var kit = Kit(out int layer);
        var glass = AssetDatabase.LoadAssetAtPath<Material>(HologramPath);
        var edge = AssetDatabase.LoadAssetAtPath<Material>(HologramEdgePath);
        var lobby = new GameObject("StartLobby");
        SceneManager.MoveGameObjectToScene(lobby, scene);
        lobby.transform.position = start; // the player stands at its origin facing +Z, where the casino will start them
        var t = lobby.transform;

        // Floor: a dark disc on a brass rim, standing on an invisible floor; invisible walls keep the player on it.
        Part(kit.brassGold, t, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 0.004f, 0f), new Vector3(5.2f, 0.004f, 5.2f), false, false);
        Part(kit.chipBlack, t, "Disc", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(5f, 0.01f, 5f), false, false);
        Invisible(t, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(6f, 0.1f, 6f));
        foreach (var (pos, size) in new[]
                 {
                     (new Vector3(0f, 0.6f, 2.7f), new Vector3(6f, 1.2f, 0.1f)), (new Vector3(0f, 0.6f, -2.7f), new Vector3(6f, 1.2f, 0.1f)),
                     (new Vector3(2.7f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 6f)), (new Vector3(-2.7f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 6f)),
                 })
            Invisible(t, "Rail", pos, size);
        var startPoint = new GameObject("StartPoint").transform;
        startPoint.SetParent(t, false);

        // Title hologram behind the console, facing the player.
        var board = new GameObject("TitleHologram").transform;
        board.SetParent(t, false);
        board.localPosition = new Vector3(0f, 1.05f, 1.9f);
        const float W = 2.2f, H = 1.1f;
        Part(glass, board, "Glass", PrimitiveType.Quad, new Vector3(0f, H / 2f, 0f), new Vector3(W, H, 1f), false, false);
        foreach (float y in new[] { 0f, H })
            Part(edge, board, "Edge", PrimitiveType.Quad, new Vector3(0f, y, -0.001f), new Vector3(W, 0.008f, 1f), false, false);
        foreach (float x in new[] { -W / 2f, W / 2f })
            Part(edge, board, "Edge", PrimitiveType.Quad, new Vector3(x, H / 2f, -0.001f), new Vector3(0.008f, H, 1f), false, false);
        Line(board, "Title", 0.86f, 0.24f, 9f, kit.accentCyan, "HIGH STAKES");
        Line(board, "Subtitle", 0.68f, 0.07f, 3f, Color.white, "ONE NIGHT AT THE MERIDIAN");
        Part(edge, board, "Rule", PrimitiveType.Quad, new Vector3(0f, 0.6f, -0.001f), new Vector3(1.4f, 0.004f, 1f), false, false);
        Line(board, "Story", 0.48f, 0.06f, 2.4f, Color.white, "Win at the tables. Follow the Guide. Take the Ledger. Get out.");
        Line(board, "HowTo", 0.2f, 0.05f, 2f, kit.accentCyan, "Point at a button and pull the trigger to press it.");

        // Console table: the two comfort choices, START NIGHT and QUIT, pressed like any table button.
        var console = new GameObject("Console").transform;
        console.SetParent(t, false);
        console.localPosition = new Vector3(0f, 0f, 0.85f);
        Body(kit, console, 1.2f, 0.6f);
        var move = Button(kit.chipBlack, console, "Button_Move", -0.27f, -0.13f, 0.5f, "", Color.white, layer, out var moveLabel);
        var turn = Button(kit.chipBlack, console, "Button_Turn", 0.27f, -0.13f, 0.5f, "", Color.white, layer, out var turnLabel);
        var startButton = Button(kit.chipGreen, console, "Button_Start", -0.1f, 0.03f, 0.62f, "START NIGHT", Color.white, layer, out _, 0.1f);
        var quit = Button(kit.chipRed, console, "Button_Quit", 0.42f, 0.03f, 0.22f, "QUIT", Color.white, layer, out _, 0.1f);
        var note = FlatText(console, "Note", 0f, 0.17f, new Vector2(1.05f, 0.07f), 0.5f);

        var light = new GameObject("ConsoleLight").AddComponent<Light>();
        light.transform.SetParent(t, false);
        // Directional: the Quest pipeline drops additional (point/spot) lights, and this one goes off with the lobby.
        light.transform.localRotation = Quaternion.Euler(55f, 15f, 0f);
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        light.shadows = LightShadows.None;
        light.color = new Color(1f, 0.85f, 0.65f);

        var screen = lobby.AddComponent<StartScreen>();
        SetRef(screen, "lobby", lobby);
        SetRef(screen, "startPoint", startPoint);
        SetRef(screen, "moveLabel", moveLabel);
        SetRef(screen, "turnLabel", turnLabel);
        SetRef(screen, "noteText", note);
        UnityEventTools.AddPersistentListener(move.Pressed, screen.ToggleMove);
        UnityEventTools.AddPersistentListener(turn.Pressed, screen.ToggleTurn);
        UnityEventTools.AddPersistentListener(startButton.Pressed, screen.StartNight);
        UnityEventTools.AddPersistentListener(quit.Pressed, screen.Quit);

        var flow = Object.FindFirstObjectByType<SceneFlow>();
        if (flow != null) Set(flow, "waitForStart", p => p.boolValue = true);
        else Debug.LogWarning("[StartScreen] No SceneFlow in MVP_Main; the casino will not wait for the start screen.");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void Invisible(Transform parent, string name, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.AddComponent<BoxCollider>().size = size;
    }

    static void Line(Transform board, string name, float y, float height, float maxSize, Color color, string text)
    {
        var line = Text(board, name, new Vector3(0f, y, -0.002f), Quaternion.identity, new Vector2(2f, height), 0.1f, maxSize, color);
        line.text = text;
        line.fontStyle = FontStyles.Bold;
    }
}
