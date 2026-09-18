using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcDialoguePopup : MonoBehaviour, IGameScenePopup // 91일차: NPC 대화 창 (초상 · 이름 · 호감도 · 대사 · 대화/선물/거래)
{
    [Header("Root")] // 루트
    [Tooltip("켜고 끄는 창 전체.")]
    [SerializeField] private GameObject panelRoot; // 창 루트

    [Header("Header")] // 머리글
    [SerializeField] private Image portraitFrame; // 초상 테두리 (대표 색)
    [SerializeField] private Image portrait; // 초상
    [SerializeField] private TMP_Text nameText; // 이름
    [SerializeField] private TMP_Text jobText; // 종족 · 직업
    [SerializeField] private TMP_Text stageText; // 관계 단계 · 호감도
    [SerializeField] private RectTransform affinityFill; // 호감도 막대

    [Header("Line")] // 대사
    [SerializeField] private TMP_Text lineText; // 대사
    [SerializeField] private Button lineButton; // 대사 누르면 전체 표시
    [SerializeField] private TMP_Text messageText; // 결과 알림 (호감도 변화 등)
    [Tooltip("초당 글자 수.")]
    [SerializeField, Min(5f)] private float charactersPerSecond = 42f; // 글자 속도
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(0.5f)] private float messageDuration = 3f; // 알림 시간
    [Tooltip("플레이어가 이보다 멀어지면 창을 닫습니다.")]
    [SerializeField, Min(1f)] private float closeDistance = 4.5f; // 닫는 거리

    [Header("Buttons")] // 버튼
    [SerializeField] private Button talkButton; // 대화하기
    [SerializeField] private Button giftButton; // 선물하기
    [SerializeField] private TMP_Text giftLabel; // 선물 문구
    [SerializeField] private Button tradeButton; // 거래
    [SerializeField] private Button closeButton; // 닫기

    [Header("Gift")] // 선물
    [SerializeField] private GameObject giftPanel; // 선물 목록 창
    [SerializeField] private Transform giftListRoot; // 목록 부모
    [SerializeField] private NpcGiftSlotUI giftSlotTemplate; // 칸 템플릿 (꺼진 상태)
    [SerializeField] private TMP_Text giftEmptyText; // 빈 목록 문구
    [SerializeField] private Button giftCancelButton; // 취소

    private readonly List<NpcGiftSlotUI> giftSlots = new List<NpcGiftSlotUI>(); // 선물 칸
    private static readonly Dictionary<string, (int day, int index)> talkProgress = new Dictionary<string, (int, int)>(); // NPC별 오늘 대화 순서
    private GameUIManager manager; // 관리자
    private NpcAgent agent; // 대화 중인 NPC
    private NpcRelationshipManager relations; // 관계
    private NpcManager npcManager; // NPC 관리자
    private PlayerInventory inventory; // 인벤토리
    private Transform player; // 플레이어
    private List<string> talkLines = new List<string>(); // 대화하기 대사
    private TMP_Text tradeLabel; // 거래 버튼 문구 (92일차: 닫힘 표시)
    private float visibleCharacters; // 보이는 글자 수
    private float messageHideTime; // 알림 숨김 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && agent != null; // 열림 여부
    public NpcAgent Agent => agent; // 대화 중인 NPC (테스트용)
    public string CurrentLine => lineText != null ? lineText.text : string.Empty; // 현재 대사 (테스트용)
    public string MessageLabel => messageText != null && messageText.gameObject.activeSelf ? messageText.text : string.Empty; // 알림 (테스트용)
    public bool IsGiftPanelOpen => giftPanel != null && giftPanel.activeSelf; // 선물 창 여부 (테스트용)
    public IReadOnlyList<NpcGiftSlotUI> GiftSlots => giftSlots; // 선물 칸 (테스트용)
    public bool CanTrade => tradeButton != null && tradeButton.gameObject.activeSelf; // 거래 버튼 여부 (테스트용)
    public string TradeLabel => tradeLabel != null ? tradeLabel.text : string.Empty; // 거래 버튼 문구 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (talkButton != null) talkButton.onClick.AddListener(Talk);
        if (giftButton != null) giftButton.onClick.AddListener(OpenGiftPanel);
        if (tradeButton != null) tradeButton.onClick.AddListener(Trade);
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
        if (lineButton != null) lineButton.onClick.AddListener(FinishTyping);
        if (giftCancelButton != null) giftCancelButton.onClick.AddListener(CloseGiftPanel);
        if (giftSlotTemplate != null) giftSlotTemplate.gameObject.SetActive(false);

        if (panelRoot != null && agent == null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe();
    }

    public bool ShowFromManager(GameUIManager owner, NpcAgent target, PlayerInventory playerInventory) // 창 열기
    {
        NpcRelationshipManager targetRelations = NpcRelationshipManager.Instance;
        NpcManager targetManager = NpcManager.Instance;

        if (panelRoot == null || lineText == null || giftSlotTemplate == null || target == null || target.Character == null || playerInventory == null || targetRelations == null || targetManager == null)
        {
            Debug.LogError("NPC 대화 창 참조가 누락되었습니다. Tools > Project U > Build Content > 9. NPC Dialogue를 다시 실행하세요.", this);
            return false;
        }

        Unsubscribe();
        manager = owner;
        agent = target;
        relations = targetRelations;
        npcManager = targetManager;
        inventory = playerInventory;
        player = playerInventory.transform;
        relations.AffinityChanged += HandleAffinityChanged;

        NpcCharacterData character = agent.Character;
        int day = npcManager.CurrentDay;
        bool firstMeeting = relations.MarkMet(character);
        AffinityStage before = relations.GetStage(character);
        int points = relations.Talk(character, day); // 오늘 첫 대화 점수
        AffinityStage stage = relations.GetStage(character);
        string opening = NpcDialogueSelector.Opening(character, stage, firstMeeting, day);

        talkLines = NpcDialogueSelector.TalkLines(character, stage, npcManager.CurrentSeason, npcManager.CurrentWeather);
        agent.SetTalking(true, player.position);
        panelRoot.SetActive(true);
        CloseGiftPanel();
        ShowMessage(string.Empty, Color.clear);

        if (points > 0)
        {
            ShowMessage(StageMessage(character, $"오늘 첫 대화 · 호감도 +{points}", before, stage), ProjectUUIPalette.Accent);
        }

        ShowLine(opening);
        RefreshHeader();
        return true;
    }

    public void HideFromManager() // 창 닫기
    {
        Unsubscribe();

        if (agent != null)
        {
            agent.SetTalking(false, Vector3.zero);
        }

        agent = null;
        relations = null;
        npcManager = null;
        inventory = null;
        manager = null;

        if (giftPanel != null) giftPanel.SetActive(false);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Unsubscribe() // 이벤트 해제
    {
        if (relations != null)
        {
            relations.AffinityChanged -= HandleAffinityChanged;
        }
    }

    private void Update() // 글자 표시 · 거리 확인 · 키보드
    {
        if (!IsOpen)
        {
            return;
        }

        if (agent.IsInside || player == null || (player.position - agent.transform.position).sqrMagnitude > closeDistance * closeDistance)
        {
            RequestClose(); // NPC가 멀어지거나 집에 들어감
            return;
        }

        if (lineText.maxVisibleCharacters < lineText.textInfo.characterCount)
        {
            visibleCharacters += Time.unscaledDeltaTime * charactersPerSecond;
            lineText.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visibleCharacters), 99999);
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && !IsGiftPanelOpen && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            Talk(); // 스페이스·엔터 = 다음 말
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------ 조작

    public void Talk() // 대화하기 (글자가 나오는 중이면 먼저 전체 표시)
    {
        if (!IsOpen)
        {
            return;
        }

        if (IsTyping)
        {
            FinishTyping();
            return;
        }

        string id = agent.CharacterId;
        int day = npcManager.CurrentDay;
        int index = talkProgress.TryGetValue(id, out (int day, int index) progress) && progress.day == day ? progress.index : 0;
        talkProgress[id] = (day, index + 1);
        ShowLine(talkLines.Count > 0 ? talkLines[index % talkLines.Count] : NpcDialogueSelector.Silent);
    }

    public void OpenGiftPanel() // 선물하기
    {
        if (!IsOpen)
        {
            return;
        }

        if (relations.GiftsLeftToday(agent.Character, npcManager.CurrentDay) <= 0)
        {
            ShowMessage("오늘은 이미 선물을 받았어요. 내일 다시 주세요.", ProjectUUIPalette.TextSecondary);
            return;
        }

        FinishTyping();
        RebuildGiftList();
        giftPanel.SetActive(true);
    }

    public void CloseGiftPanel() // 선물 창 닫기
    {
        if (giftPanel != null)
        {
            giftPanel.SetActive(false);
        }
    }

    public bool Give(ItemData item) // 선물 주기
    {
        if (!IsOpen || item == null || inventory.GetItemQuantity(item) <= 0)
        {
            return false;
        }

        NpcCharacterData character = agent.Character;
        int day = npcManager.CurrentDay;
        bool birthday = character.IsBirthday(npcManager.CurrentSeason, npcManager.CurrentDayInSeason);
        AffinityStage before = relations.GetStage(character);
        NpcGiftResult result = relations.GiveGift(character, item, day, birthday);

        if (!result.Accepted)
        {
            ShowMessage(result.Reason, ProjectUUIPalette.TextSecondary);
            CloseGiftPanel();
            RefreshHeader();
            return false;
        }

        inventory.RemoveItem(item, 1);
        CloseGiftPanel();
        ShowLine(NpcDialogueSelector.GiftReaction(character, result.Preference, result.Birthday));
        string birthdayText = result.Birthday ? "생일 선물 · " : string.Empty;
        Color color = result.Points > 0 ? ProjectUUIPalette.Accent : result.Points < 0 ? ProjectUUIPalette.Danger : ProjectUUIPalette.TextSecondary;
        ShowMessage(StageMessage(character, $"{birthdayText}{item.DisplayName} · {NpcDialogueSelector.PreferenceName(result.Preference)} · 호감도 {result.Points:+0;-0;0}", before, relations.GetStage(character)), color);
        RefreshHeader();
        return true;
    }

    public void Trade() // 거래 (92일차: NPC 상점 · 영업 시간이 아니면 이유 안내 / 91일차: 가판대 상인)
    {
        if (!IsOpen)
        {
            return;
        }

        NpcShopManager shops = NpcShopManager.Instance;

        if (shops != null && shops.TryGetShop(agent.Character, out NpcShopData shop))
        {
            NpcShopStatus status = shops.GetStatus(shop, agent);

            if (!status.IsOpen)
            {
                FinishTyping();
                ShowMessage($"{status.Reason}\n오늘 영업 : {status.TodayHours}", ProjectUUIPalette.TextSecondary);
                return;
            }

            GameUIManager owner = manager;
            NpcAgent shopOwner = agent;

            if (owner == null || !owner.OpenNpcShop(shopOwner)) // 대화 창을 숨기고 상점 창으로 바로 넘어감
            {
                ShowMessage("상점 창을 열 수 없어요. Build Content > 10. NPC Shops를 실행하세요.", ProjectUUIPalette.Danger);
            }

            return;
        }

        MarketStall stall = npcManager.GetStallFor(agent);
        GameObject interactor = player != null ? player.gameObject : null;
        RequestClose();

        if (stall != null && interactor != null)
        {
            stall.Interact(interactor);
        }
    }

    private void RequestClose() // 닫기 요청
    {
        if (manager != null)
        {
            manager.CloseNpcDialogue();
            return;
        }

        HideFromManager();
    }

    // ------------------------------------------------------------ 표시

    private bool IsTyping => lineText != null && lineText.maxVisibleCharacters < lineText.textInfo.characterCount;

    private void ShowLine(string line) // 대사 한 글자씩 표시 시작
    {
        lineText.text = line;
        lineText.ForceMeshUpdate();
        visibleCharacters = 0f;
        lineText.maxVisibleCharacters = 0;
    }

    private void FinishTyping() // 대사 전체 표시
    {
        if (lineText != null)
        {
            lineText.maxVisibleCharacters = 99999;
            visibleCharacters = 99999f;
        }
    }

    private void RefreshHeader() // 이름 · 초상 · 호감도 · 버튼
    {
        NpcCharacterData character = agent.Character;
        int affinity = relations.GetAffinity(character);
        AffinityStage stage = relations.GetStage(character);

        if (portrait != null)
        {
            portrait.sprite = character.Portrait;
            portrait.enabled = character.Portrait != null;
        }

        if (portraitFrame != null)
        {
            portraitFrame.color = character.ThemeColor;
        }

        nameText.text = character.DisplayName;
        jobText.text = $"{character.Profile.raceName} · {character.Profile.jobName}";
        stageText.text = $"{NpcDialogueSelector.StageName(stage)} · 호감도 {affinity}/{character.MaxAffinity}";

        if (affinityFill != null)
        {
            affinityFill.anchorMax = new Vector2(Mathf.Clamp01(affinity / (float)character.MaxAffinity), 1f);
        }

        bool canGift = relations.GiftsLeftToday(character, npcManager.CurrentDay) > 0;

        if (giftLabel != null)
        {
            giftLabel.text = canGift ? "선물하기" : "선물 완료";
        }

        if (tradeButton != null)
        {
            NpcShopManager shops = NpcShopManager.Instance;
            NpcShopData shop = null;
            bool hasShop = shops != null && shops.TryGetShop(character, out shop);
            bool open = hasShop ? shops.GetStatus(shop, agent).IsOpen : npcManager.GetStallFor(agent) != null;
            tradeButton.gameObject.SetActive(hasShop || open); // 92일차: 상점 주인은 영업 시간이 아니어도 버튼을 보여 주고 이유를 안내

            if (tradeLabel == null)
            {
                tradeLabel = tradeButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (tradeLabel != null)
            {
                tradeLabel.text = open ? "거래" : "거래 (닫힘)";
            }
        }
    }

    private void RebuildGiftList() // 가방의 아이템 목록
    {
        Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
        List<ItemData> order = new List<ItemData>();

        for (int index = 0; index < inventory.SlotCapacity; index++)
        {
            InventorySlot slot = inventory.GetSlot(index);

            if (slot == null || slot.ItemData == null || slot.Quantity <= 0)
            {
                continue;
            }

            if (!counts.ContainsKey(slot.ItemData))
            {
                counts[slot.ItemData] = 0;
                order.Add(slot.ItemData);
            }

            counts[slot.ItemData] += slot.Quantity;
        }

        while (giftSlots.Count < order.Count)
        {
            NpcGiftSlotUI created = Instantiate(giftSlotTemplate, giftListRoot);
            created.name = $"GiftSlot_{giftSlots.Count:00}";
            giftSlots.Add(created);
        }

        for (int index = 0; index < giftSlots.Count; index++)
        {
            bool used = index < order.Count;
            giftSlots[index].gameObject.SetActive(used);

            if (used)
            {
                ItemData item = order[index];
                giftSlots[index].Bind(item, counts[item], () => Give(item));
            }
        }

        if (giftEmptyText != null)
        {
            giftEmptyText.gameObject.SetActive(order.Count == 0);
        }
    }

    private void ShowMessage(string message, Color color) // 알림
    {
        if (messageText == null)
        {
            return;
        }

        bool show = !string.IsNullOrEmpty(message);
        messageText.gameObject.SetActive(show);

        if (show)
        {
            messageText.text = message;
            messageText.color = color;
            messageHideTime = Time.unscaledTime + messageDuration;
        }
    }

    private void HandleAffinityChanged(NpcCharacterData character, int delta, AffinityStage before, AffinityStage after) // 머리 위 숫자
    {
        if (agent == null || character != agent.Character)
        {
            return;
        }

        Color color = delta > 0 ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger;
        CombatDamagePopup.SpawnText(agent.transform.position + Vector3.up * 2.3f, $"호감도 {delta:+0;-0}", color, 1.6f);
    }

    private static string StageMessage(NpcCharacterData character, string message, AffinityStage before, AffinityStage after) // 단계가 바뀌면 알림 덧붙이기
    {
        if (after == before)
        {
            return message;
        }

        string particle = HasFinalConsonant(character.DisplayName) ? "과" : "와";
        string change = after > before ? "가까워졌어요" : "멀어졌어요";
        return $"{message}\n{character.DisplayName}{particle}의 관계가 '{NpcDialogueSelector.StageName(after)}'(으)로 {change}!";
    }

    private static bool HasFinalConsonant(string word) // 마지막 글자 받침 여부 (와/과)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        char last = word[word.Length - 1];
        return last >= '\uAC00' && last <= '\uD7A3' && (last - '\uAC00') % 28 != 0;
    }
}
