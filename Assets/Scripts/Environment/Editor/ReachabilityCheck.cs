using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HighStakes.Environment;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Answers the question the locomotion play check deliberately does not: not "does this area have teleport surfaces?"
/// but "can the player actually get to the things they have to touch?" The teleport map keeps a 0.25 m body radius
/// clear, so a prop standing in a gap narrower than about a metre has no teleport target next to it — a player in
/// teleport mode (roughly half our testers) simply cannot reach it, even though it is plainly visible and reachable
/// in smooth mode. That is the failure a test session would otherwise spend five testers discovering.
/// <br/>
/// Plays MVP_Main, walks every area, and for each interactable asks whether any mapped walkable cell sits within
/// arm's reach of it. Writes Logs/LocomotionQA/reachability.txt. A diagnostic only: it reports, it never moves
/// anyone's props, because the props belong to the slot that built them.
/// </summary>
[InitializeOnLoad]
public static class ReachabilityCheck
{
    const string Pending = "Locomotion.ReachabilityCheck";
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string ResultPath = "Logs/LocomotionQA/reachability.txt";

    /// <summary>
    /// How far from a prop a standing player's feet may be and still reach it. The body keeps 0.25 m clear, so the
    /// feet never get closer than that; 0.85 m is about as far as a comfortable lean, and anything beyond it reads to
    /// a player as "I can see it but I can't touch it".
    /// </summary>
    const float Reach = 0.85f;

    /// <summary>Highest a prop may sit above the floor and still be reachable standing (m).</summary>
    const float ReachHeight = 2.1f;

    /// <summary>Directions sampled around a prop when looking for somewhere to stand.</summary>
    const int Samples = 24;

    // Same tour as the play check: every area SceneFlow can load, and the door to arrive at.
    static readonly (string Scene, string Arrival)[] Tour =
    {
        ("MVP_VIP", "VIP_HallStart"),
        ("MVP_Casino", "Casino_VIP"),
        ("MVP_SecurityHall", "Hub_Start"),
        ("MVP_SecurityOffice", "HallA_Start"),
        ("MVP_SecurityHall", "Hub_A"),
        ("MVP_CameraRoom", "HallB_Start"),
        ("MVP_SecurityHall", "Hub_B"),
        ("MVP_Vault", "HallC_Start"),
    };

    static readonly List<string> lines = new List<string>();
    static readonly HashSet<string> audited = new HashSet<string>();

    static int step, reachable, unreachable;
    static double deadline, waitUntil;
    static bool passed;
    static string destination;

    static ReachabilityCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0;
                reachable = 0;
                unreachable = 0;
                passed = false;
                destination = null;
                lines.Clear();
                audited.Clear();
                deadline = EditorApplication.timeSinceStartup + 420;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
            }
        };
    }

    [MenuItem("High Stakes/Run Reachability Check")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Start();
    }

    /// <summary>Batch entry point (-executeMethod ReachabilityCheck.RunBatch).</summary>
    public static void RunBatch() => Start();

    static void Start()
    {
        SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene(MainPath);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new Exception($"Timed out at tour step {step}/{Tour.Length}, " +
                                    $"{audited.Count} areas audited, heading for {destination ?? "the first area"}");
            if (EditorApplication.timeSinceStartup < waitUntil) return;

            var surfaces = TeleportSurfaces.Instance;
            if (surfaces == null) return;

            var flow = SceneFlow.Instance;
            if (flow == null) throw new Exception("No SceneFlow in MVP_Main");

            if (flow.WaitingForStart)
            {
                var start = UnityEngine.Object.FindFirstObjectByType<HighStakes.UI.StartScreen>();
                if (start == null) throw new Exception("MVP_Main has no StartScreen to press");
                start.StartNight();
                return;
            }
            if (flow.Busy || flow.CurrentScene == null || surfaces.Mapping) return;
            if (surfaces.BuiltFor != flow.CurrentScene) return;

            if (audited.Add(flow.CurrentScene)) Audit(flow.CurrentScene, surfaces);

            if (step >= Tour.Length)
            {
                Finish(true);
                return;
            }

            var next = Tour[step++];
            destination = next.Scene;
            if (!flow.Go(next.Scene, next.Arrival))
                throw new Exception($"SceneFlow refused to travel to {next.Scene}");
            waitUntil = EditorApplication.timeSinceStartup + 1.0;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            lines.Add("ERROR: " + e.Message);
            Debug.Log("[Reachability] CHECK FAILED");
            Finish(false);
        }
    }

    /// <summary>Every interactable in this area, and whether a teleporting player can get within reach of it.</summary>
    static void Audit(string scene, TeleportSurfaces surfaces)
    {
        var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
        var rig = origin != null ? origin.transform : null;

        lines.Add($"## {scene}  ({surfaces.SurfaceCount} surfaces, {surfaces.CellCount} walkable cells)");

        // Where the mapped floor actually is. A prop reported as having no walkable cell near it is either genuinely
        // stranded or belongs to another loaded scene (the CCTV wall loads rooms additively to observe them), and
        // comparing its position against these bounds is what tells the two apart.
        var holder = GameObject.Find("Teleport_Surfaces");
        if (holder != null && holder.transform.childCount > 0)
        {
            Bounds? map = null;
            foreach (Transform child in holder.transform)
            {
                var box = child.GetComponent<BoxCollider>();
                if (box == null) continue;
                if (map == null) map = box.bounds;
                else { var b = map.Value; b.Encapsulate(box.bounds); map = b; }
            }
            if (map != null)
                lines.Add($"   mapped floor spans x {map.Value.min.x:0.0}..{map.Value.max.x:0.0}, " +
                          $"z {map.Value.min.z:0.0}..{map.Value.max.z:0.0}");
        }

        int here = 0, missed = 0;
        foreach (var interactable in UnityEngine.Object.FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None))
        {
            if (!Counts(interactable, rig)) continue;
            here++;

            var at = Middle(interactable);
            if (Reachable(surfaces, at, out float nearest, out float height))
            {
                reachable++;
            }
            else
            {
                missed++;
                unreachable++;
                lines.Add($"  UNREACHABLE  [{interactable.gameObject.scene.name}] {Path(interactable.transform)}");
                lines.Add($"               at ({at.x:0.00}, {at.y:0.00}, {at.z:0.00}); " +
                          (height > ReachHeight
                              ? $"{height:0.00} m above the nearest walkable floor, over the {ReachHeight:0.0} m standing reach"
                              : nearest < float.MaxValue
                                  ? $"nearest somewhere-to-stand is {nearest:0.00} m away, over the {Reach:0.00} m reach"
                                  : "no mapped walkable cell anywhere near it"));
            }
        }

        lines.Add(here == 0
            ? "  (no interactables found in this area)"
            : $"  {here - missed}/{here} reachable by teleport" + (missed > 0 ? $", {missed} NOT" : ""));
        lines.Add(string.Empty);
    }

    /// <summary>
    /// Is this something the player has to reach? Skips the generated teleport surfaces (they are interactables too,
    /// on layer 31), anything attached to the rig, and anything switched off.
    /// </summary>
    static bool Counts(XRBaseInteractable interactable, Transform rig)
    {
        if (interactable == null || interactable is TeleportSurface) return false;
        if (interactable.gameObject.layer == TeleportSurfaces.Layer) return false;
        if (!interactable.gameObject.activeInHierarchy || !interactable.enabled) return false;
        if (rig != null && interactable.transform.IsChildOf(rig)) return false;
        return true;
    }

    /// <summary>The middle of an interactable's colliders, falling back to its transform.</summary>
    static Vector3 Middle(XRBaseInteractable interactable)
    {
        Bounds? box = null;
        foreach (var collider in interactable.colliders)
        {
            if (collider == null) continue;
            if (box == null) box = collider.bounds;
            else { var b = box.Value; b.Encapsulate(collider.bounds); box = b; }
        }
        return box?.center ?? interactable.transform.position;
    }

    /// <summary>
    /// Looks for somewhere to stand: the prop's own cell first, then rings out to <see cref="Reach"/>. Reports the
    /// closest walkable cell found and how high the prop sits above it, so a failure says which rule it broke.
    /// </summary>
    static bool Reachable(TeleportSurfaces surfaces, Vector3 at, out float nearest, out float height)
    {
        nearest = float.MaxValue;
        height = 0f;

        if (surfaces.Covers(at))
        {
            nearest = 0f;
            height = Height(at);
            return height <= ReachHeight;
        }

        for (float radius = StyleScale.GridSnap * 0.5f; radius <= Reach + 0.001f; radius += StyleScale.GridSnap * 0.5f)
        {
            for (int i = 0; i < Samples; i++)
            {
                float angle = i * Mathf.PI * 2f / Samples;
                var stand = at + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (!surfaces.Covers(stand)) continue;
                if (radius < nearest) { nearest = radius; height = Height(at); }
                return height <= ReachHeight;
            }
        }

        // Nothing within reach: say how far the nearest cell actually is, so "widen by how much?" has an answer.
        nearest = Farther(surfaces, at);
        height = Height(at);
        return false;
    }

    /// <summary>How far out the nearest walkable cell is, searched well past <see cref="Reach"/> for the report.</summary>
    static float Farther(TeleportSurfaces surfaces, Vector3 at)
    {
        for (float radius = Reach; radius <= 4f; radius += StyleScale.GridSnap * 0.5f)
            for (int i = 0; i < Samples; i++)
            {
                float angle = i * Mathf.PI * 2f / Samples;
                var stand = at + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (surfaces.Covers(stand)) return radius;
            }
        return float.MaxValue;
    }

    /// <summary>How far the prop sits above the floor under it.</summary>
    static float Height(Vector3 at)
    {
        int layers = TeleportClearance.BlockingLayers();
        return Physics.Raycast(at + Vector3.up * 0.05f, Vector3.down, out var hit, 6f, layers,
            QueryTriggerInteraction.Ignore)
            ? at.y - hit.point.y
            : at.y;
    }

    static string Path(Transform t)
    {
        var name = t.name;
        for (var p = t.parent; p != null; p = p.parent) name = p.name + "/" + name;
        return name;
    }

    static void Finish(bool completed)
    {
        var report = new StringBuilder();
        report.AppendLine(completed && unreachable == 0
            ? $"PASS: every one of {reachable} interactables in {audited.Count} areas is reachable by teleport"
            : completed
                ? $"FAIL: {unreachable} of {reachable + unreachable} interactables in {audited.Count} areas cannot be " +
                  "reached in teleport mode"
                : $"INCOMPLETE: the tour did not finish; {audited.Count} areas audited");
        report.AppendLine($"Reach {Reach:0.00} m from a walkable cell, standing reach {ReachHeight:0.0} m. " +
                          "Smooth movement is less strict, so these are teleport-only failures.");
        report.AppendLine();
        foreach (var line in lines) report.AppendLine(line);

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, report.ToString());
        Debug.Log("[Reachability]\n" + report);

        passed = completed && unreachable == 0;
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
