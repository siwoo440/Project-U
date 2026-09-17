using System; // 이벤트 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class AnimalCardUI : MonoBehaviour // 86일차: 우리 창의 동물 한 마리 (또는 빈 자리) 카드
{
    public enum CardAction // 카드 버튼 종류
    {
        Feed = 0, // 먹이 주기
        Pet = 1, // 쓰다듬기
        Collect = 2, // 생산물 꺼내기
        Release = 3, // 내보내기
        Attract = 4 // 빈 자리에 불러오기
    }

    [Header("Frame")] // 틀
    [Tooltip("카드 테두리.")]
    [SerializeField] private Image outline; // 테두리
    [Tooltip("동물 카드 내용.")]
    [SerializeField] private GameObject contentRoot; // 내용
    [Tooltip("빈 자리 내용.")]
    [SerializeField] private GameObject emptyRoot; // 빈 자리

    [Header("Animal")] // 동물 정보
    [Tooltip("동물 아이콘.")]
    [SerializeField] private Image icon; // 아이콘
    [Tooltip("개체 이름.")]
    [SerializeField] private TMP_Text nameText; // 이름
    [Tooltip("기분 문구.")]
    [SerializeField] private TMP_Text moodText; // 기분
    [Tooltip("기분 막대 (Filled).")]
    [SerializeField] private Image moodFill; // 기분 막대
    [Tooltip("먹이 상태 알약.")]
    [SerializeField] private CookingChipUI feedChip; // 먹이 상태
    [Tooltip("생산 상태 알약.")]
    [SerializeField] private CookingChipUI productChip; // 생산 상태

    [Header("Buttons")] // 버튼
    [SerializeField] private Button feedButton; // 먹이
    [SerializeField] private TMP_Text feedLabel; // 먹이 문구
    [SerializeField] private Button petButton; // 쓰다듬기
    [SerializeField] private TMP_Text petLabel; // 쓰다듬기 문구
    [SerializeField] private Button collectButton; // 꺼내기
    [SerializeField] private TMP_Text collectLabel; // 꺼내기 문구
    [SerializeField] private Button releaseButton; // 내보내기
    [SerializeField] private TMP_Text releaseLabel; // 내보내기 문구
    [SerializeField] private Button attractButton; // 불러오기
    [SerializeField] private TMP_Text attractLabel; // 불러오기 문구
    [SerializeField] private TMP_Text emptyTitle; // 빈 자리 제목

    private Action<int, CardAction> clicked; // 클릭 콜백
    private int cardIndex; // 카드 번호

    private void Awake() // 버튼 연결
    {
        Hook(feedButton, CardAction.Feed); // 먹이
        Hook(petButton, CardAction.Pet); // 쓰다듬기
        Hook(collectButton, CardAction.Collect); // 꺼내기
        Hook(releaseButton, CardAction.Release); // 내보내기
        Hook(attractButton, CardAction.Attract); // 불러오기
    }

    private void Hook(Button button, CardAction action) // 버튼 연결 도우미
    {
        if (button != null) // 버튼 확인
        {
            button.onClick.AddListener(() => clicked?.Invoke(cardIndex, action)); // 연결
        }
    }

    public void BindAnimal(int index, AnimalPen pen, PenAnimal animal, int feedInBag, bool confirmRelease, FoodEffectIconSet icons, Sprite productSprite, Action<int, CardAction> onClick) // 동물 카드 표시
    {
        cardIndex = index; // 번호
        clicked = onClick; // 콜백
        gameObject.SetActive(true); // 표시
        contentRoot.SetActive(true); // 내용
        emptyRoot.SetActive(false); // 빈 자리 숨김
        AnimalData data = animal.Data; // 종류

        icon.sprite = data.Icon; // 아이콘
        icon.enabled = data.Icon != null; // 없으면 숨김
        nameText.SetText(animal.DisplayName); // 이름

        AnimalMoodLevel level = animal.MoodLevel; // 기분 단계
        Color moodColor = GetMoodColor(level); // 기분 색
        moodText.SetText($"{GetMoodLabel(level)}  {animal.Mood}"); // 기분 문구
        moodText.color = moodColor; // 색
        moodFill.fillAmount = animal.Mood / (float)PenAnimal.MaxMood; // 막대
        moodFill.color = moodColor; // 막대 색

        if (animal.FedToday) // 먹음
        {
            feedChip.Bind("FED TODAY", ProjectUUIPalette.Teal, icons != null ? icons.Get(new FoodEffectEntry { Kind = FoodEffectKind.Hunger }) : null); // 먹음
        }
        else // 굶음
        {
            feedChip.Bind("HUNGRY", ProjectUUIPalette.Danger, icons != null ? icons.Get(new FoodEffectEntry { Kind = FoodEffectKind.Hunger }) : null); // 굶음
        }

        string productName = data.ProductItem.DisplayName; // 생산물 이름

        if (animal.HasProduct) // 생산물 있음
        {
            productChip.Bind($"{productName} x{animal.ProductReady} READY", ProjectUUIPalette.Accent, productSprite); // 준비
        }
        else if (level == AnimalMoodLevel.Sad) // 우울
        {
            productChip.Bind("TOO SAD TO PRODUCE", ProjectUUIPalette.Danger, productSprite); // 생산 안 함
        }
        else // 다음 생산
        {
            int days = animal.DaysUntilProduct; // 남은 날
            string when = animal.FedToday && days <= 1 ? "TOMORROW" : days <= 1 ? "TOMORROW IF FED" : $"IN {days} FED DAYS"; // 시점
            productChip.Bind($"{productName} {when}", ProjectUUIPalette.TextSecondary, productSprite); // 다음 생산
        }

        int feedCost = data.FeedPerDay; // 먹이 수량
        feedButton.interactable = !animal.FedToday && feedInBag >= feedCost; // 먹이 가능
        feedLabel.SetText(animal.FedToday ? "FED" : $"FEED  x{feedCost}"); // 문구
        petButton.interactable = !animal.PettedToday; // 쓰다듬기 가능
        petLabel.SetText(animal.PettedToday ? "PETTED" : "PET"); // 문구
        collectButton.interactable = animal.HasProduct; // 꺼내기 가능
        collectLabel.SetText(animal.HasProduct ? $"TAKE  x{animal.ProductReady}" : "TAKE"); // 문구
        releaseLabel.SetText(confirmRelease ? "SURE?" : "RELEASE"); // 문구
        releaseLabel.color = confirmRelease ? ProjectUUIPalette.Danger : ProjectUUIPalette.TextSecondary; // 색
        outline.color = animal.HasProduct ? ProjectUUIPalette.Accent : animal.FedToday ? new Color(0.31f, 0.76f, 0.69f, 0.55f) : new Color(0.88f, 0.34f, 0.31f, 0.55f); // 테두리
    }

    public void BindEmpty(int index, AnimalData animal, int feedInBag, Action<int, CardAction> onClick) // 빈 자리 카드 표시
    {
        cardIndex = index; // 번호
        clicked = onClick; // 콜백
        gameObject.SetActive(true); // 표시
        contentRoot.SetActive(false); // 내용 숨김
        emptyRoot.SetActive(true); // 빈 자리
        outline.color = new Color(1f, 1f, 1f, 0.1f); // 테두리
        int cost = animal != null ? animal.AttractFeedCost : 0; // 비용
        string feedName = animal != null && animal.FeedItem != null ? animal.FeedItem.DisplayName : "FEED"; // 먹이 이름
        emptyTitle.SetText("EMPTY SPACE"); // 제목
        attractLabel.SetText(animal != null ? $"ATTRACT {animal.DisplayName}\n<size=75%>{cost} {feedName}  ({feedInBag} IN BAG)</size>" : "NO ANIMAL"); // 문구
        attractButton.interactable = animal != null && feedInBag >= cost; // 가능 여부
    }

    public static string GetMoodLabel(AnimalMoodLevel level) // 기분 문구
    {
        switch (level) // 단계 분기
        {
            case AnimalMoodLevel.Happy: return "HAPPY"; // 행복
            case AnimalMoodLevel.Content: return "CONTENT"; // 보통
            default: return "SAD"; // 우울
        }
    }

    public static Color GetMoodColor(AnimalMoodLevel level) // 기분 색
    {
        switch (level) // 단계 분기
        {
            case AnimalMoodLevel.Happy: return ProjectUUIPalette.Teal; // 청록
            case AnimalMoodLevel.Content: return ProjectUUIPalette.Accent; // 주황
            default: return ProjectUUIPalette.Danger; // 빨강
        }
    }
}
