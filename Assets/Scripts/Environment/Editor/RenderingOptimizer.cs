using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Quest 3 rendering pass over the area scenes. Two parts:
/// LOD: the world is built from primitives, so there are no simplified meshes to swap to; instead small fixed details
/// (bottles, chip stacks, slot reels, chairs) get a culling-only LODGroup and stop drawing once they are a few pixels tall.
/// Occlusion: fixed renderers are marked occludee, the big opaque ones occluder, and each area gets a baked (Umbra, CPU
/// only, fine without a GPU) occlusion culling, so walls hide the rooms, hallways and city street behind them.
/// SceneFlow makes each loaded area the active scene, which is the scene Unity reads occlusion data from, so every area
/// is baked on its own. Re-running replaces this tool's LODGroups (the one-level ones) and re-bakes.
/// </summary>
public static class RenderingOptimizer
{
    // Details whose largest side is at most this cull on their own; bigger things are walls, tables and rooms.
    const float DetailMaxSize = 0.8f;
    // Prefab instances up to this size (a chair, a bar stool) cull as one piece, so they never lose a leg first.
    const float PropMaxSize = 1.5f;
    // Culled below this fraction of screen height, about 10 px on Quest 3 (~96° vertical FOV):
    // a 5 cm chip stack at ~4.5 m, a 30 cm bottle at ~27 m, a 1 m chair at ~90 m.
    const float CullHeight = 0.005f;

    // Umbra bake settings for indoor rooms: walls and doors occlude; doorways, cage bars and gaps under tables stay open.
    const float SmallestOccluder = 1f;
    const float SmallestHole = 0.1f;
    const float BackfaceThreshold = 100f;
    // Bigger than this is a backdrop (the casino's 600 m starry-sky dome): always on screen, and baking it would
    // stretch the bake grid over the whole sky (hours instead of minutes), so it is left out of occlusion entirely.
    const float BackdropSize = 250f;
    // Root name of the 190x130 m city block seen through the entrance windows (ExteriorDistrictBuilder). The door is
    // permanently sealed (see CasinoEntranceBuilder's ClosedDoorCollision), so it is never walkable, only ever seen
    // through glass; including it in the bake stretches the view-cell grid from one interior to a whole city block,
    // which is what turned the bake from minutes into 30+ minutes with no result.
    const string ExteriorBackdropRoot = "ExteriorDistrict";

    // Any of these means the object never moves (static batching and lightmaps already depend on that).
    const StaticEditorFlags Fixed = StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                    StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

    [MenuItem("High Stakes/Rendering/Optimize Open Scene (LOD + Occlusion)")]
    static void OptimizeOpenSceneMenu()
    {
        var path = SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(path)) { Debug.LogError("[Rendering] Save the scene first."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        OptimizeScenes(new[] { path });
    }

    [MenuItem("High Stakes/Rendering/Optimize All Area Scenes (LOD + Occlusion)")]
    static void OptimizeAllMenu()
    {
        if (!EditorUtility.DisplayDialog("Optimize all area scenes?",
                "Adds culling LODs to small details, sets occluder/occludee flags and bakes occlusion culling in the casino, VIP room, security hub and wings (CPU only, takes a few minutes). Lightmaps are untouched.",
                "Optimize", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        OptimizeAll();
    }

    /// <summary>Batch entry point (-executeMethod RenderingOptimizer.OptimizeAll).</summary>
    public static void OptimizeAll() => OptimizeScenes(WorldBuilder.AreaScenePaths());

    public static void OptimizeScenes(IEnumerable<string> paths)
    {
        RaiseLodBias();
        foreach (var path in paths)
        {
            // Opened alone: a bake covers every loaded scene, and each area must own its data.
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var lods = AddDetailLods(scene);
            SetOcclusionFlags(scene, out var occluders, out var occludees);
            EditorSceneManager.SaveScene(scene);

            StaticOcclusionCulling.smallestOccluder = SmallestOccluder;
            StaticOcclusionCulling.smallestHole = SmallestHole;
            StaticOcclusionCulling.backfaceThreshold = BackfaceThreshold;
            var baked = StaticOcclusionCulling.Compute();
            EditorSceneManager.SaveScene(scene);

            if (baked) Debug.Log($"[Rendering] {scene.name}: {lods} culling LODs, {occluders} occluders / {occludees} occludees, occlusion baked.");
            else Debug.LogError($"[Rendering] {scene.name}: occlusion bake failed ({lods} culling LODs, {occluders} occluders set).");
        }
    }

    // ------------------------------------------------------------------ LOD

    static int AddDetailLods(Scene scene)
    {
        // One-level (culling-only) groups are this tool's; authored LOD chains have two or more levels and are kept.
        foreach (var group in Components<LODGroup>(scene))
            if (group.lodCount == 1 && !PrefabUtility.IsPartOfPrefabAsset(group) &&
                (!PrefabUtility.IsPartOfPrefabInstance(group) || PrefabUtility.IsAddedComponentOverride(group)))
                Object.DestroyImmediate(group);

        var grouped = new HashSet<Renderer>();
        foreach (var group in Components<LODGroup>(scene))
            foreach (var lod in group.GetLODs())
                foreach (var r in lod.renderers) if (r != null) grouped.Add(r);

        var count = 0;
        foreach (var root in scene.GetRootGameObjects()) count += AddPropLods(root.transform, grouped);

        foreach (var renderer in Components<MeshRenderer>(scene))
        {
            if (grouped.Contains(renderer) || !IsCullableDetail(renderer)) continue;
            if (renderer.bounds.size.MaxComponent() > DetailMaxSize) continue;
            if (renderer.GetComponent<LODGroup>() != null) continue;
            AddCullingLod(renderer.gameObject, new Renderer[] { renderer });
            grouped.Add(renderer);
            count++;
        }
        return count;
    }

    /// <summary>Walks down from <paramref name="t"/>; the first small prefab instance on each branch culls as one piece.</summary>
    static int AddPropLods(Transform t, HashSet<Renderer> grouped)
    {
        if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && t.GetComponent<LODGroup>() == null)
        {
            var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length > 0 && System.Array.TrueForAll(renderers, r => IsCullableDetail(r) && !grouped.Contains(r)))
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                if (bounds.size.MaxComponent() <= PropMaxSize)
                {
                    AddCullingLod(t.gameObject, renderers);
                    grouped.UnionWith(renderers);
                    return 1;
                }
            }
        }
        var count = 0;
        foreach (Transform child in t) count += AddPropLods(child, grouped);
        return count;
    }

    /// <summary>A fixed mesh. Text is left alone (TMP rebuilds its mesh and bounds at runtime).</summary>
    static bool IsCullableDetail(MeshRenderer renderer)
    {
        if (!IsFixed(renderer.gameObject) || renderer.GetComponent<TMP_Text>() != null) return false;
        var filter = renderer.GetComponent<MeshFilter>();
        return filter != null && filter.sharedMesh != null;
    }

    static void AddCullingLod(GameObject go, Renderer[] renderers)
    {
        var group = go.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.None; // a dithered cross-fade costs more on Quest than the pop at 10 px
        group.SetLODs(new[] { new LOD(CullHeight, renderers) });
        group.RecalculateBounds();
    }

    /// <summary>
    /// The thresholds above are tuned for a LOD bias of 1; Android's default quality level ships with 0.4, which would
    /// cull details 2.5x closer. Levels already above 1 (PC) are kept.
    /// </summary>
    static void RaiseLodBias()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels = settings.FindProperty("m_QualitySettings");
        for (var i = 0; i < levels.arraySize; i++)
        {
            var bias = levels.GetArrayElementAtIndex(i).FindPropertyRelative("lodBias");
            if (bias.floatValue < 1f) bias.floatValue = 1f;
        }
        if (settings.ApplyModifiedPropertiesWithoutUndo()) AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ occlusion

    /// <summary>
    /// Every fixed renderer can be hidden; only big opaque ones hide others (glass, cutouts and text never do).
    /// Moving things (chips, cards, the roulette ball) keep their flags: they are still culled behind walls at runtime.
    /// </summary>
    static void SetOcclusionFlags(Scene scene, out int occluders, out int occludees)
    {
        occluders = occludees = 0;
        foreach (var renderer in Components<MeshRenderer>(scene))
        {
            var go = renderer.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((flags & Fixed) == 0) continue;

            if (renderer.bounds.size.MaxComponent() > BackdropSize || HasAncestorNamed(renderer.transform, ExteriorBackdropRoot))
            {
                var backdrop = flags & ~(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                if (backdrop != flags) GameObjectUtility.SetStaticEditorFlags(go, backdrop);
                continue;
            }

            var updated = flags | StaticEditorFlags.OccludeeStatic;
            if (IsOccluder(renderer)) { updated |= StaticEditorFlags.OccluderStatic; occluders++; }
            else updated &= ~StaticEditorFlags.OccluderStatic;
            occludees++;
            if (updated != flags) GameObjectUtility.SetStaticEditorFlags(go, updated);
        }
    }

    static bool IsOccluder(MeshRenderer renderer)
    {
        if (renderer.GetComponent<TMP_Text>() != null) return false;
        foreach (var material in renderer.sharedMaterials)
            if (material == null || material.renderQueue >= (int)RenderQueue.AlphaTest) return false;
        // At least two sides as big as the smallest occluder: a wall or a floor, not a pole or a rail.
        var size = renderer.bounds.size;
        var sides = 0;
        if (size.x >= SmallestOccluder) sides++;
        if (size.y >= SmallestOccluder) sides++;
        if (size.z >= SmallestOccluder) sides++;
        return sides >= 2;
    }

    // ------------------------------------------------------------------ helpers

    static bool IsFixed(GameObject go) => (GameObjectUtility.GetStaticEditorFlags(go) & Fixed) != 0;

    static bool HasAncestorNamed(Transform t, string name)
    {
        for (var p = t; p != null; p = p.parent) if (p.name == name) return true;
        return false;
    }

    static float MaxComponent(this Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

    static IEnumerable<T> Components<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
                yield return component;
    }
}
