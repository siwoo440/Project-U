using System; // 배열·문자열 기능
using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "FishData_New", menuName = "Project U/Fishing/Fish Data")] // 물고기 데이터 생성 메뉴
public sealed class FishData : ScriptableObject // 물고기 출현 조건·난이도·결과 아이템 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("물고기 고유 ID입니다. fish_ 접두사를 사용합니다.")]
    [SerializeField] private string fishId = "fish_new"; // 물고기 고유 ID

    [Tooltip("물고기 표시 이름입니다.")]
    [SerializeField] private string displayName = "NEW FISH"; // 표시 이름

    [Tooltip("물고기 설명입니다.")]
    [TextArea(2, 4)] // 여러 줄 설명 입력
    [SerializeField] private string description = "NO DESCRIPTION"; // 설명

    [Tooltip("물고기 희귀도입니다.")]
    [SerializeField] private FishRarity rarity = FishRarity.Common; // 희귀도

    [Header("Reward")] // 보상 묶음
    [Tooltip("낚았을 때 얻는 아이템입니다.")]
    [SerializeField] private ItemData resultItem; // 결과 아이템

    [Header("Spawn Conditions")] // 출현 조건 묶음
    [Tooltip("출현하는 물가 종류입니다.")]
    [SerializeField] private WaterBodyType[] waterBodies = Array.Empty<WaterBodyType>(); // 출현 물가

    [Tooltip("출현하는 시간대입니다.")]
    [SerializeField] private FishTimeWindow timeWindow = FishTimeWindow.AnyTime; // 출현 시간대

    [Tooltip("출현하는 계절입니다. 비어 있으면 모든 계절입니다.")]
    [SerializeField] private SeasonType[] seasons = Array.Empty<SeasonType>(); // 출현 계절

    [Tooltip("출현하는 날씨입니다. 비어 있으면 모든 날씨입니다.")]
    [SerializeField] private WeatherType[] weathers = Array.Empty<WeatherType>(); // 출현 날씨

    [Tooltip("필요한 최소 낚싯대 등급입니다. (1 기본 ~ 4 심해)")]
    [SerializeField, Range(1, 4)] private int requiredRodTier = 1; // 필요 낚싯대 등급

    [Tooltip("조건을 만족하는 물고기 중 뽑힐 상대 가중치입니다.")]
    [SerializeField, Min(0f)] private float spawnWeight = 10f; // 출현 가중치

    [Header("Minigame")] // 미니게임 묶음 (83일차)
    [Tooltip("미니게임 난이도입니다. 0은 쉬움, 1은 매우 어려움입니다.")]
    [SerializeField, Range(0f, 1f)] private float difficulty = 0.3f; // 난이도

    [Tooltip("낚아 올리려면 성공해야 하는 버튼 입력 횟수입니다.")]
    [SerializeField, Min(1)] private int requiredSuccessCount = 3; // 필요 성공 횟수

    public string FishId => fishId; // ID 제공
    public string DisplayName => displayName; // 이름 제공
    public string Description => description; // 설명 제공
    public FishRarity Rarity => rarity; // 희귀도 제공
    public ItemData ResultItem => resultItem; // 결과 아이템 제공
    public IReadOnlyList<WaterBodyType> WaterBodies => waterBodies; // 출현 물가 제공
    public FishTimeWindow TimeWindow => timeWindow; // 출현 시간대 제공
    public IReadOnlyList<SeasonType> Seasons => seasons; // 출현 계절 제공
    public IReadOnlyList<WeatherType> Weathers => weathers; // 출현 날씨 제공
    public int RequiredRodTier => requiredRodTier; // 필요 낚싯대 등급 제공
    public float SpawnWeight => Mathf.Max(0f, spawnWeight); // 출현 가중치 제공
    public float Difficulty => Mathf.Clamp01(difficulty); // 난이도 제공
    public int RequiredSuccessCount => Mathf.Max(1, requiredSuccessCount); // 필요 성공 횟수 제공

    public bool CanAppear(WaterBodyType waterBody, SeasonType season, WeatherType weather, float hour, int rodTier) // 현재 조건 출현 가능 여부
    {
        if (rodTier < requiredRodTier || Array.IndexOf(waterBodies, waterBody) < 0) // 낚싯대와 물가 확인
        {
            return false; // 출현 불가
        }

        if ((timeWindow & FishTimeWindowUtility.FromHour(hour)) == 0) // 시간대 확인
        {
            return false; // 출현 불가
        }

        if (seasons.Length > 0 && Array.IndexOf(seasons, season) < 0) // 계절 확인
        {
            return false; // 출현 불가
        }

        return weathers.Length == 0 || Array.IndexOf(weathers, weather) >= 0; // 날씨 확인 결과 반환
    }

    public bool TryValidate(out string errorMessage) // 물고기 데이터 필수 조건 검사
    {
        if (string.IsNullOrWhiteSpace(fishId) || !fishId.StartsWith("fish_", StringComparison.Ordinal)) // ID 규칙 확인
        {
            errorMessage = $"{name}: Fish ID는 fish_ 접두사로 시작해야 합니다. ({fishId})"; // ID 오류
            return false; // 검사 실패
        }

        if (resultItem == null) // 결과 아이템 확인
        {
            errorMessage = $"{fishId}: 결과 아이템이 연결되지 않았습니다."; // 아이템 누락 오류
            return false; // 검사 실패
        }

        if (waterBodies.Length == 0) // 출현 물가 확인
        {
            errorMessage = $"{fishId}: 출현 물가가 비어 있습니다."; // 물가 누락 오류
            return false; // 검사 실패
        }

        if (timeWindow == FishTimeWindow.None) // 시간대 확인
        {
            errorMessage = $"{fishId}: 출현 시간대가 비어 있습니다."; // 시간대 누락 오류
            return false; // 검사 실패
        }

        if (spawnWeight <= 0f) // 가중치 확인
        {
            errorMessage = $"{fishId}: 출현 가중치는 0보다 커야 합니다."; // 가중치 오류
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }

    private void OnValidate() // Inspector 값 정리
    {
        fishId = string.IsNullOrWhiteSpace(fishId) ? string.Empty : fishId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 공백 제거
        waterBodies ??= Array.Empty<WaterBodyType>(); // 배열 누락 방지
        seasons ??= Array.Empty<SeasonType>(); // 배열 누락 방지
        weathers ??= Array.Empty<WeatherType>(); // 배열 누락 방지
    }
}
