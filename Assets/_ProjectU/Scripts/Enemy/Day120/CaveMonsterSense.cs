using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CaveMonsterSense : MonoBehaviour // 120일차: 동굴 몬스터가 사람을 알아채는 거리 (횃불을 들면 멀리서도 들킨다)
{
    private const float CheckInterval = 0.3f; // 확인 간격 (초)

    [Header("Sense")] // 알아채기 설정 묶음
    [Tooltip("횃불을 들지 않았을 때 알아채는 거리 (m).")]
    [SerializeField, Min(2f)] private float darkRange = 7f; // 어두울 때 알아채는 거리
    [Tooltip("횃불을 들었을 때 알아채는 거리 (m).")]
    [SerializeField, Min(2f)] private float litRange = 16f; // 횃불을 들었을 때 거리
    [Tooltip("한 번 깨어나면 이 거리까지는 계속 쫓아옵니다 (m).")]
    [SerializeField, Min(4f)] private float keepRange = 22f; // 깨어난 뒤 유지 거리

    private EnemyCombatController combat; // 전투 관리자
    private EnemyHealth health; // 체력
    private PlayerTorchLight torch; // 플레이어 횃불
    private Transform player; // 플레이어
    private float nextCheckTime; // 다음 확인 시각
    private bool awake; // 깨어난 상태

    public bool IsAwake => awake; // 깨어난 상태 제공 (테스트용)
    public float DarkRange => darkRange; // 어두울 때 거리 제공 (테스트용)
    public float LitRange => litRange; // 횃불 거리 제공 (테스트용)

#if UNITY_EDITOR
    public void EditorAssign(float dark, float lit) // 생성 도구 전용
    {
        darkRange = dark;
        litRange = lit;
        keepRange = lit + 6f;
    }
#endif

    private void Awake()
    {
        combat = GetComponent<EnemyCombatController>();
        health = GetComponent<EnemyHealth>();

        if (combat != null)
        {
            combat.enabled = false; // 처음에는 잠들어 있다
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime || combat == null)
        {
            return;
        }

        nextCheckTime = Time.time + CheckInterval;

        if (health != null && health.IsDead)
        {
            combat.enabled = false;
            return;
        }

        if (!FindReferences())
        {
            return;
        }

        float distance = Flat(transform.position, player.position);
        bool lit = torch != null && torch.IsLit;
        float wakeRange = lit ? litRange : darkRange;
        awake = awake ? distance <= keepRange : distance <= wakeRange;
        combat.enabled = awake;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool FindReferences()
    {
        if (player == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            player = inventory != null ? inventory.transform : null;
        }

        if (torch == null)
        {
            torch = FindFirstObjectByType<PlayerTorchLight>();
        }

        return player != null;
    }
}
