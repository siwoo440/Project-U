using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class IslandShoreGuard : MonoBehaviour // 105일차: 무인도 바다 경계 → 106일차: 파도 경계 (먼 바다에서 섬 쪽으로 밀어내고, 너무 멀면 되돌림)
{
    public const string BlockedMessage = "파도가 너무 거세요. 더 멀리 갈 수 없어요."; // 파도가 밀어낼 때 말풍선
    public const int CoastSamples = 72; // 해안선 반지름 개수 (5°마다)

    [Tooltip("바다 수면 높이 (월드 y).")]
    [SerializeField] private float seaLevel = -0.5f; // 수면
    [Tooltip("플레이어 (비우면 시작할 때 찾음).")]
    [SerializeField] private Transform player; // 플레이어
    [Tooltip("안전한 위치를 모를 때 되돌아갈 곳 (시작 해변).")]
    [SerializeField] private Transform fallbackPoint; // 시작 해변
    [Tooltip("말풍선 간격 (초).")]
    [SerializeField, Min(0.5f)] private float messageInterval = 3f; // 말풍선 간격

    [Header("Waves")]
    [Tooltip("섬 가운데에서 본 방향별 해안선 반지름 (0°부터 5°마다, 22 · 23번 메뉴가 채움).")]
    [SerializeField] private float[] coastRadii = new float[0]; // 방향별 해안선
    [Tooltip("해안선 밖 이 거리부터 파도가 밀기 시작합니다 (m).")]
    [SerializeField, Min(5f)] private float waveStart = 70f; // 파도 시작
    [Tooltip("해안선 밖 이 거리에서 파도가 가장 셉니다 (m).")]
    [SerializeField, Min(10f)] private float waveFull = 110f; // 가장 센 파도
    [Tooltip("가장 센 파도가 섬 쪽으로 미는 속도 (가장 빠른 헤엄보다 빨라야 함).")]
    [SerializeField, Min(0.5f)] private float wavePushSpeed = 5.5f; // 파도 속도
    [Tooltip("해안선 밖 이 거리보다 멀면 안전한 곳으로 되돌립니다 (안전장치).")]
    [SerializeField, Min(20f)] private float hardLimit = 145f; // 되돌리는 거리

    private Vector3 lastSafePosition; // 마지막으로 뭍 · 얕은 물에 있던 위치
    private bool hasSafePosition; // 안전 위치 기록 여부
    private float nextMessageTime; // 다음 말풍선 시각
    private Terrain terrain; // 섬 Terrain

    public static IslandShoreGuard Instance { get; private set; } // Scene 관리자
    public float SeaLevel => seaLevel; // 수면 제공
    public Transform Player => player; // 플레이어 제공 (테스트용)
    public Transform FallbackPoint => fallbackPoint; // 시작 해변 제공 (테스트용)
    public int CoastSampleCount => coastRadii != null ? coastRadii.Length : 0; // 해안선 반지름 개수 (검사용)
    public float WaveStart => waveStart; // 파도 시작 거리
    public float WaveFull => waveFull; // 가장 센 파도 거리
    public float WavePushSpeed => wavePushSpeed; // 가장 센 파도 속도
    public float HardLimit => hardLimit; // 되돌리는 거리
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

    public float CoastRadiusAt(Vector3 position) // 이 위치 방향의 해안선 반지름 (섬 가운데 기준)
    {
        if (coastRadii == null || coastRadii.Length == 0)
        {
            return 800f;
        }

        float angle = Mathf.Repeat(Mathf.Atan2(position.z, position.x) * Mathf.Rad2Deg, 360f) / 360f * coastRadii.Length; // 칸 위치
        int index = Mathf.FloorToInt(angle) % coastRadii.Length;
        int next = (index + 1) % coastRadii.Length;
        return Mathf.Lerp(coastRadii[index], coastRadii[next], angle - Mathf.Floor(angle));
    }

    public float DistanceBeyondCoast(Vector3 position) // 해안선 밖으로 나간 거리 (섬 안쪽은 음수)
    {
        return new Vector2(position.x, position.z).magnitude - CoastRadiusAt(position);
    }

    public float WaveStrengthAt(Vector3 position) // 파도 세기 0 ~ 1
    {
        float t = Mathf.Clamp01((DistanceBeyondCoast(position) - waveStart) / Mathf.Max(1f, waveFull - waveStart));
        return t * t * (3f - 2f * t);
    }

    public Vector3 WavePushAt(Vector3 position) // 파도가 섬 쪽으로 미는 속도 (m/s)
    {
        float strength = WaveStrengthAt(position);
        Vector3 inward = new Vector3(-position.x, 0f, -position.z);

        if (strength <= 0f || inward.sqrMagnitude < 1f)
        {
            return Vector3.zero;
        }

        return inward.normalized * (wavePushSpeed * strength);
    }

    public void ShowBlockedMessage() // 파도 말풍선 (간격을 두고)
    {
        if (player == null || Time.unscaledTime < nextMessageTime)
        {
            return;
        }

        nextMessageTime = Time.unscaledTime + messageInterval;
        CombatDamagePopup.SpawnText(player.position + Vector3.up * 2.2f, BlockedMessage, new Color(0.62f, 0.82f, 0.95f, 1f), 2f);
    }

    private float GroundHeight(Vector3 position) // Terrain 높이
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        }

        return terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : float.MinValue;
    }

    private void LateUpdate() // 플레이어 이동 뒤 안전장치 확인
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

        if (CaveManager.Instance != null && CaveManager.Instance.IsInside) // 116일차: 동굴 맵은 섬 밖이라 바다 경계를 보지 않음
        {
            return;
        }

        Vector3 position = player.position;
        bool tooFar = DistanceBeyondCoast(position) > hardLimit; // 파도를 넘어 너무 멀리 나감 (순간이동 · 오류)
        bool underGround = position.y < GroundHeight(position) - 4f; // 땅 밑으로 빠짐

        if (!tooFar && !underGround)
        {
            if (FeetDepth(position) <= 0.3f && DistanceBeyondCoast(position) < waveStart) // 뭍 · 얕은 물 · 부두 위
            {
                lastSafePosition = position;
                hasSafePosition = true;
            }

            return;
        }

        Vector3 target = hasSafePosition ? lastSafePosition : fallbackPoint != null ? fallbackPoint.position : new Vector3(0f, 1f, 0f); // 되돌아갈 곳
        CharacterController controller = player.GetComponent<CharacterController>(); // 이동 충돌체

        if (controller != null) controller.enabled = false;
        player.position = target;
        if (controller != null) controller.enabled = true;

        PushCount++;
        nextMessageTime = 0f;
        ShowBlockedMessage();
    }

    public void ResetSafePosition() // 불러오기 · 부활 뒤 기록 초기화
    {
        hasSafePosition = false;
    }

#if UNITY_EDITOR
    public void EditorAssign(float level, Transform playerTransform, Transform fallback, float[] radii) // 생성 도구 전용
    {
        seaLevel = level;
        player = playerTransform;
        fallbackPoint = fallback;
        coastRadii = radii;
    }
#endif
}
