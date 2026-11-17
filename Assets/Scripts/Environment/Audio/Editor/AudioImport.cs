using UnityEditor;
using UnityEngine;

/// <summary>
/// The project's audio import settings, in one callable place. **All four slots call this** from their own audio
/// builder rather than copying an import block — it is Slot D's file and, like <c>tools/audio/synth.py</c>, it is
/// append-only: if you need a new kind, ask and it gets added.
/// <para>
/// Why it matters: the import settings are most of the audio memory budget (StyleGuide §7 — resident audio under
/// 8 MB across all four slots). Getting them wrong doesn't sound wrong, it just quietly eats the frame budget on
/// device, where none of us can see it in the simulator.
/// </para>
/// <example>
/// From your own builder:
/// <code>
/// AudioImport.Apply("Assets/Content/Tables/Audio/SFX_ChipClink_1.wav", AudioImport.Kind.OneShot);
/// AudioImport.Apply("Assets/Content/Environment/Audio/AMB_CasinoFloor.wav", AudioImport.Kind.Bed);
/// </code>
/// </example>
/// </summary>
public static class AudioImport
{
    /// <summary>What a clip is for, which decides how it is compressed and when it is loaded.</summary>
    public enum Kind
    {
        /// <summary>
        /// Short, fires on an interaction, must start instantly. Decompressed on load so there is no hitch when
        /// it plays, ADPCM so that costs little, forced to mono because a positioned source is spatialised from
        /// mono anyway (stereo would be double the memory for nothing).
        /// </summary>
        OneShot,

        /// <summary>
        /// A positioned looping emitter (a hum, a wheel, a monitor wall). Kept compressed in memory: it plays for
        /// a long time, so the decode cost is amortised, and there may be a lot of them.
        /// </summary>
        Loop,

        /// <summary>
        /// A music bed or room tone. Streamed from disk and stereo, because these are long, not positioned, and
        /// would otherwise dominate the whole audio budget on their own.
        /// </summary>
        Bed,
    }

    /// <summary>
    /// Apply the settings for <paramref name="kind"/> to the clip at <paramref name="assetPath"/> and reimport it.
    /// Returns false (with a warning) if there is no audio clip there. Safe to call repeatedly — it only
    /// reimports when something actually changed, so builders can run every time without churning the asset.
    /// </summary>
    public static bool Apply(string assetPath, Kind kind)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[AudioImport] No audio clip at '{assetPath}'.");
            return false;
        }

        bool mono = kind != Kind.Bed;
        var settings = importer.defaultSampleSettings;
        settings.loadType = kind == Kind.OneShot ? AudioClipLoadType.DecompressOnLoad
                          : kind == Kind.Loop ? AudioClipLoadType.CompressedInMemory
                                              : AudioClipLoadType.Streaming;
        settings.compressionFormat = kind == Kind.OneShot ? AudioCompressionFormat.ADPCM
                                                          : AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = kind == Kind.OneShot;

        bool changed = importer.forceToMono != mono
                       || importer.loadInBackground != (kind == Kind.Bed)
                       || !SameAs(importer.defaultSampleSettings, settings);

        importer.forceToMono = mono;
        importer.loadInBackground = kind == Kind.Bed;      // a streamed bed must not block the main thread
        importer.defaultSampleSettings = settings;

        // Android is the primary target (Progress_Vol1.md, Decisions 2026-09-30), so set it explicitly rather
        // than inheriting whatever the default platform happens to be.
        if (!importer.ContainsSampleSettingsOverride("Android")
            || !SameAs(importer.GetOverrideSampleSettings("Android"), settings))
        {
            importer.SetOverrideSampleSettings("Android", settings);
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            return true;
        }
        return false;
    }

    static bool SameAs(AudioImporterSampleSettings a, AudioImporterSampleSettings b)
        => a.loadType == b.loadType
           && a.compressionFormat == b.compressionFormat
           && Mathf.Approximately(a.quality, b.quality)
           && a.sampleRateSetting == b.sampleRateSetting
           && a.preloadAudioData == b.preloadAudioData;

    /// <summary>
    /// Guess the kind from a file name, so a builder can import a whole folder without listing every clip:
    /// <c>AMB_</c>/<c>MUS_</c> are beds, anything with <c>_Loop</c> is a loop, everything else is a one-shot.
    /// Pass the kind explicitly if your naming doesn't follow that.
    /// </summary>
    public static Kind KindFor(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return Kind.OneShot;
        if (fileName.StartsWith("AMB_") || fileName.StartsWith("MUS_")) return Kind.Bed;
        if (fileName.Contains("_Loop") || fileName.Contains("_loop")) return Kind.Loop;
        return Kind.OneShot;
    }

    /// <summary>
    /// Rough resident-memory estimate in bytes for one clip as imported, for budget reporting. Streamed beds cost
    /// almost nothing resident, which is the whole reason they are streamed.
    /// </summary>
    public static long EstimateBytes(AudioClip clip, Kind kind)
    {
        if (clip == null) return 0;
        if (kind == Kind.Bed) return 64 * 1024;                       // streaming buffers only
        long samples = (long)clip.samples * (kind == Kind.OneShot ? 1 : clip.channels);
        return kind == Kind.OneShot
            ? samples / 2                                              // ADPCM ~4 bits/sample, forced mono
            : (long)(samples * 0.08);                                  // Vorbis q0.7, compressed in memory
    }
}
