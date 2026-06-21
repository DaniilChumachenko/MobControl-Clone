using UnityEngine;

/// <summary>
/// Replaces the old cannon mesh with the Roman siege tower while preserving
/// the cannon's movement, collision, charge UI and legionary spawn point.
/// </summary>
[DisallowMultipleComponent]
public sealed class RomanSiegeTowerVisual : MonoBehaviour
{
    private const string TowerModelResource =
        "Structures/RomanSiegeTower/RomanSiegeTower";
    private const string TowerShaderResource =
        "Structures/RomanSiegeTower/RomanSiegeTowerPBR";
    private const string TowerAlbedoResource =
        "Structures/RomanSiegeTower/RomanSiegeTower_Albedo";
    private const string TowerMetallicResource =
        "Structures/RomanSiegeTower/RomanSiegeTower_Metallic";
    private const string TowerRoughnessResource =
        "Structures/RomanSiegeTower/RomanSiegeTower_Roughness";
    private const string TowerNormalResource =
        "Structures/RomanSiegeTower/RomanSiegeTower_Normal";
    private const string TowerEmissionResource =
        "Structures/RomanSiegeTower/RomanSiegeTower_Emission";

    private static readonly Vector3 TowerLocalPosition = new Vector3(4.9f, 5.41f, 11.3f);
    private static readonly Vector3 TowerLocalEuler = new Vector3(-96.86f, -179.636f, 269.983f);
    private static readonly Vector3 TowerLocalScale = Vector3.one * 806.7463f;

    private static Material towerMaterial;

    public static void Attach(GameObject cannon)
    {
        if (cannon == null || cannon.GetComponent<RomanSiegeTowerVisual>() != null)
        {
            return;
        }

        cannon.AddComponent<RomanSiegeTowerVisual>();
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        GameObject towerPrefab = Resources.Load<GameObject>(TowerModelResource);
        if (towerPrefab == null)
        {
            Debug.LogWarning("Roman siege tower model was not found in Resources.", this);
            return;
        }

        HideOldCannonMesh();
        EnsureMaterial();

        GameObject visualObject = new GameObject("Roman Siege Tower Visual");
        Transform visualRoot = visualObject.transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = InverseScale(transform.lossyScale);

        GameObject tower = Instantiate(towerPrefab, visualRoot, false);
        tower.name = "Roman Siege Tower Model";
        tower.transform.localPosition = TowerLocalPosition;
        tower.transform.localRotation = Quaternion.Euler(TowerLocalEuler);
        tower.transform.localScale = TowerLocalScale;

        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(visualObject);
            Debug.LogWarning("Roman siege tower does not contain a renderer.", this);
            return;
        }

        ApplyMaterial(renderers);
        MoveSpawnPointToTowerCenter(renderers);
    }

    private void HideOldCannonMesh()
    {
        Transform topModel = transform.Find("TopModel");
        if (topModel == null)
        {
            return;
        }

        Renderer[] oldRenderers = topModel.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < oldRenderers.Length; i++)
        {
            oldRenderers[i].enabled = false;
        }
    }

    private static void EnsureMaterial()
    {
        if (towerMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(TowerShaderResource);
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        towerMaterial = new Material(shader)
        {
            name = "Roman Siege Tower PBR"
        };
        towerMaterial.SetTexture("_MainTex", Resources.Load<Texture2D>(TowerAlbedoResource));
        towerMaterial.SetTexture("_MetallicTex", Resources.Load<Texture2D>(TowerMetallicResource));
        towerMaterial.SetTexture("_RoughnessTex", Resources.Load<Texture2D>(TowerRoughnessResource));
        towerMaterial.SetTexture("_BumpMap", Resources.Load<Texture2D>(TowerNormalResource));
        towerMaterial.SetTexture("_EmissionMap", Resources.Load<Texture2D>(TowerEmissionResource));
    }

    private static void ApplyMaterial(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            if (materials.Length == 0)
            {
                renderers[i].sharedMaterial = towerMaterial;
            }
            else
            {
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = towerMaterial;
                }
                renderers[i].sharedMaterials = materials;
            }

            renderers[i].receiveShadows = true;
        }
    }

    private void MoveSpawnPointToTowerCenter(Renderer[] towerRenderers)
    {
        Transform firePoint = FindDescendant(transform, "FirePoint");
        if (firePoint == null || towerRenderers.Length == 0)
        {
            return;
        }

        Bounds towerBounds = towerRenderers[0].bounds;
        for (int i = 1; i < towerRenderers.Length; i++)
        {
            towerBounds.Encapsulate(towerRenderers[i].bounds);
        }

        Vector3 right = transform.right.normalized;
        Vector3 forward = transform.forward.normalized;
        Vector3 spawnPosition = firePoint.position;

        // Center the exit across the width of the tower.
        float lateralOffset = Vector3.Dot(towerBounds.center - spawnPosition, right);
        spawnPosition += right * lateralOffset;

        // Place the exit just in front of the tower while retaining the
        // original spawn height that already matches the legionary prefab.
        float towerFront = Vector3.Dot(towerBounds.center, forward)
            + Vector3.Dot(towerBounds.extents, Abs(forward));
        float currentDepth = Vector3.Dot(spawnPosition, forward);
        spawnPosition += forward * (towerFront + 0.55f - currentDepth);
        firePoint.position = spawnPosition;
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }
        return null;
    }

    private static Vector3 InverseScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Abs(scale.x) > 0.0001f ? 1f / scale.x : 1f,
            Mathf.Abs(scale.y) > 0.0001f ? 1f / scale.y : 1f,
            Mathf.Abs(scale.z) > 0.0001f ? 1f / scale.z : 1f);
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
