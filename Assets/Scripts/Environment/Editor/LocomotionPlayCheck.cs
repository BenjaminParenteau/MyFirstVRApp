using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HighStakes.Environment;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Plays MVP_Main for real and walks the whole building, checking that every area ends up with teleport surfaces under
/// the player and that a queued teleport actually lands the rig on one. Teleporting is the one movement mode that can
/// silently have nowhere to go, which is exactly what this catches. Writes Logs/LocomotionQA/play-check.txt — not
/// Temp/, which Unity deletes on a clean shutdown, so a batch run's own result vanished with it.
/// </summary>
[InitializeOnLoad]
public static class LocomotionPlayCheck
{
    const string Pending = "Locomotion.PlayCheck";
    const string MainPath = "Assets/Scenes/MVP/MVP_Main.unity";
    const string ResultPath = "Logs/LocomotionQA/play-check.txt";

    // The tour: every area SceneFlow can load, and the door id to arrive at in each.
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

    static readonly List<string> checks = new List<string>();

    static int step, checkedStep;
    static double deadline, waitUntil;
    static bool teleportTested, passed;

    /// <summary>
    /// Every wait in <see cref="Tick"/> used to be a silent <c>return</c>, so anything that never became true showed up
    /// 300 seconds later as "Timed out" with no clue which area or which condition. Each wait now names itself and gets
    /// its own <see cref="StallSeconds"/> budget, and the trail is flushed to disk as it goes, so a stall says what it
    /// was waiting for and how far the tour got.
    /// </summary>
    const double StallSeconds = 45;

    static string waitingFor = "play mode to start";
    static double waitingSince;

    /// <summary>The area the last <see cref="SceneFlow.Go"/> asked for, so a stuck swap names where it was going.</summary>
    static string destination;

    static LocomotionPlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0;
                checkedStep = -1;
                teleportTested = false;
                passed = false;
                checks.Clear();
                waitingFor = "MVP_Main to wake up";
                waitingSince = EditorApplication.timeSinceStartup;
                destination = null;
                deadline = EditorApplication.timeSinceStartup + 300;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
            }
        };
    }

    [MenuItem("High Stakes/Run Locomotion Play Check")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Start();
    }

    /// <summary>Batch entry point (-executeMethod LocomotionPlayCheck.RunBatch).</summary>
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception(Stalled("the 300 s budget ran out"));
            if (EditorApplication.timeSinceStartup < waitUntil) return;

            var surfaces = TeleportSurfaces.Instance;
            if (surfaces == null)
            {
                Waiting("a TeleportSurfaces in MVP_Main (run High Stakes > Build Locomotion & Comfort)");
                return;
            }

            var flow = SceneFlow.Instance;
            if (flow == null) throw new Exception("No SceneFlow in MVP_Main");

            // Past the start screen first; the casino only loads once START NIGHT is pressed.
            if (flow.WaitingForStart)
            {
                var start = UnityEngine.Object.FindFirstObjectByType<HighStakes.UI.StartScreen>();
                if (start == null)
                    throw new Exception("SceneFlow is waiting for start but MVP_Main has no StartScreen to press " +
                                        "(run High Stakes > Build Start Screen, or turn Wait For Start off)");
                start.StartNight();
                Waiting("the start screen to hand over after START NIGHT");
                return;
            }
            // CurrentScene is still the room being left while a swap runs, so the message names where we asked to go.
            if (flow.Busy)
            {
                Waiting($"SceneFlow to finish swapping out of {flow.CurrentScene ?? "the lobby"} " +
                        $"to {destination ?? "the first area"}");
                return;
            }
            if (flow.CurrentScene == null) { Waiting("SceneFlow to load the first area"); return; }
            if (surfaces.Mapping) { Waiting($"the floor map of {flow.CurrentScene}"); return; }
            if (surfaces.BuiltFor != flow.CurrentScene)
            {
                Waiting($"the map to catch up: built for {surfaces.BuiltFor ?? "nothing"}, now in {flow.CurrentScene}");
                return;
            }

            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null) throw new Exception("No XR Origin");

            // Every area: surfaces exist, and the ground the player is standing on is one of them.
            if (checkedStep != step)
            {
                checkedStep = step;
                Require(surfaces.SurfaceCount > 0, $"{flow.CurrentScene} has no teleport surface");
                Require(surfaces.Covers(origin.transform.position),
                    $"{flow.CurrentScene}: the player is standing outside the mapped floor");
                Note($"{flow.CurrentScene}: {surfaces.SurfaceCount} surfaces over {surfaces.CellCount} walkable " +
                     "cells, the player's own ground among them");
            }

            // Once, in the casino: a teleport request to a surface really moves the rig there, and a point inside the
            // furniture the surfaces were built around is rejected as blocked.
            if (!teleportTested && flow.CurrentScene == "MVP_Casino")
            {
                teleportTested = true;
                TestTeleport(origin, surfaces);
                return;
            }

            if (step >= Tour.Length)
            {
                Pass();
                return;
            }

            var next = Tour[step++];
            destination = next.Scene;
            Require(flow.Go(next.Scene, next.Arrival), $"SceneFlow refused to travel to {next.Scene}");
            Waiting($"step {step}/{Tour.Length}: arrival at {next.Scene} door {next.Arrival}");
            waitUntil = EditorApplication.timeSinceStartup + 1.0;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            WriteFail(e.Message);
            Debug.Log("[Locomotion] PLAY CHECK FAILED");
            Finish();
        }
    }

    static void TestTeleport(XROrigin origin, TeleportSurfaces surfaces)
    {
        var provider = origin.GetComponentInChildren<TeleportationProvider>(true);
        Require(provider != null, "The rig has no TeleportationProvider");

        var holder = GameObject.Find("Teleport_Surfaces");
        Require(holder != null, "No Teleport_Surfaces holder");

        // Aim at the surface whose middle is furthest from the player, so the rig has to actually travel.
        Transform surface = null;
        float best = 0f;
        foreach (Transform child in holder.transform)
        {
            float distance = Vector3.Distance(child.position, origin.transform.position);
            if (surface != null && distance <= best) continue;
            surface = child;
            best = distance;
        }
        Require(surface != null && best > 0.5f, "No teleport surface far enough from the player to aim at");
        var target = surface.position;

        provider.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = target,
            destinationRotation = origin.transform.rotation,
            matchOrientation = MatchOrientation.WorldSpaceUp,
        });

        var box = surface.GetComponent<BoxCollider>();
        var size = box.size;
        Note($"MVP_Casino: queued a teleport onto a {size.x:0.0} x {size.z:0.0} m surface at " +
             $"({target.x:0.0}, {target.y:0.00}, {target.z:0.0})");
        Waiting("the teleportation provider to apply the queued request");

        // Checked on the next tick, once the provider has applied it.
        waitUntil = EditorApplication.timeSinceStartup + 0.5;
        EditorApplication.update -= Tick;
        EditorApplication.update += AfterTeleport;
        teleportTarget = target;
    }

    static Vector3 teleportTarget;

    static void AfterTeleport()
    {
        try
        {
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            Require(origin != null, "No XR Origin after teleporting");

            var landed = origin.transform.position;
            float off = Vector3.Distance(new Vector3(landed.x, 0f, landed.z), new Vector3(teleportTarget.x, 0f, teleportTarget.z));
            Require(off < 0.3f, $"Teleport landed {off:0.00} m from the surface it aimed at");
            Require(Mathf.Abs(landed.y - teleportTarget.y) < 0.3f,
                $"Teleport landed {landed.y - teleportTarget.y:0.00} m above/below the floor");
            Note($"MVP_Casino: the rig arrived {off:0.00} m from that point, standing on the floor");

            // The clearance rule the surfaces are built on: inside a wall is not a place to stand.
            var surfaces = TeleportSurfaces.Instance;
            int layers = TeleportClearance.BlockingLayers();
            var buffer = new Collider[16];
            Require(TeleportClearance.IsClear(teleportTarget, 0.25f, StyleScale.FigureHeight, StyleScale.MaxStepHeight,
                    layers, buffer, origin.transform),
                "A surface the map built is reported blocked");
            bool blockedSomewhere = false;
            foreach (var wall in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (wall.bounds.size.y < 1.5f || wall.transform.IsChildOf(origin.transform)) continue;
                if (!TeleportClearance.IsClear(wall.bounds.center - Vector3.up * wall.bounds.extents.y, 0.25f,
                        StyleScale.FigureHeight, StyleScale.MaxStepHeight, layers, buffer, origin.transform))
                {
                    blockedSomewhere = true;
                    break;
                }
            }
            Require(blockedSomewhere, "Nothing in the casino counts as blocked, so the clearance test is not working");
            Require(!surfaces.Covers(new Vector3(0f, 0f, 9999f)), "A point far outside the area is mapped as walkable");
            Note("Clearance: a mapped surface is clear, the inside of solid geometry is blocked, " +
                 "and a point outside the area is not mapped");

            EditorApplication.update -= AfterTeleport;
            EditorApplication.update += Tick;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            WriteFail(e.Message);
            Debug.Log("[Locomotion] PLAY CHECK FAILED");
            EditorApplication.update -= AfterTeleport;
            Finish();
        }
    }

    static void Pass()
    {
        var report = new StringBuilder();
        report.AppendLine($"PASS: {checks.Count} checks");
        foreach (var line in checks) report.AppendLine("  - " + line);
        Write(report.ToString());
        Debug.Log("[Locomotion] PLAY CHECK PASSED\n" + report);
        passed = true;
        Finish();
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
    }

    /// <summary>
    /// Names what the check is blocked on. The first call for a given reason starts its clock; once one condition has
    /// held for <see cref="StallSeconds"/> the run fails with that reason rather than sitting until the 300 s budget
    /// expires with nothing to show. Called from inside the Tick try blocks, so the throw lands in their catch.
    /// </summary>
    static void Waiting(string reason)
    {
        if (reason != waitingFor)
        {
            waitingFor = reason;
            waitingSince = EditorApplication.timeSinceStartup;
            WriteProgress("RUNNING");
            return;
        }

        if (EditorApplication.timeSinceStartup - waitingSince > StallSeconds)
            throw new Exception(Stalled($"nothing happened for {StallSeconds:0} s"));
    }

    /// <summary>The message a stall or a timeout fails with: what it wanted, how far it got, and the live state.</summary>
    static string Stalled(string why)
    {
        var flow = SceneFlow.Instance;
        var surfaces = TeleportSurfaces.Instance;
        return $"{why} while waiting for {waitingFor}. Reached tour step {step}/{Tour.Length}, {checks.Count} checks " +
               $"passed. Play time {Time.unscaledTime:0.0} s, frame {Time.frameCount}. SceneFlow: " +
               (flow == null
                   ? "missing"
                   : $"scene={flow.CurrentScene ?? "none"} busy={flow.Busy} stage=\"{flow.Stage}\" " +
                     $"waitingForStart={flow.WaitingForStart} on \"{flow.gameObject.name}\" in " +
                     $"{flow.gameObject.scene.name} active={flow.gameObject.activeInHierarchy} " +
                     $"enabled={flow.enabled}") +
               ". TeleportSurfaces: " +
               (surfaces == null
                   ? "missing"
                   : $"builtFor={surfaces.BuiltFor ?? "nothing"} mapping={surfaces.Mapping} " +
                     $"surfaces={surfaces.SurfaceCount} cells={surfaces.CellCount}");
    }

    /// <summary>Records a passed check and flushes the trail, so a later stall still shows what already worked.</summary>
    static void Note(string line)
    {
        checks.Add(line);
        WriteProgress("RUNNING");
    }

    static void WriteProgress(string status)
    {
        var report = new StringBuilder();
        report.AppendLine($"{status}: {checks.Count} checks, tour step {step}/{Tour.Length}, waiting for {waitingFor}");
        foreach (var line in checks) report.AppendLine("  - " + line);
        Write(report.ToString());
    }

    static void WriteFail(string message)
    {
        var report = new StringBuilder();
        report.AppendLine("FAIL: " + message);
        if (checks.Count > 0)
        {
            report.AppendLine($"Checks that did pass ({checks.Count}):");
            foreach (var line in checks) report.AppendLine("  - " + line);
        }
        Write(report.ToString());
    }

    static void Write(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, text);
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update -= AfterTeleport;
        EditorApplication.isPlaying = false;

        // Run headlessly (-executeMethod LocomotionPlayCheck.RunBatch) nothing would ever close the Editor, so the run
        // would hang. Exit with the result instead, which also makes the check usable as a gate.
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }
}
