public interface IBuildRemovalGuard // 건축물 철거 제한 규칙
{
    bool CanRemove { get; } // 현재 철거 가능 여부

    string RemovalBlockedMessage { get; } // 철거 차단 안내 문구
}