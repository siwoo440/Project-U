using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerWetness : MonoBehaviour // 플레이어 젖음 수치 관리
{
    [Header("Wetness")] // 젖음 설정 묶음
    [Tooltip("최대 젖음 수치.")]
    [SerializeField][Min(1f)] private float maxWetness = 100f; // 최대 젖음 수치
    [Tooltip("비 초당 젖음 증가량.")]
    [SerializeField][Min(0f)] private float rainWetnessPerSecond = 2f; // 비 초당 젖음 증가량
    [Tooltip("폭풍 초당 젖음 증가량.")]
    [SerializeField][Min(0f)] private float stormWetnessPerSecond = 4f; // 폭풍 초당 젖음 증가량
    [Tooltip("비노출 초당 건조량.")]
    [SerializeField][Min(0f)] private float dryingPerSecond = 1f; // 비노출 초당 건조량

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("현재 젖음 수치.")]
    [SerializeField] private float currentWetness; // 현재 젖음 수치

    public float CurrentWetness => currentWetness; // 현재 젖음 수치 제공
    public float MaxWetness => maxWetness; // 최대 젖음 수치 제공
    public float NormalizedWetness => currentWetness / maxWetness; // 젖음 비율 제공
    public bool IsSoaked => currentWetness >= maxWetness; // 완전히 젖은 상태 제공

    private void Awake() // 젖음 수치 초기화
    {
        ClampSettings(); // 설정값 범위 보정
    }

    private void OnValidate() // Inspector 설정값 검증
    {
        ClampSettings(); // 설정값 범위 보정
    }

    public void UpdateWeatherExposure(WeatherType weather, bool isSheltered, float deltaTime) // 날씨 노출에 따른 젖음 갱신
    {
        if (deltaTime <= 0f) // 정상 시간값 확인
        {
            return; // 젖음 갱신 중단
        }

        float wetnessChangePerSecond = -dryingPerSecond; // 기본 건조량 적용

        if (!isSheltered && weather == WeatherType.Rain) // 야외 비 노출 확인
        {
            wetnessChangePerSecond = rainWetnessPerSecond; // 비 젖음 증가량 적용
        }
        else if (!isSheltered && weather == WeatherType.Storm) // 야외 폭풍 노출 확인
        {
            wetnessChangePerSecond = stormWetnessPerSecond; // 폭풍 젖음 증가량 적용
        }

        float wetnessChange = wetnessChangePerSecond * deltaTime; // 현재 프레임 변화량 계산
        currentWetness = Mathf.Clamp(currentWetness + wetnessChange, 0f, maxWetness); // 젖음 범위 적용
    }

    public void SetCurrentWetness(float wetnessAmount) // 저장된 젖음 수치 적용
    {
        currentWetness = Mathf.Clamp(wetnessAmount, 0f, maxWetness); // 젖음 범위 적용
    }

    private void ClampSettings() // 젖음 설정값 보정
    {
        maxWetness = Mathf.Max(1f, maxWetness); // 최대 젖음 최소값 적용
        rainWetnessPerSecond = Mathf.Max(0f, rainWetnessPerSecond); // 비 증가량 음수 방지
        stormWetnessPerSecond = Mathf.Max(0f, stormWetnessPerSecond); // 폭풍 증가량 음수 방지
        dryingPerSecond = Mathf.Max(0f, dryingPerSecond); // 건조량 음수 방지
        currentWetness = Mathf.Clamp(currentWetness, 0f, maxWetness); // 현재 젖음 범위 적용
    }
}