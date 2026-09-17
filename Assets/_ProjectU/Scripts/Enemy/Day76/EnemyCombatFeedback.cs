using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 76일차: 적 체력바·피해 숫자·피격 번쩍임·처치 효과·공격 예고 표시·효과음을 한곳에서 관리
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyCombatFeedback : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("References")]
    [Tooltip("비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyCombatController combatController;
    [SerializeField] private EnemyRangedAttackController rangedAttackController;
    [SerializeField] private ContentVisualRoot visualRoot;

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [Tooltip("UIAnchor가 없을 때 사용할 적 Root 기준 높이입니다.")]
    [SerializeField] private float fallbackBarHeight = 2.2f;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.35f, 0f);
    [Tooltip("피격 또는 전투 종료 후 체력바를 유지할 시간입니다.")]
    [SerializeField, Min(0f)] private float healthBarVisibleDuration = 4f;
    [Tooltip("추적·공격 상태일 때도 체력바를 표시합니다.")]
    [SerializeField] private bool showHealthBarInCombat = true;
    [SerializeField, Min(1f)] private float healthBarMaxDistance = 30f;

    [Header("Damage Number")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageNumberColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField] private Color killingBlowColor = new Color(1f, 0.35f, 0.25f, 1f);
    [SerializeField, Min(0.1f)] private float damageNumberSize = 4f;

    [Header("Hit Flash")]
    [SerializeField] private bool useHitFlash = true;
    [SerializeField] private Color hitFlashColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;
    [SerializeField, Min(0f)] private float hitPunchScale = 0.12f;
    [SerializeField, Min(0.01f)] private float hitPunchDuration = 0.15f;
    [Tooltip("피격 지점에 생성할 선택 이펙트 Prefab입니다. 비워두면 기본 파편을 사용합니다.")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField, Range(0, 12)] private int hitSparkCount = 4;
    [SerializeField] private Color hitSparkColor = new Color(1f, 0.85f, 0.4f, 1f);

    [Header("Death")]
    [SerializeField] private bool useDeathEffect = true;
    [Tooltip("사망 위치에 생성할 선택 이펙트 Prefab입니다. 비워두면 기본 파편을 사용합니다.")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField, Range(0, 30)] private int deathDebrisCount = 12;
    [SerializeField] private Color deathDebrisColor = new Color(0.55f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(0f)] private float deathShrinkDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float deathShrinkDuration = 0.6f;
    [SerializeField, Min(0f)] private float deathSinkDistance = 0.4f;

    [Header("Attack Warning")]
    [SerializeField] private bool showAttackWarningMarker = true;
    [SerializeField] private Color warningStartColor = new Color(1f, 0.8f, 0.1f, 1f);
    [SerializeField] private Color warningEndColor = new Color(1f, 0.15f, 0.1f, 1f);
    [Tooltip("근접 적의 발밑 공격 범위 원에 사용할 Material입니다. 비워두면 원을 만들지 않습니다.")]
    [SerializeField] private Material groundTelegraphMaterial;

    [Header("Audio")]
    [SerializeField] private bool playSounds = true;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;
    [Tooltip("비워두면 코드로 만든 기본 효과음을 사용합니다.")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip killClip;
    [SerializeField] private AudioClip warningClip;

    [Header("Runtime")]
    [SerializeField] private bool isHealthBarVisible;
    [SerializeField] private EnemyAttackPhase observedPhase = EnemyAttackPhase.Ready;

    private readonly List<Renderer> flashRenderers = new List<Renderer>();
    private readonly List<Color> flashBaseColors = new List<Color>();
    private readonly List<bool> flashHadBlockColor = new List<bool>();
    // 여러 Material 슬롯을 가진 저폴리 모델은 슬롯별 원래 색을 따로 기억
    private readonly List<Color[]> flashSlotColors = new List<Color[]>();
    private MaterialPropertyBlock propertyBlock;
    private MaterialPropertyBlock emptyBlock;

    private EnemyWorldHealthBar healthBar;
    private TextMeshPro warningMarker;
    private LineRenderer groundTelegraphLine;
    private Material groundTelegraphInstance;
    private Transform visualTransform;
    private Vector3 visualBaseScale = Vector3.one;
    private Vector3 visualBaseLocalPosition;
    private Camera cachedCamera;

    private float healthBarHideTime;
    private float flashEndTime;
    private bool isFlashing;
    private float punchStartTime = -100f;
    private float deathTime = -100f;
    private bool deathVisualRunning;

    public EnemyWorldHealthBar HealthBar => healthBar;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        propertyBlock = new MaterialPropertyBlock();
        emptyBlock = new MaterialPropertyBlock();

        if (enemyHealth == null)
        {
            Debug.LogError("EnemyCombatFeedback에 EnemyHealth가 필요합니다.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        visualTransform = visualRoot != null && visualRoot.VisualRoot != null
            ? visualRoot.VisualRoot
            : null;

        if (visualTransform != null)
        {
            visualBaseScale = visualTransform.localScale;
            visualBaseLocalPosition = visualTransform.localPosition;
        }

        if (showHealthBar)
        {
            healthBar = EnemyWorldHealthBar.Create(transform, GetDisplayName());
            healthBar.SetNormalized(enemyHealth.NormalizedHealth, true);
            healthBar.SetVisible(false);
        }

        if (showAttackWarningMarker)
        {
            warningMarker = CreateWarningMarker();
        }

        if (groundTelegraphMaterial != null
            && rangedAttackController == null
            && combatController != null)
        {
            CreateGroundTelegraph();
        }
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Damaged += HandleDamaged;
        enemyHealth.Died += HandleDied;
        enemyHealth.Revived += HandleRevived;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Damaged -= HandleDamaged;
            enemyHealth.Died -= HandleDied;
            enemyHealth.Revived -= HandleRevived;
        }

        if (isFlashing)
        {
            EndFlash();
        }
    }

    private void OnDestroy()
    {
        if (groundTelegraphInstance != null)
        {
            Destroy(groundTelegraphInstance);
        }
    }

    private void LateUpdate()
    {
        cachedCamera = GetCamera();
        UpdateHealthBar();
        UpdateFlash();
        UpdateVisualMotion();
        UpdateAttackWarning();
    }

    private void HandleDamaged(CombatHitData hitData, float appliedDamage)
    {
        bool isKillingBlow = enemyHealth.CurrentHealth <= 0f;
        Vector3 hitPoint = ResolveHitPoint(hitData);

        if (healthBar != null)
        {
            healthBar.SetNormalized(enemyHealth.NormalizedHealth, false);
            healthBarHideTime = Time.time + healthBarVisibleDuration;
        }

        if (showDamageNumbers)
        {
            CombatDamagePopup.Spawn(
                hitPoint,
                appliedDamage,
                isKillingBlow ? killingBlowColor : damageNumberColor,
                isKillingBlow ? damageNumberSize * 1.4f : damageNumberSize);
        }

        if (useHitFlash)
        {
            BeginFlash();
            punchStartTime = Time.time;
        }

        if (hitEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(-hitData.HitDirection);
            Destroy(Instantiate(hitEffectPrefab, hitPoint, rotation), 3f);
        }
        else if (hitSparkCount > 0)
        {
            CombatFeedbackDebris.Burst(
                hitPoint,
                -hitData.HitDirection,
                hitSparkCount,
                hitSparkColor,
                0.06f,
                3.5f,
                0.3f);
        }

        if (!isKillingBlow)
        {
            PlaySound(hitClip != null ? hitClip : CombatFeedbackAudio.HitClip, hitPoint, 1f);
        }
    }

    private void HandleDied(CombatHitData killingHitData)
    {
        Vector3 effectPosition = GetEffectOrigin();
        PlaySound(killClip != null ? killClip : CombatFeedbackAudio.KillClip, effectPosition, 1f);
        SetWarningVisible(false);

        if (healthBar != null)
        {
            healthBar.SetNormalized(0f, false);
            healthBarHideTime = Time.time + 0.6f;
        }

        if (!useDeathEffect)
        {
            return;
        }

        if (deathEffectPrefab != null)
        {
            Destroy(Instantiate(deathEffectPrefab, effectPosition, Quaternion.identity), 5f);
        }
        else if (deathDebrisCount > 0)
        {
            CombatFeedbackDebris.Burst(
                effectPosition,
                Vector3.up + killingHitData.HitDirection * 0.5f,
                deathDebrisCount,
                deathDebrisColor,
                0.14f,
                5f,
                0.9f);
        }

        deathTime = Time.time;
        deathVisualRunning = visualTransform != null;
    }

    private void HandleRevived()
    {
        deathVisualRunning = false;
        RestoreVisualTransform();

        if (healthBar != null)
        {
            healthBar.SetNormalized(1f, true);
            healthBar.SetVisible(false);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        bool inCombat = showHealthBarInCombat
            && combatController != null
            && enemyHealth.IsAlive
            && (combatController.CurrentState == EnemyCombatState.Chasing
                || combatController.CurrentState == EnemyCombatState.Attacking
                || combatController.CurrentState == EnemyCombatState.Hit);

        if (inCombat)
        {
            healthBarHideTime = Mathf.Max(healthBarHideTime, Time.time + 0.5f);
        }

        bool shouldShow = Time.time < healthBarHideTime;

        if (shouldShow && cachedCamera != null)
        {
            float sqrDistance = (cachedCamera.transform.position - transform.position).sqrMagnitude;
            shouldShow = sqrDistance <= healthBarMaxDistance * healthBarMaxDistance;
        }

        if (shouldShow != isHealthBarVisible)
        {
            isHealthBarVisible = shouldShow;
            healthBar.SetVisible(shouldShow);
        }

        if (shouldShow)
        {
            healthBar.UpdatePlacement(GetUiAnchorPosition() + healthBarOffset, cachedCamera);
        }
    }

    private void BeginFlash()
    {
        if (!isFlashing)
        {
            CollectFlashRenderers();
        }

        isFlashing = flashRenderers.Count > 0;
        flashEndTime = Time.time + hitFlashDuration;
        ApplyFlashColor(1f);
    }

    private void UpdateFlash()
    {
        if (!isFlashing)
        {
            return;
        }

        float remaining = flashEndTime - Time.time;

        if (remaining <= 0f)
        {
            EndFlash();
            return;
        }

        ApplyFlashColor(remaining / hitFlashDuration);
    }

    private void CollectFlashRenderers()
    {
        flashRenderers.Clear();
        flashBaseColors.Clear();
        flashHadBlockColor.Clear();
        flashSlotColors.Clear();

        Transform searchRoot = visualTransform != null ? visualTransform : transform;
        Renderer[] renderers = searchRoot.GetComponentsInChildren<Renderer>(false);

        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer current = renderers[index];

            if (current == null
                || !current.enabled
                || current is LineRenderer
                || current is TrailRenderer
                || current is ParticleSystemRenderer
                || current.GetComponent<TMP_Text>() != null)
            {
                continue;
            }

            current.GetPropertyBlock(propertyBlock);
            bool hasBlockColor = propertyBlock.HasColor(BaseColorId);
            Color baseColor = hasBlockColor
                ? propertyBlock.GetColor(BaseColorId)
                : GetMaterialColor(current.sharedMaterial);

            flashRenderers.Add(current);
            flashBaseColors.Add(baseColor);
            flashHadBlockColor.Add(hasBlockColor);
            flashSlotColors.Add(hasBlockColor ? null : GetSlotColors(current));
            propertyBlock.Clear();
        }
    }

    private static Color[] GetSlotColors(Renderer target)
    {
        Material[] materials = target.sharedMaterials;

        if (materials.Length <= 1)
        {
            return null;
        }

        Color[] colors = new Color[materials.Length];

        for (int index = 0; index < materials.Length; index++)
        {
            colors[index] = GetMaterialColor(materials[index]);
        }

        return colors;
    }

    private void ApplyFlashColor(float weight)
    {
        for (int index = 0; index < flashRenderers.Count; index++)
        {
            Renderer current = flashRenderers[index];

            if (current == null)
            {
                continue;
            }

            Color[] slotColors = flashSlotColors[index];

            if (slotColors != null)
            {
                for (int slot = 0; slot < slotColors.Length; slot++)
                {
                    Color slotColor = Color.Lerp(slotColors[slot], hitFlashColor, weight);
                    propertyBlock.Clear();
                    propertyBlock.SetColor(BaseColorId, slotColor);
                    propertyBlock.SetColor(ColorId, slotColor);
                    current.SetPropertyBlock(propertyBlock, slot);
                }

                propertyBlock.Clear();
                continue;
            }

            Color color = Color.Lerp(flashBaseColors[index], hitFlashColor, weight);
            current.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            current.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }
    }

    private void EndFlash()
    {
        for (int index = 0; index < flashRenderers.Count; index++)
        {
            Renderer current = flashRenderers[index];

            if (current == null)
            {
                continue;
            }

            Color[] slotColors = flashSlotColors[index];

            if (slotColors != null)
            {
                for (int slot = 0; slot < slotColors.Length; slot++)
                {
                    current.SetPropertyBlock(emptyBlock, slot);
                }

                continue;
            }

            if (flashHadBlockColor[index])
            {
                current.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, flashBaseColors[index]);
                propertyBlock.SetColor(ColorId, flashBaseColors[index]);
                current.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
            else
            {
                // 원래 PropertyBlock 색상이 없었다면 Material 색상으로 되돌리기 위해 Block 제거
                current.SetPropertyBlock(null);
            }
        }

        flashRenderers.Clear();
        flashBaseColors.Clear();
        flashHadBlockColor.Clear();
        flashSlotColors.Clear();
        isFlashing = false;
    }

    private void UpdateVisualMotion()
    {
        if (visualTransform == null)
        {
            return;
        }

        if (deathVisualRunning)
        {
            float t = Mathf.Clamp01((Time.time - deathTime - deathShrinkDelay) / deathShrinkDuration);
            float eased = t * t;
            visualTransform.localScale = Vector3.Lerp(visualBaseScale, visualBaseScale * 0.05f, eased);
            visualTransform.localPosition = visualBaseLocalPosition + Vector3.down * (deathSinkDistance * eased);
            return;
        }

        float punchT = (Time.time - punchStartTime) / hitPunchDuration;

        if (punchT >= 0f && punchT <= 1f)
        {
            float punch = Mathf.Sin(punchT * Mathf.PI) * hitPunchScale;
            visualTransform.localScale = visualBaseScale * (1f + punch);
        }
        else if (punchT > 1f && punchT < 2f)
        {
            visualTransform.localScale = visualBaseScale;
        }
    }

    private void RestoreVisualTransform()
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.localScale = visualBaseScale;
        visualTransform.localPosition = visualBaseLocalPosition;
    }

    private void UpdateAttackWarning()
    {
        EnemyAttackPhase phase = GetCurrentAttackPhase(out float normalized);

        if (phase == EnemyAttackPhase.Windup && observedPhase != EnemyAttackPhase.Windup && enemyHealth.IsAlive)
        {
            PlaySound(warningClip != null ? warningClip : CombatFeedbackAudio.WarningClip, GetUiAnchorPosition(), 0.6f);
            healthBarHideTime = Mathf.Max(healthBarHideTime, Time.time + healthBarVisibleDuration);
        }

        observedPhase = phase;
        bool warningActive = phase == EnemyAttackPhase.Windup && enemyHealth.IsAlive;
        SetWarningVisible(warningActive);

        if (warningActive && warningMarker != null)
        {
            Color color = Color.Lerp(warningStartColor, warningEndColor, normalized);
            warningMarker.color = color;
            float pulse = 1f + Mathf.Sin(Time.time * 20f) * 0.08f + normalized * 0.35f;
            Transform markerTransform = warningMarker.transform;
            float barHeight = healthBar != null && isHealthBarVisible ? 0.35f : 0f;
            markerTransform.position = GetUiAnchorPosition() + healthBarOffset + Vector3.up * (0.3f + barHeight);
            markerTransform.localScale = Vector3.one * pulse;

            if (cachedCamera != null)
            {
                markerTransform.rotation = cachedCamera.transform.rotation;
            }
        }

        if (groundTelegraphLine != null && groundTelegraphLine.enabled && groundTelegraphInstance != null)
        {
            // URP Unlit은 정점 색상을 쓰지 않으므로 LineRenderer 색상을 Material 색상으로 복사
            groundTelegraphInstance.SetColor(BaseColorId, groundTelegraphLine.startColor);
        }
    }

    private EnemyAttackPhase GetCurrentAttackPhase(out float normalized)
    {
        if (rangedAttackController != null && rangedAttackController.enabled)
        {
            normalized = rangedAttackController.PhaseNormalized;

            if (rangedAttackController.CurrentPhase != EnemyAttackPhase.Ready)
            {
                return rangedAttackController.CurrentPhase;
            }
        }

        if (combatController != null)
        {
            normalized = combatController.AttackPhaseNormalized;
            return combatController.CurrentAttackPhase;
        }

        normalized = 0f;
        return EnemyAttackPhase.Ready;
    }

    private void SetWarningVisible(bool isVisible)
    {
        if (warningMarker != null && warningMarker.gameObject.activeSelf != isVisible)
        {
            warningMarker.gameObject.SetActive(isVisible);
        }
    }

    private TextMeshPro CreateWarningMarker()
    {
        GameObject markerObject = new GameObject("AttackWarningMarker");
        markerObject.transform.SetParent(transform, false);
        markerObject.layer = gameObject.layer;

        TextMeshPro text = markerObject.AddComponent<TextMeshPro>();
        text.text = "!";
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(2f, 2f);
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        markerObject.SetActive(false);
        return text;
    }

    private void CreateGroundTelegraph()
    {
        GameObject telegraphObject = new GameObject("AttackTelegraph_Runtime");
        telegraphObject.transform.SetParent(transform, false);
        telegraphObject.transform.localPosition = new Vector3(0f, GetGroundOffset(), 0f);

        groundTelegraphLine = telegraphObject.AddComponent<LineRenderer>();
        groundTelegraphInstance = new Material(groundTelegraphMaterial);
        groundTelegraphLine.sharedMaterial = groundTelegraphInstance;
        groundTelegraphLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        groundTelegraphLine.receiveShadows = false;
        groundTelegraphLine.enabled = false;
        telegraphObject.AddComponent<EnemyAttackTelegraph>();
    }

    private float GetGroundOffset()
    {
        // 적 Root가 Capsule 중심에 있으므로 Collider 바닥 높이를 찾아 원을 발밑에 둔다
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule != null)
        {
            return capsule.center.y - capsule.height * 0.5f;
        }

        return 0f;
    }

    private void PlaySound(AudioClip clip, Vector3 position, float volumeScale)
    {
        if (!playSounds || clip == null)
        {
            return;
        }

        CombatFeedbackAudio.PlayAt(clip, position, soundVolume * volumeScale);
    }

    private Vector3 ResolveHitPoint(CombatHitData hitData)
    {
        Vector3 hitPoint = hitData.HitPoint;
        bool looksInvalid = hitPoint == Vector3.zero
            || (hitPoint - transform.position).sqrMagnitude > 25f;

        return looksInvalid ? GetEffectOrigin() : hitPoint;
    }

    private Vector3 GetEffectOrigin()
    {
        if (visualRoot != null && visualRoot.EffectOrigin != null)
        {
            return visualRoot.EffectOrigin.position;
        }

        return transform.position + Vector3.up * (fallbackBarHeight * 0.5f);
    }

    private Vector3 GetUiAnchorPosition()
    {
        if (visualRoot != null && visualRoot.UiAnchor != null)
        {
            return visualRoot.UiAnchor.position;
        }

        return transform.position + Vector3.up * fallbackBarHeight;
    }

    private string GetDisplayName()
    {
        EnemyCombatData data = enemyHealth.CombatData;
        return data != null ? data.DisplayName : gameObject.name;
    }

    private Camera GetCamera()
    {
        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
        {
            return cachedCamera;
        }

        return Camera.main;
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty(BaseColorId))
        {
            return material.GetColor(BaseColorId);
        }

        return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
    }

    private void ResolveReferences()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (combatController == null)
        {
            combatController = GetComponent<EnemyCombatController>();
        }

        if (rangedAttackController == null)
        {
            rangedAttackController = GetComponent<EnemyRangedAttackController>();
        }

        if (visualRoot == null)
        {
            visualRoot = GetComponent<ContentVisualRoot>();
        }
    }
}
