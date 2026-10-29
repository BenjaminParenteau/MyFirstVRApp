using UnityEditor;

/// <summary>
/// First-import settings for Slot C's textures in Assets/Content/UI/Textures/ (StyleGuide §7), matching the casino
/// importer: mipmaps on, repeat wrap, "_Normal" files as normal maps, and on Android ASTC 6x6. Max 512 on Android:
/// these are hand-held props seen up close, never whole walls. Only applied when a file has no .meta yet, so later
/// Inspector changes (e.g. for the optimization pass) are kept.
/// </summary>
public class UITextureImporter : AssetPostprocessor
{
    const string Folder = "Assets/Content/UI/Textures/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder) || !assetImporter.importSettingsMissing) return;
        var importer = (TextureImporter)assetImporter;

        importer.textureType = assetPath.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.mipmapEnabled = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        importer.maxTextureSize = 1024;

        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 512;
        android.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
    }
}
