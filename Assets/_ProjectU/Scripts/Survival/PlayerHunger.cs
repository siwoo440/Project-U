using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
public sealed class PlayerHunger : MonoBehaviour // 플레이어 허기 관리
{
    [Header("Hunger")] // 허기 설정 묶음
    [Tooltip("최대 허기.")]
    [SerializeField] private float maxHunger = 100f; // 최대 허기
    [Tooltip("초당 허기 감소량.")]
    [SerializeField] private float depletionPerSecond = 0.1f; // 초당 허기 감소량

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("현재 허기.")]
    [SerializeField] private float currentHunger = 100f; // 현재 허기

    private PlayerEquipment playerEquipment; // 플레이어 장비 관리자

    public float CurrentHunger => currentHunger; // 현재 허기 제공
    public float MaxHunger => maxHunger; // 최대 허기 제공
    public float NormalizedHunger => currentHunger / maxHunger; // 허기 비율 제공
    public bool IsStarving => currentHunger <= 0f; // 굶주림 상태 제공

    private void Awake() // 허기 초기화
    {
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        ClampSettings(); // 설정값 범위 보정
        currentHunger = maxHunger; // 시작 허기 최대 적용
    }

    private void Update() // 허기 지속 감소
    {
        float reductionPercent = playerEquipment.TotalHungerReductionPercent; // 허기 감소 방지량 조회
        float depletionMultiplier = 1f - reductionPercent / 100f; // 허기 감소 배율 계산
        float depletionAmount = depletionPerSecond * depletionMultiplier * Time.deltaTime; // 장비 적용 허기 감소량
        currentHunger = Mathf.Max(0f, currentHunger - depletionAmount); // 허기 최소값 제한
    }

    private void OnValidate() // Inspector 값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public bool TryEat(float restoreAmount) // 음식 섭취 처리
    {
        if (restoreAmount <= 0f) // 회복량 유효성 확인
        {
            return false; // 음식 사용 실패
        }

        if (currentHunger >= maxHunger) // 최대 허기 확인
        {
            return false; // 불필요한 섭취 차단
        }

        currentHunger = Mathf.Min(maxHunger, currentHunger + restoreAmount); // 허기 회복 적용
        return true; // 음식 사용 성공
    }

    public bool TryConsume(float consumeAmount) // 허기 수치 소비
    {
        if (consumeAmount <= 0f) // 소비량 유효성 확인
        {
            return false; // 잘못된 소비 차단
        }

        if (currentHunger < consumeAmount) // 현재 허기 부족 확인
        {
            return false; // 허기 소비 실패
        }

        currentHunger -= consumeAmount; // 허기 수치 감소
        return true; // 허기 소비 성공
    }
    public void SetCurrentHunger(float hungerAmount) // 현재 허기 직접 설정
    {
        currentHunger = Mathf.Clamp(hungerAmount, 0f, maxHunger); // 허기 범위 적용
    }
    private void ClampSettings() // 설정값 보정
    {
        maxHunger = Mathf.Max(1f, maxHunger); // 최대 허기 최소값 적용
        depletionPerSecond = Mathf.Max(0f, depletionPerSecond); // 감소량 음수 방지
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger); // 현재 허기 범위 제한
    }
}