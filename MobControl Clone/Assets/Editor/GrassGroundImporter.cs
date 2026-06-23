using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class GrassGroundImporter : AssetPostprocessor
{
    private const string GrassFolder = "Assets/Resources/Environment/GrassGround/";
    private const string GrassModelPath = GrassFolder + "Grass.fbx";
    private const int BladeSamplingStep = 4;
    private const float PatchSpread = 10f;

    [InitializeOnLoadMethod]
    private static void EnsureGrassModelIsImported()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GrassModelPath) == null)
            {
                AssetDatabase.ImportAsset(
                    GrassModelPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        };
    }

    public override uint GetVersion()
    {
        return 3;
    }

    private void OnPreprocessModel()
    {
        if (assetPath != GrassModelPath)
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = true;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
    }

    private void OnPostprocessModel(GameObject root)
    {
        if (assetPath != GrassModelPath)
        {
            return;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        List<MeshFilter> grassFilters = FindGrassFilters(filters, true);
        if (grassFilters.Count == 0)
        {
            grassFilters = FindGrassFilters(filters, false);
        }

        if (grassFilters.Count == 0)
        {
            Debug.LogWarning("Grass.fbx did not contain usable blade meshes.");
            return;
        }

        Material grassMaterial = FindGrassMaterial(grassFilters);
        Matrix4x4 rootToLocal = root.transform.worldToLocalMatrix;
        List<CombineInstance> combineInstances = new List<CombineInstance>(grassFilters.Count / BladeSamplingStep + 1);

        for (int index = 0; index < grassFilters.Count; index += BladeSamplingStep)
        {
            MeshFilter filter = grassFilters[index];
            if (filter.sharedMesh == null)
            {
                continue;
            }

            Matrix4x4 matrix = rootToLocal * filter.transform.localToWorldMatrix;
            Vector4 translation = matrix.GetColumn(3);
            translation.x *= PatchSpread;
            translation.z *= PatchSpread;
            matrix.SetColumn(3, translation);

            combineInstances.Add(new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = matrix
            });
        }

        if (combineInstances.Count == 0)
        {
            return;
        }

        Mesh combinedMesh = new Mesh
        {
            name = "Optimized Volumetric Grass Patch",
            indexFormat = IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true, false);
        combinedMesh.RecalculateBounds();

        // A newly created Mesh is not retained as an FBX sub-asset by
        // AssetPostprocessor. Copy its data into one of the FBX's original
        // meshes, which Unity does serialize with the imported model.
        MeshFilter destinationFilter = grassFilters[0];
        Mesh destinationMesh = destinationFilter.sharedMesh;
        EditorUtility.CopySerialized(combinedMesh, destinationMesh);
        destinationMesh.name = "Optimized Volumetric Grass Patch";
        Object.DestroyImmediate(combinedMesh);

        KeepOnlyCombinedObject(root, destinationFilter, grassMaterial);
    }

    private static List<MeshFilter> FindGrassFilters(MeshFilter[] filters, bool requireGrassMaterial)
    {
        List<MeshFilter> result = new List<MeshFilter>();
        for (int index = 0; index < filters.Length; index++)
        {
            MeshFilter filter = filters[index];
            Renderer renderer = filter.GetComponent<Renderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            string materialName = material != null ? material.name.ToLowerInvariant() : string.Empty;
            string objectName = filter.name.ToLowerInvariant();

            if (filter.sharedMesh == null || materialName.Contains("soil") || objectName.Contains("soil"))
            {
                continue;
            }

            if (!requireGrassMaterial || materialName.Contains("grass"))
            {
                result.Add(filter);
            }
        }

        return result;
    }

    private static Material FindGrassMaterial(List<MeshFilter> grassFilters)
    {
        for (int index = 0; index < grassFilters.Count; index++)
        {
            Renderer renderer = grassFilters[index].GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }
        }

        return null;
    }

    private static void KeepOnlyCombinedObject(GameObject root, MeshFilter destinationFilter, Material grassMaterial)
    {
        GameObject destination = destinationFilter.gameObject;
        if (destination != root)
        {
            destination.transform.SetParent(root.transform, true);
            destination.transform.localPosition = Vector3.zero;
            destination.transform.localRotation = Quaternion.identity;
            destination.transform.localScale = Vector3.one;
        }

        for (int index = root.transform.childCount - 1; index >= 0; index--)
        {
            GameObject child = root.transform.GetChild(index).gameObject;
            if (child != destination)
            {
                Object.DestroyImmediate(child);
            }
        }

        for (int index = destination.transform.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(destination.transform.GetChild(index).gameObject);
        }

        destination.name = "Combined Grass Blades";
        MeshRenderer renderer = destination.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = destination.AddComponent<MeshRenderer>();
        }
        renderer.sharedMaterial = grassMaterial;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(GrassFolder))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        importer.mipmapEnabled = true;
    }
}
