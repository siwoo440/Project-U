using UnityEngine; // Unity 기본 기능

public enum ContentVisualIdentityCategory // 콘텐츠 ID를 Visual Profile ID로 변환할 대상 분류
{
    Item = 0, // 일반 아이템
    Weapon = 1, // 무기와 도구
    Enemy = 2, // 적
    Buildable = 3, // 건축물
    Resource = 4, // 채집 자원
    Other = 5 // 자동 규칙을 사용하지 않는 기타 콘텐츠
}

[DisallowMultipleComponent] // 동일 기능 Root의 Identity 중복 추가 방지
public sealed class ContentVisualIdentity : MonoBehaviour // 기능 Root의 콘텐츠 ID와 Visual Profile 연결 규칙을 보관
{
    [Header("Content Identity")] // 콘텐츠 식별 정보 묶음
    [Tooltip("이 기능 Root가 나타내는 콘텐츠 종류입니다.")] // Inspector 콘텐츠 종류 설명
    [SerializeField] private ContentVisualIdentityCategory category = ContentVisualIdentityCategory.Item; // 콘텐츠 종류

    [Tooltip("ItemData, EnemyCombatData 또는 BuildRecipeData에서 사용하는 공통 콘텐츠 ID입니다.")] // Inspector 콘텐츠 ID 설명
    [SerializeField] private string contentId = string.Empty; // 원본 콘텐츠 데이터 ID

    [Header("Visual Profile Override")] // Profile ID 예외 처리 묶음
    [Tooltip("자동 변환 규칙 대신 직접 입력한 Visual Profile ID를 사용합니다.")] // Inspector 명시적 Profile ID 사용 설명
    [SerializeField] private bool useExplicitVisualProfileId; // 명시적 Visual Profile ID 사용 여부

    [Tooltip("자동 규칙을 사용할 수 없는 콘텐츠에 직접 지정할 Visual Profile ID입니다.")] // Inspector 명시적 Profile ID 설명
    [SerializeField] private string explicitVisualProfileId = string.Empty; // 직접 지정 Visual Profile ID

    [Header("Preview")] // 계산 결과 확인 묶음
    [Tooltip("현재 설정으로 계산된 Visual Profile ID입니다.")] // Inspector 계산 결과 설명
    [SerializeField] private string resolvedVisualProfileId = string.Empty; // 현재 계산된 Visual Profile ID

    [Tooltip("현재 Identity 설정이 유효한지 표시합니다.")] // Inspector Identity 유효성 설명
    [SerializeField] private bool isIdentityValid; // 현재 Identity 유효성 여부

    public ContentVisualIdentityCategory Category => category; // 콘텐츠 종류 제공
    public string ContentId => contentId; // 원본 콘텐츠 ID 제공
    public bool UseExplicitVisualProfileId => useExplicitVisualProfileId; // 명시적 Profile ID 사용 여부 제공
    public string ExplicitVisualProfileId => explicitVisualProfileId; // 명시적 Profile ID 제공
    public string ResolvedVisualProfileId => resolvedVisualProfileId; // 계산된 Profile ID 제공
    public bool IsIdentityValid => isIdentityValid; // Identity 유효성 제공

    private void Reset() // 컴포넌트 최초 추가 시 기본 계산 상태 설정
    {
        RefreshResolvedProfileId(); // 현재 기본값으로 Profile ID 계산
    }

    private void OnValidate() // Inspector 입력값 정리와 Profile ID 미리보기 갱신
    {
        contentId = NormalizeId(contentId); // 콘텐츠 ID 양쪽 공백 제거
        explicitVisualProfileId = NormalizeId(explicitVisualProfileId); // 명시적 Profile ID 양쪽 공백 제거
        RefreshResolvedProfileId(); // Inspector 변경 후 Profile ID 계산 결과 갱신
    }

    [ContextMenu("Refresh Resolved Visual Profile ID")] // Inspector Profile ID 계산 메뉴
    public bool RefreshResolvedProfileId() // 현재 Identity 설정으로 Visual Profile ID를 계산
    {
        isIdentityValid = TryGetVisualProfileId(out string profileId, out _); // Identity 설정 유효성과 계산 결과 확인
        resolvedVisualProfileId = isIdentityValid // 계산 성공 여부 확인
            ? profileId // 계산 성공 시 Profile ID 저장
            : string.Empty; // 계산 실패 시 미리보기 초기화
        return isIdentityValid; // Identity 계산 성공 여부 반환
    }

    [ContextMenu("Validate Content Visual Identity")] // Inspector Identity 검증 메뉴
    public bool ValidateIdentity() // 콘텐츠 ID와 계산된 Profile ID를 검사하고 결과 출력
    {
        bool isValid = TryGetVisualProfileId(out string profileId, out string errorMessage); // 현재 Identity 전체 검사
        isIdentityValid = isValid; // 검사 결과 저장
        resolvedVisualProfileId = isValid // 검사 성공 여부 확인
            ? profileId // 검사 성공 시 계산 Profile ID 저장
            : string.Empty; // 검사 실패 시 계산 Profile ID 초기화

        if (!isValid) // Identity 검사 실패 여부 확인
        {
            Debug.LogError($"{name} Content Visual Identity 오류 / {errorMessage}", this); // Identity 오류 원인 출력
            return false; // Identity 검사 실패 반환
        }

        Debug.Log( // Identity 정상 결과 출력 시작
            $"{name} Content Visual Identity 검증 완료 / " // 현재 Root와 검증 완료 안내
            + $"Content ID: {contentId} / " // 원본 콘텐츠 ID 추가
            + $"Visual Profile ID: {profileId}", // 계산된 Visual Profile ID 추가
            this); // 현재 Identity를 Log Context로 지정
        return true; // Identity 검사 성공 반환
    }

    public bool TryGetVisualProfileId(out string profileId, out string errorMessage) // 현재 설정으로 Visual Profile ID 계산 시도
    {
        return ContentVisualProfileIdUtility.TryBuildProfileId( // 공통 Profile ID 변환 기능 호출
            category, // 현재 콘텐츠 종류 전달
            contentId, // 현재 원본 콘텐츠 ID 전달
            useExplicitVisualProfileId, // 명시적 Profile ID 사용 여부 전달
            explicitVisualProfileId, // 명시적 Profile ID 전달
            out profileId, // 계산된 Profile ID 반환
            out errorMessage); // 계산 실패 원인 반환
    }

    public void Configure( // Editor 도구 또는 생성 시스템에서 Identity 설정
        ContentVisualIdentityCategory targetCategory, // 적용할 콘텐츠 종류
        string targetContentId, // 적용할 원본 콘텐츠 ID
        bool useExplicitProfileId = false, // 명시적 Profile ID 사용 여부
        string targetExplicitProfileId = "") // 적용할 명시적 Profile ID
    {
        category = targetCategory; // 콘텐츠 종류 적용
        contentId = NormalizeId(targetContentId); // 정리된 콘텐츠 ID 적용
        useExplicitVisualProfileId = useExplicitProfileId; // 명시적 Profile ID 사용 여부 적용
        explicitVisualProfileId = NormalizeId(targetExplicitProfileId); // 정리된 명시적 Profile ID 적용
        RefreshResolvedProfileId(); // 변경된 설정으로 Profile ID 계산 결과 갱신
    }

    private static string NormalizeId(string value) // ID 입력값의 양쪽 공백 제거
    {
        return string.IsNullOrWhiteSpace(value) // 입력값 존재 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 반환
            : value.Trim(); // 입력이 있으면 양쪽 공백 제거 후 반환
    }
}
