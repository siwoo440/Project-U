using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FruitTree : InteractableBase // 119일차: 심어서 기르는 과일나무 (자라면 해마다 제철에 열매)
{
    [Header("Tree")] // 나무 설정 묶음
    [Tooltip("나무 종류 ID (저장 · 검사용).")]
    [SerializeField] private string speciesId = string.Empty; // 나무 종류
    [Tooltip("나무 한글 이름.")]
    [SerializeField] private string koreanName = "과일나무"; // 한글 이름
    [Tooltip("열리는 과일.")]
    [SerializeField] private ItemData fruitItem; // 과일 아이템
    [Tooltip("한 단계가 자라는 데 걸리는 날.")]
    [SerializeField, Min(1)] private int daysPerStage = 5; // 단계마다 걸리는 날
    [Tooltip("이 계절에 열매가 열립니다.")]
    [SerializeField] private SeasonType[] fruitSeasons = new SeasonType[0]; // 열매가 열리는 계절
    [Tooltip("한 번에 열리는 과일 수.")]
    [SerializeField, Min(1)] private int fruitAmount = 4; // 열리는 과일 수

    [Header("Visual")] // 외형 설정 묶음
    [Tooltip("자라는 단계별 모습 (0 묘목 · 1 어린 나무 · 2 큰 나무).")]
    [SerializeField] private GameObject[] stageVisuals = new GameObject[0]; // 단계별 모습
    [Tooltip("열매가 열렸을 때 켜지는 모습.")]
    [SerializeField] private GameObject fruitVisual; // 열매 모습

    private SeasonCycle seasons; // 계절
    private DayNightCycle cycle; // 날짜
    private int grownDays; // 자란 날 수
    private int lastDay = -1; // 마지막으로 본 날짜
    private int readyFruit; // 딸 수 있는 과일 수
    private SeasonType lastFruitSeason = SeasonType.Winter; // 마지막으로 열매를 맺은 계절
    private bool fruitedThisSeason; // 이 계절에 이미 열매를 맺었는지

    public string SpeciesId => speciesId; // 나무 종류 제공
    public int GrownDays => grownDays; // 자란 날 수 제공 (테스트용)
    public int ReadyFruit => readyFruit; // 딸 수 있는 과일 수 제공 (테스트용)
    public int Stage => Mathf.Clamp(grownDays / Mathf.Max(1, daysPerStage), 0, Mathf.Max(0, stageVisuals.Length - 1)); // 현재 단계
    public bool IsMature => Stage >= Mathf.Max(0, stageVisuals.Length - 1); // 다 자랐는지
    public int DaysToMature => Mathf.Max(0, Mathf.Max(1, daysPerStage) * Mathf.Max(0, stageVisuals.Length - 1) - grownDays); // 다 자라기까지 남은 날
    public ItemData FruitItem => fruitItem; // 과일 아이템 제공
    public SeasonType[] FruitSeasons => fruitSeasons; // 열매 계절 제공

    public override string PromptMessage => readyFruit > 0 // 안내 문구
        ? $"F - {koreanName} 열매 따기 ({readyFruit}개)"
        : !IsMature
            ? $"{koreanName} : 자라는 중 ({DaysToMature}일 남음)"
            : $"{koreanName} : 열매가 없어요 ({SeasonNames()} 제철)";

#if UNITY_EDITOR
    public void EditorAssign(string id, string korean, ItemData fruit, int stageDays, SeasonType[] newSeasons, int amount, GameObject[] visuals, GameObject fruitObject) // 생성 도구 전용
    {
        speciesId = id;
        koreanName = korean;
        fruitItem = fruit;
        daysPerStage = stageDays;
        fruitSeasons = newSeasons ?? new SeasonType[0];
        fruitAmount = amount;
        stageVisuals = visuals ?? new GameObject[0];
        fruitVisual = fruitObject;
    }
#endif

    private string SeasonNames() // 제철 안내 (봄 · 가을)
    {
        if (fruitSeasons == null || fruitSeasons.Length == 0)
        {
            return "아무 때나";
        }

        return string.Join(" · ", fruitSeasons.Select(SeasonLabel));
    }

    private static string SeasonLabel(SeasonType season)
    {
        switch (season)
        {
            case SeasonType.Spring: return "봄";
            case SeasonType.Summer: return "여름";
            case SeasonType.Autumn: return "가을";
            default: return "겨울";
        }
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void Update() // 날이 바뀌면 자라고, 제철이 되면 열매가 열린다
    {
        if (!FindReferences())
        {
            return;
        }

        if (lastDay < 0)
        {
            lastDay = cycle.CurrentDay;
            return;
        }

        if (lastDay == cycle.CurrentDay)
        {
            return;
        }

        int passed = Mathf.Max(1, cycle.CurrentDay - lastDay);
        lastDay = cycle.CurrentDay;
        grownDays += passed;

        if (seasons.CurrentSeason != lastFruitSeason) // 계절이 바뀌면 다시 열릴 수 있다
        {
            lastFruitSeason = seasons.CurrentSeason;
            fruitedThisSeason = false;
        }

        if (IsMature && !fruitedThisSeason && readyFruit <= 0 && IsFruitSeason(seasons.CurrentSeason))
        {
            readyFruit = fruitAmount;
            fruitedThisSeason = true;
        }

        RefreshVisual();
    }

    public bool IsFruitSeason(SeasonType season) // 열매가 열리는 계절인지
    {
        return fruitSeasons == null || fruitSeasons.Length == 0 || fruitSeasons.Contains(season);
    }

    public override void Interact(GameObject interactor) // 열매 따기
    {
        if (readyFruit <= 0 || interactor == null || fruitItem == null)
        {
            return;
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        int leftover = inventory.AddItem(fruitItem, readyFruit);
        int taken = readyFruit - leftover;

        if (taken <= 0)
        {
            Debug.Log("가방이 가득 차 열매를 따지 못했습니다.", this);
            return;
        }

        readyFruit = leftover;
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 1.6f, $"+{taken} {fruitItem.KoreanName}", new Color(1f, 0.82f, 0.4f, 1f), 2f);
        RefreshVisual();
    }

    private void RefreshVisual() // 단계와 열매 모습 맞추기
    {
        for (int index = 0; index < stageVisuals.Length; index++)
        {
            if (stageVisuals[index] != null)
            {
                stageVisuals[index].SetActive(index == Stage);
            }
        }

        if (fruitVisual != null)
        {
            fruitVisual.SetActive(readyFruit > 0);
        }
    }

    private bool FindReferences()
    {
        if (cycle == null)
        {
            cycle = FindFirstObjectByType<DayNightCycle>();
        }

        if (seasons == null)
        {
            seasons = FindFirstObjectByType<SeasonCycle>();
        }

        return cycle != null && seasons != null;
    }

    // ---------------------------------------------------------------- 저장

    public void ApplyState(int savedGrownDays, int savedReadyFruit, bool savedFruited) // 불러오기
    {
        grownDays = Mathf.Max(0, savedGrownDays);
        readyFruit = Mathf.Max(0, savedReadyFruit);
        fruitedThisSeason = savedFruited;
        lastDay = -1;
        RefreshVisual();
    }

    public bool FruitedThisSeason => fruitedThisSeason; // 이 계절에 열매를 맺었는지 (저장용)
}
