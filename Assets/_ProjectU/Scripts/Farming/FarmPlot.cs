using System.Text; // 안내 문구 조립 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlacedBuildObject))] // 설치 건축물 정보 요구
public sealed class FarmPlot : InteractableBase, IBuildRemovalGuard // 밭 한 칸의 작물·물 상태와 상호작용
{
    public const int NeverWatered = -1; // 물을 받은 적 없는 날짜 값

    [Header("Visual")] // 외형 참조 묶음
    [Tooltip("작물 단계 외형을 붙일 기준점입니다.")]
    [SerializeField] private Transform cropAnchor; // 작물 외형 기준점

    [Tooltip("물을 받은 날 켜는 젖은 흙 외형입니다.")]
    [SerializeField] private GameObject wetSoilVisual; // 젖은 흙 외형

    [Tooltip("수확 가능할 때 켜는 반짝이 표시입니다.")]
    [SerializeField] private GameObject readyMarker; // 수확 가능 표시

    [Header("Runtime - State")] // 칸별 상태 묶음
    [Tooltip("현재 작물 상태입니다.")]
    [SerializeField] private FarmPlotState state = FarmPlotState.Empty; // 작물 상태

    [Tooltip("현재 심은 작물입니다.")]
    [SerializeField] private CropData currentCrop; // 현재 작물

    [Tooltip("작물을 심은 날짜입니다.")]
    [SerializeField] private int plantedDay; // 심은 날짜

    [Tooltip("물을 받아 성장한 누적 일수입니다.")]
    [SerializeField] private int grownDays; // 누적 성장 일수

    [Tooltip("마지막으로 물을 받은 날짜입니다. -1은 받은 적 없음입니다.")]
    [SerializeField] private int lastWateredDay = NeverWatered; // 마지막 물 받은 날짜

    [Tooltip("성장 계산을 마지막으로 처리한 날짜입니다.")]
    [SerializeField] private int lastGrowthDay; // 마지막 성장 처리 날짜

    [Tooltip("폭풍 피해 판정을 마지막으로 한 날짜입니다.")]
    [SerializeField] private int lastStormCheckDay; // 마지막 폭풍 판정 날짜

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 기본 색상 속성
    private static readonly int ColorId = Shader.PropertyToID("_Color"); // 기본 색상 속성
    private static readonly Color WitheredColor = new Color(0.46f, 0.36f, 0.22f, 1f); // 시든 작물 색상
    private static readonly Color HarvestPopupColor = new Color(1f, 0.86f, 0.35f, 1f); // 수확 알림 색상

    private PlacedBuildObject placedBuildObject; // 설치 건축물 정보
    private GameObject cropVisual; // 현재 작물 외형
    private int cropVisualStage = -1; // 현재 외형 단계
    private CropData cropVisualSource; // 현재 외형 작물
    private bool cropVisualWithered; // 현재 외형 시듦 여부
    private readonly StringBuilder promptBuilder = new StringBuilder(64); // 안내 문구 조립 버퍼
    private string cachedPrompt = string.Empty; // 마지막 안내 문구
    private int cachedPromptKey = int.MinValue; // 마지막 안내 문구 상태 키

    public FarmPlotState State => state; // 작물 상태 제공
    public CropData CurrentCrop => currentCrop; // 현재 작물 제공
    public int PlantedDay => plantedDay; // 심은 날짜 제공
    public int GrownDays => grownDays; // 누적 성장 일수 제공
    public int LastWateredDay => lastWateredDay; // 마지막 물 받은 날짜 제공
    public int LastGrowthDay => lastGrowthDay; // 마지막 성장 처리 날짜 제공
    public int LastStormCheckDay => lastStormCheckDay; // 마지막 폭풍 판정 날짜 제공
    public bool HasCrop => state != FarmPlotState.Empty && currentCrop != null; // 작물 존재 여부 제공
    public string StructureId => placedBuildObject != null ? placedBuildObject.StructureId : string.Empty; // 저장 ID 제공
    public bool CanRemove => !HasCrop; // 철거 가능 여부 제공
    public string RemovalBlockedMessage => HasCrop ? "CLEAR THE CROP FIRST" : string.Empty; // 철거 차단 문구 제공

    public bool IsWateredToday // 오늘 물을 받았는지 제공
    {
        get // 상태 계산
        {
            FarmManager manager = FarmManager.Instance; // 밭 관리자 조회
            return manager != null && lastWateredDay == manager.CurrentDay; // 오늘 날짜 일치 여부 반환
        }
    }

    public override string PromptMessage // 현재 손에 든 아이템에 맞는 안내 문구
    {
        get // 안내 문구 계산
        {
            FarmManager manager = FarmManager.Instance; // 밭 관리자 조회
            FarmingToolController tools = FarmingToolController.Local; // 플레이어 농사 도구 조회

            if (manager == null || manager.Rules == null) // 필수 관리자 확인
            {
                return "FARM PLOT"; // 기본 문구 반환
            }

            ItemData held = tools != null ? tools.SelectedItem : null; // 손에 든 아이템
            int key = BuildPromptKey(manager, held); // 현재 상태 키 계산

            if (key != cachedPromptKey) // 상태 변경 확인
            {
                cachedPromptKey = key; // 상태 키 저장
                cachedPrompt = BuildPrompt(manager, held); // 안내 문구 재조립
            }

            return cachedPrompt; // 안내 문구 반환
        }
    }

    private void Awake() // 참조 준비
    {
        placedBuildObject = GetComponent<PlacedBuildObject>(); // 설치 건축물 정보 조회

        if (cropAnchor == null) // 기준점 누락 확인
        {
            Transform found = transform.Find("CropAnchor"); // 이름으로 검색
            cropAnchor = found != null ? found : transform; // 기준점 적용
        }
    }

    private void OnEnable() // 관리자 등록
    {
        FarmManager.Register(this); // 활성 밭 등록
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회

        if (manager != null && manager.IsAutoWateringNow) // 비·폭풍 중 생성 확인
        {
            lastWateredDay = manager.CurrentDay; // 오늘 물 받은 상태 적용
        }

        RefreshVisuals(); // 외형 갱신
    }

    private void OnDisable() // 관리자 해제
    {
        FarmManager.Unregister(this); // 활성 밭 해제
    }

    public override void Interact(GameObject interactor) // 손에 든 아이템에 맞는 밭 작업 실행
    {
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회
        FarmingToolController tools = FarmingToolController.Local; // 플레이어 농사 도구 조회

        if (manager == null || manager.Rules == null || tools == null || !tools.CanAct) // 작업 가능 상태 확인
        {
            return; // 작업 중단
        }

        if (state == FarmPlotState.Ready) // 수확 가능 확인 (손에 든 아이템과 관계없이 수확 우선)
        {
            TryHarvest(manager, tools); // 수확 시도
            return; // 처리 종료
        }

        ItemData held = tools.SelectedItem; // 손에 든 아이템

        if (held == null) // 빈손 확인
        {
            return; // 작업 없음
        }

        if (held.IsTool && held.ToolType == manager.Rules.WateringTool) // 물뿌리개 확인
        {
            TryWater(manager, tools); // 물주기 시도
            return; // 처리 종료
        }

        if (held.IsTool && held.ToolType == manager.Rules.TillingTool) // 괭이 확인
        {
            if (state == FarmPlotState.Withered) // 시든 작물 확인
            {
                ClearCrop(); // 작물 걷어내기
            }
            else if (!HasCrop) // 빈 밭 확인
            {
                RemovePlot(); // 밭을 땅으로 되돌리기
            }

            return; // 처리 종료
        }

        if (held.ItemCategory == ItemCategory.Seed) // 씨앗 확인
        {
            TryPlant(manager, tools, held); // 심기 시도
        }
    }

    public bool TryPlant(FarmManager manager, FarmingToolController tools, ItemData seedItem) // 씨앗 심기
    {
        if (HasCrop || !manager.TryGetCropBySeed(seedItem, out CropData crop)) // 빈 밭과 작물 데이터 확인
        {
            return false; // 심기 실패
        }

        if (!CanPlantInCurrentSeason(manager, crop)) // 계절 확인
        {
            return false; // 계절이 맞지 않음
        }

        PlayerInventory inventory = tools.Inventory; // 플레이어 인벤토리
        int slotIndex = inventory.SelectedHotbarIndex; // 선택 슬롯
        InventorySlot slot = inventory.GetSlot(slotIndex); // 선택 슬롯 조회

        if (slot == null || slot.ItemData != seedItem || inventory.RemoveItemFromSlot(slotIndex, 1) != 1) // 씨앗 1개 소비
        {
            return false; // 소비 실패
        }

        int day = manager.CurrentDay; // 오늘 날짜
        currentCrop = crop; // 작물 저장
        state = FarmPlotState.Growing; // 성장 상태
        plantedDay = day; // 심은 날짜
        grownDays = 0; // 성장 일수 초기화
        lastGrowthDay = day; // 오늘부터 성장 계산

        if (manager.IsAutoWateringNow) // 비·폭풍 확인
        {
            lastWateredDay = day; // 자동 물주기
        }

        RefreshVisuals(); // 외형 갱신
        return true; // 심기 성공
    }

    public bool TryWater(FarmManager manager, FarmingToolController tools) // 물뿌리개로 물주기
    {
        if (lastWateredDay == manager.CurrentDay) // 오늘 이미 물 받음
        {
            return false; // 중복 물주기 차단
        }

        if (manager.WateringCanWater <= 0 || !tools.TryConsumeStamina(manager.Rules.WateringStaminaCost)) // 물과 스태미나 확인
        {
            return false; // 물주기 실패
        }

        manager.TryUseWater(); // 물 1회 사용
        ReceiveWater(manager.CurrentDay); // 물 받은 날 기록
        return true; // 물주기 성공
    }

    public void ProcessDays(FarmManager manager, int today) // 지난 날짜들의 성장 처리 (새 날짜가 시작될 때 호출)
    {
        if (state != FarmPlotState.Growing || currentCrop == null) // 성장 중 작물 확인
        {
            lastGrowthDay = Mathf.Max(lastGrowthDay, today); // 처리 날짜만 갱신
            RefreshVisuals(); // 날짜에 따른 외형 갱신
            return; // 성장 처리 생략
        }

        FarmingRulesData rules = manager.Rules; // 농사 규칙
        int firstDay = lastGrowthDay > 0 ? lastGrowthDay : plantedDay; // 처리 시작 날짜

        // 물 기록은 마지막으로 받은 날 하나이므로, 건너뛴 여러 날 중 그날만 물 받은 날로 인정한다
        for (int day = firstDay; day < today && state == FarmPlotState.Growing; day++) // 지난 날짜 순회
        {
            bool watered = !currentCrop.RequiresWater || lastWateredDay == day; // 그날 물 받음 여부

            if (!watered && (rules == null || rules.PauseGrowthWhenDry)) // 물 부족 확인
            {
                continue; // 성장 정지
            }

            bool inSeason = currentCrop.CanGrowInSeason(manager.GetSeasonForDay(day)); // 그날 계절 확인

            if (!inSeason && (rules == null || rules.PauseGrowthOutOfSeason)) // 계절 확인
            {
                continue; // 성장 정지
            }

            grownDays++; // 하루 성장

            if (currentCrop.IsFullyGrown(grownDays)) // 완성 확인
            {
                grownDays = currentCrop.GrowthDays; // 성장 일수 고정
                state = FarmPlotState.Ready; // 수확 가능
            }
        }

        lastGrowthDay = Mathf.Max(lastGrowthDay, today); // 처리 날짜 기록
        RefreshVisuals(); // 외형 갱신
    }

    public void ApplyStormCheck(int today, float normalizedRoll) // 폭풍 피해 판정 (하루 한 번)
    {
        if (state != FarmPlotState.Growing || currentCrop == null || lastStormCheckDay == today) // 판정 대상 확인
        {
            return; // 판정 생략
        }

        lastStormCheckDay = today; // 판정 날짜 기록

        if (normalizedRoll < currentCrop.StormDamageChance) // 피해 확률 확인
        {
            state = FarmPlotState.Withered; // 시든 작물
            RefreshVisuals(); // 외형 갱신
        }
    }

    public bool TryHarvest(FarmManager manager, FarmingToolController tools) // 다 자란 작물 수확
    {
        if (state != FarmPlotState.Ready || currentCrop == null) // 수확 가능 확인
        {
            return false; // 수확 실패
        }

        CropData crop = currentCrop; // 수확 작물
        int amount = crop.RollHarvestAmount(Random.value); // 수확량
        bool returnSeed = crop.SeedItem != null && Random.value < crop.SeedReturnChance; // 씨앗 반환 여부
        Vector3 popupPosition = transform.position + Vector3.up * 0.7f; // 알림 위치

        GiveItem(manager, tools.Inventory, crop.HarvestItem, amount); // 수확물 지급
        CombatDamagePopup.SpawnText(popupPosition, $"+{amount} {crop.HarvestItem.DisplayName}", HarvestPopupColor, 2.4f); // 수확 알림

        if (returnSeed) // 씨앗 반환 확인
        {
            GiveItem(manager, tools.Inventory, crop.SeedItem, 1); // 씨앗 지급
            CombatDamagePopup.SpawnText(popupPosition + Vector3.up * 0.35f, "+1 SEED", new Color(0.7f, 0.95f, 0.5f, 1f), 2f); // 씨앗 알림
        }

        ClearCrop(); // 빈 밭으로 되돌리기

        if (manager.Rules != null && !manager.Rules.KeepPlotAfterHarvest) // 밭 제거 규칙 확인
        {
            RemovePlot(); // 밭 제거
        }

        return true; // 수확 성공
    }

    private void GiveItem(FarmManager manager, PlayerInventory inventory, ItemData itemData, int amount) // 인벤토리 지급, 넘치면 바닥 드롭
    {
        if (itemData == null || amount <= 0) // 요청 확인
        {
            return; // 지급 생략
        }

        int remaining = inventory != null ? inventory.AddItem(itemData, amount) : amount; // 인벤토리 추가

        if (remaining > 0) // 넘친 수량 확인
        {
            manager.TryDropItem(itemData, remaining, transform.position); // 밭 옆에 떨어뜨리기
        }
    }

    public void ReceiveWater(int day) // 지정 날짜에 물 받은 상태 적용
    {
        lastWateredDay = day; // 날짜 기록
        RefreshVisuals(); // 외형 갱신
    }

    public void ClearCrop() // 작물을 걷어내고 빈 밭으로 되돌리기
    {
        state = FarmPlotState.Empty; // 빈 밭
        currentCrop = null; // 작물 제거
        grownDays = 0; // 성장 일수 초기화
        plantedDay = 0; // 심은 날짜 초기화
        RefreshVisuals(); // 외형 갱신
    }

    public void RemovePlot() // 빈 밭을 땅으로 되돌리기
    {
        if (HasCrop) // 작물 존재 확인
        {
            return; // 제거 차단
        }

        gameObject.SetActive(false); // 즉시 목록에서 제외
        Destroy(gameObject); // 밭 제거
    }

    public FarmPlotSaveData CaptureSaveData() // 저장 데이터 생성
    {
        return new FarmPlotSaveData
        {
            structureId = StructureId, // 건축물 ID
            state = (int)state, // 작물 상태
            cropId = HasCrop ? currentCrop.CropId : string.Empty, // 작물 ID
            plantedDay = plantedDay, // 심은 날짜
            grownDays = grownDays, // 성장 일수
            lastWateredDay = lastWateredDay, // 물 받은 날짜
            lastGrowthDay = lastGrowthDay, // 성장 처리 날짜
            lastStormCheckDay = lastStormCheckDay // 폭풍 판정 날짜
        };
    }

    public void ApplySaveData(FarmPlotSaveData data, CropData crop) // 저장 데이터 적용
    {
        state = crop != null ? (FarmPlotState)data.state : FarmPlotState.Empty; // 작물 상태 적용
        currentCrop = state == FarmPlotState.Empty ? null : crop; // 작물 적용
        plantedDay = data.plantedDay; // 심은 날짜 적용
        grownDays = Mathf.Max(0, data.grownDays); // 성장 일수 적용
        lastWateredDay = data.lastWateredDay; // 물 받은 날짜 적용
        lastGrowthDay = data.lastGrowthDay; // 성장 처리 날짜 적용
        lastStormCheckDay = data.lastStormCheckDay; // 폭풍 판정 날짜 적용
        RefreshVisuals(); // 외형 갱신
    }

    public void ResetForLoad() // 저장 데이터가 없는 밭을 빈 상태로 초기화
    {
        state = FarmPlotState.Empty; // 빈 밭
        currentCrop = null; // 작물 제거
        plantedDay = 0; // 심은 날짜 초기화
        grownDays = 0; // 성장 일수 초기화
        lastWateredDay = NeverWatered; // 물 기록 초기화
        lastGrowthDay = 0; // 성장 기록 초기화
        lastStormCheckDay = 0; // 폭풍 기록 초기화
        RefreshVisuals(); // 외형 갱신
    }

    public void RefreshVisuals() // 젖은 흙과 작물 단계 외형 갱신
    {
        FarmManager manager = FarmManager.Instance; // 밭 관리자 조회
        bool isWet = manager != null && lastWateredDay == manager.CurrentDay; // 오늘 물 받음 여부

        if (wetSoilVisual != null && wetSoilVisual.activeSelf != isWet) // 젖은 흙 상태 변경 확인
        {
            wetSoilVisual.SetActive(isWet); // 젖은 흙 표시 적용
        }

        bool isReady = state == FarmPlotState.Ready; // 수확 가능 여부

        if (readyMarker != null && readyMarker.activeSelf != isReady) // 표시 상태 변경 확인
        {
            readyMarker.SetActive(isReady); // 수확 가능 표시 적용
        }

        int stage = -1; // 표시할 단계

        if (HasCrop) // 작물 존재 확인
        {
            stage = state == FarmPlotState.Ready
                ? currentCrop.GrowthStages.Count - 1
                : currentCrop.GetStageIndex(grownDays); // 성장 일수 기준 단계
        }

        bool withered = state == FarmPlotState.Withered; // 시듦 여부

        if (stage == cropVisualStage && currentCrop == cropVisualSource && withered == cropVisualWithered) // 같은 외형 확인
        {
            return; // 외형 교체 생략
        }

        if (cropVisual != null) // 이전 외형 확인
        {
            Destroy(cropVisual); // 이전 외형 제거
            cropVisual = null; // 참조 제거
        }

        cropVisualStage = stage; // 단계 기록
        cropVisualSource = currentCrop; // 작물 기록
        cropVisualWithered = withered; // 시듦 기록
        CropGrowthStage growthStage = stage >= 0 ? currentCrop.GetStage(stage) : null; // 단계 데이터

        if (growthStage == null || growthStage.VisualPrefab == null) // 표시할 외형 확인
        {
            return; // 외형 없음
        }

        cropVisual = Instantiate(growthStage.VisualPrefab, cropAnchor, false); // 단계 외형 생성
        SetLayerRecursively(cropVisual.transform, gameObject.layer); // 밭과 같은 레이어 적용

        if (withered) // 시든 작물 확인
        {
            ApplyWitheredLook(cropVisual); // 갈색으로 처진 외형 적용
        }
    }

    private static void ApplyWitheredLook(GameObject visual) // 시든 작물 외형
    {
        visual.transform.localScale = new Vector3(1f, 0.7f, 1f); // 처진 모양
        MaterialPropertyBlock block = new MaterialPropertyBlock(); // 색상 블록
        block.SetColor(BaseColorId, WitheredColor); // URP 색상
        block.SetColor(ColorId, WitheredColor); // 기본 색상

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) // 전체 Renderer 순회
        {
            renderer.SetPropertyBlock(block); // 갈색 적용
        }
    }

    private bool CanPlantInCurrentSeason(FarmManager manager, CropData crop) // 현재 계절 심기 가능 여부
    {
        return !manager.Rules.PauseGrowthOutOfSeason || crop.CanGrowInSeason(manager.CurrentSeason); // 계절 제한 결과 반환
    }

    private int BuildPromptKey(FarmManager manager, ItemData held) // 안내 문구 상태 키 계산
    {
        unchecked // 해시 계산
        {
            int key = (int)state; // 상태
            key = key * 31 + (currentCrop != null ? currentCrop.GetInstanceID() : 0); // 작물
            key = key * 31 + grownDays; // 성장 일수
            key = key * 31 + (lastWateredDay == manager.CurrentDay ? 1 : 0); // 오늘 물 받음
            key = key * 31 + (held != null ? held.GetInstanceID() : 0); // 손에 든 아이템
            key = key * 31 + manager.WateringCanWater; // 남은 물
            key = key * 31 + (int)manager.CurrentSeason; // 계절
            return key; // 상태 키 반환
        }
    }

    private string BuildPrompt(FarmManager manager, ItemData held) // 안내 문구 조립
    {
        FarmingRulesData rules = manager.Rules; // 농사 규칙
        promptBuilder.Clear(); // 버퍼 초기화

        if (state == FarmPlotState.Ready && currentCrop != null) // 수확 가능
        {
            promptBuilder.Append("F - HARVEST ").Append(currentCrop.DisplayName); // 수확 안내
            return promptBuilder.ToString(); // 문구 반환
        }

        if (held != null && held.IsTool && held.ToolType == rules.WateringTool) // 물뿌리개
        {
            if (lastWateredDay == manager.CurrentDay) // 오늘 물 받음
            {
                AppendStatus(manager); // 현재 상태 표시
            }
            else if (manager.WateringCanWater <= 0) // 물 없음
            {
                promptBuilder.Append("WATERING CAN EMPTY | REFILL AT THE WELL"); // 물 채우기 안내
            }
            else // 물주기 가능
            {
                promptBuilder.Append("F - WATER (").Append(manager.WateringCanWater).Append('/').Append(manager.WateringCanCapacity).Append(')'); // 물주기 안내
            }

            return promptBuilder.ToString(); // 문구 반환
        }

        if (held != null && held.IsTool && held.ToolType == rules.TillingTool) // 괭이
        {
            if (state == FarmPlotState.Withered && currentCrop != null) // 시든 작물
            {
                promptBuilder.Append("F - CLEAR WITHERED ").Append(currentCrop.DisplayName); // 작물 걷어내기 안내
            }
            else if (HasCrop) // 자라는 작물은 괭이로 건드리지 않음
            {
                AppendStatus(manager); // 현재 상태 표시
            }
            else // 빈 밭
            {
                promptBuilder.Append("F - REMOVE PLOT"); // 밭 제거 안내
            }

            return promptBuilder.ToString(); // 문구 반환
        }

        if (held != null && held.ItemCategory == ItemCategory.Seed && !HasCrop) // 빈 밭에 씨앗
        {
            if (!manager.TryGetCropBySeed(held, out CropData crop)) // 작물 데이터 확인
            {
                promptBuilder.Append("UNKNOWN SEED"); // 알 수 없는 씨앗
            }
            else if (!CanPlantInCurrentSeason(manager, crop)) // 계절 불일치
            {
                promptBuilder.Append(crop.DisplayName).Append(" GROWS IN ");
                AppendSeasons(crop); // 성장 계절 표시
            }
            else // 심기 가능
            {
                promptBuilder.Append("F - PLANT ").Append(crop.DisplayName); // 심기 안내
            }

            return promptBuilder.ToString(); // 문구 반환
        }

        AppendStatus(manager); // 현재 상태 표시
        return promptBuilder.ToString(); // 문구 반환
    }

    private void AppendStatus(FarmManager manager) // 현재 밭 상태 문구 추가
    {
        bool watered = lastWateredDay == manager.CurrentDay; // 오늘 물 받음

        switch (state) // 상태별 문구
        {
            case FarmPlotState.Growing when currentCrop != null: // 성장 중
                promptBuilder.Append(currentCrop.DisplayName).Append(" | DAY ").Append(grownDays).Append('/').Append(currentCrop.GrowthDays);
                promptBuilder.Append(watered ? " | WATERED" : " | NEEDS WATER");
                break;

            case FarmPlotState.Ready when currentCrop != null: // 수확 가능
                promptBuilder.Append(currentCrop.DisplayName).Append(" | READY TO HARVEST");
                break;

            case FarmPlotState.Withered when currentCrop != null: // 시듦
                promptBuilder.Append(currentCrop.DisplayName).Append(" | WITHERED | CLEAR WITH A HOE");
                break;

            default: // 빈 밭
                promptBuilder.Append(watered ? "EMPTY PLOT | WATERED | HOLD SEEDS TO PLANT" : "EMPTY PLOT | HOLD SEEDS TO PLANT");
                break;
        }
    }

    private void AppendSeasons(CropData crop) // 성장 계절 목록 추가
    {
        for (int index = 0; index < crop.GrowingSeasons.Count; index++) // 계절 순회
        {
            if (index > 0) // 구분자 확인
            {
                promptBuilder.Append('/'); // 구분자 추가
            }

            promptBuilder.Append(crop.GrowingSeasons[index].ToString().ToUpperInvariant()); // 계절 이름 추가
        }
    }

    private static void SetLayerRecursively(Transform target, int layer) // 하위 전체 레이어 적용
    {
        target.gameObject.layer = layer; // 현재 레이어 적용

        for (int index = 0; index < target.childCount; index++) // 자식 순회
        {
            SetLayerRecursively(target.GetChild(index), layer); // 자식 레이어 적용
        }
    }
}
