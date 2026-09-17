using System; // 이벤트 기능
using System.Collections.Generic; // 밭 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FarmManager : MonoBehaviour // 거점 밭 전체의 날짜·계절·날씨·물뿌리개 상태 관리
{
    [Header("Time And Weather")] // 시간과 날씨 참조 묶음
    [Tooltip("현재 날짜를 제공하는 낮밤 순환입니다.")]
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜 제공

    [Tooltip("현재 계절을 제공하는 계절 순환입니다.")]
    [SerializeField] private SeasonCycle seasonCycle; // 계절 제공

    [Tooltip("현재 날씨를 제공하는 날씨 순환입니다.")]
    [SerializeField] private WeatherCycle weatherCycle; // 날씨 제공

    [Header("Data")] // 데이터 참조 묶음
    [Tooltip("씨앗과 작물 검색에 사용할 Registry입니다. 게임 Scene만 열어 실행해도 동작하도록 직접 연결합니다.")]
    [SerializeField] private GameDataRegistry gameDataRegistry; // 작물 검색 Registry

    [Tooltip("밭·물주기·날씨 공통 규칙입니다.")]
    [SerializeField] private FarmingRulesData farmingRules; // 농사 규칙

    [Header("Harvest Drop")] // 수확물 바닥 드롭 묶음
    [Tooltip("인벤토리에 넣지 못한 수확물을 바닥에 떨어뜨릴 때 사용할 Pickup Registry입니다.")]
    [SerializeField] private WorldItemPickupRegistry pickupRegistry; // 월드 아이템 Registry

    [Tooltip("바닥에 떨어진 수확물을 모아 둘 부모입니다.")]
    [SerializeField] private WorldItemDropContainer dropContainer; // 드롭 아이템 부모

    [Header("Watering Can")] // 물뿌리개 설정 묶음
    [Tooltip("새 게임을 시작할 때 물뿌리개를 가득 채운 상태로 시작합니다.")]
    [SerializeField] private bool startWithFullCan = true; // 시작 시 물 가득 채움

    [Header("Runtime")] // 실행 상태 묶음
    [Tooltip("물뿌리개에 남은 물 사용 횟수입니다.")]
    [SerializeField, Min(0)] private int wateringCanWater; // 남은 물

    private static readonly List<FarmPlot> activePlots = new List<FarmPlot>(); // 활성 밭 목록
    private static FarmManager instance; // 현재 Scene 밭 관리자
    private int lastKnownDay = int.MinValue; // 마지막으로 확인한 날짜
    private bool pendingWeatherCheck; // 프레임 끝에 날씨 확인 필요 여부

    public static FarmManager Instance // 현재 Scene 밭 관리자 제공
    {
        get // 관리자 조회
        {
            if (instance == null) // 등록 전 조회 확인
            {
                instance = FindFirstObjectByType<FarmManager>(); // Scene에서 한 번 검색
            }

            return instance; // 관리자 반환
        }
    }

    public static IReadOnlyList<FarmPlot> ActivePlots => activePlots; // 활성 밭 목록 제공
    public event Action WaterChanged; // 물뿌리개 물 변경 알림
    public event Action<int> DayStarted; // 새 날짜 시작 알림
    public FarmingRulesData Rules => farmingRules; // 농사 규칙 제공
    public int CurrentDay => dayNightCycle != null ? dayNightCycle.CurrentDay : 1; // 현재 날짜 제공
    public SeasonType CurrentSeason => seasonCycle != null ? seasonCycle.CurrentSeason : SeasonType.Spring; // 현재 계절 제공
    public WeatherType CurrentWeather => weatherCycle != null ? weatherCycle.CurrentWeather : WeatherType.Clear; // 현재 날씨 제공
    public bool IsAutoWateringNow => farmingRules != null && farmingRules.IsAutoWateredBy(CurrentWeather); // 자동 물주기 날씨 여부 제공
    public int WateringCanWater => wateringCanWater; // 남은 물 제공
    public int WateringCanCapacity => farmingRules != null ? farmingRules.WateringCanCapacity : 1; // 물뿌리개 용량 제공

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재로드 없는 Play 대비
    private static void ResetStatics() // 정적 상태 초기화
    {
        activePlots.Clear(); // 밭 목록 초기화
        instance = null; // 관리자 참조 초기화
    }

    public static void Register(FarmPlot plot) // 활성 밭 등록
    {
        if (plot != null && !activePlots.Contains(plot)) // 중복 등록 방지
        {
            activePlots.Add(plot); // 목록 추가
        }
    }

    public static void Unregister(FarmPlot plot) // 비활성 밭 해제
    {
        activePlots.Remove(plot); // 목록 제거
    }

    private void Awake() // 관리자 초기화
    {
        if (instance != null && instance != this) // 중복 관리자 확인
        {
            Debug.LogWarning("FarmManager가 여러 개 있습니다. 먼저 등록된 관리자를 사용합니다.", this); // 중복 경고
            enabled = false; // 중복 관리자 비활성화
            return; // 초기화 중단
        }

        instance = this; // 현재 관리자 등록

        if (farmingRules == null || gameDataRegistry == null) // 필수 데이터 확인
        {
            Debug.LogError("FarmManager의 Farming Rules와 Game Data Registry를 연결해야 합니다.", this); // 참조 누락 오류
        }

        if (dropContainer == null) // 드롭 부모 누락 확인
        {
            dropContainer = FindFirstObjectByType<WorldItemDropContainer>(); // Scene에서 검색 (없으면 최상위에 생성)
        }

        if (startWithFullCan) // 시작 물 설정 확인
        {
            wateringCanWater = WateringCanCapacity; // 물뿌리개 가득 채움
        }

        lastKnownDay = CurrentDay; // 시작 날짜 기록
    }

    private void OnEnable() // 날씨 이벤트 연결
    {
        if (weatherCycle != null) // 날씨 순환 확인
        {
            weatherCycle.WeatherChanged += HandleWeatherChanged; // 날씨 변경 구독
        }
    }

    private void OnDisable() // 날씨 이벤트 해제
    {
        if (weatherCycle != null) // 날씨 순환 확인
        {
            weatherCycle.WeatherChanged -= HandleWeatherChanged; // 날씨 변경 구독 해제
        }

        if (instance == this) // 현재 관리자 확인
        {
            instance = null; // 관리자 참조 해제
        }
    }

    private void LateUpdate() // 날짜·날씨 변경을 프레임 끝에 한 번 처리
    {
        int day = CurrentDay; // 현재 날짜 조회
        bool dayChanged = day != lastKnownDay; // 날짜 변경 여부

        if (!dayChanged && !pendingWeatherCheck) // 변경 없음 확인
        {
            return; // 처리 생략
        }

        pendingWeatherCheck = false; // 날씨 확인 요청 처리

        if (dayChanged) // 새 날짜 확인
        {
            lastKnownDay = day; // 새 날짜 기록
            ProcessGrowth(day); // 지난 날짜들의 작물 성장 처리
            DayStarted?.Invoke(day); // 새 날짜 알림
        }

        if (farmingRules != null && CurrentWeather == farmingRules.DamagingWeather) // 폭풍 확인
        {
            ApplyStormDamage(day); // 하루 한 번 작물 피해 판정
        }

        // 시간을 건너뛸 때 지나간 중간 날씨가 아니라 현재 날씨로만 자동 물주기를 판단한다
        if (IsAutoWateringNow) // 비·폭풍 확인
        {
            ApplyWeatherWatering(); // 전체 밭 물주기
        }
        else if (dayChanged) // 맑은 새 날짜
        {
            RefreshAllPlots(); // 젖은 흙 표시 갱신
        }
    }

    private void HandleWeatherChanged(WeatherType weather) // 날씨 변경 처리
    {
        pendingWeatherCheck = true; // 프레임 끝에 최종 날씨 확인
    }

    public void ApplyWeatherWatering() // 비·폭풍으로 전체 밭에 물 적용
    {
        int day = CurrentDay; // 현재 날짜 조회

        for (int index = 0; index < activePlots.Count; index++) // 전체 밭 순회
        {
            activePlots[index].ReceiveWater(day); // 물 받은 날 기록
        }
    }

    public void ProcessGrowth(int today) // 전체 밭의 지난 날짜 성장 처리
    {
        for (int index = 0; index < activePlots.Count; index++) // 전체 밭 순회
        {
            activePlots[index].ProcessDays(this, today); // 날짜별 성장 계산
        }
    }

    private void ApplyStormDamage(int today) // 폭풍 작물 피해 판정
    {
        for (int index = 0; index < activePlots.Count; index++) // 전체 밭 순회
        {
            activePlots[index].ApplyStormCheck(today, UnityEngine.Random.value); // 하루 한 번 피해 판정
        }
    }

    public SeasonType GetSeasonForDay(int day) // 지정 날짜의 계절 계산 (SeasonCycle과 같은 규칙)
    {
        int daysPerSeason = seasonCycle != null ? Mathf.Max(1, seasonCycle.DaysPerSeason) : 28; // 계절 길이
        int zeroBasedDay = Mathf.Max(1, day) - 1; // 0 기준 날짜
        return (SeasonType)(zeroBasedDay / daysPerSeason % 4); // 계절 반환
    }

    public bool TryDropItem(ItemData itemData, int quantity, Vector3 position) // 수확물 바닥 드롭
    {
        return WorldItemDropUtility.TryDrop(pickupRegistry, dropContainer, itemData, quantity, position, this); // 공통 드롭 결과 반환
    }

    public void RefreshAllPlots() // 전체 밭 외형 갱신
    {
        for (int index = 0; index < activePlots.Count; index++) // 전체 밭 순회
        {
            activePlots[index].RefreshVisuals(); // 외형 갱신
        }
    }

    public bool TryUseWater() // 물뿌리개 물 1회 사용
    {
        if (wateringCanWater <= 0) // 남은 물 확인
        {
            return false; // 사용 실패
        }

        wateringCanWater--; // 물 감소
        WaterChanged?.Invoke(); // 변경 알림
        return true; // 사용 성공
    }

    public int Refill() // 물뿌리개 가득 채우기
    {
        int added = WateringCanCapacity - wateringCanWater; // 채운 양 계산
        wateringCanWater = WateringCanCapacity; // 가득 채움
        WaterChanged?.Invoke(); // 변경 알림
        return added; // 채운 양 반환
    }

    public void SetWaterForLoad(int savedWater) // 저장된 물 양 적용
    {
        wateringCanWater = Mathf.Clamp(savedWater, 0, WateringCanCapacity); // 범위 제한 적용
        WaterChanged?.Invoke(); // 변경 알림
    }

    public bool TryGetCropBySeed(ItemData seedItem, out CropData cropData) // 씨앗으로 작물 검색
    {
        cropData = null; // 기본 결과

        if (seedItem == null || seedItem.ItemCategory != ItemCategory.Seed) // 씨앗 여부 확인
        {
            return false; // 검색 실패
        }

        if (gameDataRegistry != null) // 직접 연결 Registry 확인
        {
            return gameDataRegistry.TryGetCropBySeed(seedItem, out cropData); // Registry 검색 결과 반환
        }

        return GameDataRegistryRuntime.HasInstance
            && GameDataRegistryRuntime.Instance.TryGetCropBySeed(seedItem, out cropData); // 전역 Registry 검색 결과 반환
    }

    public bool TryGetCrop(string cropId, out CropData cropData) // 작물 ID로 검색
    {
        cropData = null; // 기본 결과

        if (gameDataRegistry != null) // 직접 연결 Registry 확인
        {
            return gameDataRegistry.TryGetCrop(cropId, out cropData); // Registry 검색 결과 반환
        }

        return GameDataRegistryRuntime.HasInstance
            && GameDataRegistryRuntime.Instance.TryGetCrop(cropId, out cropData); // 전역 Registry 검색 결과 반환
    }

    public bool TryFindPlotAt(Vector3 worldPoint, out FarmPlot plot) // 지정 위치 칸의 밭 검색
    {
        const float maximumDistanceSqr = 0.36f; // 한 칸 안쪽 거리 제곱
        plot = null; // 기본 결과
        float nearest = maximumDistanceSqr; // 최소 거리

        for (int index = 0; index < activePlots.Count; index++) // 전체 밭 순회
        {
            Vector3 offset = activePlots[index].transform.position - worldPoint; // 위치 차이
            float distanceSqr = offset.x * offset.x + offset.z * offset.z; // 수평 거리 제곱

            if (distanceSqr > nearest || Mathf.Abs(offset.y) > 1.5f) // 거리와 높이 확인
            {
                continue; // 먼 밭 제외
            }

            nearest = distanceSqr; // 최소 거리 갱신
            plot = activePlots[index]; // 밭 저장
        }

        return plot != null; // 검색 결과 반환
    }
}
