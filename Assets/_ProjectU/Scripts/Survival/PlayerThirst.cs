using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlayerEquipment))] // 필수 장비 컴포넌트
public sealed class PlayerThirst : MonoBehaviour // 플레이어 갈증 관리
{
    [Header("Thirst")] // 갈증 설정 묶음
    [SerializeField] private float maxThirst = 100f; // 최대 갈증
    [SerializeField] private float depletionPerSecond = 0.15f; // 초당 갈증 감소량

    [Header("Runtime")] // 실행 상태 묶음
    [SerializeField] private float currentThirst = 100f; // 현재 갈증

    private PlayerEquipment playerEquipment; // 플레이어 장비 관리자

    public float CurrentThirst => currentThirst; // 현재 갈증 제공
    public float MaxThirst => maxThirst; // 최대 갈증 제공
    public float NormalizedThirst => currentThirst / maxThirst; // 갈증 비율 제공
    public bool IsDehydrated => currentThirst <= 0f; // 탈수 상태 제공

    private void Awake() // 갈증 초기화
    {
        playerEquipment = GetComponent<PlayerEquipment>(); // 장비 관리자 가져오기
        ClampSettings(); // 설정값 범위 보정
        currentThirst = maxThirst; // 시작 갈증 최대 적용
    }

    private void Update() // 갈증 지속 감소
    {
        float reductionPercent = playerEquipment.TotalThirstReductionPercent; // 갈증 감소 방지량 조회
        float depletionMultiplier = 1f - reductionPercent / 100f; // 갈증 감소 배율 계산
        float depletionAmount = depletionPerSecond * depletionMultiplier * Time.deltaTime; // 장비 적용 갈증 감소량
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
    public bool TryConsume(float consumeAmount) // 갈증 수치 소비
    {
        if (consumeAmount <= 0f) // 소비량 유효성 확인
        {
            return false; // 잘못된 소비 차단
        }

        if (currentThirst < consumeAmount) // 현재 갈증 부족 확인
        {
            return false; // 갈증 소비 실패
        }

        currentThirst -= consumeAmount; // 갈증 수치 감소
        return true; // 갈증 소비 성공
    }
    public void SetCurrentThirst(float thirstAmount) // 현재 갈증 직접 설정
    {
        currentThirst = Mathf.Clamp(thirstAmount, 0f, maxThirst); // 갈증 범위 적용
    }

    private void ClampSettings() // 설정값 보정
    {
        maxThirst = Mathf.Max(1f, maxThirst); // 최대 갈증 최소값 적용
        depletionPerSecond = Mathf.Max(0f, depletionPerSecond); // 감소량 음수 방지
        currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst); // 현재 갈증 범위 제한
    }
}