using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(-500)] // Registry 초기화 이후 다른 콘텐츠 로직보다 먼저 Profile 적용
[DisallowMultipleComponent] // 동일 Binder 컴포넌트 중복 방지
[RequireComponent(typeof(ContentVisualRoot))] // ContentVisualRoot 필수 구성 보장
public sealed class ContentVisualProfileBinder : MonoBehaviour // Visual Profile을 ContentVisualRoot에 연결하는 적용 관리자
{
    [Header("References")] // Profile 적용 참조 묶음
    [Tooltip("Profile 설정을 적용할 현재 Root의 ContentVisualRoot입니다.")] // Inspector Visual Root 설명
    [SerializeField] private ContentVisualRoot visualRoot; // Profile 적용 대상 Visual Root

    [Tooltip("직접 연결하여 적용할 ContentVisualProfile Asset입니다.")] // Inspector 직접 Profile 설명
    [SerializeField] private ContentVisualProfile visualProfile; // 직접 연결 Visual Profile

    [Header("Registry Resolve")] // Registry ID 검색 설정 묶음
    [Tooltip("직접 참조 대신 GameDataRegistry에서 Profile ID로 검색합니다.")] // Inspector Registry 검색 사용 설명
    [SerializeField] private bool resolveFromRegistryById; // Registry ID 검색 사용 여부

    [Tooltip("Registry에서 검색할 Visual Profile ID입니다.")] // Inspector Registry Profile ID 설명
    [SerializeField] private string visualProfileId = string.Empty; // Registry 검색 Visual Profile ID

    [Header("Apply Rules")] // Profile 적용 시점과 처리 규칙 묶음
    [Tooltip("Awake 시 연결된 Profile을 자동으로 적용합니다.")] // Inspector 자동 적용 설명
    [SerializeField] private bool applyOnAwake = true; // Awake 자동 Profile 적용 여부

    [Tooltip("Profile을 적용할 때 VisualInstance 외형을 다시 생성합니다.")] // Inspector 외형 재생성 설명
    [SerializeField] private bool rebuildVisualWhenApplied = true; // Profile 적용 시 외형 재생성 여부

    [Tooltip("Profile 적용 후 Root에 남은 기존 Renderer를 비활성화합니다.")] // Inspector 기존 Renderer 비활성화 설명
    [SerializeField] private bool disableLegacyRootRenderersAfterApply = true; // Profile 적용 후 Root Renderer 비활성화 여부

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("마지막으로 정상 적용된 Visual Profile ID입니다.")] // Inspector 마지막 적용 ID 설명
    [SerializeField] private string appliedProfileId = string.Empty; // 마지막 적용 Profile ID

    [Tooltip("마지막 Profile 적용 성공 여부입니다.")] // Inspector 마지막 적용 성공 설명
    [SerializeField] private bool lastApplySucceeded; // 마지막 Profile 적용 성공 여부

    public ContentVisualRoot VisualRoot => visualRoot; // Profile 적용 대상 Visual Root 제공
    public ContentVisualProfile VisualProfile => visualProfile; // 직접 연결 Profile 제공
    public string AppliedProfileId => appliedProfileId; // 마지막 적용 Profile ID 제공
    public bool LastApplySucceeded => lastApplySucceeded; // 마지막 적용 성공 여부 제공

    private void Reset() // 컴포넌트 최초 추가 시 기본 참조 연결
    {
        visualRoot = GetComponent<ContentVisualRoot>(); // 현재 Root의 ContentVisualRoot 자동 연결
    }

    private void Awake() // 실행 시작 시 선택적 Profile 자동 적용
    {
        ResolveVisualRootReference(); // ContentVisualRoot 참조 보장

        if (applyOnAwake) // Awake 자동 적용 설정 확인
        {
            ApplyAssignedProfile(); // 연결 또는 Registry Profile 적용
        }
    }

    private void OnValidate() // Inspector 입력값 검증
    {
        ResolveVisualRootReference(); // ContentVisualRoot 참조 보장
        visualProfileId = string.IsNullOrWhiteSpace(visualProfileId) // Registry ID 입력 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 적용
            : visualProfileId.Trim(); // 입력이 있으면 양쪽 공백 제거
    }

    [ContextMenu("Apply Assigned Profile")] // Inspector 연결 Profile 적용 메뉴
    public bool ApplyAssignedProfile() // 직접 참조 또는 Registry ID로 Profile을 찾아 현재 Root에 적용
    {
        ResolveVisualRootReference(); // ContentVisualRoot 참조 보장

        if (visualRoot == null) // 적용 대상 Visual Root 존재 여부 확인
        {
            Debug.LogError($"{name}에 ContentVisualRoot가 없습니다.", this); // Visual Root 누락 오류 출력
            lastApplySucceeded = false; // 적용 실패 상태 저장
            return false; // Profile 적용 실패 반환
        }

        ContentVisualProfile resolvedProfile = ResolveProfile(); // 현재 설정 방식에 맞는 Profile 검색

        if (resolvedProfile == null) // 적용 가능한 Profile 존재 여부 확인
        {
            lastApplySucceeded = false; // 적용 실패 상태 저장
            appliedProfileId = string.Empty; // 마지막 적용 ID 초기화
            return false; // Profile 적용 실패 반환
        }

        if (!GameDataRegistry.IsValidContentId(resolvedProfile.ProfileId)) // Profile ID 공통 규칙 검사
        {
            Debug.LogError( // 잘못된 Profile ID 오류 출력 시작
                $"{resolvedProfile.name}의 Profile ID가 올바르지 않습니다. " // 오류 Profile 안내
                + $"ID: '{resolvedProfile.ProfileId}'", // 현재 Profile ID 추가
                resolvedProfile); // Profile Asset을 Log Context로 지정
            lastApplySucceeded = false; // 마지막 적용 실패 상태 저장
            appliedProfileId = string.Empty; // 마지막 적용 Profile ID 초기화
            return false; // 잘못된 Profile 적용 중단
        }

        if (!resolvedProfile.HasVisualSource) // 실제 또는 임시 외형 생성 가능 여부 확인
        {
            Debug.LogError( // 외형 생성 정보 누락 오류 출력 시작
                $"{resolvedProfile.name}에 Visual Prefab이 없고 " // 실제 Prefab 누락 안내
                + "Placeholder 생성도 비활성화되어 있습니다.", // 임시 외형 비활성 안내
                resolvedProfile); // Profile Asset을 Log Context로 지정
            lastApplySucceeded = false; // 마지막 적용 실패 상태 저장
            appliedProfileId = string.Empty; // 마지막 적용 Profile ID 초기화
            return false; // 외형 정보가 없는 Profile 적용 중단
        }
        lastApplySucceeded = visualRoot.ApplyProfile(resolvedProfile, rebuildVisualWhenApplied); // Visual Root에 Profile 설정 적용

        if (!lastApplySucceeded) // Visual Root Profile 적용 실패 여부 확인
        {
            appliedProfileId = string.Empty; // 마지막 적용 ID 초기화
            return false; // Profile 적용 실패 반환
        }

        visualProfile = resolvedProfile; // 실제 적용된 Profile 참조 저장
        visualProfileId = resolvedProfile.ProfileId; // 실제 적용된 Profile ID 저장
        appliedProfileId = resolvedProfile.ProfileId; // 마지막 적용 Profile ID 저장

        if (disableLegacyRootRenderersAfterApply) // 기존 Root Renderer 비활성화 설정 확인
        {
            visualRoot.DisableLegacyRootRenderers(); // Root에 남은 기존 Renderer 비활성화
        }

        Debug.Log( // Profile 적용 결과 출력 시작
            $"{name} Visual Profile 적용 완료 / " // 현재 Root와 적용 완료 안내
            + $"ID: {resolvedProfile.ProfileId} / " // 적용 Profile ID 추가
            + $"Profile: {resolvedProfile.name}", // 적용 Profile Asset 이름 추가
            this); // 현재 Binder를 Log Context로 지정
        return true; // Profile 적용 성공 반환
    }

    [ContextMenu("Validate Assigned Profile")] // Inspector 연결 Profile 검증 메뉴
    public bool ValidateAssignedProfile() // Profile 연결과 Visual Root 구조 유효성 검사
    {
        ResolveVisualRootReference(); // ContentVisualRoot 참조 보장
        ContentVisualProfile resolvedProfile = ResolveProfile(); // 현재 설정 방식에 맞는 Profile 검색

        if (visualRoot == null || resolvedProfile == null) // 필수 참조 존재 여부 확인
        {
            return false; // Binder 검증 실패 반환
        }

        bool isProfileIdValid = GameDataRegistry.IsValidContentId(resolvedProfile.ProfileId); // Profile ID 공통 규칙 검사

        if (!isProfileIdValid) // Profile ID 규칙 오류 여부 확인
        {
            Debug.LogError( // 잘못된 Profile ID 오류 출력 시작
                $"{resolvedProfile.name}의 Profile ID가 올바르지 않습니다. " // 오류 Profile 안내
                + $"ID: '{resolvedProfile.ProfileId}'", // 현재 ID 정보 추가
                resolvedProfile); // Profile Asset을 Log Context로 지정
        }

        if (!resolvedProfile.HasVisualSource) // 실제 또는 임시 외형 생성 가능 여부 확인
        {
            Debug.LogWarning($"{resolvedProfile.name}에는 Visual Prefab과 임시 외형 설정이 없습니다.", resolvedProfile); // 외형 생성 정보 누락 경고 출력
        }

        bool isStructureValid = visualRoot.ValidateVisualStructure(); // ContentVisualRoot 표준 구조 검사
        bool isValid = isProfileIdValid && isStructureValid; // Binder 전체 검증 결과 계산

        if (isValid) // 전체 Binder 설정 정상 여부 확인
        {
            Debug.Log($"{name} Visual Profile 연결 검증 완료", this); // Binder 정상 검증 결과 출력
        }

        return isValid; // Binder 전체 검증 결과 반환
    }

    public bool ApplyProfileById(string profileId) // Runtime Registry에서 지정 ID Profile을 찾아 적용
    {
        resolveFromRegistryById = true; // Registry ID 검색 방식 활성화
        visualProfileId = string.IsNullOrWhiteSpace(profileId) // 전달 ID 입력 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 적용
            : profileId.Trim(); // 입력이 있으면 양쪽 공백 제거
        return ApplyAssignedProfile(); // 지정 Registry ID Profile 적용 결과 반환
    }

    private ContentVisualProfile ResolveProfile() // 직접 참조 또는 Registry ID를 사용하여 적용 Profile 검색
    {
        if (!resolveFromRegistryById) // 직접 Profile 참조 방식인지 확인
        {
            if (visualProfile == null) // 직접 Profile 연결 여부 확인
            {
                Debug.LogError($"{name}에 ContentVisualProfile을 연결해야 합니다.", this); // 직접 Profile 누락 오류 출력
            }

            return visualProfile; // 직접 연결 Profile 반환
        }

        if (string.IsNullOrWhiteSpace(visualProfileId)) // Registry 검색 ID 입력 여부 확인
        {
            Debug.LogError($"{name}에 Registry 검색용 Visual Profile ID를 입력해야 합니다.", this); // Registry ID 누락 오류 출력
            return null; // Registry Profile 검색 실패 반환
        }

        if (!GameDataRegistryRuntime.HasInstance || GameDataRegistryRuntime.Instance.Registry == null) // Registry Runtime 준비 여부 확인
        {
            Debug.LogError($"{name}에서 Visual Profile을 검색할 GameDataRegistryRuntime이 없습니다.", this); // Registry Runtime 누락 오류 출력
            return null; // Registry Profile 검색 실패 반환
        }

        if (!GameDataRegistryRuntime.Instance.TryGetVisualProfile(visualProfileId, out ContentVisualProfile resolvedProfile)) // Registry Profile 검색 성공 여부 확인
        {
            Debug.LogError($"{name}에서 Visual Profile ID를 찾지 못했습니다. ID: {visualProfileId}", this); // Registry Profile 검색 실패 오류 출력
            return null; // Registry Profile 검색 실패 반환
        }

        return resolvedProfile; // Registry에서 검색된 Profile 반환
    }

    private void ResolveVisualRootReference() // 현재 Root의 ContentVisualRoot 참조 보장
    {
        if (visualRoot == null) // 저장된 Visual Root 참조 존재 여부 확인
        {
            visualRoot = GetComponent<ContentVisualRoot>(); // 현재 Root에서 ContentVisualRoot 검색
        }
    }
}
