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

    [Header("Visual Source")] // 외형 생성 설정 묶음
    [Tooltip("VisualInstance 아래에 생성할 모델 또는 임시 Prefab입니다.")] // Inspector Visual Prefab 설명
    [SerializeField] private GameObject configuredVisualPrefab; // 생성할 외형 Prefab

    [Tooltip("Visual Prefab이 없을 때 Unity Primitive를 임시 외형으로 생성합니다.")] // Inspector 임시 외형 사용 설명
    [SerializeField] private bool createPlaceholderWhenPrefabMissing = true; // Prefab 누락 시 임시 외형 생성 여부

    [Tooltip("Visual Prefab이 없을 때 생성할 Unity Primitive 종류입니다.")] // Inspector 임시 Primitive 종류 설명
    [SerializeField] private PrimitiveType placeholderPrimitive = PrimitiveType.Capsule; // 임시 외형 Primitive 종류

    [Tooltip("임시 Primitive에 적용할 Material입니다. 비어 있으면 Unity 기본 Material을 사용합니다.")] // Inspector 임시 Material 설명
    [SerializeField] private Material placeholderMaterial; // 임시 외형 Material

    [Header("Visual Transform")] // 생성 외형 Transform 설정 묶음
    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 위치입니다.")] // Inspector 외형 위치 설명
    [SerializeField] private Vector3 visualLocalPosition = Vector3.zero; // 생성 외형 로컬 위치

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 회전입니다.")] // Inspector 외형 회전 설명
    [SerializeField] private Vector3 visualLocalEulerAngles = Vector3.zero; // 생성 외형 로컬 회전

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 크기입니다.")] // Inspector 외형 크기 설명
    [SerializeField] private Vector3 visualLocalScale = Vector3.one; // 생성 외형 로컬 크기

    [Header("Anchor Defaults")] // 표준 기준점 기본 위치 묶음
    [Tooltip("새 InteractionPoint를 생성할 때 적용할 로컬 위치입니다.")] // Inspector 상호작용 위치 기본값 설명
    [SerializeField] private Vector3 defaultInteractionPointPosition = new Vector3(0f, 1f, 0f); // 상호작용 기준점 기본 위치

    [Tooltip("새 EffectOrigin을 생성할 때 적용할 로컬 위치입니다.")] // Inspector 효과 위치 기본값 설명
    [SerializeField] private Vector3 defaultEffectOriginPosition = new Vector3(0f, 1f, 0f); // 효과 기준점 기본 위치

    [Tooltip("새 UIAnchor를 생성할 때 적용할 로컬 위치입니다.")] // Inspector UI 위치 기본값 설명
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

    [ContextMenu("Rebuild Configured Visual")] // Inspector 설정 외형 재생성 메뉴
    public void RebuildConfiguredVisual() // VisualInstance 아래 외형을 Prefab 또는 임시 Primitive로 다시 생성
    {
        EnsureStandardStructure(); // 외형 생성 전 표준 자식 구조 보장
        ClearVisualInstance(); // 기존 VisualInstance 자식 외형 제거

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
            ApplyPlaceholderMaterial(activeVisualObject); // 임시 외형 Material 적용
        }
        else // Prefab과 임시 외형을 모두 사용하지 않는 경우
        {
            activeVisualObject = null; // 현재 생성 외형 참조 초기화
            return; // 외형 생성 처리 종료
        }

        ApplyVisualTransform(activeVisualObject.transform); // 생성 외형 로컬 Transform 적용

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
    public bool ValidateVisualStructure() // 현재 Root의 표준 Visual 구조를 검사하고 결과 출력
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
            Debug.LogError($"{name}의 Visual 참조는 Root 자신이 아닌 자식 Transform이어야 합니다.", this); // 잘못된 Visual 참조 오류 출력
            return false; // Visual 구조 검증 실패 반환
        }

        Collider[] visualColliders = visualInstanceRoot.GetComponentsInChildren<Collider>(true); // VisualInstance 내부 Collider 검색

        if (visualColliders.Length > 0) // Visual 내부 Collider 존재 여부 확인
        {
            Debug.LogWarning( // Visual Collider 중복 가능성 경고 출력 시작
                $"{name}의 VisualInstance 안에 Collider가 {visualColliders.Length}개 남아 있습니다. " // 남은 Collider 개수 안내
                + "충돌 판정은 Root Collider에 두는 것을 권장합니다.", // 권장 구조 안내
                this); // 현재 컴포넌트를 Log Context로 지정
        }

        Debug.Log($"{name} Visual 구조 검증 완료", this); // Visual 구조 정상 검증 결과 출력
        return true; // Visual 구조 검증 성공 반환
    }

    public void SetConfiguredVisualPrefab(GameObject visualPrefab, bool rebuildImmediately = true) // 외형 Prefab을 변경하고 선택적으로 즉시 재생성
    {
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

    private void ApplyPlaceholderMaterial(GameObject placeholderObject) // 임시 Primitive 전체 Renderer에 지정 Material 적용
    {
        if (placeholderObject == null || placeholderMaterial == null) // 임시 외형과 Material 존재 여부 확인
        {
            return; // 임시 Material 적용 처리 종료
        }

        Renderer[] renderers = placeholderObject.GetComponentsInChildren<Renderer>(true); // 임시 외형 전체 Renderer 검색

        for (int index = 0; index < renderers.Length; index++) // 임시 외형 Renderer 전체 순회
        {
            Renderer currentRenderer = renderers[index]; // 현재 Renderer 가져오기

            if (currentRenderer != null) // Renderer 참조 존재 여부 확인
            {
                currentRenderer.sharedMaterial = placeholderMaterial; // 지정 임시 Material 적용
            }
        }
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
