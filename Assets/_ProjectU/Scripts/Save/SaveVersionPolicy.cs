public enum SaveVersionStatus // 저장 버전 판정 종류
{
    Invalid = 0, // 잘못된 버전
    UnsupportedOlderVersion = 1, // 지원 중단 버전
    RequiresMigration = 2, // 변환 필요 버전
    Current = 3, // 현재 정상 버전
    NewerThanGame = 4 // 게임보다 최신 버전
}

public static class SaveVersionPolicy // 저장 버전 규칙 관리
{
    public const int CurrentVersion = 1; // 현재 저장 형식 버전
    public const int MinimumSupportedVersion = 1; // 최소 지원 버전

    public static SaveVersionStatus GetStatus(int saveVersion) // 저장 버전 상태 판정
    {
        if (saveVersion <= 0) // 정상 버전 범위 확인
        {
            return SaveVersionStatus.Invalid; // 잘못된 버전 반환
        }

        if (saveVersion > CurrentVersion) // 현재 게임보다 최신인지 확인
        {
            return SaveVersionStatus.NewerThanGame; // 최신 게임 데이터 반환
        }

        if (saveVersion < MinimumSupportedVersion) // 최소 지원 범위 확인
        {
            return SaveVersionStatus.UnsupportedOlderVersion; // 지원 중단 반환
        }

        if (saveVersion < CurrentVersion) // 이전 버전 여부 확인
        {
            return SaveVersionStatus.RequiresMigration; // 변환 필요 반환
        }

        return SaveVersionStatus.Current; // 현재 버전 반환
    }

    public static bool CanLoadWithoutMigration(int saveVersion) // 즉시 불러오기 가능 여부
    {
        return GetStatus(saveVersion) == SaveVersionStatus.Current; // 현재 버전 여부 반환
    }
}