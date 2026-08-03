using System; // 문자열 비교 기능

public static class ContentVisualProfileIdUtility // 콘텐츠 ID를 표준 Visual Profile ID로 변환하는 공통 기능
{
    private const string ItemPrefix = "item_"; // 일반 아이템 콘텐츠 ID 접두사
    private const string WeaponPrefix = "weapon_"; // 무기 전용 콘텐츠 ID 접두사
    private const string EnemyPrefix = "enemy_"; // 적 콘텐츠 ID 접두사
    private const string StructurePrefix = "structure_"; // 건축물 콘텐츠 ID 접두사
    private const string ResourcePrefix = "resource_"; // 채집 자원 콘텐츠 ID 접두사
    private const string VisualItemPrefix = "visual_item_"; // 일반 아이템 Visual Profile 접두사
    private const string VisualWeaponPrefix = "visual_weapon_"; // 무기 Visual Profile 접두사
    private const string VisualEnemyPrefix = "visual_enemy_"; // 적 Visual Profile 접두사
    private const string VisualBuildablePrefix = "visual_buildable_"; // 건축물 Visual Profile 접두사
    private const string VisualResourcePrefix = "visual_resource_"; // 채집 자원 Visual Profile 접두사

    public static bool TryBuildProfileId( // 콘텐츠 Identity 설정으로 Visual Profile ID 계산
        ContentVisualIdentityCategory category, // 콘텐츠 종류
        string contentId, // 원본 콘텐츠 ID
        bool useExplicitProfileId, // 명시적 Profile ID 사용 여부
        string explicitProfileId, // 명시적 Visual Profile ID
        out string profileId, // 계산 결과 Profile ID
        out string errorMessage) // 계산 실패 원인
    {
        profileId = string.Empty; // 계산 결과 기본값 초기화
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화
        string normalizedContentId = NormalizeId(contentId); // 원본 콘텐츠 ID 공백 제거
        string normalizedExplicitProfileId = NormalizeId(explicitProfileId); // 명시적 Profile ID 공백 제거

        if (useExplicitProfileId) // 명시적 Profile ID 사용 설정 확인
        {
            if (!GameDataRegistry.IsValidContentId(normalizedExplicitProfileId)) // 명시적 Profile ID 공통 규칙 검사
            {
                errorMessage = $"명시적 Visual Profile ID가 올바르지 않습니다. ID: '{normalizedExplicitProfileId}'"; // 명시적 ID 오류 원인 설정
                return false; // Profile ID 계산 실패 반환
            }

            if (!normalizedExplicitProfileId.StartsWith("visual_", StringComparison.Ordinal)) // Visual Profile 권장 접두사 확인
            {
                errorMessage = $"명시적 Visual Profile ID는 'visual_'로 시작해야 합니다. ID: '{normalizedExplicitProfileId}'"; // 접두사 오류 원인 설정
                return false; // Profile ID 계산 실패 반환
            }

            profileId = normalizedExplicitProfileId; // 검증된 명시적 Profile ID 적용
            return true; // 명시적 Profile ID 계산 성공 반환
        }

        if (!GameDataRegistry.IsValidContentId(normalizedContentId)) // 원본 콘텐츠 ID 공통 규칙 검사
        {
            errorMessage = $"콘텐츠 ID가 올바르지 않습니다. ID: '{normalizedContentId}'"; // 원본 콘텐츠 ID 오류 원인 설정
            return false; // Profile ID 계산 실패 반환
        }

        switch (category) // 콘텐츠 종류에 맞는 변환 규칙 선택
        {
            case ContentVisualIdentityCategory.Item: // 일반 아이템 변환 규칙
                return TryReplacePrefix( // 일반 아이템 접두사 변환 실행
                    normalizedContentId, // 원본 콘텐츠 ID 전달
                    ItemPrefix, // 제거할 item 접두사 전달
                    VisualItemPrefix, // 적용할 Visual Item 접두사 전달
                    out profileId, // 계산 결과 반환
                    out errorMessage); // 계산 오류 반환

            case ContentVisualIdentityCategory.Weapon: // 무기와 도구 변환 규칙
                return TryBuildWeaponProfileId( // Item 또는 Weapon ID를 무기 Profile ID로 변환
                    normalizedContentId, // 원본 콘텐츠 ID 전달
                    out profileId, // 계산 결과 반환
                    out errorMessage); // 계산 오류 반환

            case ContentVisualIdentityCategory.Enemy: // 적 변환 규칙
                return TryReplacePrefix( // 적 접두사 변환 실행
                    normalizedContentId, // 원본 콘텐츠 ID 전달
                    EnemyPrefix, // 제거할 enemy 접두사 전달
                    VisualEnemyPrefix, // 적용할 Visual Enemy 접두사 전달
                    out profileId, // 계산 결과 반환
                    out errorMessage); // 계산 오류 반환

            case ContentVisualIdentityCategory.Buildable: // 건축물 변환 규칙
                return TryReplacePrefix( // 건축물 접두사 변환 실행
                    normalizedContentId, // 원본 콘텐츠 ID 전달
                    StructurePrefix, // 제거할 structure 접두사 전달
                    VisualBuildablePrefix, // 적용할 Visual Buildable 접두사 전달
                    out profileId, // 계산 결과 반환
                    out errorMessage); // 계산 오류 반환

            case ContentVisualIdentityCategory.Resource: // 채집 자원 변환 규칙
                return TryBuildResourceProfileId( // Item 또는 Resource ID를 자원 Profile ID로 변환
                    normalizedContentId, // 원본 콘텐츠 ID 전달
                    out profileId, // 계산 결과 반환
                    out errorMessage); // 계산 오류 반환

            case ContentVisualIdentityCategory.Other: // 자동 변환 규칙이 없는 기타 콘텐츠
                errorMessage = "Other 분류는 명시적 Visual Profile ID를 사용해야 합니다."; // 기타 콘텐츠 오류 원인 설정
                return false; // Profile ID 계산 실패 반환

            default: // 정의되지 않은 콘텐츠 종류
                errorMessage = $"지원하지 않는 ContentVisualIdentityCategory입니다. 값: {category}"; // 지원하지 않는 분류 오류 설정
                return false; // Profile ID 계산 실패 반환
        }
    }

    private static bool TryBuildWeaponProfileId( // 무기 콘텐츠 ID를 Visual Weapon Profile ID로 변환
        string contentId, // 원본 무기 또는 아이템 ID
        out string profileId, // 계산 결과 Profile ID
        out string errorMessage) // 계산 실패 원인
    {
        if (contentId.StartsWith(ItemPrefix, StringComparison.Ordinal)) // 무기가 ItemData ID를 사용하는지 확인
        {
            return TryReplacePrefix( // item 접두사를 visual_weapon 접두사로 변환
                contentId, // 원본 ItemData ID 전달
                ItemPrefix, // 제거할 item 접두사 전달
                VisualWeaponPrefix, // 적용할 visual_weapon 접두사 전달
                out profileId, // 계산 결과 반환
                out errorMessage); // 계산 오류 반환
        }

        return TryReplacePrefix( // weapon 접두사를 visual_weapon 접두사로 변환
            contentId, // 원본 Weapon ID 전달
            WeaponPrefix, // 제거할 weapon 접두사 전달
            VisualWeaponPrefix, // 적용할 visual_weapon 접두사 전달
            out profileId, // 계산 결과 반환
            out errorMessage); // 계산 오류 반환
    }

    private static bool TryBuildResourceProfileId( // 채집 자원 콘텐츠 ID를 Visual Resource Profile ID로 변환
        string contentId, // 원본 자원 또는 아이템 ID
        out string profileId, // 계산 결과 Profile ID
        out string errorMessage) // 계산 실패 원인
    {
        if (contentId.StartsWith(ItemPrefix, StringComparison.Ordinal)) // 자원이 ItemData ID를 사용하는지 확인
        {
            return TryReplacePrefix( // item 접두사를 visual_resource 접두사로 변환
                contentId, // 원본 ItemData ID 전달
                ItemPrefix, // 제거할 item 접두사 전달
                VisualResourcePrefix, // 적용할 visual_resource 접두사 전달
                out profileId, // 계산 결과 반환
                out errorMessage); // 계산 오류 반환
        }

        return TryReplacePrefix( // resource 접두사를 visual_resource 접두사로 변환
            contentId, // 원본 Resource ID 전달
            ResourcePrefix, // 제거할 resource 접두사 전달
            VisualResourcePrefix, // 적용할 visual_resource 접두사 전달
            out profileId, // 계산 결과 반환
            out errorMessage); // 계산 오류 반환
    }

    private static bool TryReplacePrefix( // 원본 콘텐츠 접두사를 Visual Profile 접두사로 교체
        string contentId, // 변환할 원본 콘텐츠 ID
        string requiredPrefix, // 원본 ID에 필요한 접두사
        string visualPrefix, // 결과 ID에 적용할 Visual 접두사
        out string profileId, // 변환 결과 Profile ID
        out string errorMessage) // 변환 실패 원인
    {
        profileId = string.Empty; // 변환 결과 기본값 초기화
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (!contentId.StartsWith(requiredPrefix, StringComparison.Ordinal)) // 원본 ID 접두사 일치 여부 확인
        {
            errorMessage = $"콘텐츠 ID는 '{requiredPrefix}'로 시작해야 합니다. ID: '{contentId}'"; // 접두사 불일치 오류 설정
            return false; // 접두사 변환 실패 반환
        }

        string suffix = contentId.Substring(requiredPrefix.Length); // 원본 ID에서 접두사를 제외한 고유 부분 추출

        if (string.IsNullOrWhiteSpace(suffix)) // 고유 부분 존재 여부 확인
        {
            errorMessage = $"콘텐츠 ID에 고유 이름이 없습니다. ID: '{contentId}'"; // 고유 부분 누락 오류 설정
            return false; // 접두사 변환 실패 반환
        }

        string resolvedProfileId = visualPrefix + suffix; // Visual 접두사와 고유 부분으로 Profile ID 구성

        if (!GameDataRegistry.IsValidContentId(resolvedProfileId)) // 계산 결과 ID 공통 규칙 검사
        {
            errorMessage = $"계산된 Visual Profile ID가 올바르지 않습니다. ID: '{resolvedProfileId}'"; // 계산 결과 오류 설정
            return false; // Profile ID 계산 실패 반환
        }

        profileId = resolvedProfileId; // 검증된 계산 결과 적용
        return true; // 접두사 변환 성공 반환
    }

    private static string NormalizeId(string value) // ID 입력값 양쪽 공백 제거
    {
        return string.IsNullOrWhiteSpace(value) // 입력값 존재 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 반환
            : value.Trim(); // 입력이 있으면 양쪽 공백 제거 후 반환
    }
}
