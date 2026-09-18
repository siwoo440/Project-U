using System.Collections.Generic; // 목록 기능
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcQuestBoardPopup : MonoBehaviour, IGameScenePopup // 93일차: 마을 게시판 창 (오늘의 의뢰 · 진행 중 의뢰 · 받기 / 포기)
{
    private struct Entry // 목록 한 줄의 내용
    {
        public string QuestId;
        public bool IsActive;
    }

    [Header("Root")] // 루트
    [SerializeField] private GameObject panelRoot; // 창 전체
    [SerializeField] private TMP_Text titleText; // 제목
    [SerializeField] private TMP_Text summaryText; // 오늘의 의뢰 수 · 진행 수
    [SerializeField] private Button closeButton; // 닫기

    [Header("List")] // 목록
    [SerializeField] private Transform listRoot; // 줄 부모
    [SerializeField] private NpcQuestRowUI rowTemplate; // 줄 템플릿 (꺼진 상태)
    [SerializeField] private TMP_Text emptyText; // 빈 목록 문구

    [Header("Detail")] // 상세
    [SerializeField] private GameObject detailRoot; // 상세 묶음
    [SerializeField] private Image portrait; // 의뢰인 초상
    [SerializeField] private Image portraitFrame; // 초상 테두리 (대표 색)
    [SerializeField] private TMP_Text ownerText; // 의뢰인 이름 · 직업
    [SerializeField] private TMP_Text questTitleText; // 의뢰 제목
    [SerializeField] private TMP_Text requestText; // 의뢰 대사
    [SerializeField] private Transform requirementRoot; // 필요한 물건 부모
    [SerializeField] private NpcQuestItemUI requirementTemplate; // 필요한 물건 템플릿 (꺼진 상태)
    [SerializeField] private TMP_Text rewardText; // 보상
    [SerializeField] private TMP_Text deadlineText; // 기한
    [SerializeField] private Button actionButton; // 받기 · 포기
    [SerializeField] private TMP_Text actionLabel; // 버튼 문구

    [Header("Footer")] // 아래쪽
    [SerializeField] private TMP_Text hintText; // 안내
    [SerializeField] private TMP_Text messageText; // 결과 알림
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(0.5f)] private float messageDuration = 3f; // 알림 시간
    [Tooltip("플레이어가 게시판에서 이보다 멀어지면 창을 닫습니다.")]
    [SerializeField, Min(1f)] private float closeDistance = 6f; // 닫는 거리
    [Tooltip("포기 버튼을 두 번 눌러야 하는 시간.")]
    [SerializeField, Min(0.5f)] private float abandonConfirmSeconds = 3f; // 포기 확인 시간

    private readonly List<NpcQuestRowUI> rows = new List<NpcQuestRowUI>(); // 목록 줄
    private readonly List<NpcQuestItemUI> requirementSlots = new List<NpcQuestItemUI>(); // 필요한 물건 칸
    private readonly List<Entry> entries = new List<Entry>(); // 목록 내용
    private GameUIManager manager; // 관리자
    private NpcQuestManager quests; // 의뢰 관리자
    private QuestBoard board; // 게시판
    private PlayerInventory inventory; // 인벤토리
    private Transform player; // 플레이어
    private int selectedIndex; // 선택 줄
    private bool isDirty; // 다시 그리기 필요
    private float messageHideTime; // 알림 숨김 시각
    private string abandonPendingId; // 포기 확인 중인 의뢰
    private float abandonPendingUntil; // 포기 확인 마감 시각

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf && quests != null; // 열림 여부
    public int EntryCount => entries.Count; // 목록 줄 수 (테스트용)
    public int SelectedIndex => selectedIndex; // 선택 줄 (테스트용)
    public string SelectedQuestId => selectedIndex >= 0 && selectedIndex < entries.Count ? entries[selectedIndex].QuestId : string.Empty; // 선택한 의뢰 (테스트용)
    public string ActionLabel => actionLabel != null ? actionLabel.text : string.Empty; // 버튼 문구 (테스트용)
    public bool CanPressAction => actionButton != null && actionButton.interactable && actionButton.gameObject.activeSelf; // 버튼 사용 가능 (테스트용)
    public string MessageLabel => messageText != null && messageText.gameObject.activeSelf ? messageText.text : string.Empty; // 알림 (테스트용)
    public string SummaryLabel => summaryText != null ? summaryText.text : string.Empty; // 요약 (테스트용)
    public IReadOnlyList<NpcQuestRowUI> Rows => rows; // 줄 (테스트용)
    public IReadOnlyList<NpcQuestItemUI> RequirementSlots => requirementSlots; // 필요한 물건 칸 (테스트용)

    private void Awake() // 버튼 연결
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
        if (actionButton != null) actionButton.onClick.AddListener(PressAction);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        if (requirementTemplate != null) requirementTemplate.gameObject.SetActive(false);

        if (panelRoot != null && quests == null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDestroy() // 구독 해제
    {
        Unsubscribe();
    }

    public bool ShowFromManager(GameUIManager owner, QuestBoard targetBoard, PlayerInventory playerInventory) // 창 열기
    {
        NpcQuestManager targetQuests = NpcQuestManager.Instance;

        if (panelRoot == null || rowTemplate == null || requirementTemplate == null || targetBoard == null || playerInventory == null || targetQuests == null)
        {
            Debug.LogError("게시판 창 참조가 누락되었습니다. Tools > Project U > Build Content > 11. NPC Quests를 다시 실행하세요.", this);
            return false;
        }

        Unsubscribe();
        manager = owner;
        quests = targetQuests;
        board = targetBoard;
        inventory = playerInventory;
        player = playerInventory.transform;
        quests.QuestsChanged += MarkDirty;
        inventory.InventoryChanged += MarkDirty;
        selectedIndex = 0;
        abandonPendingId = null;
        panelRoot.SetActive(true);
        ShowMessage(string.Empty, Color.clear);
        Rebuild();
        return true;
    }

    public void HideFromManager() // 창 닫기
    {
        Unsubscribe();
        quests = null;
        board = null;
        inventory = null;
        manager = null;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Unsubscribe() // 이벤트 해제
    {
        if (quests != null) quests.QuestsChanged -= MarkDirty;
        if (inventory != null) inventory.InventoryChanged -= MarkDirty;
    }

    private void MarkDirty() // 다시 그리기 요청
    {
        isDirty = true;
    }

    private void Update() // 거리 확인 · 다시 그리기 · 알림 시간
    {
        if (!IsOpen)
        {
            return;
        }

        if (board == null || !board.isActiveAndEnabled || player == null || (player.position - board.transform.position).sqrMagnitude > closeDistance * closeDistance)
        {
            RequestClose();
            return;
        }

        if (abandonPendingId != null && Time.unscaledTime > abandonPendingUntil)
        {
            abandonPendingId = null; // 포기 확인 시간 지남
            isDirty = true;
        }

        if (isDirty)
        {
            Rebuild();
        }

        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void RequestClose() // 닫기 요청
    {
        if (manager != null)
        {
            manager.CloseQuestBoard();
            return;
        }

        HideFromManager();
    }

    // ------------------------------------------------------------ 조작

    public void Select(int index) // 줄 선택
    {
        if (index != selectedIndex)
        {
            abandonPendingId = null;
        }

        selectedIndex = index;
        Rebuild();
    }

    public void PressAction() // 받기 (게시판 의뢰) · 포기 (진행 중, 두 번 눌러 확인)
    {
        if (!IsOpen || selectedIndex < 0 || selectedIndex >= entries.Count)
        {
            return;
        }

        Entry entry = entries[selectedIndex];

        if (!entry.IsActive)
        {
            bool accepted = quests.Accept(entry.QuestId, out string message);
            ShowMessage(message, accepted ? ProjectUUIPalette.Teal : ProjectUUIPalette.Danger);

            if (accepted)
            {
                selectedIndex = entries.FindIndex(candidate => candidate.QuestId == entry.QuestId); // 받은 의뢰를 계속 선택 (진행 중 목록으로 이동)
            }

            Rebuild();
            return;
        }

        if (abandonPendingId != entry.QuestId)
        {
            abandonPendingId = entry.QuestId; // 한 번 더 눌러야 포기
            abandonPendingUntil = Time.unscaledTime + abandonConfirmSeconds;
            Rebuild();
            return;
        }

        abandonPendingId = null;
        bool abandoned = quests.Abandon(entry.QuestId, out string abandonMessage);
        ShowMessage(abandonMessage, abandoned ? ProjectUUIPalette.TextSecondary : ProjectUUIPalette.Danger);
        Rebuild();
    }

    // ------------------------------------------------------------ 그리기

    private void Rebuild() // 전체 다시 그리기
    {
        isDirty = false;

        if (quests == null)
        {
            return;
        }

        string keepId = selectedIndex >= 0 && selectedIndex < entries.Count ? entries[selectedIndex].QuestId : null;
        entries.Clear();

        foreach (string questId in quests.BoardIds)
        {
            entries.Add(new Entry { QuestId = questId, IsActive = false });
        }

        foreach (NpcActiveQuest active in quests.Active)
        {
            entries.Add(new Entry { QuestId = active.Quest.QuestId, IsActive = true });
        }

        if (keepId != null)
        {
            int kept = entries.FindIndex(candidate => candidate.QuestId == keepId);
            selectedIndex = kept >= 0 ? kept : selectedIndex;
        }

        selectedIndex = entries.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, entries.Count - 1);
        titleText.text = "마을 게시판";
        summaryText.text = $"오늘의 의뢰 {quests.BoardIds.Count}  ·  진행 중 {quests.Active.Count}/{quests.ActiveLimit}";

        for (int index = 0; index < entries.Count; index++)
        {
            BindRow(index, entries[index]);
        }

        for (int index = entries.Count; index < rows.Count; index++)
        {
            rows[index].gameObject.SetActive(false);
        }

        emptyText.gameObject.SetActive(entries.Count == 0);
        emptyText.text = "오늘은 붙어 있는 의뢰가 없어요.\n내일 다시 확인해 보세요.";
        hintText.text = $"의뢰는 동시에 {quests.ActiveLimit}개까지  ·  물건을 모아 의뢰한 주민에게 말을 걸어 전달하세요";

        if (entries.Count == 0)
        {
            detailRoot.SetActive(false);
            return;
        }

        detailRoot.SetActive(true);
        BindDetail(entries[selectedIndex]);
    }

    private void BindRow(int index, Entry entry) // 목록 한 줄
    {
        quests.TryGetQuest(entry.QuestId, out NpcQuestBook book, out NpcQuestBook.Quest quest);
        NpcCharacterData owner = quests.GetOwner(book);
        string ownerName = owner != null ? owner.DisplayName : book.OwnerId;
        string subtitle;
        string tag;
        Color tagColor;

        if (entry.IsActive)
        {
            NpcActiveQuest active = quests.GetActive(entry.QuestId);
            bool ready = quests.IsReady(quest);
            subtitle = $"{ownerName}  ·  {NpcQuestManager.DaysLeftText(quests.DaysLeft(active))}";
            tag = ready ? "전달 가능" : "진행 중";
            tagColor = ready ? ProjectUUIPalette.Accent : ProjectUUIPalette.Teal;
        }
        else
        {
            subtitle = $"{ownerName}  ·  {quest.Days}일 안에";
            tag = quest.IsSpecial ? "특별" : string.Empty;
            tagColor = ProjectUUIPalette.Accent;
        }

        GetRow(index).Bind(index, owner != null ? owner.Portrait : null, quest.Title, subtitle, tag, tagColor, index == selectedIndex, Select);
    }

    private void BindDetail(Entry entry) // 오른쪽 상세
    {
        quests.TryGetQuest(entry.QuestId, out NpcQuestBook book, out NpcQuestBook.Quest quest);
        NpcCharacterData owner = quests.GetOwner(book);
        portrait.sprite = owner != null ? owner.Portrait : null;
        portrait.enabled = portrait.sprite != null;
        portraitFrame.color = owner != null ? owner.ThemeColor : Color.white;
        ownerText.text = owner != null ? $"{owner.DisplayName}  <size=70%><color=#B8B3A6>{owner.Profile.jobName}</color></size>" : book.OwnerId;
        questTitleText.text = quest.IsSpecial ? $"{quest.Title}  <size=70%><color=#F2B84B>특별 의뢰</color></size>" : quest.Title;
        requestText.text = $"“{quest.RequestLine}”";
        rewardText.text = NpcQuestManager.RewardText(quest);

        int count = quest.Requirements.Count;

        while (requirementSlots.Count < count)
        {
            NpcQuestItemUI created = Instantiate(requirementTemplate, requirementRoot);
            created.name = $"Requirement_{requirementSlots.Count:00}";
            requirementSlots.Add(created);
        }

        for (int index = 0; index < requirementSlots.Count; index++)
        {
            if (index < count && quest.Requirements[index]?.Item != null)
            {
                NpcQuestBook.Requirement requirement = quest.Requirements[index];
                requirementSlots[index].Bind(requirement.Item, quests.CountInBag(requirement.Item), requirement.Amount);
            }
            else
            {
                requirementSlots[index].gameObject.SetActive(false);
            }
        }

        if (entry.IsActive)
        {
            NpcActiveQuest active = quests.GetActive(entry.QuestId);
            bool ready = quests.IsReady(quest);
            deadlineText.text = ready ? $"준비 완료! {(owner != null ? owner.DisplayName : "의뢰인")}에게 가져다주세요  ·  {NpcQuestManager.DaysLeftText(quests.DaysLeft(active))}" : $"진행 중  ·  {NpcQuestManager.DaysLeftText(quests.DaysLeft(active))}";
            deadlineText.color = ready ? ProjectUUIPalette.Accent : ProjectUUIPalette.Teal;
            actionButton.interactable = true;
            actionLabel.text = abandonPendingId == entry.QuestId ? "한 번 더 누르면 포기" : "의뢰 포기";
            return;
        }

        deadlineText.text = $"받은 날부터 {quest.Days}일 안에 전달";
        deadlineText.color = ProjectUUIPalette.TextSecondary;
        bool canAccept = quests.CanAccept(entry.QuestId, out string reason);
        actionButton.interactable = canAccept;
        actionLabel.text = canAccept ? "의뢰 받기" : reason;
    }

    private NpcQuestRowUI GetRow(int index) // 줄 가져오기 (없으면 만들기)
    {
        while (rows.Count <= index)
        {
            NpcQuestRowUI created = Instantiate(rowTemplate, listRoot);
            created.name = $"Row_{rows.Count:00}";
            rows.Add(created);
        }

        rows[index].transform.SetSiblingIndex(index + 1); // 템플릿 다음
        return rows[index];
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
}
