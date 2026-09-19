using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class IslandShoreGuard : MonoBehaviour // 105일차: 무인도 바다 경계 (허리 깊이보다 깊은 바다에 들어가면 얕은 곳으로 되돌림, 106일차 수영 전까지)
{
    public const string BlockedMessage = "물이 너무 깊어요. 더 나갈 수 없어요."; // 되돌릴 때 말풍선

    [Tooltip("바다 수면 높이 (월드 y).")]
    [SerializeField] private float seaLevel = -0.5f; // 수면
    [Tooltip("발이 수면보다 이만큼 아래로 내려가면 되돌립니다 (m).")]
    [SerializeField, Min(0.3f)] private float wadeDepth = 1.1f; // 걸어 들어갈 수 있는 깊이
    [Tooltip("플레이어 (비우면 시작할 때 찾음).")]
    [SerializeField] private Transform player; // 플레이어
    [Tooltip("안전한 위치를 모를 때 되돌아갈 곳 (시작 해변).")]
    [SerializeField] private Transform fallbackPoint; // 시작 해변
    [Tooltip("말풍선 간격 (초).")]
    [SerializeField, Min(0.5f)] private float messageInterval = 2.5f; // 말풍선 간격

    private Vector3 lastSafePosition; // 마지막으로 얕았던 위치
    private bool hasSafePosition; // 안전 위치 기록 여부
    private float nextMessageTime; // 다음 말풍선 시각

    public static IslandShoreGuard Instance { get; private set; } // Scene 관리자
    public float SeaLevel => seaLevel; // 수면 제공
    public float WadeDepth => wadeDepth; // 걸어 들어갈 수 있는 깊이 제공
    public Transform Player => player; // 플레이어 제공 (테스트용)
    public Transform FallbackPoint => fallbackPoint; // 시작 해변 제공 (테스트용)
    public int PushCount { get; private set; } // 되돌린 횟수 (테스트용)

    private void Awake() // 준비
    {
        Instance = this;
    }

    private void OnDestroy() // 정리
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public float FeetDepth(Vector3 position) // 발이 수면 아래로 잠긴 깊이 (물 밖이면 0 이하)
    {
        return seaLevel - position.y;
    }

    private void LateUpdate() // 플레이어 이동 뒤 확인
    {
        if (player == null)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>(); // 플레이어 검색
            player = movement != null ? movement.transform : null;

            if (player == null)
            {
                return;
            }
        }

        float depth = FeetDepth(player.position); // 발 깊이

        if (depth <= wadeDepth * 0.7f) // 얕은 곳 · 땅 · 부두 위
        {
            lastSafePosition = player.position;
            hasSafePosition = true;
            return;
        }

        if (depth <= wadeDepth) // 경계 근처 : 그대로 둠
        {
            return;
        }

        Vector3 target = hasSafePosition ? lastSafePosition : fallbackPoint != null ? fallbackPoint.position : player.position; // 되돌아갈 곳
        CharacterController controller = player.GetComponent<CharacterController>(); // 이동 충돌체

        if (controller != null) controller.enabled = false;
        player.position = target;
        if (controller != null) controller.enabled = true;

        PushCount++;

        if (Time.unscaledTime >= nextMessageTime) // 말풍선
        {
            nextMessageTime = Time.unscaledTime + messageInterval;
            CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.2f, BlockedMessage, new Color(0.62f, 0.82f, 0.95f, 1f), 2f);
        }
    }

    public void ResetSafePosition() // 불러오기 · 부활 뒤 기록 초기화
    {
        hasSafePosition = false;
    }

#if UNITY_EDITOR
    public void EditorAssign(float level, float depth, Transform playerTransform, Transform fallback) // 생성 도구 전용
    {
        seaLevel = level;
        wadeDepth = depth;
        player = playerTransform;
        fallbackPoint = fallback;
    }
#endif
}
