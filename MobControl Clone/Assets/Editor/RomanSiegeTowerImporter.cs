using UnityEditor;

public sealed class RomanSiegeTowerImporter : AssetPostprocessor
{
    private const string TowerFolder =
        "Assets/Resources/Structures/RomanSiegeTower/";

    private bool IsTowerAsset => assetPath.StartsWith(TowerFolder);

    private void OnPreprocessTexture()
    {
        if (!IsTowerAsset)
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
        if (!IsTowerAsset)
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
