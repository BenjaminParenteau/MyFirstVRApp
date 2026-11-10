using HighStakes.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts the movement and comfort pieces into MVP_Main: <see cref="TeleportSurfaces"/> (maps the walkable floor of
/// whichever area is loaded so there is always somewhere to teleport), <see cref="TeleportBlink"/> (fade in on arrival)
/// and <see cref="MoveRamp"/> (ease up to walking speed). Also reserves the physics layer the generated teleport
/// surfaces sit on. Re-running replaces them; <see cref="WorldBuilder"/>.BuildMain calls <see cref="AddTo"/> too, so a
/// world rebuild keeps them. Comfort choices themselves live on the ComfortSettings object (StartScreenBuilder).
/// </summary>
public static class LocomotionBuilder
{
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string RootName = "Locomotion";

    /// <summary>Name given to physics layer <see cref="TeleportSurfaces.Layer"/>, which the teleport ray already reads.</summary>
    public const string LayerName = "Teleport";

    [MenuItem("High Stakes/Build Locomotion & Comfort")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ReserveTeleportLayer();
        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        AddTo(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Locomotion] MVP_Main has the teleport mapper, the arrival fade and the move ramp. Press Play: the " +
                  "left stick walks, pushing it with MOVE set to TELEPORT (or the right stick any time) aims the arc.");
    }

    /// <summary>Batch entry point (-executeMethod LocomotionBuilder.Build).</summary>
    public static void BuildBatch()
    {
        ReserveTeleportLayer();
        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        AddTo(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void AddTo(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == RootName) Object.DestroyImmediate(root);

        var holder = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(holder, scene);
        holder.AddComponent<TeleportSurfaces>();
        holder.AddComponent<MoveRamp>();

        // The arrival fade shares SceneFlow's black quad in front of the camera rather than adding a second one.
        var blink = holder.AddComponent<TeleportBlink>();
        var flow = Object.FindFirstObjectByType<SceneFlow>();
        if (flow != null)
        {
            var fade = new SerializedObject(flow).FindProperty("fade").objectReferenceValue;
            var so = new SerializedObject(blink);
            so.FindProperty("fade").objectReferenceValue = fade;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (fade == null) Debug.LogWarning("[Locomotion] SceneFlow has no ScreenFade quad; teleports will not fade in.");
        }
        else
        {
            Debug.LogWarning("[Locomotion] No SceneFlow in the scene; teleports will not fade in.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    /// <summary>
    /// The generated teleport surfaces need a physics layer that the teleport ray's raycast mask already includes
    /// (layer 31) and that collides with nothing, so they are aim-only: invisible, and never in the way of the player,
    /// a dropped chip or another system's physics. Layers and the collision matrix live in shared files Slot D owns
    /// (ProjectSettings/TagManager.asset and DynamicsManager.asset); naming an unused layer is additive and safe to pull.
    /// </summary>
    public static void ReserveTeleportLayer()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAsset == null || tagManagerAsset.Length == 0)
        {
            Debug.LogWarning($"[Locomotion] Could not open TagManager.asset; name layer {TeleportSurfaces.Layer} " +
                             $"'{LayerName}' by hand.");
        }
        else
        {
            var tagManager = new SerializedObject(tagManagerAsset[0]);
            var layers = tagManager.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(TeleportSurfaces.Layer);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = LayerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[Locomotion] Named layer {TeleportSurfaces.Layer} '{LayerName}'.");
            }
            else if (slot.stringValue != LayerName)
            {
                Debug.LogError($"[Locomotion] Layer {TeleportSurfaces.Layer} is already '{slot.stringValue}'. " +
                               "Teleport surfaces would share it; give TeleportSurfaces.Layer a free layer instead.");
            }
        }

        for (int other = 0; other < 32; other++) Physics.IgnoreLayerCollision(TeleportSurfaces.Layer, other, true);
        var dynamics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
        if (dynamics != null && dynamics.Length > 0) EditorUtility.SetDirty(dynamics[0]);
    }
}
