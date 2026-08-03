using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(-500)] // Registry 초기화 이후 다른 콘텐츠 로직보다 먼저 Profile 적용
[DisallowMultipleComponent] // 동일 Binder 컴포넌트 중복 방지
[RequireComponent(typeof(ContentVisualRoot))] // ContentVisualRoot 필수 구성 보장
public sealed class ContentVisualProfileBinder : MonoBehaviour // Visual Profile을 ContentVisualRoot에 연결하는 적용 관리자
{
    [Header("References")] // Profile 적용 참조 묶음
    [Tooltip("Profile 설정을 적용할 현재 Root의 ContentVisualRoot입니다.")] // Inspector Visual Root 설명
    [SerializeField] private ContentVisualRoot visualRoot; // Profile 적용 대상 Visual Root

    [Tooltip("직접 연결하거나 Identity 검색 결과로 캐시한 ContentVisualProfile Asset입니다.")] // Inspector Profile 설명
    [SerializeField] private ContentVisualProfile visualProfile; // 직접 연결 또는 자동 검색된 Visual Profile

    [Header("Content Identity Resolve")] // 콘텐츠 Identity 자동 연결 설정 묶음
    [Tooltip("ContentVisualIdentity의 콘텐츠 ID를 사용해 Visual Profile ID를 자동 계산합니다.")] // Inspector Identity 자동 연결 설명
    [SerializeField] private bool resolveFromContentIdentity; // Content Identity 자동 연결 사용 여부

    [Tooltip("Visual Profile ID 계산에 사용할 현재 Root의 ContentVisualIdentity입니다.")] // Inspector Content Identity 참조 설명
    [SerializeField] private ContentVisualIdentity contentIdentity; // Profile ID 계산에 사용할 콘텐츠 Identity

    [Header("Registry Resolve")] // Registry ID 검색 설정 묶음
    [Tooltip("직접 참조 대신 GameDataRegistry에서 Profile ID로 검색합니다. Identity 모드가 우선 적용됩니다.")] // Inspector Registry 검색 사용 설명
    [SerializeField] private bool resolveFromRegistryById; // Registry ID 검색 사용 여부

    [Tooltip("Registry에서 검색하거나 Identity에서 계산한 Visual Profile ID입니다.")] // Inspector Registry Profile ID 설명
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
    public ContentVisualProfile VisualProfile => visualProfile; // 직접 연결 또는 캐시된 Profile 제공
    public ContentVisualIdentity ContentIdentity => contentIdentity; // 연결된 Content Identity 제공
    public bool ResolveFromContentIdentity => resolveFromContentIdentity; // Identity 자동 연결 사용 여부 제공
    public bool ResolveFromRegistryById => resolveFromRegistryById; // Registry ID 검색 사용 여부 제공
    public string VisualProfileId => visualProfileId; // 현재 검색 대상 Profile ID 제공
    public string AppliedProfileId => appliedProfileId; // 마지막 적용 Profile ID 제공
    public bool LastApplySucceeded => lastApplySucceeded; // 마지막 적용 성공 여부 제공

    private void Reset() // 컴포넌트 최초 추가 시 기본 참조 연결
    {
        ResolveComponentReferences(); // 현재 Root의 필수 컴포넌트 참조 연결
    }

    private void Awake() // 실행 시작 시 선택적 Profile 자동 적용
    {
        ResolveComponentReferences(); // ContentVisualRoot와 Identity 참조 보장

        if (applyOnAwake) // Awake 자동 적용 설정 확인
        {
            ApplyAssignedProfile(); // 현재 연결 방식에 맞는 Profile 적용
        }
    }

    private void OnValidate() // Inspector 입력값 검증과 Identity 계산 결과 갱신
    {
        ResolveComponentReferences(); // ContentVisualRoot와 Identity 참조 보장
        visualProfileId = NormalizeId(visualProfileId); // Registry 검색 ID 양쪽 공백 제거

        if (resolveFromContentIdentity // Identity 자동 연결 사용 여부 확인
            && contentIdentity != null // Content Identity 연결 여부 확인
            && contentIdentity.TryGetVisualProfileId(out string identityProfileId, out _)) // Identity Profile ID 계산 성공 여부 확인
        {
            visualProfileId = identityProfileId; // 계산된 Profile ID를 Inspector 검색 ID에 반영
        }
    }

    [ContextMenu("Apply Assigned Profile")] // Inspector 연결 Profile 적용 메뉴
    public bool ApplyAssignedProfile() // 직접 참조, Registry ID 또는 Content Identity Profile 적용
    {
        ResolveComponentReferences(); // ContentVisualRoot와 Identity 참조 보장

        if (visualRoot == null) // 적용 대상 Visual Root 존재 여부 확인
        {
            Debug.LogError($"{name}에 ContentVisualRoot가 없습니다.", this); // Visual Root 누락 오류 출력
            SetApplyFailureState(); // Profile 적용 실패 상태 저장
            return false; // Profile 적용 실패 반환
        }

        ContentVisualProfile resolvedProfile = ResolveProfile(); // 현재 설정 방식에 맞는 Profile 검색

        if (resolvedProfile == null) // 적용 가능한 Profile 존재 여부 확인
        {
            SetApplyFailureState(); // Profile 적용 실패 상태 저장
            return false; // Profile 적용 실패 반환
        }

        if (!ValidateResolvedProfile(resolvedProfile)) // 검색된 Profile 자체 유효성 확인
        {
            SetApplyFailureState(); // Profile 적용 실패 상태 저장
            return false; // 잘못된 Profile 적용 중단
        }

        lastApplySucceeded = visualRoot.ApplyProfile(resolvedProfile, rebuildVisualWhenApplied); // Visual Root에 Profile 설정 적용

        if (!lastApplySucceeded) // Visual Root Profile 적용 실패 여부 확인
        {
            appliedProfileId = string.Empty; // 마지막 적용 ID 초기화
            return false; // Profile 적용 실패 반환
        }

        visualProfile = resolvedProfile; // 실제 적용된 Profile을 캐시 참조로 저장
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
    public bool ValidateAssignedProfile() // Identity와 Profile 연결 및 Visual Root 구조 검사
    {
        ResolveComponentReferences(); // ContentVisualRoot와 Identity 참조 보장
        bool isIdentityValid = ValidateIdentitySettings(); // Identity 또는 Registry 검색 설정 검사
        ContentVisualProfile resolvedProfile = ResolveProfile(); // 현재 설정 방식에 맞는 Profile 검색

        if (visualRoot == null || resolvedProfile == null) // 필수 참조 존재 여부 확인
        {
            return false; // Binder 검증 실패 반환
        }

        bool isProfileValid = ValidateResolvedProfile(resolvedProfile); // 검색된 Profile 자체 검사
        bool isStructureValid = visualRoot.ValidateVisualStructure(); // ContentVisualRoot 표준 구조 검사
        bool isValid = isIdentityValid && isProfileValid && isStructureValid; // Binder 전체 검증 결과 계산

        if (isValid) // 전체 Binder 설정 정상 여부 확인
        {
            Debug.Log( // Binder 정상 검증 결과 출력 시작
                $"{name} Visual Profile 연결 검증 완료 / " // 현재 Root와 검증 완료 안내
                + $"ID: {resolvedProfile.ProfileId}", // 최종 Profile ID 추가
                this); // 현재 Binder를 Log Context로 지정
        }

        return isValid; // Binder 전체 검증 결과 반환
    }

    public bool ApplyProfileById(string profileId) // Runtime Registry에서 지정 ID Profile을 찾아 적용
    {
        resolveFromContentIdentity = false; // 직접 Profile ID 검색을 위해 Identity 모드 비활성화
        resolveFromRegistryById = true; // Registry ID 검색 방식 활성화
        visualProfileId = NormalizeId(profileId); // 정리된 Registry 검색 Profile ID 저장
        return ApplyAssignedProfile(); // 지정 Registry ID Profile 적용 결과 반환
    }

    public bool ApplyProfileFromContentIdentity() // 현재 Content Identity에서 Profile ID를 계산하여 적용
    {
        resolveFromContentIdentity = true; // Content Identity 자동 연결 모드 활성화
        return ApplyAssignedProfile(); // Identity 계산 결과 Profile 적용
    }

    public bool TryGetRequestedProfileId(out string requestedProfileId, out string errorMessage) // 현재 연결 방식이 요구하는 최종 Profile ID 계산
    {
        requestedProfileId = string.Empty; // 최종 Profile ID 기본값 초기화
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (resolveFromContentIdentity) // Content Identity 자동 연결 모드 확인
        {
            ResolveComponentReferences(); // Content Identity 참조 보장

            if (contentIdentity == null) // Content Identity 존재 여부 확인
            {
                errorMessage = "Content Identity 자동 연결이 켜져 있지만 ContentVisualIdentity가 없습니다."; // Identity 누락 오류 설정
                return false; // Profile ID 계산 실패 반환
            }

            if (!contentIdentity.TryGetVisualProfileId(out requestedProfileId, out errorMessage)) // Identity Profile ID 계산 성공 여부 확인
            {
                return false; // Identity 계산 실패 반환
            }

            return true; // Identity Profile ID 계산 성공 반환
        }

        if (resolveFromRegistryById) // Registry ID 직접 검색 모드 확인
        {
            requestedProfileId = NormalizeId(visualProfileId); // 정리된 Registry Profile ID 적용

            if (!GameDataRegistry.IsValidContentId(requestedProfileId)) // Registry 검색 ID 공통 규칙 검사
            {
                errorMessage = $"Registry 검색용 Visual Profile ID가 올바르지 않습니다. ID: '{requestedProfileId}'"; // Registry ID 오류 설정
                return false; // Profile ID 계산 실패 반환
            }

            return true; // Registry Profile ID 확인 성공 반환
        }

        if (visualProfile == null) // 직접 Profile 참조 존재 여부 확인
        {
            errorMessage = "직접 적용할 ContentVisualProfile이 연결되지 않았습니다."; // 직접 Profile 누락 오류 설정
            return false; // Profile ID 확인 실패 반환
        }

        requestedProfileId = NormalizeId(visualProfile.ProfileId); // 직접 Profile의 ID 적용
        return true; // 직접 Profile ID 확인 성공 반환
    }

    public void CacheResolvedProfile(ContentVisualProfile resolvedProfile) // Editor 자동 검색 결과를 Binder에 캐시
    {
        visualProfile = resolvedProfile; // 자동 검색된 Profile 참조 저장
        visualProfileId = resolvedProfile == null // 검색 Profile 존재 여부 확인
            ? string.Empty // Profile이 없으면 검색 ID 초기화
            : resolvedProfile.ProfileId; // Profile이 있으면 해당 ID 저장
    }

    public void SetContentIdentityMode(bool enabled) // Editor 설정 도구에서 Identity 모드 변경
    {
        resolveFromContentIdentity = enabled; // Content Identity 자동 연결 사용 여부 적용

        if (enabled) // Identity 모드 활성화 여부 확인
        {
            resolveFromRegistryById = false; // 중복 검색 모드를 막기 위해 직접 Registry ID 모드 비활성화
        }
    }

    private ContentVisualProfile ResolveProfile() // 현재 연결 방식으로 적용할 ContentVisualProfile 검색
    {
        if (!TryGetRequestedProfileId(out string requestedProfileId, out string errorMessage)) // 최종 Profile ID 계산 성공 여부 확인
        {
            Debug.LogError($"{name} Visual Profile 검색 설정 오류 / {errorMessage}", this); // Profile ID 계산 오류 출력
            return null; // Profile 검색 실패 반환
        }

        visualProfileId = requestedProfileId; // 현재 최종 검색 Profile ID 갱신

        if (!resolveFromContentIdentity && !resolveFromRegistryById) // 직접 Profile 참조 모드인지 확인
        {
            return visualProfile; // 직접 연결 Profile 반환
        }

        if (visualProfile != null // 캐시된 Profile 존재 여부 확인
            && visualProfile.ProfileId == requestedProfileId) // 캐시 Profile ID와 최종 요청 ID 일치 여부 확인
        {
            return visualProfile; // 일치하는 캐시 Profile 반환
        }

        if (!GameDataRegistryRuntime.HasInstance || GameDataRegistryRuntime.Instance.Registry == null) // Registry Runtime 준비 여부 확인
        {
            Debug.LogError( // Registry Runtime 누락 오류 출력 시작
                $"{name}에서 Visual Profile ID '{requestedProfileId}'를 검색할 " // 현재 검색 ID 안내
                + "GameDataRegistryRuntime이 없습니다. " // Registry Runtime 누락 안내
                + "Prefab Mode에서는 Resolve Identity Profile From Registry 버튼으로 Profile을 캐시하세요.", // Edit Mode 해결 방법 안내
                this); // 현재 Binder를 Log Context로 지정
            return null; // Registry Profile 검색 실패 반환
        }

        if (!GameDataRegistryRuntime.Instance.TryGetVisualProfile(requestedProfileId, out ContentVisualProfile resolvedProfile)) // Registry Profile 검색 성공 여부 확인
        {
            Debug.LogError($"{name}에서 Visual Profile ID를 찾지 못했습니다. ID: {requestedProfileId}", this); // Registry Profile 검색 실패 오류 출력
            return null; // Registry Profile 검색 실패 반환
        }

        return resolvedProfile; // Registry에서 검색된 Profile 반환
    }

    private bool ValidateIdentitySettings() // 현재 Binder의 Identity 또는 Registry 검색 설정 검사
    {
        if (!TryGetRequestedProfileId(out string requestedProfileId, out string errorMessage)) // 최종 Profile ID 계산 성공 여부 확인
        {
            Debug.LogError($"{name} Visual Profile 연결 설정 오류 / {errorMessage}", this); // 연결 설정 오류 출력
            return false; // Binder 검색 설정 검증 실패 반환
        }

        if (!requestedProfileId.StartsWith("visual_", System.StringComparison.Ordinal)) // Visual Profile 권장 접두사 확인
        {
            Debug.LogError($"{name}의 최종 Profile ID는 'visual_'로 시작해야 합니다. ID: {requestedProfileId}", this); // Profile 접두사 오류 출력
            return false; // Binder 검색 설정 검증 실패 반환
        }

        return true; // Binder 검색 설정 검증 성공 반환
    }

    private bool ValidateResolvedProfile(ContentVisualProfile resolvedProfile) // 검색된 Profile ID와 외형 생성 정보 검사
    {
        if (resolvedProfile == null) // Profile 존재 여부 확인
        {
            return false; // Profile 검증 실패 반환
        }

        if (!GameDataRegistry.IsValidContentId(resolvedProfile.ProfileId)) // Profile ID 공통 규칙 검사
        {
            Debug.LogError( // 잘못된 Profile ID 오류 출력 시작
                $"{resolvedProfile.name}의 Profile ID가 올바르지 않습니다. " // 오류 Profile 안내
                + $"ID: '{resolvedProfile.ProfileId}'", // 현재 Profile ID 추가
                resolvedProfile); // Profile Asset을 Log Context로 지정
            return false; // Profile 검증 실패 반환
        }

        if (!resolvedProfile.HasVisualSource) // 실제 또는 임시 외형 생성 가능 여부 확인
        {
            Debug.LogError( // 외형 생성 정보 누락 오류 출력 시작
                $"{resolvedProfile.name}에 Visual Prefab이 없고 " // 실제 Prefab 누락 안내
                + "Placeholder 생성도 비활성화되어 있습니다.", // 임시 외형 비활성 안내
                resolvedProfile); // Profile Asset을 Log Context로 지정
            return false; // Profile 검증 실패 반환
        }

        if (resolveFromContentIdentity // Identity 자동 연결 모드 확인
            && contentIdentity != null // Content Identity 존재 여부 확인
            && contentIdentity.TryGetVisualProfileId(out string identityProfileId, out _) // Identity Profile ID 계산 성공 여부 확인
            && resolvedProfile.ProfileId != identityProfileId) // 계산 ID와 실제 Profile ID 일치 여부 확인
        {
            Debug.LogError( // Identity와 캐시 Profile 불일치 오류 출력 시작
                $"{name}의 Identity 계산 ID와 연결 Profile ID가 다릅니다. " // 오류 원인 안내
                + $"계산 ID: {identityProfileId} / " // Identity 계산 ID 추가
                + $"Profile ID: {resolvedProfile.ProfileId}", // 실제 Profile ID 추가
                this); // 현재 Binder를 Log Context로 지정
            return false; // Profile 검증 실패 반환
        }

        return true; // Profile 검증 성공 반환
    }

    private void SetApplyFailureState() // Profile 적용 실패 실행값 초기화
    {
        lastApplySucceeded = false; // 마지막 적용 성공 상태 해제
        appliedProfileId = string.Empty; // 마지막 적용 Profile ID 초기화
    }

    private void ResolveComponentReferences() // 현재 Root의 Visual Root와 Content Identity 참조 보장
    {
        if (visualRoot == null) // 저장된 Visual Root 참조 존재 여부 확인
        {
            visualRoot = GetComponent<ContentVisualRoot>(); // 현재 Root에서 ContentVisualRoot 검색
        }

        if (contentIdentity == null) // 저장된 Content Identity 참조 존재 여부 확인
        {
            contentIdentity = GetComponent<ContentVisualIdentity>(); // 현재 Root에서 ContentVisualIdentity 검색
        }
    }

    private static string NormalizeId(string value) // ID 입력값 양쪽 공백 제거
    {
        return string.IsNullOrWhiteSpace(value) // 입력값 존재 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 반환
            : value.Trim(); // 입력이 있으면 양쪽 공백 제거 후 반환
    }
}
