using UnityEditor;

public sealed class GreekWallImporter : AssetPostprocessor
{
    private const string WallFolder = "Assets/Resources/Structures/GreekWall/";

    private bool IsWallAsset => assetPath.StartsWith(WallFolder);

    private void OnPreprocessTexture()
    {
        if (!IsWallAsset)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        if (assetPath.EndsWith("_Normal.png"))
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
        }
        else if (!assetPath.EndsWith("_Albedo.png"))
        {
            importer.sRGBTexture = false;
        }
    }

    private void OnPreprocessModel()
    {
        if (!IsWallAsset)
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }
}
