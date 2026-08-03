using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 추가 방지
public sealed class ContentVisualRoot : MonoBehaviour // 게임 로직 Root와 교체 가능한 Visual을 분리하는 공통 관리자
{
    private const string VisualRootName = "Visual"; // 표준 Visual Root 오브젝트 이름
    private const string VisualInstanceRootName = "VisualInstance"; // 실제 모델을 배치할 표준 자식 이름
    private const string InteractionPointName = "InteractionPoint"; // 상호작용 기준 위치 이름
    private const string EffectOriginName = "EffectOrigin"; // 효과 생성 기준 위치 이름
    private const string UiAnchorName = "UIAnchor"; // 월드 UI 기준 위치 이름

    [Header("Standard Structure")] // 표준 자식 Transform 참조 묶음
    [Tooltip("교체 가능한 외형 전체를 담는 Visual 자식입니다.")] // Inspector Visual Root 설명
    [SerializeField] private Transform visualRoot; // 교체 가능한 외형 Root

    [Tooltip("실제 임시 Primitive 또는 모델 Prefab을 담는 VisualInstance 자식입니다.")] // Inspector Visual Instance 설명
    [SerializeField] private Transform visualInstanceRoot; // 실제 모델 배치 Root

    [Tooltip("플레이어 상호작용 거리와 표시 위치에 사용할 기준 Transform입니다.")] // Inspector 상호작용 위치 설명
    [SerializeField] private Transform interactionPoint; // 상호작용 기준 위치

    [Tooltip("공격, 피격, 채집과 건축 효과 생성에 사용할 기준 Transform입니다.")] // Inspector 효과 위치 설명
    [SerializeField] private Transform effectOrigin; // 효과 생성 기준 위치

    [Tooltip("체력바, 이름과 상호작용 UI를 표시할 기준 Transform입니다.")] // Inspector UI 위치 설명
    [SerializeField] private Transform uiAnchor; // 월드 UI 기준 위치

    [Header("Applied Profile")] // 현재 적용 Profile 정보 묶음
    [Tooltip("현재 ContentVisualRoot 설정의 출처가 된 Visual Profile입니다.")] // Inspector 적용 Profile 설명
    [SerializeField] private ContentVisualProfile appliedProfile; // 현재 적용된 Visual Profile

    [Header("Visual Source")] // 외형 생성 설정 묶음
    [Tooltip("VisualInstance 아래에 생성할 모델 또는 임시 Prefab입니다.")] // Inspector Visual Prefab 설명
    [SerializeField] private GameObject configuredVisualPrefab; // 생성할 외형 Prefab

    [Tooltip("Visual Prefab이 없을 때 Unity Primitive를 임시 외형으로 생성합니다.")] // Inspector 임시 외형 사용 설명
    [SerializeField] private bool createPlaceholderWhenPrefabMissing = true; // Prefab 누락 시 임시 외형 생성 여부

    [Tooltip("Visual Prefab이 없을 때 생성할 Unity Primitive 종류입니다.")] // Inspector 임시 Primitive 종류 설명
    [SerializeField] private PrimitiveType placeholderPrimitive = PrimitiveType.Capsule; // 임시 외형 Primitive 종류

    [Tooltip("임시 Primitive에 적용할 Material입니다. 비어 있으면 Unity 기본 Material을 사용합니다.")] // Inspector 임시 Material 설명
    [SerializeField] private Material placeholderMaterial; // 임시 외형 Material

    [Tooltip("임시 Primitive에 MaterialPropertyBlock으로 적용할 기본 색상입니다.")] // Inspector 임시 색상 설명
    [SerializeField] private Color placeholderColor = Color.white; // 임시 외형 기본 색상

    [Tooltip("임시 Primitive에 기본 색상을 적용합니다.")] // Inspector 임시 색상 사용 설명
    [SerializeField] private bool usePlaceholderColor; // 기존 임시 Material 색상을 보존하기 위해 기본값을 비활성화

    [Tooltip("실제 Visual Prefab의 모든 Renderer에 적용할 Material Override입니다.")] // Inspector Material Override 설명
    [SerializeField] private Material visualMaterialOverride; // 실제 외형 Material Override

    [Tooltip("Material Override를 실제 Visual Prefab에 적용합니다.")] // Inspector Material Override 적용 설명
    [SerializeField] private bool applyMaterialOverrideToVisualPrefab = true; // 실제 Prefab Material Override 적용 여부

    [Tooltip("생성된 외형의 Animator에 적용할 Animator Controller입니다.")] // Inspector Animator Controller 설명
    [SerializeField] private RuntimeAnimatorController animatorControllerOverride; // 외형 Animator Controller Override

    [Header("Visual Transform")] // 생성 외형 Transform 설정 묶음
    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 위치입니다.")] // Inspector 외형 위치 설명
    [SerializeField] private Vector3 visualLocalPosition = Vector3.zero; // 생성 외형 로컬 위치

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 회전입니다.")] // Inspector 외형 회전 설명
    [SerializeField] private Vector3 visualLocalEulerAngles = Vector3.zero; // 생성 외형 로컬 회전

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 크기입니다.")] // Inspector 외형 크기 설명
    [SerializeField] private Vector3 visualLocalScale = Vector3.one; // 생성 외형 로컬 크기

    [Header("Anchor Defaults")] // 표준 기준점 기본 위치 묶음
    [Tooltip("InteractionPoint에 적용할 로컬 위치입니다.")] // Inspector 상호작용 위치 설명
    [SerializeField] private Vector3 defaultInteractionPointPosition = new Vector3(0f, 1f, 0f); // 상호작용 기준점 기본 위치

    [Tooltip("EffectOrigin에 적용할 로컬 위치입니다.")] // Inspector 효과 위치 설명
    [SerializeField] private Vector3 defaultEffectOriginPosition = new Vector3(0f, 1f, 0f); // 효과 기준점 기본 위치

    [Tooltip("UIAnchor에 적용할 로컬 위치입니다.")] // Inspector UI 위치 설명
    [SerializeField] private Vector3 defaultUiAnchorPosition = new Vector3(0f, 2f, 0f); // UI 기준점 기본 위치

    [Header("Runtime Rules")] // 실행 시 외형 규칙 묶음
    [Tooltip("Awake 시 표준 자식이 없으면 자동으로 생성합니다.")] // Inspector 실행 시 구조 생성 설명
    [SerializeField] private bool createMissingStructureOnAwake = true; // 실행 시 누락 구조 자동 생성 여부

    [Tooltip("Awake 시 설정된 Prefab 또는 임시 Primitive로 VisualInstance를 다시 만듭니다.")] // Inspector 실행 시 외형 재생성 설명
    [SerializeField] private bool rebuildVisualOnAwake; // 실행 시 외형 자동 재생성 여부

    [Tooltip("생성한 외형의 Layer를 Root 오브젝트 Layer와 동일하게 맞춥니다.")] // Inspector Layer 상속 설명
    [SerializeField] private bool inheritRootLayer = true; // 생성 외형 Layer 상속 여부

    [Tooltip("생성한 외형 내부 Collider를 제거하여 Root Collider와 중복 충돌하지 않게 합니다.")] // Inspector 외형 Collider 제거 설명
    [SerializeField] private bool removeVisualColliders = true; // 생성 외형 Collider 제거 여부

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("현재 VisualInstance 아래에 생성된 외형 오브젝트입니다.")] // Inspector 현재 외형 설명
    [SerializeField] private GameObject activeVisualObject; // 현재 생성된 외형 오브젝트

    [Tooltip("현재 표준 자식 구조가 모두 연결되었는지 표시합니다.")] // Inspector 구조 준비 상태 설명
    [SerializeField] private bool isStructureReady; // 표준 구조 준비 여부

    public Transform VisualRoot => visualRoot; // Visual Root 제공
    public Transform VisualInstanceRoot => visualInstanceRoot; // Visual Instance Root 제공
    public Transform InteractionPoint => interactionPoint; // 상호작용 기준 위치 제공
    public Transform EffectOrigin => effectOrigin; // 효과 생성 기준 위치 제공
    public Transform UiAnchor => uiAnchor; // 월드 UI 기준 위치 제공
    public ContentVisualProfile AppliedProfile => appliedProfile; // 현재 적용 Profile 제공
    public GameObject ConfiguredVisualPrefab => configuredVisualPrefab; // 설정된 외형 Prefab 제공
    public GameObject ActiveVisualObject => activeVisualObject; // 현재 생성 외형 제공
    public bool IsStructureReady => isStructureReady; // 표준 구조 준비 여부 제공

    private void Reset() // 컴포넌트 최초 추가 시 기존 표준 자식 참조 검색
    {
        ResolveExistingReferences(); // 현재 Root 아래의 기존 표준 자식 참조 연결
        RefreshStructureReadyState(); // 표준 구조 준비 상태 갱신
    }

    private void Awake() // 실행 시작 시 표준 구조와 선택적 외형 생성 준비
    {
        ResolveExistingReferences(); // Scene에 저장된 표준 자식 참조 다시 검색

        if (createMissingStructureOnAwake) // 실행 시 누락 구조 자동 생성 설정 확인
        {
            EnsureStandardStructure(); // 누락된 표준 자식 구조 생성
        }
        else // 실행 시 구조 자동 생성을 사용하지 않는 경우
        {
            RefreshStructureReadyState(); // 현재 저장된 구조 준비 상태만 갱신
        }

        if (rebuildVisualOnAwake) // 실행 시 외형 자동 재생성 설정 확인
        {
            RebuildConfiguredVisual(); // 설정 Prefab 또는 임시 Primitive로 외형 재생성
        }
        else // 실행 시 외형 자동 재생성을 사용하지 않는 경우
        {
            FindCurrentVisualObject(); // 현재 VisualInstance 안의 첫 번째 외형 참조 검색
        }
    }

    private void OnValidate() // Inspector 입력값과 기존 표준 자식 참조 검증
    {
        visualLocalScale.x = Mathf.Max(0.01f, visualLocalScale.x); // 외형 X 크기 최소값 적용
        visualLocalScale.y = Mathf.Max(0.01f, visualLocalScale.y); // 외형 Y 크기 최소값 적용
        visualLocalScale.z = Mathf.Max(0.01f, visualLocalScale.z); // 외형 Z 크기 최소값 적용
        ResolveExistingReferences(); // Inspector 변경 후 기존 표준 자식 참조 갱신
        RefreshStructureReadyState(); // 표준 구조 준비 상태 갱신
    }

    [ContextMenu("Ensure Standard Structure")] // Inspector 표준 구조 생성 메뉴
    public void EnsureStandardStructure() // Visual과 기준점 표준 자식 구조를 생성하고 연결
    {
        visualRoot = GetOrCreateDirectChild(transform, VisualRootName, Vector3.zero); // Visual Root 검색 또는 생성
        visualInstanceRoot = GetOrCreateDirectChild(visualRoot, VisualInstanceRootName, Vector3.zero); // Visual Instance Root 검색 또는 생성
        interactionPoint = GetOrCreateDirectChild(transform, InteractionPointName, defaultInteractionPointPosition); // 상호작용 기준점 검색 또는 생성
        effectOrigin = GetOrCreateDirectChild(transform, EffectOriginName, defaultEffectOriginPosition); // 효과 기준점 검색 또는 생성
        uiAnchor = GetOrCreateDirectChild(transform, UiAnchorName, defaultUiAnchorPosition); // UI 기준점 검색 또는 생성
        RefreshStructureReadyState(); // 표준 구조 준비 상태 갱신
    }

    public bool ApplyProfile(ContentVisualProfile visualProfile, bool rebuildImmediately = true) // Visual Profile의 전체 설정을 현재 Root에 적용
    {
        if (visualProfile == null) // 적용할 Profile 존재 여부 확인
        {
            Debug.LogError($"{name}에 적용할 ContentVisualProfile이 없습니다.", this); // Profile 누락 오류 출력
            return false; // Profile 적용 실패 반환
        }

        appliedProfile = visualProfile; // 현재 적용 Profile 저장
        configuredVisualPrefab = visualProfile.VisualPrefab; // 실제 외형 Prefab 설정 적용
        createPlaceholderWhenPrefabMissing = visualProfile.CreatePlaceholderWhenPrefabMissing; // 임시 외형 사용 설정 적용
        placeholderPrimitive = visualProfile.PlaceholderPrimitive; // 임시 Primitive 종류 적용
        placeholderMaterial = visualProfile.PlaceholderMaterial; // 임시 Material 적용
        placeholderColor = visualProfile.PlaceholderColor; // 임시 색상 적용
        usePlaceholderColor = visualProfile.UsePlaceholderColor; // 임시 색상 사용 설정 적용
        visualMaterialOverride = visualProfile.MaterialOverride; // 실제 외형 Material Override 적용
        applyMaterialOverrideToVisualPrefab = visualProfile.ApplyMaterialOverrideToVisualPrefab; // Material Override 사용 설정 적용
        animatorControllerOverride = visualProfile.AnimatorController; // Animator Controller 적용
        visualLocalPosition = visualProfile.VisualLocalPosition; // 외형 로컬 위치 적용
        visualLocalEulerAngles = visualProfile.VisualLocalEulerAngles; // 외형 로컬 회전 적용
        visualLocalScale = visualProfile.VisualLocalScale; // 외형 로컬 크기 적용
        defaultInteractionPointPosition = visualProfile.InteractionPointPosition; // 상호작용 기준점 위치 적용
        defaultEffectOriginPosition = visualProfile.EffectOriginPosition; // 효과 기준점 위치 적용
        defaultUiAnchorPosition = visualProfile.UiAnchorPosition; // UI 기준점 위치 적용
        inheritRootLayer = visualProfile.InheritRootLayer; // Root Layer 상속 설정 적용
        removeVisualColliders = visualProfile.RemoveVisualColliders; // 외형 Collider 제거 설정 적용
        EnsureStandardStructure(); // 표준 자식 구조 보장
        ApplyAnchorPositions(); // Profile 기준점 위치를 기존 표준 자식에도 적용

        if (rebuildImmediately) // 즉시 외형 재생성 설정 확인
        {
            RebuildConfiguredVisual(); // Profile 설정으로 외형 재생성
        }
        else // 즉시 재생성을 사용하지 않는 경우
        {
            ApplyCurrentVisualSettings(); // 현재 외형에 Profile Transform과 표시 설정 적용
        }

        return true; // Profile 적용 성공 반환
    }

    [ContextMenu("Rebuild Configured Visual")] // Inspector 설정 외형 재생성 메뉴
    public void RebuildConfiguredVisual() // VisualInstance 아래 외형을 Prefab 또는 임시 Primitive로 다시 생성
    {
        EnsureStandardStructure(); // 외형 생성 전 표준 자식 구조 보장
        ClearVisualInstance(); // 기존 VisualInstance 자식 외형 제거
        bool createdPlaceholder = false; // 이번 생성 외형이 임시 Primitive인지 저장

        if (configuredVisualPrefab != null) // 설정된 외형 Prefab 존재 여부 확인
        {
            activeVisualObject = Instantiate(configuredVisualPrefab, visualInstanceRoot); // 설정된 외형 Prefab 생성
            activeVisualObject.name = configuredVisualPrefab.name; // 생성 외형 이름을 Prefab 이름으로 정리
        }
        else if (createPlaceholderWhenPrefabMissing) // Prefab 누락 시 임시 외형 생성 설정 확인
        {
            activeVisualObject = GameObject.CreatePrimitive(placeholderPrimitive); // 지정 Unity Primitive 생성
            activeVisualObject.name = $"TEMP_{placeholderPrimitive}_Visual"; // 임시 외형 이름 설정
            activeVisualObject.transform.SetParent(visualInstanceRoot, false); // 임시 외형을 VisualInstance 아래에 배치
            createdPlaceholder = true; // 임시 Primitive 생성 상태 저장
        }
        else // Prefab과 임시 외형을 모두 사용하지 않는 경우
        {
            activeVisualObject = null; // 현재 생성 외형 참조 초기화
            return; // 외형 생성 처리 종료
        }

        ApplyVisualTransform(activeVisualObject.transform); // 생성 외형 로컬 Transform 적용
        ApplyVisualAppearance(activeVisualObject, createdPlaceholder); // 생성 외형 Material과 색상 적용
        ApplyAnimatorController(activeVisualObject); // 생성 외형 Animator Controller 적용

        if (inheritRootLayer) // Root Layer 상속 설정 확인
        {
            ApplyLayerRecursively(activeVisualObject.transform, gameObject.layer); // 생성 외형 전체 Layer를 Root와 동일하게 적용
        }

        if (removeVisualColliders) // 외형 Collider 제거 설정 확인
        {
            RemoveCollidersFromVisual(activeVisualObject); // 생성 외형 내부 전체 Collider 제거
        }
    }

    [ContextMenu("Clear Visual Instance")] // Inspector 현재 외형 제거 메뉴
    public void ClearVisualInstance() // VisualInstance 아래 현재 외형 자식을 모두 제거
    {
        ResolveExistingReferences(); // VisualInstance Root 참조 검색

        if (visualInstanceRoot == null) // VisualInstance Root 존재 여부 확인
        {
            activeVisualObject = null; // 현재 외형 참조 초기화
            return; // 외형 제거 처리 종료
        }

        for (int index = visualInstanceRoot.childCount - 1; index >= 0; index--) // VisualInstance 자식을 역순으로 순회
        {
            GameObject childObject = visualInstanceRoot.GetChild(index).gameObject; // 현재 제거할 외형 자식 가져오기
            childObject.SetActive(false); // 지연 제거 전 현재 외형 즉시 비활성화

            if (Application.isPlaying) // Play Mode 실행 여부 확인
            {
                Destroy(childObject); // Play Mode에서 현재 외형 지연 제거
            }
            else // Edit Mode에서 제거하는 경우
            {
                DestroyImmediate(childObject); // Edit Mode에서 현재 외형 즉시 제거
            }
        }

        activeVisualObject = null; // 현재 외형 참조 초기화
    }

    [ContextMenu("Disable Legacy Root Renderers")] // Inspector Root Renderer 비활성화 메뉴
    public void DisableLegacyRootRenderers() // Root에 남아 있는 기존 Renderer만 비활성화
    {
        Renderer[] rootRenderers = GetComponents<Renderer>(); // 현재 Root 오브젝트의 Renderer 목록 가져오기

        for (int index = 0; index < rootRenderers.Length; index++) // Root Renderer 전체 순회
        {
            Renderer currentRenderer = rootRenderers[index]; // 현재 Root Renderer 가져오기

            if (currentRenderer != null) // Renderer 참조 존재 여부 확인
            {
                currentRenderer.enabled = false; // 기존 Root Renderer 비활성화
            }
        }
    }

    [ContextMenu("Enable Legacy Root Renderers")] // Inspector Root Renderer 활성화 메뉴
    public void EnableLegacyRootRenderers() // Root에 남아 있는 기존 Renderer를 다시 활성화
    {
        Renderer[] rootRenderers = GetComponents<Renderer>(); // 현재 Root 오브젝트의 Renderer 목록 가져오기

        for (int index = 0; index < rootRenderers.Length; index++) // Root Renderer 전체 순회
        {
            Renderer currentRenderer = rootRenderers[index]; // 현재 Root Renderer 가져오기

            if (currentRenderer != null) // Renderer 참조 존재 여부 확인
            {
                currentRenderer.enabled = true; // 기존 Root Renderer 활성화
            }
        }
    }

    [ContextMenu("Validate Visual Structure")] // Inspector Visual 구조 검증 메뉴
    public bool ValidateVisualStructure() // 현재 Root의 표준 Visual 구조와 생성된 외형을 검사
    {
        ResolveExistingReferences(); // 현재 저장된 표준 자식 참조 검색
        RefreshStructureReadyState(); // 표준 구조 준비 상태 갱신

        if (!isStructureReady) // 표준 구조 누락 여부 확인
        {
            Debug.LogError( // 표준 구조 누락 오류 출력 시작
                $"{name}의 Visual 표준 구조가 완성되지 않았습니다. " // 현재 Root 이름과 오류 안내
                + "Ensure Standard Structure를 실행하세요.", // 해결 방법 추가
                this); // 현재 컴포넌트를 Log Context로 지정
            return false; // Visual 구조 검증 실패 반환
        }

        if (visualRoot == transform || visualInstanceRoot == transform) // Visual 참조가 Root 자체를 가리키는지 확인
        {
            Debug.LogError( // 잘못된 Visual 참조 오류 출력 시작
                $"{name}의 Visual 참조는 Root 자신이 아닌 자식 Transform이어야 합니다.", // 오류 내용 출력
                this); // 현재 컴포넌트를 Log Context로 지정
            return false; // Visual 구조 검증 실패 반환
        }

        bool hasVisualSource = configuredVisualPrefab != null // 실제 Visual Prefab 존재 여부 확인
            || createPlaceholderWhenPrefabMissing; // 임시 Placeholder 생성 가능 여부 확인

        if (!hasVisualSource) // 외형 생성 정보가 전혀 없는지 확인
        {
            Debug.LogError( // 외형 생성 정보 누락 오류 출력 시작
                $"{name}에 Visual Prefab이 없고 Placeholder 생성도 비활성화되어 있습니다.", // 오류 내용 출력
                this); // 현재 컴포넌트를 Log Context로 지정
            return false; // Visual 구조 검증 실패 반환
        }

        if (visualInstanceRoot.childCount <= 0) // 실제 생성된 외형 자식 존재 여부 확인
        {
            Debug.LogError( // VisualInstance 외형 누락 오류 출력 시작
                $"{name}의 VisualInstance에 생성된 외형이 없습니다. " // 현재 Root와 누락 내용 안내
                + "Rebuild Configured Visual을 실행하세요.", // 해결 방법 추가
                this); // 현재 컴포넌트를 Log Context로 지정
            return false; // Visual 구조 검증 실패 반환
        }

        Collider[] visualColliders = // VisualInstance 내부 전체 Collider 검색 시작
            visualInstanceRoot.GetComponentsInChildren<Collider>(true); // 비활성 자식까지 포함하여 Collider 검색

        if (visualColliders.Length > 0) // Visual 내부 Collider 존재 여부 확인
        {
            Debug.LogError( // 중복 충돌 가능성을 오류로 출력 시작
                $"{name}의 VisualInstance 안에 Collider가 {visualColliders.Length}개 남아 있습니다. " // 남은 Collider 개수 안내
                + "Rebuild Configured Visual을 실행하거나 Visual Collider를 제거하세요.", // 해결 방법 추가
                this); // 현재 컴포넌트를 Log Context로 지정
            return false; // 중복 Collider가 있으면 검증 실패 반환
        }

        Debug.Log($"{name} Visual 구조 검증 완료", this); // Visual 구조 정상 검증 결과 출력
        return true; // Visual 구조 검증 성공 반환
    }

    public void SetConfiguredVisualPrefab(GameObject visualPrefab, bool rebuildImmediately = true) // 외형 Prefab을 변경하고 선택적으로 즉시 재생성
    {
        appliedProfile = null; // 수동 Prefab 변경 시 적용 Profile 참조 해제
        configuredVisualPrefab = visualPrefab; // 새로운 외형 Prefab 저장

        if (rebuildImmediately) // 즉시 재생성 설정 확인
        {
            RebuildConfiguredVisual(); // 변경된 Prefab으로 외형 재생성
        }
    }

    public void SetVisualVisible(bool isVisible) // Visual Root 전체 표시 상태 변경
    {
        ResolveExistingReferences(); // Visual Root 참조 검색

        if (visualRoot != null) // Visual Root 존재 여부 확인
        {
            visualRoot.gameObject.SetActive(isVisible); // Visual Root 활성 상태 변경
        }
    }

    public void ApplyCurrentVisualTransform() // 현재 생성 외형에 Inspector Transform 설정 다시 적용
    {
        FindCurrentVisualObject(); // 현재 VisualInstance 외형 참조 검색

        if (activeVisualObject == null) // 현재 생성 외형 존재 여부 확인
        {
            return; // Transform 적용 처리 종료
        }

        ApplyVisualTransform(activeVisualObject.transform); // 현재 생성 외형 로컬 Transform 적용
    }

    public void ApplyCurrentVisualSettings() // 현재 외형에 Transform과 Material 및 Animator 설정 다시 적용
    {
        FindCurrentVisualObject(); // 현재 VisualInstance 외형 참조 검색
        ApplyAnchorPositions(); // 현재 기준점 기본 위치 적용

        if (activeVisualObject == null) // 현재 생성 외형 존재 여부 확인
        {
            return; // 현재 외형 설정 적용 종료
        }

        bool isPlaceholder = configuredVisualPrefab == null; // 현재 외형이 임시 Primitive인지 추정
        ApplyVisualTransform(activeVisualObject.transform); // 현재 외형 Transform 적용
        ApplyVisualAppearance(activeVisualObject, isPlaceholder); // 현재 외형 Material과 색상 적용
        ApplyAnimatorController(activeVisualObject); // 현재 외형 Animator Controller 적용
    }

    private void ResolveExistingReferences() // Root 아래의 기존 표준 자식 Transform을 이름으로 검색
    {
        if (visualRoot == null || visualRoot.parent != transform) // 저장된 Visual Root 참조 유효성 확인
        {
            visualRoot = FindDirectChild(transform, VisualRootName); // Root 직속 Visual 자식 검색
        }

        if (visualRoot != null && (visualInstanceRoot == null || visualInstanceRoot.parent != visualRoot)) // Visual Instance 참조 유효성 확인
        {
            visualInstanceRoot = FindDirectChild(visualRoot, VisualInstanceRootName); // Visual 직속 VisualInstance 자식 검색
        }

        if (interactionPoint == null || interactionPoint.parent != transform) // 저장된 InteractionPoint 참조 유효성 확인
        {
            interactionPoint = FindDirectChild(transform, InteractionPointName); // Root 직속 InteractionPoint 자식 검색
        }

        if (effectOrigin == null || effectOrigin.parent != transform) // 저장된 EffectOrigin 참조 유효성 확인
        {
            effectOrigin = FindDirectChild(transform, EffectOriginName); // Root 직속 EffectOrigin 자식 검색
        }

        if (uiAnchor == null || uiAnchor.parent != transform) // 저장된 UIAnchor 참조 유효성 확인
        {
            uiAnchor = FindDirectChild(transform, UiAnchorName); // Root 직속 UIAnchor 자식 검색
        }
    }

    private void RefreshStructureReadyState() // 표준 자식 참조가 모두 존재하는지 갱신
    {
        isStructureReady = visualRoot != null // Visual Root 존재 확인
            && visualInstanceRoot != null // Visual Instance Root 존재 확인
            && interactionPoint != null // InteractionPoint 존재 확인
            && effectOrigin != null // EffectOrigin 존재 확인
            && uiAnchor != null; // UIAnchor 존재 확인
    }

    private void FindCurrentVisualObject() // VisualInstance 아래 첫 번째 외형 오브젝트 검색
    {
        ResolveExistingReferences(); // VisualInstance Root 참조 검색

        if (visualInstanceRoot == null || visualInstanceRoot.childCount <= 0) // VisualInstance와 자식 존재 여부 확인
        {
            activeVisualObject = null; // 현재 외형 참조 초기화
            return; // 외형 검색 처리 종료
        }

        activeVisualObject = visualInstanceRoot.GetChild(0).gameObject; // 첫 번째 VisualInstance 자식을 현재 외형으로 연결
    }

    private Transform GetOrCreateDirectChild(Transform parentTransform, string childName, Vector3 defaultLocalPosition) // 지정 부모 아래 표준 자식을 검색하거나 생성
    {
        Transform existingChild = FindDirectChild(parentTransform, childName); // 같은 이름의 기존 직속 자식 검색

        if (existingChild != null) // 기존 표준 자식 존재 여부 확인
        {
            return existingChild; // 기존 표준 자식 반환
        }

        GameObject childObject = new GameObject(childName); // 새로운 표준 자식 GameObject 생성
        Transform childTransform = childObject.transform; // 새 자식 Transform 가져오기
        childTransform.SetParent(parentTransform, false); // 지정 부모 아래 로컬 기준으로 배치
        childTransform.localPosition = defaultLocalPosition; // 표준 자식 기본 로컬 위치 적용
        childTransform.localRotation = Quaternion.identity; // 표준 자식 로컬 회전 초기화
        childTransform.localScale = Vector3.one; // 표준 자식 로컬 크기 초기화
        childObject.layer = gameObject.layer; // 표준 자식 Layer를 Root Layer와 동일하게 적용
        return childTransform; // 새로 생성한 표준 자식 Transform 반환
    }

    private Transform FindDirectChild(Transform parentTransform, string childName) // 지정 부모의 직속 자식 중 같은 이름 검색
    {
        if (parentTransform == null) // 부모 Transform 존재 여부 확인
        {
            return null; // 자식 검색 실패 반환
        }

        for (int index = 0; index < parentTransform.childCount; index++) // 부모의 전체 직속 자식 순회
        {
            Transform currentChild = parentTransform.GetChild(index); // 현재 직속 자식 가져오기

            if (currentChild.name == childName) // 현재 자식 이름 일치 여부 확인
            {
                return currentChild; // 일치하는 표준 자식 반환
            }
        }

        return null; // 같은 이름의 직속 자식 검색 실패 반환
    }

    private void ApplyAnchorPositions() // 현재 설정된 기본 위치를 표준 기준점 Transform에 적용
    {
        if (interactionPoint != null) // InteractionPoint 존재 여부 확인
        {
            interactionPoint.localPosition = defaultInteractionPointPosition; // 상호작용 기준점 위치 적용
        }

        if (effectOrigin != null) // EffectOrigin 존재 여부 확인
        {
            effectOrigin.localPosition = defaultEffectOriginPosition; // 효과 기준점 위치 적용
        }

        if (uiAnchor != null) // UIAnchor 존재 여부 확인
        {
            uiAnchor.localPosition = defaultUiAnchorPosition; // UI 기준점 위치 적용
        }
    }

    private void ApplyVisualTransform(Transform targetVisualTransform) // 생성 외형에 Inspector 로컬 Transform 설정 적용
    {
        if (targetVisualTransform == null) // 적용 대상 외형 Transform 존재 여부 확인
        {
            return; // 외형 Transform 적용 처리 종료
        }

        targetVisualTransform.localPosition = visualLocalPosition; // 생성 외형 로컬 위치 적용
        targetVisualTransform.localRotation = Quaternion.Euler(visualLocalEulerAngles); // 생성 외형 로컬 회전 적용
        targetVisualTransform.localScale = visualLocalScale; // 생성 외형 로컬 크기 적용
    }

    private void ApplyVisualAppearance(GameObject visualObject, bool isPlaceholder) // 생성 외형에 Material과 임시 색상 적용
    {
        if (visualObject == null) // 생성 외형 존재 여부 확인
        {
            return; // 외형 표시 설정 적용 종료
        }

        if (isPlaceholder) // 임시 Primitive 외형 여부 확인
        {
            ApplyMaterialToRenderers(visualObject, placeholderMaterial); // 임시 Material 적용

            if (usePlaceholderColor) // 임시 색상 사용 여부 확인
            {
                ApplyColorToRenderers(visualObject, placeholderColor); // 임시 색상 적용
            }

            return; // 실제 Prefab Material 적용 생략
        }

        if (applyMaterialOverrideToVisualPrefab) // 실제 Prefab Material Override 사용 여부 확인
        {
            ApplyMaterialToRenderers(visualObject, visualMaterialOverride); // 실제 외형 Material Override 적용
        }
    }

    private void ApplyMaterialToRenderers(GameObject visualObject, Material targetMaterial) // 외형 전체 Renderer에 지정 Material 적용
    {
        if (visualObject == null || targetMaterial == null) // 외형과 Material 존재 여부 확인
        {
            return; // Material 적용 처리 종료
        }

        Renderer[] renderers = visualObject.GetComponentsInChildren<Renderer>(true); // 외형 전체 Renderer 검색

        for (int index = 0; index < renderers.Length; index++) // 외형 Renderer 전체 순회
        {
            Renderer currentRenderer = renderers[index]; // 현재 Renderer 가져오기

            if (currentRenderer != null) // Renderer 참조 존재 여부 확인
            {
                currentRenderer.sharedMaterial = targetMaterial; // 지정 Material 적용
            }
        }
    }

    private void ApplyColorToRenderers(GameObject visualObject, Color targetColor) // MaterialPropertyBlock으로 외형 Renderer 색상 적용
    {
        if (visualObject == null) // 외형 존재 여부 확인
        {
            return; // 색상 적용 처리 종료
        }

        Renderer[] renderers = visualObject.GetComponentsInChildren<Renderer>(true); // 외형 전체 Renderer 검색
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock(); // Renderer별 색상 PropertyBlock 생성

        for (int index = 0; index < renderers.Length; index++) // 외형 Renderer 전체 순회
        {
            Renderer currentRenderer = renderers[index]; // 현재 Renderer 가져오기

            if (currentRenderer == null) // Renderer 참조 존재 여부 확인
            {
                continue; // 다음 Renderer로 이동
            }

            currentRenderer.GetPropertyBlock(propertyBlock); // 현재 Renderer의 기존 PropertyBlock 가져오기
            propertyBlock.SetColor("_BaseColor", targetColor); // URP Lit 기본 색상 Property 설정
            propertyBlock.SetColor("_Color", targetColor); // 호환 Shader 기본 색상 Property 설정
            currentRenderer.SetPropertyBlock(propertyBlock); // 현재 Renderer에 변경된 PropertyBlock 적용
            propertyBlock.Clear(); // 다음 Renderer 적용 전 PropertyBlock 초기화
        }
    }

    private void ApplyAnimatorController(GameObject visualObject) // 생성 외형 Animator에 Controller Override 적용
    {
        if (visualObject == null || animatorControllerOverride == null) // 외형과 Animator Controller 존재 여부 확인
        {
            return; // Animator Controller 적용 처리 종료
        }

        Animator targetAnimator = visualObject.GetComponentInChildren<Animator>(true); // 생성 외형 내부 Animator 검색

        if (targetAnimator == null) // Animator 존재 여부 확인
        {
            Debug.LogWarning( // Animator 누락 경고 출력 시작
                $"{name}에 적용한 Visual Profile에는 Animator Controller가 있지만 " // 현재 Root와 설정 안내
                + "생성된 Visual Prefab에 Animator가 없습니다.", // Animator 누락 원인 안내
                this); // 현재 ContentVisualRoot를 Log Context로 지정
            return; // Animator Controller 적용 처리 종료
        }

        targetAnimator.runtimeAnimatorController = animatorControllerOverride; // 생성 외형 Animator Controller 적용
    }

    private void RemoveCollidersFromVisual(GameObject visualObject) // 생성 외형 내부 Collider를 모두 제거
    {
        if (visualObject == null) // 생성 외형 존재 여부 확인
        {
            return; // Collider 제거 처리 종료
        }

        Collider[] visualColliders = visualObject.GetComponentsInChildren<Collider>(true); // 생성 외형 전체 Collider 검색

        for (int index = visualColliders.Length - 1; index >= 0; index--) // 생성 외형 Collider를 역순으로 순회
        {
            Collider currentCollider = visualColliders[index]; // 현재 제거할 Collider 가져오기

            if (currentCollider == null) // Collider 참조 존재 여부 확인
            {
                continue; // 다음 Collider로 이동
            }

            if (Application.isPlaying) // Play Mode 실행 여부 확인
            {
                Destroy(currentCollider); // Play Mode에서 외형 Collider 지연 제거
            }
            else // Edit Mode에서 Collider를 제거하는 경우
            {
                DestroyImmediate(currentCollider); // Edit Mode에서 외형 Collider 즉시 제거
            }
        }
    }

    private void ApplyLayerRecursively(Transform targetTransform, int targetLayer) // 지정 Transform과 전체 자식 Layer를 동일하게 적용
    {
        if (targetTransform == null) // Layer 적용 대상 존재 여부 확인
        {
            return; // Layer 적용 처리 종료
        }

        targetTransform.gameObject.layer = targetLayer; // 현재 Transform GameObject Layer 적용

        for (int index = 0; index < targetTransform.childCount; index++) // 현재 Transform의 전체 자식 순회
        {
            ApplyLayerRecursively(targetTransform.GetChild(index), targetLayer); // 현재 자식과 하위 계층 Layer 재귀 적용
        }
    }
}
