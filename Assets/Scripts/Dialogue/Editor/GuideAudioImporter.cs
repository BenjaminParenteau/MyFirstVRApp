using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the Guide's voice lines (Content/UI/Guide/Audio): mono Vorbis, kept compressed in memory, so
/// a minute of speech costs well under a megabyte on Quest.
/// </summary>
public class GuideAudioImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Content/UI/Guide/Audio/"))
            return;

        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        importer.loadInBackground = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.5f;
        importer.defaultSampleSettings = settings;
    }
}
