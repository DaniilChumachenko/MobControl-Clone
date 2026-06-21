using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces the original single-colour crowd mesh with a lightweight,
/// procedural low-poly historical unit while keeping the gameplay prefab,
/// collider and movement code unchanged.
/// </summary>
[DisallowMultipleComponent]
public sealed class HistoricalUnitVisual : MonoBehaviour
{
    public enum Faction
    {
        Roman,
        Greek
    }

    private Transform visualRoot;
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;
    private Transform importedRomanModelRoot;
    private Transform importedGreekModelRoot;
    private Transform importedScutum;
    private Transform importedScutumHand;
    private Transform importedGladius;
    private Transform importedGladiusHand;
    private Transform importedGreekShield;
    private Transform importedGreekShieldHand;
    private Transform importedGreekSpear;
    private Transform importedGreekSpearHand;
    private Transform importedGreekEquipmentRoot;
    private Transform romanChest;
    private Transform romanLeftUpperArm;
    private Transform romanLeftForearm;
    private Transform romanRightUpperArm;
    private Transform romanRightForearm;
    private Transform greekLeftUpperArm;
    private Transform greekLeftForearm;
    private Transform greekRightUpperArm;
    private Transform greekRightForearm;
    private Quaternion importedScutumGripRotationOffset;
    private Quaternion importedGladiusGripRotationOffset;
    private Quaternion importedScutumCombatLocalRotation;
    private Quaternion importedGladiusCombatLocalRotation;
    private Quaternion importedGladiusVerticalLocalRotation;
    private Quaternion importedGreekSpearForearmRotationOffset;
    private Vector3 importedScutumGripPositionOffset;
    private Vector3 importedGreekSpearForearmPositionOffset;
    private Collider unitCollider;
    private Vector3 combatDirection;
    private Vector3 romanShieldDirection;
    private Vector3 greekSpearCombatDirection;
    private Vector3 greekShieldDirection;
    private float combatPoseWeight;
    private float greekShieldGuardWeight;
    private float greekSpearAttackWeight;
    private float animationOffset;
    private float romanAttackCycleStartedAt;
    private bool greekSpearCombatLocked;
    private bool romanWasInCombat;
    private bool usesImportedRoman;
    private bool usesImportedGreek;

    private static Material skinMaterial;
    private static Material romanBlueMaterial;
    private static Material romanRedMaterial;
    private static Material steelMaterial;
    private static Material goldMaterial;
    private static Material leatherMaterial;
    private static Material darkLeatherMaterial;
    private static Material barbarianRedMaterial;
    private static Material furMaterial;
    private static Material lightFurMaterial;
    private static Material boneMaterial;
    private static Material woodMaterial;
    private static Material eyeMaterial;
    private static Material importedRomanMaterial;
    private static Material importedScutumMaterial;
    private static Material importedGladiusMaterial;
    private static Material importedGreekMaterial;
    private static RuntimeAnimatorController importedGreekRunController;

    private const string RomanModelResource = "Units/RomanLegionary/RomanLegionary";
    private const string RomanControllerResource = "Units/RomanLegionary/RomanLegionaryWalk";
    private const string RomanAlbedoResource = "Units/RomanLegionary/RomanLegionary_Albedo";
    private const string RomanMetallicResource = "Units/RomanLegionary/RomanLegionary_MetallicSmoothness";
    private const string GreekModelResource = "Units/GreekHopliteAnimated/GreekHopliteAnimated";
    private const string GreekEquipmentResource = "Units/GreekHoplite/GreekHoplite";
    private const string GreekAlbedoResource = "Units/GreekHopliteAnimated/GreekHopliteAnimated_Albedo";
    private const string GreekMetallicResource = "Units/GreekHopliteAnimated/GreekHopliteAnimated_MetallicSmoothness";
    private const string GreekNormalResource = "Units/GreekHopliteAnimated/GreekHopliteAnimated_Normal";
    private const string ScutumModelResource = "Units/RomanLegionary/Scutum/Scutum";
    private const string ScutumAlbedoResource = "Units/RomanLegionary/Scutum/Scutum_Albedo";
    private const string ScutumMetallicResource = "Units/RomanLegionary/Scutum/Scutum_MetallicSmoothness";
    private const string ScutumNormalResource = "Units/RomanLegionary/Scutum/Scutum_Normal";
    private const string GladiusModelResource = "Units/RomanLegionary/Gladius/Gladius";
    private const string GladiusAlbedoResource = "Units/RomanLegionary/Gladius/Gladius_Albedo";
    private const string GladiusMetallicResource = "Units/RomanLegionary/Gladius/Gladius_MetallicSmoothness";
    private const string GladiusNormalResource = "Units/RomanLegionary/Gladius/Gladius_Normal";
    private const float CombatStanceDistance = 3f;
    private const float CombatPoseBlendSpeed = 5f;
    private const float RomanShieldTrackingHalfAngle = 60f;
    private const float GreekShieldGuardDistance = 3f;
    private const float GreekShieldGuardBlendSpeed = 6f;
    private const float GreekShieldTrackingHalfAngle = 60f;
    private const float GreekSpearLoweringBlendSpeed = 3.5f;
    private const float RomanAttackCycleDuration = 2.6f;
    private static readonly Vector3 GreekSpearLocalPosition = new Vector3(0.435f, 1.323f, -1.184f);
    private static readonly Vector3 GreekSpearLocalEuler = new Vector3(-2.855f, -6.134f, 3.983f);
    private static readonly Vector3 GreekSpearLocalScale = new Vector3(0.1009013f, 0.1009013f, 0.1902007f);
    private static readonly Vector3 GreekShieldFixedLocalPosition = new Vector3(-0.094f, 1.572f, 0.141f);
    private static readonly Vector3 GreekShieldRunningLocalEuler = new Vector3(20.611f, 115.746f, -47.185f);
    private static readonly Vector3 GreekShieldLocalScale = new Vector3(0.7f, 0.6f, 1f);

    public static void Attach(GameObject unit, Faction faction)
    {
        HistoricalUnitVisual visual = unit.GetComponent<HistoricalUnitVisual>();
        if (visual == null)
        {
            visual = unit.AddComponent<HistoricalUnitVisual>();
        }

        if (visual.visualRoot == null)
        {
            visual.Build(faction);
        }
    }

    private void Build(Faction faction)
    {
        RemoveInheritedVisuals();
        DisableOriginalCharacterMesh();
        EnsureMaterials();
        unitCollider = GetComponent<Collider>();

        animationOffset = Mathf.Abs(GetInstanceID() * 0.173f) % (Mathf.PI * 2f);

        GameObject rootObject = new GameObject(faction == Faction.Roman
            ? "Roman Legionary Visual"
            : "Greek Hoplite Visual");
        visualRoot = rootObject.transform;
        visualRoot.SetParent(transform, false);

        bool isChampion = gameObject.name.IndexOf("Big", System.StringComparison.OrdinalIgnoreCase) >= 0;
        visualRoot.localScale = Vector3.one * (isChampion ? 1.9f : 1f);

        if (faction == Faction.Roman && TryBuildImportedRoman())
        {
            return;
        }
        if (faction == Faction.Greek && TryBuildImportedGreek())
        {
            return;
        }

        BuildSharedBody(faction);
        if (faction == Faction.Roman)
        {
            BuildRomanEquipment();
        }
        else
        {
            BuildBarbarianEquipment();
        }
    }

    private void RemoveInheritedVisuals()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Roman Legionary Visual" || child.name == "Greek Hoplite Visual")
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private bool TryBuildImportedGreek()
    {
        GameObject modelPrefab = Resources.Load<GameObject>(GreekModelResource);
        RuntimeAnimatorController runController = GetImportedGreekRunController();
        if (modelPrefab == null || runController == null)
        {
            return false;
        }

        EnsureImportedGreekMaterial();
        GameObject model = Instantiate(modelPrefab, visualRoot, false);
        model.name = "Greek Hoplite Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(model);
            return false;
        }

        ApplyMaterial(renderers, importedGreekMaterial);

        FitImportedModel(model.transform, renderers, 3.45f, 0.45f);

        Animator animator = model.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = runController;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        animator.Play("Walk", 0, Mathf.Repeat(animationOffset / (Mathf.PI * 2f), 1f));
        animator.Update(0f);

        importedGreekModelRoot = model.transform;
        greekLeftUpperArm = FindDescendant(model.transform, "LeftArm");
        greekLeftForearm = FindDescendant(model.transform, "LeftForeArm");
        greekRightUpperArm = FindDescendant(model.transform, "RightArm");
        greekRightForearm = FindDescendant(model.transform, "RightForeArm");
        TryAttachGreekEquipment(model.transform);

        usesImportedGreek = true;
        return true;
    }

    private static RuntimeAnimatorController GetImportedGreekRunController()
    {
        if (importedGreekRunController != null)
        {
            return importedGreekRunController;
        }

        RuntimeAnimatorController baseController = Resources.Load<RuntimeAnimatorController>(
            RomanControllerResource);
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(GreekModelResource);
        AnimationClip runClip = null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].length > 0f
                && clips[i].name.IndexOf("run", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                runClip = clips[i];
                break;
            }
        }

        if (baseController == null || runClip == null)
        {
            return null;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        if (overrides.Count == 0)
        {
            return null;
        }

        overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, runClip);
        overrideController.ApplyOverrides(overrides);
        importedGreekRunController = overrideController;
        return importedGreekRunController;
    }

    private void TryAttachGreekEquipment(Transform modelRoot)
    {
        GameObject equipmentPrefab = Resources.Load<GameObject>(GreekEquipmentResource);
        Transform leftHand = FindDescendant(modelRoot, "LeftHand");
        Transform rightHand = FindDescendant(modelRoot, "RightHand");
        if (equipmentPrefab == null || leftHand == null || rightHand == null)
        {
            return;
        }

        GameObject equipmentSource = Instantiate(equipmentPrefab, visualRoot, false);
        equipmentSource.name = "Greek Shield and Spear";
        Renderer[] sourceRenderers = equipmentSource.GetComponentsInChildren<Renderer>(true);
        if (sourceRenderers.Length == 0)
        {
            Destroy(equipmentSource);
            return;
        }

        FitImportedModel(equipmentSource.transform, sourceRenderers, 3.45f, 0.45f);
        Transform shield = FindDescendant(equipmentSource.transform, "Cylinder001");
        Transform spear = FindDescendant(equipmentSource.transform, "Box001");
        if (shield == null || spear == null)
        {
            Destroy(equipmentSource);
            return;
        }

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            bool isShield = sourceRenderers[i].transform == shield
                || sourceRenderers[i].transform.IsChildOf(shield);
            bool isSpear = sourceRenderers[i].transform == spear
                || sourceRenderers[i].transform.IsChildOf(spear);
            sourceRenderers[i].enabled = isShield || isSpear;
        }

        importedGreekShield = shield;
        importedGreekShieldHand = leftHand;
        importedGreekSpear = spear;
        importedGreekSpearHand = rightHand;
        importedGreekEquipmentRoot = equipmentSource.transform;

        // Exact poses adjusted in the Unity inspector by the user. The blue
        // back of the shield faces the hoplite; the spear points straight
        // along the attack direction.
        spear.localPosition = GreekSpearLocalPosition;
        spear.localRotation = Quaternion.Euler(GreekSpearLocalEuler);
        spear.localScale = GreekSpearLocalScale;
        shield.localPosition = GreekShieldFixedLocalPosition;
        shield.localRotation = Quaternion.Euler(GreekShieldRunningLocalEuler);
        shield.localScale = GreekShieldLocalScale;

        if (greekRightForearm != null)
        {
            importedGreekSpearForearmPositionOffset = Quaternion.Inverse(
                greekRightForearm.rotation) * (spear.position - greekRightForearm.position);
            importedGreekSpearForearmRotationOffset = Quaternion.Inverse(
                greekRightForearm.rotation) * spear.rotation;
        }
        greekSpearCombatDirection = modelRoot.forward;
    }

    private bool TryBuildImportedRoman()
    {
        GameObject modelPrefab = Resources.Load<GameObject>(RomanModelResource);
        RuntimeAnimatorController walkController = Resources.Load<RuntimeAnimatorController>(RomanControllerResource);
        if (modelPrefab == null || walkController == null)
        {
            return false;
        }

        EnsureImportedRomanMaterial();
        GameObject model = Instantiate(modelPrefab, visualRoot, false);
        model.name = "Roman Legionary Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(model);
            return false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            if (materials.Length == 0)
            {
                renderers[i].sharedMaterial = importedRomanMaterial;
            }
            else
            {
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = importedRomanMaterial;
                }
                renderers[i].sharedMaterials = materials;
            }
            renderers[i].receiveShadows = true;
        }

        FitImportedModel(model.transform, renderers, 3.9f, 0.45f);

        Animator animator = model.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = walkController;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        animator.Play("Walk", 0, Mathf.Repeat(animationOffset / (Mathf.PI * 2f), 1f));
        animator.Update(0f);

        romanChest = FindDescendant(model.transform, "Spine02");
        if (romanChest == null)
        {
            romanChest = FindDescendant(model.transform, "Spine01");
        }

        TryAttachImportedScutum(model.transform);
        TryAttachImportedGladius(model.transform);

        usesImportedRoman = true;
        return true;
    }

    private void TryAttachImportedScutum(Transform modelRoot)
    {
        GameObject shieldPrefab = Resources.Load<GameObject>(ScutumModelResource);
        Transform leftHandBone = FindDescendant(modelRoot, "LeftHand");
        if (shieldPrefab == null || leftHandBone == null)
        {
            return;
        }

        EnsureImportedScutumMaterial();
        GameObject shield = Instantiate(shieldPrefab, modelRoot, false);
        shield.name = "Roman Scutum";
        shield.transform.localScale *= 1.18f;
        Quaternion authoredLocalRotation = shield.transform.localRotation;

        // Keep the FBX-authored axis conversion, then move the rear handle onto
        // the palm. A late update follows the animated hand without inheriting
        // the rig's tiny, non-uniform service scale.

        Renderer[] renderers = shield.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(shield);
            return;
        }

        Quaternion runningWorldRotation = modelRoot.rotation
            * Quaternion.Euler(0f, -90f, 0f)
            * authoredLocalRotation;
        Vector3 runningWorldPosition = leftHandBone.position
            - modelRoot.right * (0.12f * modelRoot.lossyScale.x);
        shield.transform.SetPositionAndRotation(runningWorldPosition, runningWorldRotation);

        importedRomanModelRoot = modelRoot;
        importedScutum = shield.transform;
        importedScutumHand = leftHandBone;
        romanLeftUpperArm = FindDescendant(modelRoot, "LeftArm");
        romanLeftForearm = FindDescendant(modelRoot, "LeftForeArm");
        importedScutumGripRotationOffset = Quaternion.Inverse(leftHandBone.rotation) * runningWorldRotation;
        importedScutumGripPositionOffset = Quaternion.Inverse(leftHandBone.rotation)
            * (runningWorldPosition - leftHandBone.position);
        importedScutumCombatLocalRotation = authoredLocalRotation;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            if (materials.Length == 0)
            {
                renderers[i].sharedMaterial = importedScutumMaterial;
            }
            else
            {
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = importedScutumMaterial;
                }
                renderers[i].sharedMaterials = materials;
            }
            renderers[i].receiveShadows = true;
        }
    }

    private void TryAttachImportedGladius(Transform modelRoot)
    {
        GameObject swordPrefab = Resources.Load<GameObject>(GladiusModelResource);
        Transform rightHandBone = FindDescendant(modelRoot, "RightHand");
        if (swordPrefab == null || rightHandBone == null)
        {
            return;
        }

        EnsureImportedGladiusMaterial();
        GameObject sword = Instantiate(swordPrefab, modelRoot, false);
        sword.name = "Roman Gladius";
        sword.transform.localScale *= 1.15f;
        Quaternion authoredLocalRotation = sword.transform.localRotation;

        Renderer[] renderers = sword.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(sword);
            return;
        }

        // Keep the blade vertical and pointing upward in the resting pose.
        // The weapon stays rigidly fixed in the wrist throughout every strike.
        importedGladiusVerticalLocalRotation = Quaternion.Euler(90f, 0f, 0f)
            * authoredLocalRotation;
        Quaternion runningWorldRotation = modelRoot.rotation
            * importedGladiusVerticalLocalRotation;
        sword.transform.SetPositionAndRotation(rightHandBone.position, runningWorldRotation);

        importedGladius = sword.transform;
        importedGladiusHand = rightHandBone;
        romanRightUpperArm = FindDescendant(modelRoot, "RightArm");
        romanRightForearm = FindDescendant(modelRoot, "RightForeArm");
        importedGladiusGripRotationOffset = Quaternion.Inverse(rightHandBone.rotation) * runningWorldRotation;
        importedGladiusCombatLocalRotation = Quaternion.Euler(0f, 180f, 0f) * authoredLocalRotation;

        ApplyMaterial(renderers, importedGladiusMaterial);
    }

    private void LateUpdate()
    {
        if (importedRomanModelRoot != null)
        {
            UpdateCombatPose();

            if (importedScutum != null && importedScutumHand != null)
            {
                importedScutum.position = importedScutumHand.position
                    + importedScutumHand.rotation * importedScutumGripPositionOffset;
                importedScutum.rotation = importedScutumHand.rotation * importedScutumGripRotationOffset;
            }

            if (importedGladius != null && importedGladiusHand != null)
            {
                importedGladius.position = importedGladiusHand.position;
                importedGladius.rotation = importedGladiusHand.rotation * importedGladiusGripRotationOffset;
            }
        }

        if (importedGreekModelRoot != null)
        {
            UpdateGreekEquipmentPose();
        }
    }

    private void UpdateGreekEquipmentPose()
    {
        Vector3 runDirection = importedGreekModelRoot.forward;
        runDirection.y = 0f;
        if (runDirection.sqrMagnitude <= 0.0001f)
        {
            runDirection = transform.forward;
        }
        runDirection.Normalize();

        Vector3 nearestRomanDirection;
        bool romanIsClose = PlayerController.TryGetClosestWithinSurfaceDistance(
            unitCollider,
            GreekShieldGuardDistance,
            out nearestRomanDirection);

        if (romanIsClose && !greekSpearCombatLocked)
        {
            greekSpearCombatLocked = true;
            greekSpearCombatDirection = nearestRomanDirection;
        }
        if (greekSpearCombatDirection.sqrMagnitude <= 0.0001f)
        {
            greekSpearCombatDirection = runDirection;
        }
        greekSpearCombatDirection.y = 0f;
        greekSpearCombatDirection.Normalize();
        greekSpearAttackWeight = Mathf.MoveTowards(
            greekSpearAttackWeight,
            greekSpearCombatLocked ? 1f : 0f,
            Time.deltaTime * GreekSpearLoweringBlendSpeed);

        // The upper arm is fixed for the whole run. Only the forearm lowers
        // the spear from a near-vertical carry pose into the attack pose.
        Vector3 rightUpperArmDirection = (
            runDirection * 0.34f
            - importedGreekModelRoot.up * 0.58f
            + importedGreekModelRoot.right * 0.04f).normalized;
        Vector3 raisedForearmDirection = (
            runDirection * 0.12f
            + importedGreekModelRoot.up * 0.98f
            + importedGreekModelRoot.right * 0.02f).normalized;
        Vector3 attackForearmDirection = (
            greekSpearCombatDirection * 0.98f
            + importedGreekModelRoot.up * 0.04f
            + importedGreekModelRoot.right * 0.02f).normalized;
        Vector3 rightForearmDirection = Vector3.Slerp(
            raisedForearmDirection,
            attackForearmDirection,
            greekSpearAttackWeight).normalized;
        AimBoneTowards(greekRightUpperArm, greekRightForearm, rightUpperArmDirection, 1f);
        AimBoneTowards(greekRightForearm, importedGreekSpearHand, rightForearmDirection, 1f);

        // Lock the shield arm for the whole run. The animation may continue on
        // the body and legs, but it can no longer swing the wrist or shield.
        Vector3 leftUpperArmDirection = (
            runDirection * 0.32f
            - importedGreekModelRoot.up * 0.55f
            - importedGreekModelRoot.right * 0.14f).normalized;
        Vector3 leftForearmDirection = (
            runDirection * 0.68f
            + importedGreekModelRoot.up * 0.48f
            - importedGreekModelRoot.right * 0.1f).normalized;
        AimBoneTowards(greekLeftUpperArm, greekLeftForearm, leftUpperArmDirection, 1f);
        AimBoneTowards(greekLeftForearm, importedGreekShieldHand, leftForearmDirection, 1f);

        greekShieldGuardWeight = Mathf.MoveTowards(
            greekShieldGuardWeight,
            romanIsClose ? 1f : 0f,
            Time.deltaTime * GreekShieldGuardBlendSpeed);

        if (greekShieldDirection.sqrMagnitude <= 0.0001f)
        {
            greekShieldDirection = runDirection;
        }

        nearestRomanDirection.y = 0f;
        if (nearestRomanDirection.sqrMagnitude > 0.0001f)
        {
            nearestRomanDirection.Normalize();
        }
        float shieldTargetAngle = Vector3.SignedAngle(
            runDirection,
            nearestRomanDirection,
            importedGreekModelRoot.up);
        if (romanIsClose && Mathf.Abs(shieldTargetAngle) <= GreekShieldTrackingHalfAngle)
        {
            greekShieldDirection = nearestRomanDirection;
        }
        greekShieldDirection.y = 0f;
        greekShieldDirection.Normalize();

        if (importedGreekShield != null
            && importedGreekShieldHand != null
            && importedGreekEquipmentRoot != null)
        {
            // Keep the shield centre at one authored point beside the wrist.
            // Its position is independent of every frame of the run clip.
            Vector3 fixedWristPosition = importedGreekEquipmentRoot.TransformPoint(
                GreekShieldFixedLocalPosition);

            Quaternion runningRotation = importedGreekEquipmentRoot.rotation
                * Quaternion.Euler(GreekShieldRunningLocalEuler);
            Quaternion forwardRotation = Quaternion.AngleAxis(
                90f,
                importedGreekModelRoot.up) * runningRotation;
            Quaternion enemyYaw = Quaternion.FromToRotation(
                runDirection,
                greekShieldDirection);
            Quaternion enemyRotation = enemyYaw * forwardRotation;

            importedGreekShield.position = fixedWristPosition;
            importedGreekShield.rotation = Quaternion.Slerp(
                runningRotation,
                enemyRotation,
                greekShieldGuardWeight);
            importedGreekShield.localScale = GreekShieldLocalScale;
        }

        if (importedGreekSpear != null && greekRightForearm != null)
        {
            importedGreekSpear.position = greekRightForearm.position
                + greekRightForearm.rotation * importedGreekSpearForearmPositionOffset;
            importedGreekSpear.rotation = greekRightForearm.rotation
                * importedGreekSpearForearmRotationOffset;
            importedGreekSpear.localScale = GreekSpearLocalScale;
        }
    }

    private void UpdateCombatPose()
    {
        Vector3 nearestEnemyDirection;
        bool enemyIsClose = EnemyController.TryGetClosestWithinSurfaceDistance(
            unitCollider,
            CombatStanceDistance,
            out nearestEnemyDirection);

        if (enemyIsClose)
        {
            combatDirection = nearestEnemyDirection;
            if (!romanWasInCombat)
            {
                romanWasInCombat = true;
                romanAttackCycleStartedAt = Time.time;
            }
        }

        if (combatDirection.sqrMagnitude <= 0.0001f)
        {
            combatDirection = importedRomanModelRoot.forward;
        }
        combatDirection.y = 0f;
        combatDirection.Normalize();

        if (romanShieldDirection.sqrMagnitude <= 0.0001f)
        {
            romanShieldDirection = importedRomanModelRoot.forward;
        }

        float shieldTargetAngle = Vector3.SignedAngle(
            importedRomanModelRoot.forward,
            combatDirection,
            importedRomanModelRoot.up);
        if (enemyIsClose && Mathf.Abs(shieldTargetAngle) <= RomanShieldTrackingHalfAngle)
        {
            romanShieldDirection = combatDirection;
        }
        romanShieldDirection.y = 0f;
        romanShieldDirection.Normalize();

        float targetWeight = enemyIsClose ? 1f : 0f;
        combatPoseWeight = Mathf.MoveTowards(
            combatPoseWeight,
            targetWeight,
            Time.deltaTime * CombatPoseBlendSpeed);

        if (!enemyIsClose && combatPoseWeight <= 0f)
        {
            romanWasInCombat = false;
        }

        Vector3 initialUpperDirection = (
            importedRomanModelRoot.forward * 0.16f
            - importedRomanModelRoot.up * 0.78f
            + importedRomanModelRoot.right * 0.08f).normalized;
        Vector3 initialForearmDirection = (
            importedRomanModelRoot.forward * 0.56f
            - importedRomanModelRoot.up * 0.34f
            + importedRomanModelRoot.right * 0.04f).normalized;

        Vector3 rightUpperDirection = initialUpperDirection;
        Vector3 rightForearmDirection = initialForearmDirection;
        float swordForwardWeight = 0f;

        if (romanWasInCombat && combatPoseWeight > 0f)
        {
            float attackCycle = Mathf.Repeat(
                (Time.time - romanAttackCycleStartedAt) / RomanAttackCycleDuration,
                1f);

            Vector3 slashUpperDirection = (
                combatDirection * 0.34f
                + importedRomanModelRoot.up * 0.88f
                + importedRomanModelRoot.right * 0.08f).normalized;
            Vector3 slashForearmDirection = (
                combatDirection * 0.56f
                + importedRomanModelRoot.up * 0.68f
                + importedRomanModelRoot.right * 0.04f).normalized;
            Vector3 windupUpperDirection = (
                -combatDirection * 0.5f
                - importedRomanModelRoot.up * 0.32f
                + importedRomanModelRoot.right * 0.32f).normalized;
            Vector3 windupForearmDirection = (
                -combatDirection * 0.7f
                + importedRomanModelRoot.up * 0.18f
                + importedRomanModelRoot.right * 0.16f).normalized;
            Vector3 thrustUpperDirection = (
                combatDirection * 0.7f
                + importedRomanModelRoot.up * 0.04f
                + importedRomanModelRoot.right * 0.08f).normalized;
            Vector3 thrustForearmDirection = (
                combatDirection
                + importedRomanModelRoot.up * 0.02f
                + importedRomanModelRoot.right * 0.03f).normalized;

            if (attackCycle < 0.2f)
            {
                float phase = SmoothSegment(attackCycle, 0f, 0.2f);
                rightUpperDirection = Vector3.Slerp(initialUpperDirection, slashUpperDirection, phase);
                rightForearmDirection = Vector3.Slerp(initialForearmDirection, slashForearmDirection, phase);
            }
            else if (attackCycle < 0.36f)
            {
                float phase = SmoothSegment(attackCycle, 0.2f, 0.36f);
                rightUpperDirection = Vector3.Slerp(slashUpperDirection, initialUpperDirection, phase);
                rightForearmDirection = Vector3.Slerp(slashForearmDirection, initialForearmDirection, phase);
            }
            else if (attackCycle < 0.56f)
            {
                float phase = SmoothSegment(attackCycle, 0.36f, 0.56f);
                rightUpperDirection = Vector3.Slerp(initialUpperDirection, windupUpperDirection, phase);
                rightForearmDirection = Vector3.Slerp(initialForearmDirection, windupForearmDirection, phase);
            }
            else if (attackCycle < 0.76f)
            {
                float phase = SmoothSegment(attackCycle, 0.56f, 0.76f);
                rightUpperDirection = Vector3.Slerp(windupUpperDirection, thrustUpperDirection, phase);
                rightForearmDirection = Vector3.Slerp(windupForearmDirection, thrustForearmDirection, phase);
                swordForwardWeight = phase;
            }
            else
            {
                float phase = SmoothSegment(attackCycle, 0.76f, 1f);
                rightUpperDirection = Vector3.Slerp(thrustUpperDirection, initialUpperDirection, phase);
                rightForearmDirection = Vector3.Slerp(thrustForearmDirection, initialForearmDirection, phase);
                swordForwardWeight = 1f - phase;
            }
        }

        float torsoTurnAngle = Mathf.Clamp(
            Vector3.SignedAngle(
                importedRomanModelRoot.forward,
                combatDirection,
                importedRomanModelRoot.up),
            -18f,
            18f) * combatPoseWeight;
        Quaternion torsoFacingOffset = Quaternion.AngleAxis(
            torsoTurnAngle,
            importedRomanModelRoot.up);
        if (romanChest != null)
        {
            romanChest.rotation = torsoFacingOffset * romanChest.rotation;
        }

        AimBoneTowards(romanRightUpperArm, romanRightForearm, rightUpperDirection, 1f);
        AimBoneTowards(romanRightForearm, importedGladiusHand, rightForearmDirection, 1f);

        Quaternion targetFacing = Quaternion.FromToRotation(
            importedRomanModelRoot.forward,
            combatDirection) * importedRomanModelRoot.rotation;
        Quaternion shieldFacing = Quaternion.FromToRotation(
            importedRomanModelRoot.forward,
            romanShieldDirection) * importedRomanModelRoot.rotation;

        if (combatPoseWeight > 0f)
        {
            Vector3 torsoForward = torsoFacingOffset * importedRomanModelRoot.forward;
            Vector3 torsoRight = torsoFacingOffset * importedRomanModelRoot.right;
            Vector3 torsoUp = importedRomanModelRoot.up;

            // Keep the shield arm bent and tucked against the torso. The elbow
            // no longer reaches toward the target; only the shield turns.
            Vector3 shieldUpperArmDirection = (
                torsoForward * 0.12f
                - torsoUp * 0.78f
                - torsoRight * 0.24f).normalized;
            Vector3 shieldForearmDirection = (
                torsoForward * 0.38f
                + torsoUp * 0.18f
                + torsoRight * 0.42f).normalized;
            AimBoneTowards(
                romanLeftUpperArm,
                romanLeftForearm,
                shieldUpperArmDirection,
                combatPoseWeight);
            AimBoneTowards(
                romanLeftForearm,
                importedScutumHand,
                shieldForearmDirection,
                combatPoseWeight);

            if (importedScutumHand != null)
            {
                Quaternion shieldWorldRotation = shieldFacing * importedScutumCombatLocalRotation;
                Quaternion shieldHandRotation = shieldWorldRotation
                    * Quaternion.Inverse(importedScutumGripRotationOffset);
                importedScutumHand.rotation = Quaternion.Slerp(
                    importedScutumHand.rotation,
                    shieldHandRotation,
                    combatPoseWeight);
            }
        }

        if (importedGladiusHand != null)
        {
            Quaternion verticalSwordRotation = importedRomanModelRoot.rotation
                * importedGladiusVerticalLocalRotation;
            Quaternion thrustSwordRotation = targetFacing * importedGladiusCombatLocalRotation;
            Quaternion desiredSwordRotation = Quaternion.Slerp(
                verticalSwordRotation,
                thrustSwordRotation,
                swordForwardWeight);
            Quaternion swordHandRotation = desiredSwordRotation
                * Quaternion.Inverse(importedGladiusGripRotationOffset);
            importedGladiusHand.rotation = swordHandRotation;
        }
    }

    private static float SmoothSegment(float value, float start, float end)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    }

    private static void AimBoneTowards(
        Transform bone,
        Transform child,
        Vector3 desiredDirection,
        float weight)
    {
        if (bone == null || child == null)
        {
            return;
        }

        Vector3 currentDirection = child.position - bone.position;
        if (currentDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.FromToRotation(
            currentDirection.normalized,
            desiredDirection) * bone.rotation;
        bone.rotation = Quaternion.Slerp(bone.rotation, targetRotation, weight);
    }

    private static void ApplyMaterial(Renderer[] renderers, Material material)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            if (materials.Length == 0)
            {
                renderers[i].sharedMaterial = material;
            }
            else
            {
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = material;
                }
                renderers[i].sharedMaterials = materials;
            }
            renderers[i].receiveShadows = true;
        }
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == childName)
            {
                return transforms[i];
            }
        }
        return null;
    }

    private void FitImportedModel(
        Transform model,
        Renderer[] renderers,
        float targetLocalHeight,
        float targetLocalBottom)
    {
        Bounds bounds = GetCombinedBounds(renderers);
        float targetWorldHeight = targetLocalHeight * visualRoot.lossyScale.y;
        float scale = bounds.size.y > 0.001f ? targetWorldHeight / bounds.size.y : 1f;
        model.localScale *= scale;

        bounds = GetCombinedBounds(renderers);
        Vector3 localCenter = visualRoot.InverseTransformPoint(bounds.center);
        Vector3 localBottom = visualRoot.InverseTransformPoint(
            new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        model.localPosition += new Vector3(
            -localCenter.x,
            targetLocalBottom - localBottom.y,
            -localCenter.z);
    }

    private static Bounds GetCombinedBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private void BuildSharedBody(Faction faction)
    {
        Material cloth = faction == Faction.Roman ? romanBlueMaterial : barbarianRedMaterial;
        Material boots = faction == Faction.Roman ? darkLeatherMaterial : leatherMaterial;

        CreatePart("Torso", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 2.25f, 0f), new Vector3(0.9f, 1.35f, 0.62f),
            Quaternion.identity, cloth);

        CreatePart("Head", PrimitiveType.Sphere, visualRoot,
            new Vector3(0f, 3.27f, 0.02f), new Vector3(0.62f, 0.68f, 0.62f),
            Quaternion.identity, skinMaterial);

        // A small nose makes the facing direction readable from the game camera.
        CreatePart("Nose", PrimitiveType.Sphere, visualRoot,
            new Vector3(0f, 3.28f, 0.34f), new Vector3(0.14f, 0.14f, 0.18f),
            Quaternion.identity, skinMaterial);
        CreatePart("Left Eye", PrimitiveType.Sphere, visualRoot,
            new Vector3(-0.15f, 3.38f, 0.31f), new Vector3(0.075f, 0.085f, 0.065f),
            Quaternion.identity, eyeMaterial);
        CreatePart("Right Eye", PrimitiveType.Sphere, visualRoot,
            new Vector3(0.15f, 3.38f, 0.31f), new Vector3(0.075f, 0.085f, 0.065f),
            Quaternion.identity, eyeMaterial);

        leftArm = CreatePivot("Left Arm", visualRoot, new Vector3(-0.62f, 2.7f, 0f));
        rightArm = CreatePivot("Right Arm", visualRoot, new Vector3(0.62f, 2.7f, 0f));
        CreatePart("Left Arm", PrimitiveType.Capsule, leftArm,
            new Vector3(0f, -0.48f, 0f), new Vector3(0.24f, 0.55f, 0.24f),
            Quaternion.identity, skinMaterial);
        CreatePart("Right Arm", PrimitiveType.Capsule, rightArm,
            new Vector3(0f, -0.48f, 0f), new Vector3(0.24f, 0.55f, 0.24f),
            Quaternion.identity, skinMaterial);
        CreatePart("Left Hand", PrimitiveType.Sphere, leftArm,
            new Vector3(0f, -1.02f, 0f), new Vector3(0.26f, 0.3f, 0.25f),
            Quaternion.identity, skinMaterial);
        CreatePart("Right Hand", PrimitiveType.Sphere, rightArm,
            new Vector3(0f, -1.02f, 0f), new Vector3(0.26f, 0.3f, 0.25f),
            Quaternion.identity, skinMaterial);
        CreatePart("Left Bracer", PrimitiveType.Cylinder, leftArm,
            new Vector3(0f, -0.72f, 0f), new Vector3(0.27f, 0.18f, 0.27f),
            Quaternion.identity, boots);
        CreatePart("Right Bracer", PrimitiveType.Cylinder, rightArm,
            new Vector3(0f, -0.72f, 0f), new Vector3(0.27f, 0.18f, 0.27f),
            Quaternion.identity, boots);

        leftLeg = CreatePivot("Left Leg", visualRoot, new Vector3(-0.25f, 1.55f, 0f));
        rightLeg = CreatePivot("Right Leg", visualRoot, new Vector3(0.25f, 1.55f, 0f));
        CreatePart("Left Leg", PrimitiveType.Capsule, leftLeg,
            new Vector3(0f, -0.5f, 0f), new Vector3(0.28f, 0.58f, 0.28f),
            Quaternion.identity, skinMaterial);
        CreatePart("Right Leg", PrimitiveType.Capsule, rightLeg,
            new Vector3(0f, -0.5f, 0f), new Vector3(0.28f, 0.58f, 0.28f),
            Quaternion.identity, skinMaterial);

        CreatePart("Left Boot", PrimitiveType.Cube, leftLeg,
            new Vector3(0f, -1.03f, 0.12f), new Vector3(0.36f, 0.22f, 0.55f),
            Quaternion.identity, boots);
        CreatePart("Right Boot", PrimitiveType.Cube, rightLeg,
            new Vector3(0f, -1.03f, 0.12f), new Vector3(0.36f, 0.22f, 0.55f),
            Quaternion.identity, boots);
        CreatePart("Left Leg Wrap", PrimitiveType.Cylinder, leftLeg,
            new Vector3(0f, -0.68f, 0f), new Vector3(0.3f, 0.2f, 0.3f),
            Quaternion.identity, boots);
        CreatePart("Right Leg Wrap", PrimitiveType.Cylinder, rightLeg,
            new Vector3(0f, -0.68f, 0f), new Vector3(0.3f, 0.2f, 0.3f),
            Quaternion.identity, boots);
    }

    private void BuildRomanEquipment()
    {
        // Segmentata-inspired chest armour and belt.
        CreatePart("Roman Cuirass", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 2.45f, 0.04f), new Vector3(1f, 0.82f, 0.7f),
            Quaternion.identity, steelMaterial);
        for (int band = 0; band < 4; band++)
        {
            CreatePart("Armour Band", PrimitiveType.Cube, visualRoot,
                new Vector3(0f, 2.16f + band * 0.19f, 0.405f), new Vector3(1.04f, 0.075f, 0.075f),
                Quaternion.identity, goldMaterial);
        }

        CreatePart("Left Pauldron", PrimitiveType.Sphere, visualRoot,
            new Vector3(-0.55f, 2.79f, 0f), new Vector3(0.48f, 0.25f, 0.48f),
            Quaternion.identity, steelMaterial);
        CreatePart("Right Pauldron", PrimitiveType.Sphere, visualRoot,
            new Vector3(0.55f, 2.79f, 0f), new Vector3(0.48f, 0.25f, 0.48f),
            Quaternion.identity, steelMaterial);
        CreatePart("Roman Belt", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 1.78f, 0.02f), new Vector3(0.98f, 0.18f, 0.68f),
            Quaternion.identity, goldMaterial);
        CreatePart("Belt Buckle", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 1.78f, 0.39f), new Vector3(0.24f, 0.25f, 0.08f),
            Quaternion.identity, romanRedMaterial);

        // A short cape adds a strong Roman silhouette from the high camera.
        CreatePart("Roman Cape", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 2.27f, -0.38f), new Vector3(0.82f, 1.25f, 0.09f),
            Quaternion.Euler(-5f, 0f, 0f), romanRedMaterial);

        for (int i = -1; i <= 1; i++)
        {
            CreatePart("Leather Skirt", PrimitiveType.Cube, visualRoot,
                new Vector3(i * 0.27f, 1.5f, 0.03f), new Vector3(0.2f, 0.48f, 0.5f),
                Quaternion.identity, romanRedMaterial);
        }

        CreatePart("Roman Helmet", PrimitiveType.Sphere, visualRoot,
            new Vector3(0f, 3.51f, 0f), new Vector3(0.72f, 0.55f, 0.7f),
            Quaternion.identity, steelMaterial);
        CreatePart("Helmet Brow", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 3.36f, 0.3f), new Vector3(0.76f, 0.14f, 0.12f),
            Quaternion.identity, goldMaterial);
        CreatePart("Left Cheek Guard", PrimitiveType.Cube, visualRoot,
            new Vector3(-0.28f, 3.24f, 0.28f), new Vector3(0.13f, 0.38f, 0.1f),
            Quaternion.Euler(0f, 0f, -8f), steelMaterial);
        CreatePart("Right Cheek Guard", PrimitiveType.Cube, visualRoot,
            new Vector3(0.28f, 3.24f, 0.28f), new Vector3(0.13f, 0.38f, 0.1f),
            Quaternion.Euler(0f, 0f, 8f), steelMaterial);
        CreatePart("Crest Rail", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 3.72f, -0.02f), new Vector3(0.16f, 0.14f, 0.92f),
            Quaternion.identity, goldMaterial);
        for (int feather = -2; feather <= 2; feather++)
        {
            float featherHeight = 0.42f - Mathf.Abs(feather) * 0.04f;
            CreatePart("Crest Feather", PrimitiveType.Cube, visualRoot,
                new Vector3(0f, 3.93f - Mathf.Abs(feather) * 0.015f, feather * 0.18f),
                new Vector3(0.2f, featherHeight, 0.15f), Quaternion.identity, romanRedMaterial);
        }

        // Scutum: blue keeps the player crowd immediately identifiable.
        CreatePart("Scutum", PrimitiveType.Cube, leftArm,
            new Vector3(-0.04f, -0.48f, 0.28f), new Vector3(0.68f, 1.05f, 0.16f),
            Quaternion.identity, romanBlueMaterial);
        CreatePart("Scutum Boss", PrimitiveType.Sphere, leftArm,
            new Vector3(-0.04f, -0.48f, 0.4f), new Vector3(0.23f, 0.23f, 0.16f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Top Rim", PrimitiveType.Cube, leftArm,
            new Vector3(-0.04f, 0.025f, 0.39f), new Vector3(0.7f, 0.08f, 0.07f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Bottom Rim", PrimitiveType.Cube, leftArm,
            new Vector3(-0.04f, -0.985f, 0.39f), new Vector3(0.7f, 0.08f, 0.07f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Left Rim", PrimitiveType.Cube, leftArm,
            new Vector3(-0.36f, -0.48f, 0.39f), new Vector3(0.08f, 1.02f, 0.07f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Right Rim", PrimitiveType.Cube, leftArm,
            new Vector3(0.28f, -0.48f, 0.39f), new Vector3(0.08f, 1.02f, 0.07f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Emblem Vertical", PrimitiveType.Cube, leftArm,
            new Vector3(-0.04f, -0.48f, 0.405f), new Vector3(0.09f, 0.68f, 0.06f),
            Quaternion.identity, goldMaterial);
        CreatePart("Scutum Emblem Horizontal", PrimitiveType.Cube, leftArm,
            new Vector3(-0.04f, -0.48f, 0.41f), new Vector3(0.48f, 0.09f, 0.06f),
            Quaternion.identity, goldMaterial);

        CreatePart("Gladius Grip", PrimitiveType.Cylinder, rightArm,
            new Vector3(0f, -0.78f, 0.06f), new Vector3(0.11f, 0.3f, 0.11f),
            Quaternion.identity, leatherMaterial);
        CreatePart("Gladius Guard", PrimitiveType.Cube, rightArm,
            new Vector3(0f, -1.02f, 0.06f), new Vector3(0.38f, 0.09f, 0.14f),
            Quaternion.identity, goldMaterial);
        CreatePart("Gladius Blade", PrimitiveType.Cube, rightArm,
            new Vector3(0f, -1.2f, 0.06f), new Vector3(0.12f, 0.62f, 0.08f),
            Quaternion.identity, steelMaterial);
        CreatePart("Gladius Pommel", PrimitiveType.Sphere, rightArm,
            new Vector3(0f, -0.48f, 0.06f), new Vector3(0.18f, 0.18f, 0.18f),
            Quaternion.identity, goldMaterial);
    }

    private void BuildBarbarianEquipment()
    {
        CreatePart("Leather Vest", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 2.28f, 0.04f), new Vector3(0.96f, 1.08f, 0.68f),
            Quaternion.identity, leatherMaterial);
        CreatePart("Left Chest Strap", PrimitiveType.Cube, visualRoot,
            new Vector3(-0.18f, 2.35f, 0.4f), new Vector3(0.16f, 1.12f, 0.08f),
            Quaternion.Euler(0f, 0f, -24f), darkLeatherMaterial);
        CreatePart("Right Chest Strap", PrimitiveType.Cube, visualRoot,
            new Vector3(0.18f, 2.35f, 0.405f), new Vector3(0.16f, 1.12f, 0.08f),
            Quaternion.Euler(0f, 0f, 24f), darkLeatherMaterial);
        CreatePart("Fur Mantle", PrimitiveType.Capsule, visualRoot,
            new Vector3(0f, 2.77f, -0.02f), new Vector3(0.82f, 0.25f, 0.47f),
            Quaternion.Euler(0f, 0f, 90f), furMaterial);
        for (int tuft = -2; tuft <= 2; tuft++)
        {
            CreatePart("Fur Tuft", PrimitiveType.Sphere, visualRoot,
                new Vector3(tuft * 0.24f, 2.82f + (Mathf.Abs(tuft) % 2) * 0.05f, 0.15f),
                new Vector3(0.34f, 0.27f, 0.34f), Quaternion.identity,
                tuft % 2 == 0 ? lightFurMaterial : furMaterial);
        }
        CreatePart("Red Sash", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 1.75f, 0.05f), new Vector3(1f, 0.2f, 0.7f),
            Quaternion.Euler(0f, 0f, -8f), barbarianRedMaterial);
        CreatePart("Barbarian Buckle", PrimitiveType.Cube, visualRoot,
            new Vector3(0f, 1.75f, 0.42f), new Vector3(0.25f, 0.25f, 0.08f),
            Quaternion.identity, boneMaterial);
        for (int flap = -1; flap <= 1; flap++)
        {
            CreatePart("Fur Skirt Flap", PrimitiveType.Cube, visualRoot,
                new Vector3(flap * 0.28f, 1.47f, 0.04f), new Vector3(0.23f, 0.45f, 0.52f),
                Quaternion.Euler(0f, 0f, flap * 5f), furMaterial);
        }

        CreatePart("Barbarian Hair", PrimitiveType.Sphere, visualRoot,
            new Vector3(0f, 3.52f, -0.08f), new Vector3(0.74f, 0.54f, 0.72f),
            Quaternion.identity, furMaterial);
        CreatePart("Barbarian Beard", PrimitiveType.Capsule, visualRoot,
            new Vector3(0f, 3.06f, 0.27f), new Vector3(0.42f, 0.35f, 0.2f),
            Quaternion.identity, lightFurMaterial);
        CreatePart("Left Beard Braid", PrimitiveType.Capsule, visualRoot,
            new Vector3(-0.16f, 2.87f, 0.28f), new Vector3(0.1f, 0.28f, 0.1f),
            Quaternion.Euler(0f, 0f, -8f), furMaterial);
        CreatePart("Right Beard Braid", PrimitiveType.Capsule, visualRoot,
            new Vector3(0.16f, 2.87f, 0.28f), new Vector3(0.1f, 0.28f, 0.1f),
            Quaternion.Euler(0f, 0f, 8f), furMaterial);
        CreatePart("Left War Paint", PrimitiveType.Cube, visualRoot,
            new Vector3(-0.16f, 3.31f, 0.355f), new Vector3(0.18f, 0.055f, 0.035f),
            Quaternion.Euler(0f, 0f, -12f), barbarianRedMaterial);
        CreatePart("Right War Paint", PrimitiveType.Cube, visualRoot,
            new Vector3(0.16f, 3.31f, 0.355f), new Vector3(0.18f, 0.055f, 0.035f),
            Quaternion.Euler(0f, 0f, 12f), barbarianRedMaterial);
        CreatePart("Left Horn", PrimitiveType.Capsule, visualRoot,
            new Vector3(-0.38f, 3.78f, 0f), new Vector3(0.16f, 0.38f, 0.16f),
            Quaternion.Euler(0f, 0f, -35f), boneMaterial);
        CreatePart("Right Horn", PrimitiveType.Capsule, visualRoot,
            new Vector3(0.38f, 3.78f, 0f), new Vector3(0.16f, 0.38f, 0.16f),
            Quaternion.Euler(0f, 0f, 35f), boneMaterial);

        // Layered shield: metal rim, wooden face and red clan markings.
        CreatePart("Shield Metal Rim", PrimitiveType.Cylinder, leftArm,
            new Vector3(-0.04f, -0.48f, 0.28f), new Vector3(0.64f, 0.1f, 0.64f),
            Quaternion.Euler(90f, 0f, 0f), steelMaterial);
        CreatePart("Round Wooden Shield", PrimitiveType.Cylinder, leftArm,
            new Vector3(-0.04f, -0.48f, 0.36f), new Vector3(0.56f, 0.08f, 0.56f),
            Quaternion.Euler(90f, 0f, 0f), woodMaterial);
        CreatePart("Shield Mark Left", PrimitiveType.Cube, leftArm,
            new Vector3(-0.17f, -0.48f, 0.455f), new Vector3(0.11f, 0.82f, 0.06f),
            Quaternion.Euler(0f, 0f, -25f), barbarianRedMaterial);
        CreatePart("Shield Mark Right", PrimitiveType.Cube, leftArm,
            new Vector3(0.09f, -0.48f, 0.455f), new Vector3(0.11f, 0.82f, 0.06f),
            Quaternion.Euler(0f, 0f, 25f), barbarianRedMaterial);
        CreatePart("Shield Boss", PrimitiveType.Sphere, leftArm,
            new Vector3(-0.04f, -0.48f, 0.49f), new Vector3(0.24f, 0.24f, 0.15f),
            Quaternion.identity, steelMaterial);

        CreatePart("Axe Handle", PrimitiveType.Cylinder, rightArm,
            new Vector3(0f, -0.85f, 0.04f), new Vector3(0.09f, 0.62f, 0.09f),
            Quaternion.identity, darkLeatherMaterial);
        CreatePart("Axe Head", PrimitiveType.Cube, rightArm,
            new Vector3(0.18f, -1.28f, 0.04f), new Vector3(0.5f, 0.3f, 0.12f),
            Quaternion.Euler(0f, 0f, -12f), steelMaterial);
        CreatePart("Axe Back Spike", PrimitiveType.Cube, rightArm,
            new Vector3(-0.18f, -1.28f, 0.04f), new Vector3(0.32f, 0.16f, 0.11f),
            Quaternion.Euler(0f, 0f, 16f), steelMaterial);
        CreatePart("Axe Binding", PrimitiveType.Cylinder, rightArm,
            new Vector3(0f, -1.18f, 0.04f), new Vector3(0.14f, 0.18f, 0.14f),
            Quaternion.identity, boneMaterial);
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (usesImportedRoman || usesImportedGreek)
        {
            return;
        }

        float cycle = Time.time * 10f + animationOffset;

        float swing = Mathf.Sin(cycle) * 32f;

        leftArm.localRotation = Quaternion.Euler(swing, 0f, 0f);
        rightArm.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        leftLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        rightLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);

        Vector3 position = visualRoot.localPosition;
        position.y = Mathf.Abs(Mathf.Sin(cycle)) * 0.06f;
        visualRoot.localPosition = position;
    }

    private void DisableOriginalCharacterMesh()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
    {
        GameObject pivot = new GameObject(name);
        Transform pivotTransform = pivot.transform;
        pivotTransform.SetParent(parent, false);
        pivotTransform.localPosition = localPosition;
        return pivotTransform;
    }

    private static Transform CreatePart(
        string name,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            partCollider.enabled = false;
            Destroy(partCollider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.receiveShadows = true;
        return part.transform;
    }

    private static void EnsureMaterials()
    {
        if (skinMaterial != null)
        {
            return;
        }

        skinMaterial = CreateMaterial("Unit Skin", new Color(0.72f, 0.43f, 0.27f));
        romanBlueMaterial = CreateMaterial("Roman Blue", new Color(0.06f, 0.28f, 0.72f));
        romanRedMaterial = CreateMaterial("Roman Red", new Color(0.68f, 0.06f, 0.05f));
        steelMaterial = CreateMaterial("Steel", new Color(0.58f, 0.64f, 0.68f), 0.55f, 0.35f);
        goldMaterial = CreateMaterial("Bronze", new Color(0.72f, 0.46f, 0.12f), 0.4f, 0.25f);
        leatherMaterial = CreateMaterial("Leather", new Color(0.34f, 0.16f, 0.07f));
        darkLeatherMaterial = CreateMaterial("Dark Leather", new Color(0.12f, 0.07f, 0.04f));
        barbarianRedMaterial = CreateMaterial("Barbarian Red", new Color(0.58f, 0.035f, 0.025f));
        furMaterial = CreateMaterial("Dark Fur", new Color(0.13f, 0.09f, 0.07f));
        lightFurMaterial = CreateMaterial("Light Fur", new Color(0.31f, 0.2f, 0.13f));
        boneMaterial = CreateMaterial("Bone", new Color(0.82f, 0.75f, 0.58f));
        woodMaterial = CreateMaterial("Shield Wood", new Color(0.28f, 0.11f, 0.045f));
        eyeMaterial = CreateMaterial("Eyes", new Color(0.025f, 0.02f, 0.018f));
    }

    private static void EnsureImportedRomanMaterial()
    {
        if (importedRomanMaterial != null)
        {
            return;
        }

        Texture2D albedo = Resources.Load<Texture2D>(RomanAlbedoResource);
        Texture2D metallicSmoothness = Resources.Load<Texture2D>(RomanMetallicResource);
        importedRomanMaterial = CreateMaterial("Roman Legionary PBR", Color.white, 0.8f, 1f);
        importedRomanMaterial.mainTexture = albedo;
        importedRomanMaterial.SetTexture("_MetallicGlossMap", metallicSmoothness);
        importedRomanMaterial.SetFloat("_GlossMapScale", 0.85f);
        if (metallicSmoothness != null)
        {
            importedRomanMaterial.EnableKeyword("_METALLICGLOSSMAP");
        }
    }

    private static void EnsureImportedGreekMaterial()
    {
        if (importedGreekMaterial != null)
        {
            return;
        }

        Texture2D albedo = Resources.Load<Texture2D>(GreekAlbedoResource);
        Texture2D metallicSmoothness = Resources.Load<Texture2D>(GreekMetallicResource);
        Texture2D normal = Resources.Load<Texture2D>(GreekNormalResource);
        importedGreekMaterial = CreateMaterial("Greek Hoplite PBR", Color.white, 0.75f, 1f);
        importedGreekMaterial.mainTexture = albedo;
        importedGreekMaterial.SetTexture("_MetallicGlossMap", metallicSmoothness);
        importedGreekMaterial.SetTexture("_BumpMap", normal);
        importedGreekMaterial.SetFloat("_GlossMapScale", 0.85f);
        importedGreekMaterial.SetFloat("_BumpScale", 0.75f);

        if (metallicSmoothness != null)
        {
            importedGreekMaterial.EnableKeyword("_METALLICGLOSSMAP");
        }
        if (normal != null)
        {
            importedGreekMaterial.EnableKeyword("_NORMALMAP");
        }
    }

    private static void EnsureImportedScutumMaterial()
    {
        if (importedScutumMaterial != null)
        {
            return;
        }

        Texture2D albedo = Resources.Load<Texture2D>(ScutumAlbedoResource);
        Texture2D metallicSmoothness = Resources.Load<Texture2D>(ScutumMetallicResource);
        Texture2D normal = Resources.Load<Texture2D>(ScutumNormalResource);
        importedScutumMaterial = CreateMaterial("Roman Scutum PBR", Color.white, 0.7f, 1f);
        importedScutumMaterial.mainTexture = albedo;
        importedScutumMaterial.SetTexture("_MetallicGlossMap", metallicSmoothness);
        importedScutumMaterial.SetTexture("_BumpMap", normal);
        importedScutumMaterial.SetFloat("_GlossMapScale", 0.8f);
        importedScutumMaterial.SetFloat("_BumpScale", 0.75f);

        if (metallicSmoothness != null)
        {
            importedScutumMaterial.EnableKeyword("_METALLICGLOSSMAP");
        }
        if (normal != null)
        {
            importedScutumMaterial.EnableKeyword("_NORMALMAP");
        }
    }

    private static void EnsureImportedGladiusMaterial()
    {
        if (importedGladiusMaterial != null)
        {
            return;
        }

        Texture2D albedo = Resources.Load<Texture2D>(GladiusAlbedoResource);
        Texture2D metallicSmoothness = Resources.Load<Texture2D>(GladiusMetallicResource);
        Texture2D normal = Resources.Load<Texture2D>(GladiusNormalResource);
        importedGladiusMaterial = CreateMaterial("Roman Gladius PBR", Color.white, 0.75f, 1f);
        importedGladiusMaterial.mainTexture = albedo;
        importedGladiusMaterial.SetTexture("_MetallicGlossMap", metallicSmoothness);
        importedGladiusMaterial.SetTexture("_BumpMap", normal);
        importedGladiusMaterial.SetFloat("_GlossMapScale", 0.9f);
        importedGladiusMaterial.SetFloat("_BumpScale", 0.7f);

        if (metallicSmoothness != null)
        {
            importedGladiusMaterial.EnableKeyword("_METALLICGLOSSMAP");
        }
        if (normal != null)
        {
            importedGladiusMaterial.EnableKeyword("_NORMALMAP");
        }
    }

    private static Material CreateMaterial(
        string name,
        Color color,
        float smoothness = 0.1f,
        float metallic = 0f)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }
}
