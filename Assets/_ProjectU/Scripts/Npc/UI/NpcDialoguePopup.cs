using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcDialoguePopup : MonoBehaviour, IGameScenePopup // 91일차: NPC 대화 창 (초상 · 이름 · 호감도 · 대사 · 대화/선물/거래) / 93일차: 의뢰 전달 / 94일차: 이벤트 / 95일차: 한 주 선물 제한
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
    [SerializeField] private Button questButton; // 의뢰 전달 (93일차)
    [SerializeField] private TMP_Text questLabel; // 의뢰 버튼 문구 (진행도)
    [SerializeField] private Button closeButton; // 닫기
    [SerializeField] private Button companionButton; // 109일차: 함께 가자 · 이제 돌아가
    [SerializeField] private TMP_Text companionLabel; // 동료 버튼 문구

    [Header("Event")] // 94일차: 하트 이벤트 선택지
    [SerializeField] private Button[] choiceButtons = new Button[0]; // 선택지 버튼
    [SerializeField] private TMP_Text[] choiceLabels = new TMP_Text[0]; // 선택지 문구

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
    private TMP_Text talkLabel; // 대화하기 버튼 문구 (94일차: 이벤트 중 "다음")
    private const int EventPhaseNone = 0; // 이벤트 없음
    private const int EventPhaseLines = 1; // 선택지 전 대사
    private const int EventPhaseChoosing = 2; // 선택지 고르는 중
    private const int EventPhaseAfter = 3; // 대답 · 마무리 대사
    private readonly List<string> eventQueue = new List<string>(); // 남은 이벤트 대사
    private NpcEventBook.Event activeEvent; // 진행 중인 이벤트
    private int eventPhase; // 이벤트 단계
    private int eventChoice = -1; // 고른 선택지
    private float visibleCharacters; // 보이는 글자 수
    private float messageHideTime; // 알림 숨김 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && agent != null; // 열림 여부
    public NpcAgent Agent => agent; // 대화 중인 NPC (테스트용)
    public string CurrentLine => lineText != null ? lineText.text : string.Empty; // 현재 대사 (테스트용)
    public string MessageLabel => messageText != null && messageText.gameObject.activeSelf ? messageText.text : string.Empty; // 알림 (테스트용)
    public bool IsGiftPanelOpen => giftPanel != null && giftPanel.activeSelf; // 선물 창 여부 (테스트용)
    public string GiftLabel => giftLabel != null ? giftLabel.text : string.Empty; // 선물 버튼 문구 (95일차 테스트용)
    public IReadOnlyList<NpcGiftSlotUI> GiftSlots => giftSlots; // 선물 칸 (테스트용)
    public bool CanTrade => tradeButton != null && tradeButton.gameObject.activeSelf; // 거래 버튼 여부 (테스트용)
    public string TradeLabel => tradeLabel != null ? tradeLabel.text : string.Empty; // 거래 버튼 문구 (테스트용)
    public bool CanDeliverQuest => questButton != null && questButton.gameObject.activeSelf; // 의뢰 버튼 여부 (테스트용)
    public bool CanToggleCompanion => companionButton != null && companionButton.gameObject.activeSelf; // 동료 버튼 여부 (109일차 테스트용)
    public string CompanionLabel => companionLabel != null ? companionLabel.text : string.Empty; // 동료 버튼 문구 (테스트용)
    public string QuestLabel => questLabel != null ? questLabel.text : string.Empty; // 의뢰 버튼 문구 (테스트용)
    public NpcEventBook.Event ActiveEvent => activeEvent; // 진행 중 이벤트 (테스트용)
    public bool IsChoosing => activeEvent != null && eventPhase == EventPhaseChoosing; // 선택지 표시 중 (테스트용)
    public int VisibleChoiceCount => System.Array.FindAll(choiceButtons, button => button != null && button.gameObject.activeSelf).Length; // 보이는 선택지 수 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (talkButton != null) talkButton.onClick.AddListener(Talk);
        if (giftButton != null) giftButton.onClick.AddListener(OpenGiftPanel);
        if (tradeButton != null) tradeButton.onClick.AddListener(Trade);
        if (questButton != null) questButton.onClick.AddListener(DeliverQuest);
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
        if (companionButton != null) companionButton.onClick.AddListener(ToggleCompanion); // 109일차
        if (lineButton != null) lineButton.onClick.AddListener(FinishTyping);
        if (giftCancelButton != null) giftCancelButton.onClick.AddListener(CloseGiftPanel);
        if (giftSlotTemplate != null) giftSlotTemplate.gameObject.SetActive(false);

        for (int index = 0; index < choiceButtons.Length; index++) // 94일차: 선택지
        {
            int choice = index;

            if (choiceButtons[index] != null)
            {
                choiceButtons[index].onClick.AddListener(() => Choose(choice));
                choiceButtons[index].gameObject.SetActive(false);
            }
        }

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

        activeEvent = null;
        eventPhase = EventPhaseNone;
        NpcEventManager events = NpcEventManager.Instance;

        if (events != null && events.TryGetReadyEvent(agent, out NpcEventBook.Event ready))
        {
            BeginEvent(ready); // 94일차: 기다리던 하트 이벤트가 있으면 인사 대신 이야기 시작
        }
        else
        {
            ShowLine(opening);
        }

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
        activeEvent = null; // 94일차: 이벤트 도중 닫으면 기록하지 않음 (다음에 다시 봄)
        eventPhase = EventPhaseNone;
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

        if (activeEvent != null) // 94일차: 이벤트 중에는 다음 대사 (선택지를 고를 때는 멈춤)
        {
            if (eventPhase != EventPhaseChoosing)
            {
                AdvanceEvent();
            }

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
        if (!IsOpen || activeEvent != null)
        {
            return;
        }

        string limitReason = relations.GiftLimitReason(agent.Character, npcManager.CurrentDay, IsBirthdayToday(agent.Character)); // 95일차: 하루 · 한 주 제한

        if (limitReason.Length > 0)
        {
            ShowMessage(limitReason, ProjectUUIPalette.TextSecondary);
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
        bool birthday = IsBirthdayToday(character);
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

    public void DeliverQuest() // 93일차: 이 NPC에게 받은 의뢰 전달 (모자라면 필요한 물건 안내)
    {
        NpcQuestManager quests = NpcQuestManager.Instance;
        NpcActiveQuest entry = IsOpen && quests != null ? quests.GetActiveFor(agent.Character) : null;

        if (entry == null)
        {
            return;
        }

        FinishTyping();
        NpcCharacterData character = agent.Character;

        if (!quests.TryComplete(entry.Quest.QuestId, inventory, out NpcQuestResult result, out string message))
        {
            ShowMessage($"{message}\n{entry.Quest.Title} · {quests.ProgressText(entry.Quest)} · {NpcQuestManager.DaysLeftText(quests.DaysLeft(entry))}", ProjectUUIPalette.TextSecondary);
            RefreshHeader();
            return;
        }

        List<string> rewards = new List<string>();
        if (result.Coins > 0) rewards.Add($"코인 +{result.Coins}");
        if (result.Affinity > 0) rewards.Add($"호감도 +{result.Affinity}");
        if (result.Item != null && result.ItemAmount > 0) rewards.Add($"{result.Item.DisplayName} x{result.ItemAmount}");
        ShowLine(string.IsNullOrEmpty(entry.Quest.ThanksLine) ? NpcDialogueSelector.Silent : entry.Quest.ThanksLine);
        ShowMessage(StageMessage(character, $"의뢰 완료 · {string.Join(" · ", rewards)}", result.Before, result.After), ProjectUUIPalette.Accent);
        RefreshHeader();
    }

    public void Trade() // 거래 (92일차: NPC 상점 · 영업 시간이 아니면 이유 안내 / 91일차: 가판대 상인)
    {
        if (!IsOpen)
        {
            return;
        }

        NpcShopManager shops = NpcShopManager.Instance;

        if (shops != null && !shops.TryGetShop(agent.Character, out _) && shops.TryGetCraftStation(agent.Character, out NpcCraftBook station)) // 104일차: 상점 없는 제작 NPC
        {
            NpcShopStatus craftStatus = shops.GetStatus(station, agent);

            if (!craftStatus.IsOpen)
            {
                FinishTyping();
                ShowMessage($"{craftStatus.Reason}\n오늘 제작 : {craftStatus.TodayHours}", ProjectUUIPalette.TextSecondary);
                return;
            }

            if (manager == null || !manager.OpenNpcCraft(agent))
            {
                ShowMessage("제작 창을 열 수 없어요. Build Content > 20번 메뉴를 실행하세요.", ProjectUUIPalette.Danger);
            }

            return;
        }

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

    public void ToggleCompanion() // 109일차: 함께 가자 · 이제 돌아가
    {
        NpcCompanionManager companions = NpcCompanionManager.Instance;

        if (!IsOpen || companions == null || activeEvent != null)
        {
            return;
        }

        FinishTyping();
        NpcCharacterData character = agent.Character;

        if (companions.IsCompanion(agent))
        {
            string leaveLine = companions.CurrentEntry != null ? companions.CurrentEntry.leaveLine : string.Empty;
            companions.Dismiss(false, false);
            ShowLine(leaveLine);
            ShowMessage($"{character.DisplayName} · {NpcCompanionManager.LeftMessage}", ProjectUUIPalette.TextSecondary);
            RefreshHeader();
            return;
        }

        NpcCompanionBook.Entry candidate = companions.EntryFor(character);

        if (companions.Recruit(agent, out string message))
        {
            ShowLine(message);
            ShowMessage($"{character.DisplayName} · {NpcCompanionManager.JoinedMessage} ({NpcCompanionBook.RoleName(candidate.role)})", ProjectUUIPalette.Accent);
        }
        else if (candidate != null && message == candidate.refuseLine)
        {
            ShowLine(message); // 아직 친하지 않음 : NPC가 직접 말함
        }
        else
        {
            ShowMessage(message, ProjectUUIPalette.TextSecondary);
        }

        RefreshHeader();
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

        bool birthdayToday = IsBirthdayToday(character);
        bool canGift = relations.GiftsLeftToday(character, npcManager.CurrentDay, birthdayToday) > 0;

        if (giftLabel != null) // 95일차: 한 주 제한이면 "이번 주 완료", 남은 수 표시
        {
            int weekLeft = relations.GiftsLeftThisWeek(character, npcManager.CurrentDay);
            giftLabel.text = canGift ? (birthdayToday ? "생일 선물" : $"선물하기 ({weekLeft})") : relations.GiftsLeftToday(character, npcManager.CurrentDay, true) > 0 ? "이번 주 완료" : "선물 완료";
        }

        if (questButton != null) // 93일차: 이 NPC에게 받은 의뢰가 있으면 전달 버튼
        {
            NpcQuestManager quests = NpcQuestManager.Instance;
            NpcActiveQuest entry = quests != null ? quests.GetActiveFor(character) : null;
            questButton.gameObject.SetActive(entry != null);

            if (entry != null && questLabel != null)
            {
                questLabel.text = quests.IsReady(entry.Quest) ? "의뢰 전달" : $"의뢰 ({RequirementProgress(quests, entry.Quest)})";
            }
        }

        if (tradeButton != null)
        {
            NpcShopManager shops = NpcShopManager.Instance;
            NpcShopData shop = null;
            NpcCraftBook station = null;
            bool hasShop = shops != null && shops.TryGetShop(character, out shop);
            bool hasStation = !hasShop && shops != null && shops.TryGetCraftStation(character, out station); // 104일차: 상점 없는 제작 NPC
            bool open = hasShop ? shops.GetStatus(shop, agent).IsOpen : hasStation ? shops.GetStatus(station, agent).IsOpen : npcManager.GetStallFor(agent) != null;
            tradeButton.gameObject.SetActive(hasShop || hasStation || open); // 92일차: 상점 주인은 영업 시간이 아니어도 버튼을 보여 주고 이유를 안내

            if (tradeLabel == null)
            {
                tradeLabel = tradeButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (tradeLabel != null)
            {
                string label = hasStation ? "제작" : "거래"; // 104일차: 제작만 하는 NPC
                tradeLabel.text = open ? label : $"{label} (닫힘)";
            }
        }

        if (companionButton != null) // 109일차: 동료가 될 수 있는 NPC만
        {
            NpcCompanionManager companions = NpcCompanionManager.Instance;
            bool canCompanion = companions != null && companions.EntryFor(character) != null;
            companionButton.gameObject.SetActive(canCompanion);

            if (canCompanion && companionLabel != null)
            {
                companionLabel.text = companions.IsCompanion(agent) ? "이제 돌아가" : "함께 가자";
            }
        }

        RefreshEventButtons();
    }

    private void RefreshEventButtons() // 94일차: 이벤트 중에는 다음 · 선택지만 보여 줌
    {
        bool inEvent = activeEvent != null;
        bool choosing = inEvent && eventPhase == EventPhaseChoosing;

        if (inEvent)
        {
            if (giftButton != null) giftButton.gameObject.SetActive(false);
            if (questButton != null) questButton.gameObject.SetActive(false);
            if (tradeButton != null) tradeButton.gameObject.SetActive(false);
            if (companionButton != null) companionButton.gameObject.SetActive(false); // 109일차
        }
        else if (giftButton != null)
        {
            giftButton.gameObject.SetActive(true);
        }

        if (talkButton != null)
        {
            talkButton.gameObject.SetActive(!choosing);

            if (talkLabel == null)
            {
                talkLabel = talkButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (talkLabel != null)
            {
                talkLabel.text = inEvent ? "다음" : "대화하기";
            }
        }

        for (int index = 0; index < choiceButtons.Length; index++)
        {
            bool show = choosing && index < activeEvent.Choices.Count && choiceButtons[index] != null;

            if (choiceButtons[index] != null)
            {
                choiceButtons[index].gameObject.SetActive(show);
            }

            if (show && index < choiceLabels.Length && choiceLabels[index] != null)
            {
                choiceLabels[index].text = activeEvent.Choices[index].Label;
            }
        }
    }

    // ------------------------------------------------------------ 94일차: 하트 이벤트

    private void BeginEvent(NpcEventBook.Event data) // 이벤트 장면 시작 (선택지 전 대사부터)
    {
        activeEvent = data;
        eventPhase = EventPhaseLines;
        eventChoice = -1;
        eventQueue.Clear();

        foreach (NpcEventBook.Line line in data.Lines)
        {
            eventQueue.Add(FormatEventLine(line));
        }

        CloseGiftPanel();
        ShowMessage($"이야기 · {data.Title}", ProjectUUIPalette.Accent);
        AdvanceEvent();
    }

    private void AdvanceEvent() // 다음 대사 → 선택지 → 대답 · 마무리 대사 → 끝
    {
        if (eventQueue.Count > 0)
        {
            ShowLine(eventQueue[0]);
            eventQueue.RemoveAt(0);

            if (eventQueue.Count == 0 && eventPhase == EventPhaseLines)
            {
                eventPhase = EventPhaseChoosing; // 마지막 대사와 함께 선택지를 보여 줌
            }

            RefreshHeader();
            return;
        }

        if (eventPhase == EventPhaseLines)
        {
            eventPhase = EventPhaseChoosing;
            RefreshHeader();
            return;
        }

        if (eventPhase == EventPhaseAfter)
        {
            FinishEvent();
        }
    }

    public void Choose(int index) // 선택지 고르기
    {
        if (!IsOpen || activeEvent == null || eventPhase != EventPhaseChoosing || index < 0 || index >= activeEvent.Choices.Count)
        {
            return;
        }

        FinishTyping();
        eventChoice = index;
        eventPhase = EventPhaseAfter;
        eventQueue.Clear();
        eventQueue.Add(activeEvent.Choices[index].Reply);

        foreach (NpcEventBook.Line line in activeEvent.AfterLines)
        {
            eventQueue.Add(FormatEventLine(line));
        }

        AdvanceEvent();
    }

    private void FinishEvent() // 선택 결과 적용 (호감도 · 아이템 · 기록)
    {
        NpcEventBook.Event data = activeEvent;
        NpcCharacterData character = agent.Character;
        activeEvent = null;
        eventPhase = EventPhaseNone;
        NpcEventManager events = NpcEventManager.Instance;

        if (events != null && events.Complete(data.EventId, eventChoice, inventory, out NpcEventResult result))
        {
            string item = result.Item != null && result.ItemAmount > 0 ? $" · {result.Item.DisplayName} x{result.ItemAmount}" : string.Empty;
            Color color = result.Affinity >= 0 ? ProjectUUIPalette.Accent : ProjectUUIPalette.Danger;
            ShowMessage(StageMessage(character, $"{data.Title} · 호감도 {result.Affinity:+0;-0;0}{item}", result.Before, result.After), color);
        }

        talkLines = NpcDialogueSelector.TalkLines(character, relations.GetStage(character), npcManager.CurrentSeason, npcManager.CurrentWeather);
        RefreshHeader();
    }

    private static string FormatEventLine(NpcEventBook.Line line) // 플레이어 대사는 "나" 표시
    {
        return line.FromPlayer ? $"<color=#8FC7FF>나</color>  {line.Text}" : line.Text;
    }

    private static string RequirementProgress(NpcQuestManager quests, NpcQuestBook.Quest quest) // 의뢰 버튼 진행도 "2/3"
    {
        int have = 0;
        int need = 0;

        foreach (NpcQuestBook.Requirement requirement in quest.Requirements)
        {
            if (requirement?.Item != null)
            {
                have += Mathf.Min(quests.CountInBag(requirement.Item), requirement.Amount);
                need += requirement.Amount;
            }
        }

        return $"{have}/{need}";
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

    private bool IsBirthdayToday(NpcCharacterData character) // 95일차: 오늘이 생일인지 (선물 제한 · 버튼 문구)
    {
        return character != null && npcManager != null && character.IsBirthday(npcManager.CurrentSeason, npcManager.CurrentDayInSeason);
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
