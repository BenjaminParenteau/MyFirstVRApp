using UnityEditor;

/// <summary>
/// First-import settings for the casino textures and models in Assets/Content/Environment/Casino/ (StyleGuide §7):
/// mipmaps on, repeat wrap, "_Normal" files as normal maps, and on Android ASTC 6x6 at max 1024.
/// Only applied when a file has no .meta yet, so later hand changes in the Inspector (e.g. for the optimization pass)
/// are kept.
/// </summary>
public class CasinoTextureImporter : AssetPostprocessor
{
    const string TextureFolder = "Assets/Content/Environment/Casino/Textures/";
    const string ModelFolder = "Assets/Content/Environment/Casino/Models/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TextureFolder) || !assetImporter.importSettingsMissing) return;
        var importer = (TextureImporter)assetImporter;

        importer.textureType = assetPath.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.mipmapEnabled = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        importer.anisoLevel = 2; // floors and walls are seen at grazing angles
        importer.maxTextureSize = 2048;

        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 1024;
        android.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelFolder) || !assetImporter.importSettingsMissing) return;
        var importer = (ModelImporter)assetImporter;

        // Static props: no animation, no read/write copy in memory. Embedded materials are remapped to our
        // Material Variants by CasinoTextureBuilder.
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.isReadable = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }
}
