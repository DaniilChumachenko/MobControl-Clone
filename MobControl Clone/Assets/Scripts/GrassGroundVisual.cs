using UnityEngine;

/// <summary>
/// Replaces every renderer using the old Plane material with a shared,
/// world-space grass and soil material. Geometry and colliders stay intact.
/// </summary>
public static class GrassGroundVisual
{
    private const string GrassShaderResource = "Environment/GrassGround/GrassGround";
    private const string SoilTextureResource = "Environment/GrassGround/GrassGround_Soil";
    private const string LeafTextureResource = "Environment/GrassGround/GrassGround_Leaf";
    private const string OriginalFloorMaterialName = "Plane";

    private static Material grassMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ReplaceFloorMaterials()
    {
        EnsureMaterial();
        if (grassMaterial == null)
        {
            return;
        }

        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] materials = renderers[rendererIndex].sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material original = materials[materialIndex];
                if (original != null && original.name == OriginalFloorMaterialName)
                {
                    materials[materialIndex] = grassMaterial;
                    changed = true;
                }
            }

            if (changed)
            {
                renderers[rendererIndex].sharedMaterials = materials;
            }
        }
    }

    private static void EnsureMaterial()
    {
        if (grassMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(GrassShaderResource);
        Texture2D soil = Resources.Load<Texture2D>(SoilTextureResource);
        Texture2D leaf = Resources.Load<Texture2D>(LeafTextureResource);
        if (shader == null || soil == null || leaf == null)
        {
            Debug.LogWarning("Grass ground resources could not be loaded.");
            return;
        }

        grassMaterial = new Material(shader)
        {
            name = "Grass Ground"
        };
        grassMaterial.SetTexture("_SoilTex", soil);
        grassMaterial.SetTexture("_LeafTex", leaf);
    }
}
