using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerThirst : MonoBehaviour // 플레이어 갈증 관리
{
    [Header("Thirst")] // 갈증 설정 묶음
    [SerializeField] private float maxThirst = 100f; // 최대 갈증
    [SerializeField] private float depletionPerSecond = 0.15f; // 초당 갈증 감소량

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float currentThirst = 100f; // 현재 갈증

    public float CurrentThirst => currentThirst; // 현재 갈증 제공
    public float MaxThirst => maxThirst; // 최대 갈증 제공
    public float NormalizedThirst => currentThirst / maxThirst; // 갈증 비율 제공
    public bool IsDehydrated => currentThirst <= 0f; // 탈수 상태 제공

    private void Awake() // 갈증 초기화
    {
        ClampSettings(); // 설정값 범위 보정
        currentThirst = maxThirst; // 시작 갈증 최대 적용
    }

    private void Update() // 갈증 지속 감소
    {
        float depletionAmount = depletionPerSecond * Time.deltaTime; // 현재 프레임 감소량
        currentThirst = Mathf.Max(0f, currentThirst - depletionAmount); // 갈증 최소값 제한
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public bool TryDrink(float restoreAmount) // 음료 섭취 처리
    {
        if (restoreAmount <= 0f) // 회복량 유효성 확인
        {
            return false; // 음료 사용 실패
        }

        if (currentThirst >= maxThirst) // 최대 갈증 확인
        {
            return false; // 불필요한 섭취 차단
        }

        currentThirst = Mathf.Min(maxThirst, currentThirst + restoreAmount); // 갈증 회복 적용
        return true; // 음료 사용 성공
    }

    private void ClampSettings() // 설정값 보정
    {
        maxThirst = Mathf.Max(1f, maxThirst); // 최대 갈증 최소값 적용
        depletionPerSecond = Mathf.Max(0f, depletionPerSecond); // 감소량 음수 방지
        currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst); // 현재 갈증 범위 제한
    }
}
