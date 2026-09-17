using System; // 배열 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "FarmingRules_New", menuName = "Project U/Farming/Farming Rules Data")] // 농사 규칙 데이터 생성 메뉴
public sealed class FarmingRulesData : ScriptableObject // 밭·물주기·날씨 공통 규칙
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("농사 규칙 고유 ID입니다.")]
    [SerializeField] private string rulesId = "farming_rules_default"; // 규칙 고유 ID

    [Header("Plot")] // 밭 설정 묶음
    [Tooltip("거점 건축 모드에서 설치할 밭 건축 데이터입니다.")]
    [SerializeField] private BuildRecipeData farmPlotRecipe; // 밭 건축 데이터

    [Tooltip("밭을 만들 때 손에 들고 있어야 하는 도구입니다.")]
    [SerializeField] private ToolType tillingTool = ToolType.Hoe; // 경작 도구

    [Tooltip("밭을 한 칸 만들 때 소비할 스태미나입니다.")]
    [SerializeField, Min(0f)] private float tillingStaminaCost = 4f; // 경작 스태미나 비용

    [Tooltip("밭을 만들 때 흙에서 함께 나올 수 있는 아이템입니다. (82일차: 지렁이 미끼)")]
    [SerializeField] private ItemData tillingBonusItem; // 경작 보너스 아이템

    [Tooltip("경작 보너스 아이템이 나올 확률입니다.")]
    [SerializeField, Range(0f, 1f)] private float tillingBonusChance = 0.3f; // 경작 보너스 확률

    [Header("Watering")] // 물주기 설정 묶음
    [Tooltip("작물에 물을 줄 때 손에 들고 있어야 하는 도구입니다.")]
    [SerializeField] private ToolType wateringTool = ToolType.WateringCan; // 물주기 도구

    [Tooltip("물뿌리개를 가득 채웠을 때 물을 줄 수 있는 횟수입니다.")]
    [SerializeField, Min(1)] private int wateringCanCapacity = 10; // 물뿌리개 최대 사용 횟수

    [Tooltip("물을 한 번 줄 때 소비할 스태미나입니다.")]
    [SerializeField, Min(0f)] private float wateringStaminaCost = 2f; // 물주기 스태미나 비용

    [Tooltip("이 날씨가 된 날은 모든 밭이 물을 받은 것으로 처리합니다.")]
    [SerializeField] private WeatherType[] autoWateringWeathers = { WeatherType.Rain, WeatherType.Storm }; // 자동 물주기 날씨

    [Tooltip("물을 받지 못한 날은 작물 성장을 멈춥니다.")]
    [SerializeField] private bool pauseGrowthWhenDry = true; // 물 부족 성장 정지

    [Header("Season And Weather")] // 계절과 날씨 설정 묶음
    [Tooltip("성장 계절이 아닌 날에는 작물 성장을 멈춥니다.")]
    [SerializeField] private bool pauseGrowthOutOfSeason = true; // 계절 외 성장 정지

    [Tooltip("작물 피해 확률을 적용할 날씨입니다.")]
    [SerializeField] private WeatherType damagingWeather = WeatherType.Storm; // 작물 피해 날씨

    [Header("Harvest")] // 수확 설정 묶음
    [Tooltip("켜면 수확 후 밭이 빈 밭으로 남고, 끄면 밭도 함께 사라집니다.")]
    [SerializeField] private bool keepPlotAfterHarvest = true; // 수확 후 밭 유지

    public string RulesId => rulesId; // 규칙 ID 제공
    public BuildRecipeData FarmPlotRecipe => farmPlotRecipe; // 밭 건축 데이터 제공
    public ToolType TillingTool => tillingTool; // 경작 도구 제공
    public float TillingStaminaCost => tillingStaminaCost; // 경작 스태미나 제공
    public ItemData TillingBonusItem => tillingBonusItem; // 경작 보너스 아이템 제공
    public float TillingBonusChance => tillingBonusChance; // 경작 보너스 확률 제공
    public ToolType WateringTool => wateringTool; // 물주기 도구 제공
    public int WateringCanCapacity => Mathf.Max(1, wateringCanCapacity); // 물뿌리개 용량 제공
    public float WateringStaminaCost => wateringStaminaCost; // 물주기 스태미나 제공
    public bool PauseGrowthWhenDry => pauseGrowthWhenDry; // 물 부족 성장 정지 제공
    public bool PauseGrowthOutOfSeason => pauseGrowthOutOfSeason; // 계절 외 성장 정지 제공
    public WeatherType DamagingWeather => damagingWeather; // 피해 날씨 제공
    public bool KeepPlotAfterHarvest => keepPlotAfterHarvest; // 수확 후 밭 유지 제공

    public bool IsAutoWateredBy(WeatherType weather) // 지정 날씨 자동 물주기 여부 확인
    {
        return Array.IndexOf(autoWateringWeathers, weather) >= 0; // 목록 포함 여부 반환
    }

    public bool TryValidate(out string errorMessage) // 규칙 데이터 필수 조건 검사
    {
        if (farmPlotRecipe == null) // 밭 건축 데이터 확인
        {
            errorMessage = $"{name}: 밭 건축 데이터가 연결되지 않았습니다."; // 건축 데이터 누락 오류 저장
            return false; // 검사 실패
        }

        if (tillingTool == ToolType.None || wateringTool == ToolType.None) // 도구 종류 확인
        {
            errorMessage = $"{name}: 경작 도구와 물주기 도구 종류를 지정해야 합니다."; // 도구 누락 오류 저장
            return false; // 검사 실패
        }

        if (damagingWeather == WeatherType.Clear) // 맑은 날 피해 설정 확인
        {
            errorMessage = $"{name}: 맑은 날씨는 작물 피해 날씨로 사용할 수 없습니다."; // 피해 날씨 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }

    private void OnValidate() // Inspector 값 정리
    {
        rulesId = string.IsNullOrWhiteSpace(rulesId) ? string.Empty : rulesId.Trim(); // ID 공백 제거
        wateringCanCapacity = Mathf.Max(1, wateringCanCapacity); // 용량 최소값 적용
        autoWateringWeathers ??= Array.Empty<WeatherType>(); // 날씨 배열 누락 방지
    }
}
