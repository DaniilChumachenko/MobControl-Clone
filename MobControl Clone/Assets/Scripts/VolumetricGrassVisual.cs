using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Places optimized instances of the supplied Grass.fbx over the main ground.
/// The imported model is combined into a single sparse 20x20 metre patch by
/// GrassGroundImporter, so every patch is real geometry rather than a flat decal.
/// </summary>
public static class VolumetricGrassVisual
{
    private const string GrassModelResource = "Environment/GrassGround/Grass";
    private const string GrassShaderResource = "Environment/GrassGround/GrassBlades";
    private const string LeafTextureResource = "Environment/GrassGround/GrassGround_Leaf";
    private const float TileSpacing = 19.5f;
    private const float BladeHeightScale = 1.8f;
    private const float GroundOffset = 0.025f;

    private static Material grassMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateVolumetricGrass()
    {
        GameObject grassPrefab = Resources.Load<GameObject>(GrassModelResource);
        Renderer ground = FindMainGround();
        EnsureMaterial();

        if (grassPrefab == null || ground == null || grassMaterial == null)
        {
            Debug.LogWarning("Volumetric grass resources could not be loaded.");
            return;
        }

        MeshFilter sourceFilter = FindLargestMesh(grassPrefab);
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            Debug.LogWarning("Grass.fbx does not contain a combined grass mesh.");
            return;
        }

        Bounds groundBounds = ground.bounds;
        Bounds patchBounds = sourceFilter.sharedMesh.bounds;
        float patchWidth = Mathf.Max(patchBounds.size.x, patchBounds.size.z);
        float horizontalScale = patchWidth > 0.01f ? TileSpacing / patchWidth * 1.04f : 1f;

        GameObject root = new GameObject("Volumetric Grass (Grass.fbx)");
        float startX = groundBounds.min.x + TileSpacing * 0.5f;
        float startZ = groundBounds.min.z + TileSpacing * 0.5f;
        int tileIndex = 0;

        for (float z = startZ; z < groundBounds.max.z; z += TileSpacing)
        {
            for (float x = startX; x < groundBounds.max.x; x += TileSpacing)
            {
                if (!IsInsideGroundEllipse(x, z, groundBounds))
                {
                    continue;
                }

                GameObject tile = new GameObject("Grass Patch " + tileIndex);
                tile.transform.SetParent(root.transform, false);
                tile.transform.position = new Vector3(x, groundBounds.max.y + GroundOffset, z);
                tile.transform.rotation = Quaternion.Euler(0f, (tileIndex % 4) * 90f, 0f);
                tile.transform.localScale = new Vector3(horizontalScale, BladeHeightScale, horizontalScale);

                tile.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer renderer = tile.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = grassMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                tileIndex++;
            }
        }
    }

    private static Renderer FindMainGround()
    {
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        Renderer result = null;
        float largestArea = 0f;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] materials = renderer.sharedMaterials;
            bool isGround = false;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material != null && (material.name == "Plane" || material.name == "Grass Ground"))
                {
                    isGround = true;
                    break;
                }
            }

            if (!isGround)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            float area = bounds.size.x * bounds.size.z;
            if (area > largestArea)
            {
                largestArea = area;
                result = renderer;
            }
        }

        return result;
    }

    private static MeshFilter FindLargestMesh(GameObject model)
    {
        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter result = null;
        int largestVertexCount = 0;
        for (int index = 0; index < filters.Length; index++)
        {
            Mesh mesh = filters[index].sharedMesh;
            if (mesh != null && mesh.vertexCount > largestVertexCount)
            {
                largestVertexCount = mesh.vertexCount;
                result = filters[index];
            }
        }

        return result;
    }

    private static bool IsInsideGroundEllipse(float x, float z, Bounds bounds)
    {
        float safeX = Mathf.Max(1f, bounds.extents.x - TileSpacing * 0.35f);
        float safeZ = Mathf.Max(1f, bounds.extents.z - TileSpacing * 0.35f);
        float normalizedX = (x - bounds.center.x) / safeX;
        float normalizedZ = (z - bounds.center.z) / safeZ;
        return normalizedX * normalizedX + normalizedZ * normalizedZ <= 1f;
    }

    private static void EnsureMaterial()
    {
        if (grassMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(GrassShaderResource);
        Texture2D leaf = Resources.Load<Texture2D>(LeafTextureResource);
        if (shader == null || leaf == null)
        {
            return;
        }

        grassMaterial = new Material(shader)
        {
            name = "Grass Blades (Grass.fbx)",
            enableInstancing = true
        };
        grassMaterial.SetTexture("_MainTex", leaf);
    }
}
