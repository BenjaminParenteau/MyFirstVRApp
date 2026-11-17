using System.Collections.Generic;
using System.Linq;
using System.Text;
using HighStakes.Environment.Audio;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks every slot's cue set and reports anything that would break the four-way audio split or the audio budget.
/// <b>This is what enforces the split</b> — without it, "use your own namespace" is a thing everyone has to
/// remember, and the first collision shows up as a sound mysteriously not playing (docs/milestone6/README.md
/// rule 4). Run it before you push.
/// <para>
/// What it reports: a cue id used by two slots, an id in a namespace the set's slot doesn't own, an id in no
/// namespace at all, a cue with no clips, a 3D cue whose distances are backwards, a cue whose spatial flag
/// disagrees with what its namespace implies, a loop cue played as a one-shot by mistake, and the total resident
/// audio against the §7 budget.
/// </para>
/// Headless: <c>-executeMethod CueValidator.RunBatch</c> (exits non-zero on an error).
/// </summary>
public static class CueValidator
{
    const long BudgetBytes = 8L * 1024 * 1024;      // StyleGuide §7: resident audio under 8 MB, all slots together

    [MenuItem("High Stakes/Audio/Validate Cues")]
    public static void Run()
    {
        var report = Validate(out int errors, out int warnings);
        if (errors > 0) Debug.LogError(report);
        else if (warnings > 0) Debug.LogWarning(report);
        else Debug.Log(report);
    }

    /// <summary>Batch entry point. Exits 1 if anything is actually broken.</summary>
    public static void RunBatch()
    {
        var report = Validate(out int errors, out _);
        Debug.Log(report);
        EditorApplication.Exit(errors > 0 ? 1 : 0);
    }

    /// <summary>Validate every cue set in the project and return a human-readable report.</summary>
    public static string Validate(out int errors, out int warnings)
    {
        errors = 0;
        warnings = 0;
        var sb = new StringBuilder();
        sb.AppendLine("[Validate Cues] audio split and budget");

        var guids = AssetDatabase.FindAssets("t:SoundCueSet");
        if (guids.Length == 0)
        {
            warnings++;
            sb.AppendLine("  WARNING: no cue sets found at all. Each slot builds its own from its own audio menu.");
            sb.AppendLine("  Expected: CueSet_Tables (A), CueSet_Security (B), CueSet_UI (C), CueSet_Environment (D).");
            return sb.ToString();
        }

        var owners = new Dictionary<string, string>();          // cue id -> "set name (slot X)"
        long totalBytes = 0;
        int cueCount = 0;

        foreach (var guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var set = AssetDatabase.LoadAssetAtPath<SoundCueSet>(path);
            if (set == null) continue;

            string slot = string.IsNullOrEmpty(set.slot) ? "?" : set.slot.Trim().ToUpperInvariant();
            int inSet = set.cues?.Length ?? 0;
            sb.AppendLine($"\n  {set.name}  (slot {slot}, {inSet} cue(s))  {path}");

            if (!path.Contains("/Resources/Audio/"))
            {
                errors++;
                sb.AppendLine("    ERROR: must live in a Resources/Audio/ folder or the game will never find it " +
                              "(Soundscape discovers sets with Resources.LoadAll).");
            }

            if (set.cues == null) continue;
            foreach (var cue in set.cues)
            {
                if (cue == null) continue;
                cueCount++;

                if (string.IsNullOrWhiteSpace(cue.id))
                {
                    errors++;
                    sb.AppendLine("    ERROR: a cue has no id.");
                    continue;
                }

                // --- rule 4: one namespace per slot, no duplicates anywhere ----------------------------------
                if (owners.TryGetValue(cue.id, out string already))
                {
                    errors++;
                    sb.AppendLine($"    ERROR: '{cue.id}' is already defined in {already}. Two slots cannot share " +
                                  "a cue id — rename yours.");
                }
                else owners.Add(cue.id, $"{set.name} (slot {slot})");

                string owner = SoundscapeLogic.OwnerOf(cue.id);
                if (owner == null)
                {
                    errors++;
                    sb.AppendLine($"    ERROR: '{cue.id}' is in no known namespace. Use your slot's prefix: " +
                                  "table./cage. (A), sec./item. (B), ui./vo. (C), env./mus./amb./dark. (D).");
                }
                else if (slot != "?" && owner != slot)
                {
                    errors++;
                    sb.AppendLine($"    ERROR: '{cue.id}' belongs to slot {owner}, but this is slot {slot}'s set. " +
                                  $"Ask slot {owner} to author it, or rename it into your own namespace.");
                }

                // --- a cue that cannot make a sound ----------------------------------------------------------
                if (!cue.HasClips)
                {
                    errors++;
                    sb.AppendLine($"    ERROR: '{cue.id}' has no clips — it is silent.");
                    continue;
                }

                // --- spatial settings ------------------------------------------------------------------------
                if (cue.spatial && cue.maxDistance <= cue.minDistance)
                {
                    errors++;
                    sb.AppendLine($"    ERROR: '{cue.id}' has maxDistance ({cue.maxDistance}) at or below " +
                                  $"minDistance ({cue.minDistance}); it will be inaudible or never attenuate.");
                }

                bool shouldBeSpatial = SoundscapeLogic.ShouldBeSpatial(cue.id);
                if (cue.spatial != shouldBeSpatial)
                {
                    warnings++;
                    sb.AppendLine(shouldBeSpatial
                        ? $"    WARNING: '{cue.id}' is 2D, but its namespace implies a sound in the world. " +
                          "A sound you can't locate is one you can't navigate by (§11) — intentional?"
                        : $"    WARNING: '{cue.id}' is 3D, but ui./vo.guide./mus. are meant to be 2D " +
                          "(an earpiece must not pan when you turn your head) — intentional?");
                }

                if (cue.spatial && cue.minDistance < 0.3f)
                {
                    warnings++;
                    sb.AppendLine($"    WARNING: '{cue.id}' minDistance {cue.minDistance} m is very small; it will " +
                                  "get painfully loud when a player leans into it, which they do in VR.");
                }

                // --- repetition ------------------------------------------------------------------------------
                if (!cue.loop && cue.ClipCount == 1 && cue.pitchJitter <= 0f)
                {
                    warnings++;
                    sb.AppendLine($"    WARNING: '{cue.id}' has one clip and no pitch jitter. If it fires often it " +
                                  "will sound mechanical — add variants or jitter (§11).");
                }

                if (cue.loop && cue.ClipCount > 1)
                {
                    warnings++;
                    sb.AppendLine($"    WARNING: '{cue.id}' is a loop with {cue.ClipCount} clips; only the first " +
                                  "is ever used.");
                }

                // --- budget ----------------------------------------------------------------------------------
                for (int i = 0; i < cue.ClipCount; i++)
                {
                    var clip = cue.ClipAt(i);
                    var kind = cue.loop
                        ? (cue.spatial ? AudioImport.Kind.Loop : AudioImport.Kind.Bed)
                        : AudioImport.Kind.OneShot;
                    totalBytes += AudioImport.EstimateBytes(clip, kind);

                    if (clip != null && clip.channels > 1 && cue.spatial)
                    {
                        warnings++;
                        sb.AppendLine($"    WARNING: '{cue.id}' clip '{clip.name}' is stereo but positioned in 3D. " +
                                      "Unity spatialises from mono, so this is double the memory for nothing — " +
                                      "import it with AudioImport.Kind.OneShot/Loop (force-to-mono).");
                    }
                }
            }
        }

        // --- the beds every area needs ---------------------------------------------------------------------
        string[] areas = { "MVP_Casino", "MVP_VIP", "MVP_SecurityHall", "MVP_SecurityOffice", "MVP_CameraRoom", "MVP_Vault" };
        var missing = new List<string>();
        foreach (var area in areas)
        {
            var beds = SoundscapeLogic.BedsFor(area);
            if (beds.Music != null && !owners.ContainsKey(beds.Music)) missing.Add($"{beds.Music} ({area} music)");
            if (beds.Ambience != null && !owners.ContainsKey(beds.Ambience)) missing.Add($"{beds.Ambience} ({area} room tone)");
        }
        if (missing.Count > 0)
        {
            warnings++;
            sb.AppendLine($"\n  WARNING: {missing.Distinct().Count()} area bed(s) not authored yet (slot D): "
                          + string.Join(", ", missing.Distinct()));
        }

        double mb = totalBytes / 1024.0 / 1024.0;
        sb.AppendLine($"\n  {cueCount} cues across {guids.Length} set(s); ~{mb:0.00} MB resident " +
                      $"(budget {BudgetBytes / 1024 / 1024} MB, §7)");
        if (totalBytes > BudgetBytes)
        {
            errors++;
            sb.AppendLine("  ERROR: over the audio memory budget. Stream the beds, force one-shots to mono, or cut clips.");
        }

        sb.AppendLine(errors == 0 && warnings == 0
            ? "\n  PASS — no findings."
            : $"\n  {errors} error(s), {warnings} warning(s).");
        return sb.ToString();
    }
}
