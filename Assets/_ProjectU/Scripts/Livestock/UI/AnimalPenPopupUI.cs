using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class AnimalPenPopupUI : MonoBehaviour, IGameScenePopup // 86일차: 닭장·외양간 창 (동물 카드 · 먹이 주기 · 수거)
{
    [Header("Root")] // 루트
    [Tooltip("켜고 끄는 창 전체.")]
    [SerializeField] private GameObject panelRoot; // 창 루트
    [Tooltip("제목.")]
    [SerializeField] private TMP_Text titleText; // 제목
    [Tooltip("제목 아이콘 (동물).")]
    [SerializeField] private Image titleIcon; // 제목 아이콘
    [Tooltip("우리 정보 알약.")]
    [SerializeField] private CookingChipUI infoChip; // 정보
    [Tooltip("닫기 버튼.")]
    [SerializeField] private Button closeButton; // 닫기
    [Tooltip("효과 아이콘 묶음 (허기·체력).")]
    [SerializeField] private FoodEffectIconSet iconSet; // 아이콘

    [Header("Cards")] // 카드
    [Tooltip("동물 카드 부모.")]
    [SerializeField] private Transform cardRoot; // 카드 부모
    [Tooltip("동물 카드 템플릿 (꺼진 상태).")]
    [SerializeField] private AnimalCardUI cardTemplate; // 카드 템플릿

    [Header("Footer")] // 아래쪽
    [Tooltip("가방 먹이 알약.")]
    [SerializeField] private CookingChipUI feedChip; // 먹이 알약
    [Tooltip("모두 먹이 주기 버튼.")]
    [SerializeField] private Button feedAllButton; // 모두 먹이
    [SerializeField] private TMP_Text feedAllLabel; // 모두 먹이 문구
    [Tooltip("모두 꺼내기 버튼.")]
    [SerializeField] private Button collectAllButton; // 모두 꺼내기
    [SerializeField] private TMP_Text collectAllLabel; // 모두 꺼내기 문구
    [Tooltip("하루 규칙 안내.")]
    [SerializeField] private TMP_Text hintText; // 안내
    [Tooltip("결과 알림.")]
    [SerializeField] private TMP_Text messageText; // 알림
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(0.5f)] private float messageDuration = 2.2f; // 알림 시간

    private readonly List<AnimalCardUI> cards = new List<AnimalCardUI>(); // 카드
    private GameUIManager manager; // 관리자
    private AnimalPen pen; // 현재 우리
    private PlayerInventory inventory; // 인벤토리
    private bool isDirty; // 다시 그리기 필요
    private float messageHideTime; // 알림 숨김 시각
    private int pendingReleaseIndex = -1; // 내보내기 확인 중인 카드

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && pen != null; // 열림 여부
    public AnimalPen Pen => pen; // 현재 우리
    public IReadOnlyList<AnimalCardUI> Cards => cards; // 카드

    private void Awake() // 버튼 연결
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose); // 닫기
        if (feedAllButton != null) feedAllButton.onClick.AddListener(FeedAll); // 모두 먹이
        if (collectAllButton != null) collectAllButton.onClick.AddListener(CollectAll); // 모두 꺼내기
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false); // 템플릿 숨김

        if (panelRoot != null && pen == null) // 시작 상태
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe(); // 해제
    }

    public bool ShowFromManager(GameUIManager owner, AnimalPen targetPen, PlayerInventory playerInventory) // 창 열기
    {
        if (panelRoot == null || targetPen == null || playerInventory == null || cardTemplate == null || targetPen.AcceptedAnimal == null) // 참조 확인
        {
            Debug.LogError("우리 창 참조가 누락되었습니다. Tools > Project U > Livestock > 1. Build Livestock Content를 다시 실행하세요.", this); // 오류
            return false; // 실패
        }

        Unsubscribe(); // 이전 구독 해제
        manager = owner; // 관리자
        pen = targetPen; // 우리
        inventory = playerInventory; // 인벤토리
        pen.StateChanged += MarkDirty; // 우리 변경 구독
        inventory.InventoryChanged += MarkDirty; // 인벤토리 변경 구독
        pendingReleaseIndex = -1; // 확인 초기화
        panelRoot.SetActive(true); // 표시
        ShowMessage(string.Empty, Color.clear, 0f); // 알림 초기화
        Rebuild(); // 그리기
        return true; // 성공
    }

    public void HideFromManager() // 창 닫기
    {
        Unsubscribe(); // 해제
        pen = null; // 우리 해제
        inventory = null; // 인벤토리 해제

        if (panelRoot != null) // 루트 확인
        {
            panelRoot.SetActive(false); // 숨김
        }
    }

    private void Unsubscribe() // 이벤트 해제
    {
        if (pen != null) pen.StateChanged -= MarkDirty; // 우리
        if (inventory != null) inventory.InventoryChanged -= MarkDirty; // 인벤토리
    }

    private void MarkDirty() // 다시 그리기 요청
    {
        isDirty = true; // 기록
    }

    private void Update() // 상태 갱신
    {
        if (panelRoot == null || !panelRoot.activeSelf) // 열림 확인
        {
            return; // 생략
        }

        if (pen == null || !pen.isActiveAndEnabled) // 우리가 사라짐
        {
            RequestClose(); // 닫기
            return; // 생략
        }

        if (isDirty) // 변경 확인
        {
            Rebuild(); // 다시 그리기
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime) // 알림 시간
        {
            messageText.gameObject.SetActive(false); // 숨김
        }
    }

    private void RequestClose() // 닫기 요청
    {
        if (manager != null) // 관리자 확인
        {
            manager.CloseAnimalPen(); // 관리자 통해 닫기
            return; // 완료
        }

        HideFromManager(); // 직접 닫기
    }

    // ------------------------------------------------------------ 그리기

    private void Rebuild() // 전체 다시 그리기
    {
        isDirty = false; // 기록 해제

        if (pen == null || inventory == null) // 상태 확인
        {
            return; // 생략
        }

        AnimalData animal = pen.AcceptedAnimal; // 동물 종류
        int feedInBag = animal.FeedItem != null ? inventory.GetItemQuantity(animal.FeedItem) : 0; // 가방 먹이
        Sprite productSprite = animal.ProductItem != null ? animal.ProductItem.Icon : null; // 생산물 아이콘

        titleText.SetText(pen.PenDisplayName); // 제목
        titleIcon.sprite = animal.Icon; // 아이콘
        titleIcon.enabled = animal.Icon != null; // 표시
        infoChip.Bind($"{pen.Animals.Count} / {pen.Capacity} {animal.PluralName}  ·  {pen.ProductCount} {animal.ProductItem.DisplayName} READY", ProjectUUIPalette.Accent, productSprite); // 정보

        for (int index = 0; index < pen.Capacity; index++) // 카드 그리기
        {
            AnimalCardUI card = GetCard(index); // 카드

            if (index < pen.Animals.Count) // 동물 카드
            {
                card.BindAnimal(index, pen, pen.Animals[index], feedInBag, pendingReleaseIndex == index, iconSet, productSprite, HandleCard); // 표시
            }
            else // 빈 자리
            {
                card.BindEmpty(index, animal, feedInBag, HandleCard); // 표시
            }
        }

        for (int index = pen.Capacity; index < cards.Count; index++) // 남는 카드
        {
            cards[index].gameObject.SetActive(false); // 숨김
        }

        int hungry = pen.HungryCount; // 굶은 동물
        int needed = pen.FeedNeededToday; // 필요한 먹이
        feedChip.Bind($"{animal.FeedItem.DisplayName}  {feedInBag} IN BAG", feedInBag >= needed ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, animal.FeedItem.Icon); // 먹이 알약
        feedAllButton.interactable = hungry > 0 && feedInBag >= animal.FeedPerDay; // 모두 먹이 가능
        feedAllLabel.SetText(hungry > 0 ? $"FEED ALL  x{needed}" : "ALL FED"); // 문구
        int products = pen.ProductCount; // 생산물
        collectAllButton.interactable = products > 0; // 꺼내기 가능
        collectAllLabel.SetText(products > 0 ? $"TAKE ALL  x{products}" : "NOTHING READY"); // 문구
        hintText.SetText($"FEED {animal.FeedPerDay} A DAY  ·  {animal.ProductItem.DisplayName} EVERY {(animal.ProductIntervalDays == 1 ? "DAY" : animal.ProductIntervalDays + " DAYS")} WHEN FED  ·  PET ONCE A DAY"); // 안내
    }

    private AnimalCardUI GetCard(int index) // 카드 가져오기
    {
        while (cards.Count <= index) // 부족한 카드
        {
            AnimalCardUI created = Instantiate(cardTemplate, cardRoot); // 복제
            created.name = $"Card_{cards.Count:00}"; // 이름
            cards.Add(created); // 추가
        }

        cards[index].transform.SetSiblingIndex(index + 1); // 순서 (템플릿 다음)
        return cards[index]; // 결과 반환
    }

    // ------------------------------------------------------------ 입력

    public void HandleCard(int index, AnimalCardUI.CardAction action) // 카드 버튼 처리
    {
        if (pen == null) // 상태 확인
        {
            return; // 생략
        }

        if (action != AnimalCardUI.CardAction.Release) // 다른 버튼
        {
            pendingReleaseIndex = -1; // 확인 취소
        }

        string message; // 알림

        switch (action) // 버튼 분기
        {
            case AnimalCardUI.CardAction.Feed:
                ShowResult(pen.TryFeed(index, inventory, out message), message); // 먹이
                break;

            case AnimalCardUI.CardAction.Pet:
                ShowResult(pen.TryPet(index, out message), message); // 쓰다듬기
                break;

            case AnimalCardUI.CardAction.Collect:
                int taken = pen.TryCollect(index, inventory); // 꺼내기
                ShowResult(taken > 0, taken > 0 ? $"TOOK {taken} {pen.AcceptedAnimal.ProductItem.DisplayName}" : "INVENTORY FULL"); // 알림
                break;

            case AnimalCardUI.CardAction.Attract:
                ShowResult(pen.TryAttract(inventory, out message), message); // 불러오기
                break;

            case AnimalCardUI.CardAction.Release:
                if (pendingReleaseIndex != index) // 첫 클릭
                {
                    pendingReleaseIndex = index; // 확인 대기
                    ShowMessage("CLICK AGAIN TO RELEASE", ProjectUUIPalette.Danger, messageDuration); // 안내
                    break;
                }

                pendingReleaseIndex = -1; // 확인 완료
                string releasedName = index < pen.Animals.Count ? pen.Animals[index].DisplayName : string.Empty; // 이름
                ShowResult(pen.Release(index), $"{releasedName} WANDERED OFF"); // 내보내기
                break;
        }

        Rebuild(); // 다시 그리기
    }

    public void FeedAll() // 모두 먹이 주기
    {
        if (pen == null) // 상태 확인
        {
            return; // 생략
        }

        pendingReleaseIndex = -1; // 확인 취소
        int fed = pen.FeedAll(inventory); // 먹이
        ShowResult(fed > 0, fed > 0 ? $"FED {fed} {(fed == 1 ? pen.AcceptedAnimal.DisplayName : pen.AcceptedAnimal.PluralName)}" : $"NOT ENOUGH {pen.AcceptedAnimal.FeedItem.DisplayName}"); // 알림
        Rebuild(); // 다시 그리기
    }

    public void CollectAll() // 모두 꺼내기
    {
        if (pen == null) // 상태 확인
        {
            return; // 생략
        }

        pendingReleaseIndex = -1; // 확인 취소
        int taken = pen.CollectAll(inventory); // 꺼내기
        ShowResult(taken > 0, taken > 0 ? $"TOOK {taken} {pen.AcceptedAnimal.ProductItem.DisplayName}" : "INVENTORY FULL"); // 알림
        Rebuild(); // 다시 그리기
    }

    private void ShowResult(bool success, string text) // 결과 알림
    {
        ShowMessage(text, success ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger, messageDuration); // 표시
    }

    private void ShowMessage(string text, Color color, float duration) // 알림 표시
    {
        if (messageText == null) // 확인
        {
            return; // 생략
        }

        bool visible = !string.IsNullOrEmpty(text) && duration > 0f; // 표시 여부
        messageText.gameObject.SetActive(visible); // 표시
        messageText.SetText(text); // 문구
        messageText.color = color; // 색
        messageHideTime = Time.unscaledTime + duration; // 숨김 시각
    }
}
