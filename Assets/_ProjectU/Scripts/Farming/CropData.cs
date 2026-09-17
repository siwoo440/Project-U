using System; // 직렬화 기능
using System.Collections.Generic; // 읽기 전용 목록 기능
using UnityEngine; // Unity 기본 기능

[Serializable] // Inspector 표시 허용
public sealed class CropGrowthStage // 작물 성장 단계 데이터
{
    [Tooltip("성장 단계 표시 이름입니다.")]
    [SerializeField] private string stageName = "SPROUT"; // 성장 단계 표시 이름

    [Tooltip("이 단계가 시작되는 누적 성장 일수입니다. 첫 단계는 0이어야 합니다.")]
    [SerializeField, Min(0)] private int startGrowthDay; // 단계 시작 누적 성장 일수

    [Tooltip("이 단계에서 밭 위에 표시할 외형 Prefab입니다.")]
    [SerializeField] private GameObject visualPrefab; // 단계 외형 Prefab

    public CropGrowthStage() // 직렬화용 기본 생성자
    {
    }

    public CropGrowthStage(string newStageName, int newStartGrowthDay, GameObject newVisualPrefab) // 에디터 생성 도구용 생성자
    {
        stageName = newStageName; // 단계 이름 저장
        startGrowthDay = Mathf.Max(0, newStartGrowthDay); // 시작 일수 저장
        visualPrefab = newVisualPrefab; // 외형 Prefab 저장
    }

    public string StageName => stageName; // 단계 이름 제공
    public int StartGrowthDay => Mathf.Max(0, startGrowthDay); // 단계 시작 일수 제공
    public GameObject VisualPrefab => visualPrefab; // 단계 외형 제공
}

[CreateAssetMenu(fileName = "CropData_New", menuName = "Project U/Farming/Crop Data")] // 작물 데이터 생성 메뉴
public sealed class CropData : ScriptableObject // 작물 성장·계절·수확 데이터
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("작물 고유 ID입니다. crop_ 접두사를 사용합니다.")]
    [SerializeField] private string cropId = "crop_new"; // 작물 고유 ID

    [Tooltip("작물 표시 이름입니다.")]
    [SerializeField] private string displayName = "NEW CROP"; // 작물 표시 이름

    [Tooltip("작물 설명입니다.")]
    [TextArea(2, 4)] // 여러 줄 설명 입력
    [SerializeField] private string description = "NO DESCRIPTION"; // 작물 설명

    [Tooltip("기획서의 작물 분류입니다.")]
    [SerializeField] private CropCategory cropCategory = CropCategory.Basic; // 작물 분류

    [Header("Items")] // 씨앗과 수확물 묶음
    [Tooltip("밭에 심을 씨앗 아이템입니다.")]
    [SerializeField] private ItemData seedItem; // 씨앗 아이템

    [Tooltip("다 자란 뒤 얻는 수확 아이템입니다.")]
    [SerializeField] private ItemData harvestItem; // 수확 아이템

    [Tooltip("한 번 수확할 때 얻는 최소 수량입니다.")]
    [SerializeField, Min(1)] private int minimumHarvestAmount = 1; // 최소 수확량

    [Tooltip("한 번 수확할 때 얻는 최대 수량입니다.")]
    [SerializeField, Min(1)] private int maximumHarvestAmount = 1; // 최대 수확량

    [Tooltip("수확할 때 씨앗 1개를 함께 돌려받을 확률입니다.")]
    [SerializeField, Range(0f, 1f)] private float seedReturnChance = 0.25f; // 씨앗 반환 확률

    [Header("Growth")] // 성장 설정 묶음
    [Tooltip("물을 받은 날 기준으로 다 자라기까지 필요한 일수입니다.")]
    [SerializeField, Min(1)] private int growthDays = 3; // 전체 성장 일수

    [Tooltip("누적 성장 일수에 따라 바뀌는 외형 단계 목록입니다. 시작 일수 오름차순으로 작성합니다.")]
    [SerializeField] private CropGrowthStage[] growthStages = Array.Empty<CropGrowthStage>(); // 성장 단계 목록

    [Tooltip("물을 받은 날에만 성장하는지 여부입니다.")]
    [SerializeField] private bool requiresWater = true; // 물 필요 여부

    [Header("Season")] // 계절 설정 묶음
    [Tooltip("밭에서 성장할 수 있는 계절 목록입니다.")]
    [SerializeField] private SeasonType[] growingSeasons = Array.Empty<SeasonType>(); // 성장 가능 계절

    [Tooltip("후반 온실에서 계절 제한 없이 재배할 수 있는지 여부입니다.")]
    [SerializeField] private bool greenhouseAllowed = true; // 온실 재배 허용

    [Tooltip("폭풍이 지나갈 때 작물이 피해를 입을 확률입니다.")]
    [SerializeField, Range(0f, 1f)] private float stormDamageChance = 0.1f; // 폭풍 피해 확률

    public string CropId => cropId; // 작물 ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public string Description => description; // 설명 제공
    public CropCategory CropCategory => cropCategory; // 작물 분류 제공
    public ItemData SeedItem => seedItem; // 씨앗 아이템 제공
    public ItemData HarvestItem => harvestItem; // 수확 아이템 제공
    public int MinimumHarvestAmount => Mathf.Max(1, minimumHarvestAmount); // 최소 수확량 제공
    public int MaximumHarvestAmount => Mathf.Max(MinimumHarvestAmount, maximumHarvestAmount); // 최대 수확량 제공
    public float SeedReturnChance => Mathf.Clamp01(seedReturnChance); // 씨앗 반환 확률 제공
    public int GrowthDays => Mathf.Max(1, growthDays); // 전체 성장 일수 제공
    public IReadOnlyList<CropGrowthStage> GrowthStages => growthStages; // 성장 단계 목록 제공
    public bool RequiresWater => requiresWater; // 물 필요 여부 제공
    public IReadOnlyList<SeasonType> GrowingSeasons => growingSeasons; // 성장 가능 계절 제공
    public bool GreenhouseAllowed => greenhouseAllowed; // 온실 재배 허용 제공
    public float StormDamageChance => Mathf.Clamp01(stormDamageChance); // 폭풍 피해 확률 제공

    public bool CanGrowInSeason(SeasonType season) // 지정 계절 성장 가능 여부 확인
    {
        for (int index = 0; index < growingSeasons.Length; index++) // 성장 가능 계절 순회
        {
            if (growingSeasons[index] == season) // 계절 일치 확인
            {
                return true; // 성장 가능 반환
            }
        }

        return false; // 성장 불가 반환
    }

    public bool IsFullyGrown(int grownDays) // 누적 성장 일수 기준 수확 가능 여부 확인
    {
        return grownDays >= GrowthDays; // 전체 성장 일수 도달 여부 반환
    }

    public int GetStageIndex(int grownDays) // 누적 성장 일수에 맞는 성장 단계 번호 조회
    {
        int stageIndex = 0; // 기본 첫 단계

        for (int index = 0; index < growthStages.Length; index++) // 전체 단계 순회
        {
            CropGrowthStage stage = growthStages[index]; // 현재 단계 조회

            if (stage != null && grownDays >= stage.StartGrowthDay) // 단계 시작 일수 도달 확인
            {
                stageIndex = index; // 도달한 가장 늦은 단계 저장
            }
        }

        return stageIndex; // 단계 번호 반환
    }

    public CropGrowthStage GetStage(int stageIndex) // 단계 번호로 성장 단계 조회
    {
        if (stageIndex < 0 || stageIndex >= growthStages.Length) // 범위 확인
        {
            return null; // 잘못된 번호 반환
        }

        return growthStages[stageIndex]; // 성장 단계 반환
    }

    public int RollHarvestAmount(float normalizedRoll) // 0~1 난수로 수확량 계산
    {
        float roll = Mathf.Clamp01(normalizedRoll); // 난수 범위 제한
        int range = MaximumHarvestAmount - MinimumHarvestAmount + 1; // 가능한 수확량 개수
        int offset = Mathf.Min(range - 1, Mathf.FloorToInt(roll * range)); // 최대값을 넘지 않는 추가 수확량
        return MinimumHarvestAmount + offset; // 최종 수확량 반환
    }

    public bool TryValidate(out string errorMessage) // 작물 데이터 필수 조건 검사
    {
        if (string.IsNullOrWhiteSpace(cropId) || !cropId.StartsWith("crop_", StringComparison.Ordinal)) // ID 규칙 확인
        {
            errorMessage = $"{name}: Crop ID는 crop_ 접두사로 시작해야 합니다. ({cropId})"; // ID 오류 저장
            return false; // 검사 실패
        }

        if (seedItem == null || harvestItem == null) // 아이템 연결 확인
        {
            errorMessage = $"{cropId}: 씨앗 또는 수확 아이템이 연결되지 않았습니다."; // 아이템 누락 오류 저장
            return false; // 검사 실패
        }

        if (seedItem.ItemCategory != ItemCategory.Seed) // 씨앗 분류 확인
        {
            errorMessage = $"{cropId}: {seedItem.name}의 분류가 Seed가 아닙니다."; // 씨앗 분류 오류 저장
            return false; // 검사 실패
        }

        if (seedItem == harvestItem) // 씨앗과 수확물 구분 확인
        {
            errorMessage = $"{cropId}: 씨앗과 수확 아이템이 같습니다."; // 동일 아이템 오류 저장
            return false; // 검사 실패
        }

        if (maximumHarvestAmount < minimumHarvestAmount) // 수확량 범위 확인
        {
            errorMessage = $"{cropId}: 최대 수확량이 최소 수확량보다 작습니다."; // 수확량 오류 저장
            return false; // 검사 실패
        }

        if (growingSeasons.Length == 0) // 성장 계절 존재 확인
        {
            errorMessage = $"{cropId}: 성장 가능 계절이 비어 있습니다."; // 계절 누락 오류 저장
            return false; // 검사 실패
        }

        if (growthStages.Length < 2) // 최소 단계 수 확인
        {
            errorMessage = $"{cropId}: 성장 단계는 심은 직후와 완성 단계를 포함해 2개 이상이어야 합니다."; // 단계 수 오류 저장
            return false; // 검사 실패
        }

        for (int index = 0; index < growthStages.Length; index++) // 전체 단계 순회
        {
            CropGrowthStage stage = growthStages[index]; // 현재 단계 조회

            if (stage == null || stage.VisualPrefab == null) // 단계 외형 확인
            {
                errorMessage = $"{cropId}: {index}번 성장 단계의 외형 Prefab이 비어 있습니다."; // 외형 누락 오류 저장
                return false; // 검사 실패
            }

            if (index == 0 && stage.StartGrowthDay != 0) // 첫 단계 시작 일수 확인
            {
                errorMessage = $"{cropId}: 첫 성장 단계의 시작 일수는 0이어야 합니다."; // 첫 단계 오류 저장
                return false; // 검사 실패
            }

            if (index > 0 && stage.StartGrowthDay <= growthStages[index - 1].StartGrowthDay) // 오름차순 확인
            {
                errorMessage = $"{cropId}: 성장 단계 시작 일수는 오름차순이어야 합니다. ({index}번)"; // 순서 오류 저장
                return false; // 검사 실패
            }
        }

        if (growthStages[growthStages.Length - 1].StartGrowthDay != GrowthDays) // 완성 단계 일수 확인
        {
            errorMessage = $"{cropId}: 마지막 성장 단계는 전체 성장 일수({GrowthDays}일)에 시작해야 합니다."; // 완성 단계 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }

    private void OnValidate() // Inspector 값 정리
    {
        cropId = string.IsNullOrWhiteSpace(cropId) ? string.Empty : cropId.Trim(); // ID 공백 제거
        displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(); // 이름 공백 제거
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim(); // 설명 공백 제거
        minimumHarvestAmount = Mathf.Max(1, minimumHarvestAmount); // 최소 수확량 보정
        maximumHarvestAmount = Mathf.Max(minimumHarvestAmount, maximumHarvestAmount); // 최대 수확량 보정
        growthDays = Mathf.Max(1, growthDays); // 성장 일수 보정
        growthStages ??= Array.Empty<CropGrowthStage>(); // 단계 배열 누락 방지
        growingSeasons ??= Array.Empty<SeasonType>(); // 계절 배열 누락 방지
    }
}
