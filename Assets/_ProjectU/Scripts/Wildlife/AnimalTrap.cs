using System; // 직렬화
using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[Serializable]
public sealed class TrapCatchEntry // 118일차: 덫에 걸리는 동물 하나
{
    [Tooltip("걸리는 동물 이름 (안내 문구에 쓴다).")]
    public string animalName = string.Empty; // 동물 이름
    [Tooltip("거둘 때 얻는 아이템.")]
    public ItemData item; // 얻는 아이템
    [Tooltip("얻는 수량.")]
    public int amount = 1; // 얻는 수량
    [Tooltip("함께 얻는 아이템 (없으면 비워 둔다).")]
    public ItemData extraItem; // 함께 얻는 아이템
    [Tooltip("함께 얻는 수량.")]
    public int extraAmount = 1; // 함께 얻는 수량
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class AnimalTrap : InteractableBase // 118일차: 놓아 두면 작은 동물이 걸리는 덫
{
    [Header("Trap")] // 덫 설정 묶음
    [Tooltip("동물이 걸리기까지 걸리는 게임 시간 (시간).")]
    [SerializeField, Min(0.5f)] private float catchHours = 4f; // 걸리기까지 시간
    [Tooltip("하루에 잡을 수 있는 수.")]
    [SerializeField, Min(1)] private int catchesPerDay = 1; // 하루 잡는 수
    [Tooltip("걸릴 수 있는 동물 목록 (앞에서부터 차례로 뽑는다).")]
    [SerializeField] private List<TrapCatchEntry> catches = new List<TrapCatchEntry>(); // 걸리는 동물
    [Tooltip("걸린 동물 모습 (거두면 숨는다).")]
    [SerializeField] private GameObject caughtVisual; // 걸린 동물 모습

    private DayNightCycle cycle; // 낮과 밤
    private float waitedHours; // 기다린 게임 시간
    private float lastHour = -1f; // 지난번에 본 시각
    private int caughtToday; // 오늘 잡은 수
    private int lastDay = -1; // 마지막으로 본 날짜
    private TrapCatchEntry caught; // 지금 걸린 동물

    public bool HasCatch => caught != null; // 걸린 동물이 있는지 (검사용)
    public string CaughtName => caught != null ? caught.animalName : string.Empty; // 걸린 동물 이름 (검사용)
    public float WaitedHours => waitedHours; // 기다린 시간 (검사용)
    public int CatchesToday => caughtToday; // 오늘 잡은 수 (검사용)

    public override string PromptMessage => caught != null // 안내 문구
        ? $"F - {caught.animalName} 거두기"
        : caughtToday >= catchesPerDay
            ? "덫 : 오늘은 더 걸리지 않습니다"
            : $"덫 : 기다리는 중 ({Mathf.Max(0f, catchHours - waitedHours):0.0}시간)";

#if UNITY_EDITOR
    public void EditorAssign(float hours, int perDay, List<TrapCatchEntry> entries) // 생성 도구 전용
    {
        catchHours = hours;
        catchesPerDay = perDay;
        catches = entries ?? new List<TrapCatchEntry>();
    }
#endif

    private void Awake()
    {
        if (caughtVisual == null) // 걸린 동물 모습이 없으면 이름으로 찾는다
        {
            Transform found = transform.Find("CaughtVisual");
            caughtVisual = found != null ? found.gameObject : null;
        }

        ShowCaught(false);
    }

    private void Update()
    {
        if (cycle == null)
        {
            cycle = FindFirstObjectByType<DayNightCycle>();

            if (cycle == null)
            {
                return;
            }
        }

        if (lastDay != cycle.CurrentDay) // 날이 바뀌면 다시 잡을 수 있다
        {
            lastDay = cycle.CurrentDay;
            caughtToday = 0;
        }

        float hour = cycle.CurrentHour;

        if (lastHour < 0f)
        {
            lastHour = hour;
            return;
        }

        float passed = Mathf.Repeat(hour - lastHour, 24f);
        lastHour = hour;

        if (caught != null || caughtToday >= catchesPerDay || catches.Count == 0 || passed > 12f)
        {
            return;
        }

        waitedHours += passed;

        if (waitedHours < catchHours)
        {
            return;
        }

        waitedHours = 0f;
        caught = catches[UnityEngine.Random.Range(0, catches.Count)];
        ShowCaught(true);
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 1.2f, $"덫에 {caught.animalName}이(가) 걸렸어요!", new Color(0.95f, 0.85f, 0.55f, 1f), 1.8f);
    }

    public override void Interact(GameObject interactor) // 걸린 동물 거두기
    {
        if (caught == null || interactor == null)
        {
            return;
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            return;
        }

        if (caught.item != null)
        {
            int leftover = inventory.AddItem(caught.item, Mathf.Max(1, caught.amount));

            if (leftover >= Mathf.Max(1, caught.amount)) // 가방이 가득 차면 그대로 둔다
            {
                Debug.Log("가방이 가득 차 덫을 거두지 못했습니다.", this);
                return;
            }
        }

        if (caught.extraItem != null)
        {
            inventory.AddItem(caught.extraItem, Mathf.Max(1, caught.extraAmount));
        }

        Debug.Log($"덫에서 {caught.animalName}을(를) 거뒀습니다.", this);
        caught = null;
        caughtToday++;
        waitedHours = 0f;
        ShowCaught(false);
    }

    private void ShowCaught(bool visible)
    {
        if (caughtVisual != null)
        {
            caughtVisual.SetActive(visible);
        }
    }
}
