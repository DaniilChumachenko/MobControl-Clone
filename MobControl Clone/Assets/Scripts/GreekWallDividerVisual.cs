using UnityEngine;

/// <summary>
/// Replaces the black LevelObsticles renderers with repeated Greek wall
/// sections. Original colliders remain active and continue defining gameplay.
/// </summary>
public static class GreekWallDividerVisual
{
    private const string WallModelResource = "Structures/GreekWall/GreekWall";
    private const string WallShaderResource = "Structures/GreekWall/GreekWallPBR";
    private const string WallAlbedoResource = "Structures/GreekWall/GreekWall_Albedo";
    private const string WallMetallicResource = "Structures/GreekWall/GreekWall_Metallic";
    private const string WallRoughnessResource = "Structures/GreekWall/GreekWall_Roughness";
    private const string WallNormalResource = "Structures/GreekWall/GreekWall_Normal";
    private const string WallEmissionResource = "Structures/GreekWall/GreekWall_Emission";

    private const float PreferredBlockLength = 6f;
    private const float PreferredRowWidth = 6.1f;
    private const float RowCenterSpacing = 3.8f;
    private const float WallHeight = 4.2f;
    private const float BlockOverlap = 0.55f;
    private static readonly Vector3 WallModelEuler = new Vector3(-90f, 180f, 270f);

    private static Material wallMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ReplaceBlackDividers()
    {
        GameObject obstacleContainer = GameObject.Find("LevelObsticles");
        GameObject wallPrefab = Resources.Load<GameObject>(WallModelResource);
        if (obstacleContainer == null || wallPrefab == null)
        {
            return;
        }

        EnsureMaterial();

        GameObject visualObject = new GameObject("Greek Wall Dividers Visual");
        Transform visualRoot = visualObject.transform;
        visualRoot.SetParent(obstacleContainer.transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        for (int i = 0; i < obstacleContainer.transform.childCount; i++)
        {
            Transform obstacle = obstacleContainer.transform.GetChild(i);
            if (obstacle == visualRoot)
            {
                continue;
            }

            Renderer oldRenderer = obstacle.GetComponent<Renderer>();
            Collider obstacleCollider = obstacle.GetComponent<Collider>();
            if (oldRenderer == null || obstacleCollider == null)
            {
                continue;
            }

            oldRenderer.enabled = false;
            BuildWallAlongObstacle(wallPrefab, visualRoot, obstacle, obstacleCollider);
        }
    }

    private static void BuildWallAlongObstacle(
        GameObject wallPrefab,
        Transform visualRoot,
        Transform obstacle,
        Collider obstacleCollider)
    {
        Vector3 wallAxis;
        float obstacleLength;
        float obstacleWidth;
        GetObstacleDimensions(
            obstacle,
            obstacleCollider,
            out wallAxis,
            out obstacleLength,
            out obstacleWidth);

        wallAxis.y = 0f;
        if (wallAxis.sqrMagnitude <= 0.0001f)
        {
            wallAxis = Vector3.forward;
        }
        wallAxis.Normalize();

        int blockCount = Mathf.Max(1, Mathf.CeilToInt(obstacleLength / PreferredBlockLength));
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(obstacleWidth / PreferredRowWidth));
        float blockLength = obstacleLength / blockCount + BlockOverlap;
        Vector3 wallCenter = obstacleCollider.bounds.center;
        wallCenter.y = WallHeight * 0.5f;
        Vector3 rowAxis = Vector3.Cross(Vector3.up, wallAxis).normalized;

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            float rowOffset = (rowIndex - (rowCount - 1) * 0.5f)
                * RowCenterSpacing;

            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                float distance = -obstacleLength * 0.5f
                    + obstacleLength * (blockIndex + 0.5f) / blockCount;
                CreateWallBlock(
                    wallPrefab,
                    visualRoot,
                    wallCenter + wallAxis * distance + rowAxis * rowOffset,
                    wallAxis,
                    blockLength,
                    rowIndex * blockCount + blockIndex);
            }
        }
    }

    private static void CreateWallBlock(
        GameObject wallPrefab,
        Transform visualRoot,
        Vector3 position,
        Vector3 wallAxis,
        float blockLength,
        int blockIndex)
    {
        GameObject segmentObject = new GameObject("Greek Wall Block " + blockIndex);
        Transform segment = segmentObject.transform;
        segment.SetParent(visualRoot, true);
        segment.position = position;
        segment.rotation = Quaternion.LookRotation(
            Vector3.Cross(wallAxis, Vector3.up).normalized,
            Vector3.up);
        segment.localScale = Vector3.one;

        GameObject stretchObject = new GameObject("Wall Fit");
        Transform stretch = stretchObject.transform;
        stretch.SetParent(segment, false);

        GameObject wall = Object.Instantiate(wallPrefab, stretch, false);
        wall.name = "Greek Wall Model";
        wall.transform.localPosition = Vector3.zero;
        wall.transform.localRotation = Quaternion.Euler(WallModelEuler);
        wall.transform.localScale = Vector3.one;

        Renderer[] renderers = wall.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Object.Destroy(segmentObject);
            return;
        }

        ApplyMaterial(renderers);
        Bounds localBounds = GetLocalMeshBounds(segment, wall);

        if (localBounds.size.x < localBounds.size.z)
        {
            wall.transform.localRotation = Quaternion.Euler(0f, 90f, 0f)
                * wall.transform.localRotation;
            localBounds = GetLocalMeshBounds(segment, wall);
        }

        if (localBounds.size.y <= 0.001f || localBounds.size.x <= 0.001f)
        {
            Object.Destroy(segmentObject);
            return;
        }

        float uniformScale = WallHeight / localBounds.size.y;
        wall.transform.localScale *= uniformScale;
        localBounds = GetLocalMeshBounds(segment, wall);
        wall.transform.localPosition -= localBounds.center;
        stretch.localScale = new Vector3(blockLength / localBounds.size.x, 1f, 1f);
    }

    private static void GetObstacleDimensions(
        Transform obstacle,
        Collider obstacleCollider,
        out Vector3 axis,
        out float length,
        out float width)
    {
        CapsuleCollider capsule = obstacleCollider as CapsuleCollider;
        if (capsule != null)
        {
            Vector3 localAxis = capsule.direction == 0
                ? Vector3.right
                : capsule.direction == 1 ? Vector3.up : Vector3.forward;
            axis = obstacle.TransformDirection(localAxis).normalized;
            Vector3 scale = Abs(obstacle.lossyScale);
            float axisScale = capsule.direction == 0
                ? scale.x
                : capsule.direction == 1 ? scale.y : scale.z;
            length = Mathf.Max(capsule.height, capsule.radius * 2f) * axisScale;
            Vector3 horizontalAxis = Vector3.Cross(Vector3.up, axis).normalized;
            width = ProjectBoundsSize(obstacleCollider.bounds, horizontalAxis);
            return;
        }

        BoxCollider box = obstacleCollider as BoxCollider;
        if (box != null)
        {
            axis = obstacle.right;
            length = box.size.x * Mathf.Abs(obstacle.lossyScale.x);
            width = box.size.z * Mathf.Abs(obstacle.lossyScale.z);
            return;
        }

        Bounds bounds = obstacleCollider.bounds;
        if (bounds.size.x >= bounds.size.z)
        {
            axis = Vector3.right;
            length = bounds.size.x;
            width = bounds.size.z;
        }
        else
        {
            axis = Vector3.forward;
            length = bounds.size.z;
            width = bounds.size.x;
        }
    }

    private static float ProjectBoundsSize(Bounds bounds, Vector3 axis)
    {
        Vector3 absoluteAxis = Abs(axis.normalized);
        return 2f * Vector3.Dot(bounds.extents, absoluteAxis);
    }

    private static Bounds GetLocalMeshBounds(Transform root, GameObject model)
    {
        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        bool hasBounds = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);

        for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
        {
            Mesh mesh = filters[filterIndex].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                point = root.InverseTransformPoint(filters[filterIndex].transform.TransformPoint(point));

                if (!hasBounds)
                {
                    result = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        return result;
    }

    private static void EnsureMaterial()
    {
        if (wallMaterial != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>(WallShaderResource);
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        wallMaterial = new Material(shader)
        {
            name = "Greek Wall PBR",
            enableInstancing = true
        };
        wallMaterial.SetTexture("_MainTex", Resources.Load<Texture2D>(WallAlbedoResource));
        wallMaterial.SetTexture("_MetallicTex", Resources.Load<Texture2D>(WallMetallicResource));
        wallMaterial.SetTexture("_RoughnessTex", Resources.Load<Texture2D>(WallRoughnessResource));
        wallMaterial.SetTexture("_BumpMap", Resources.Load<Texture2D>(WallNormalResource));
        wallMaterial.SetTexture("_EmissionMap", Resources.Load<Texture2D>(WallEmissionResource));
    }

    private static void ApplyMaterial(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            if (materials.Length == 0)
            {
                renderers[i].sharedMaterial = wallMaterial;
            }
            else
            {
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = wallMaterial;
                }
                renderers[i].sharedMaterials = materials;
            }

            renderers[i].receiveShadows = true;
        }
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
