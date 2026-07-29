using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SleepingBagInteractable : InteractableBase, IBuildRemovalGuard // 침낭 상호작용과 철거 제한 처리
{
    [SerializeField] private SleepSystem sleepSystem; // 수면 시스템 참조
    [SerializeField] private PlayerRespawnSystem playerRespawnSystem; // 플레이어 부활 시스템 참조
    [SerializeField] private Transform respawnPoint; // 침낭 부활 위치

    public override string PromptMessage => GetPromptMessage(); // 현재 침낭 안내 문구 제공
    public Transform RespawnPoint => respawnPoint; // 침낭 부활 위치 제공
    public bool CanRemove => playerRespawnSystem == null || !playerRespawnSystem.IsRegisteredRespawnPoint(respawnPoint); // 활성 부활 침낭 철거 차단
    public string RemovalBlockedMessage => "ACTIVE RESPAWN POINT"; // 철거 차단 원인 제공

    private void Awake() // 침낭 시스템 참조 초기화
    {
        if (sleepSystem == null) // 수면 시스템 미연결 확인
        {
            sleepSystem = FindFirstObjectByType<SleepSystem>(); // Scene 수면 시스템 검색
        }

        if (playerRespawnSystem == null) // 부활 시스템 미연결 확인
        {
            playerRespawnSystem = FindFirstObjectByType<PlayerRespawnSystem>(); // Scene 부활 시스템 검색
        }

        bool hasMissingReference = sleepSystem == null
            || playerRespawnSystem == null
            || respawnPoint == null; // 필수 침낭 참조 확인

        if (hasMissingReference) // 필수 참조 누락 여부 확인
        {
            Debug.LogError($"{gameObject.name}의 수면 및 부활 참조가 누락되었습니다.", this); // 참조 누락 오류 출력
            enabled = false; // 침낭 상호작용 비활성화
        }
    }

    public override void Interact(GameObject interactor) // 침낭 사용과 부활 지점 등록 처리
    {
        if (!enabled || sleepSystem == null || playerRespawnSystem == null) // 침낭 기능 사용 가능 여부 확인
        {
            return; // 상호작용 중단
        }

        if (interactor == null) // 상호작용 주체 존재 확인
        {
            return; // 잘못된 상호작용 차단
        }

        if (!sleepSystem.TrySleep()) // 현재 조건으로 수면 시작 시도
        {
            return; // 수면 실패 시 부활 지점 등록 차단
        }

        playerRespawnSystem.RegisterRespawnPoint(respawnPoint); // 수면한 침낭을 부활 지점으로 등록
    }

    private string GetPromptMessage() // 수면과 부활 지점 안내 문구 생성
    {
        if (sleepSystem == null) // 수면 시스템 연결 확인
        {
            return "SLEEP UNAVAILABLE"; // 수면 불가 문구 반환
        }

        string promptMessage = sleepSystem.SleepPrompt; // 기본 수면 문구 저장

        if (playerRespawnSystem == null) // 부활 시스템 연결 확인
        {
            return promptMessage; // 기본 수면 문구 반환
        }

        if (!playerRespawnSystem.IsRegisteredRespawnPoint(respawnPoint)) // 현재 침낭 등록 여부 확인
        {
            return promptMessage; // 미등록 침낭 기본 문구 반환
        }

        return $"{promptMessage}\nRESPAWN POINT ACTIVE"; // 활성 부활 지점 문구 추가
    }
}