using System.Text; // 문자열
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcQuestTrackerUI : MonoBehaviour // 93일차: 화면 왼쪽 진행 중 의뢰 표시 (의뢰인 · 진행도 · 남은 날 · 기한 만료 알림)
{
    [SerializeField] private GameObject panel; // 켜고 끄는 판
    [SerializeField] private TMP_Text titleText; // "의뢰 1/3"
    [SerializeField] private TMP_Text bodyText; // 진행 중 의뢰 목록
    [SerializeField] private TMP_Text noticeText; // 기한 만료 · 완료 알림
    [Tooltip("알림 표시 시간.")]
    [SerializeField, Min(1f)] private float noticeSeconds = 6f; // 알림 시간

    private NpcQuestManager quests; // 의뢰 관리자
    private PlayerInventory inventory; // 인벤토리
    private float noticeHideTime; // 알림 숨김 시각
    private int shownDay = int.MinValue; // 마지막으로 그린 날짜 (남은 날 갱신)
    private bool isDirty = true; // 다시 그리기 필요

    public string BodyLabel => bodyText != null ? bodyText.text : string.Empty; // 목록 (테스트용)
    public string NoticeLabel => noticeText != null && noticeText.gameObject.activeSelf ? noticeText.text : string.Empty; // 알림 (테스트용)
    public bool IsShown => panel != null && panel.activeSelf; // 표시 여부 (테스트용)

    private void OnEnable() // 구독
    {
        Subscribe();
    }

    private void OnDisable() // 해제
    {
        if (quests != null)
        {
            quests.QuestsChanged -= MarkDirty;
            quests.Notice -= ShowNotice;
            quests.QuestCompleted -= HandleCompleted;
        }

        if (inventory != null)
        {
            inventory.InventoryChanged -= MarkDirty;
        }

        quests = null;
        inventory = null;
    }

    private void Subscribe() // 관리자 찾기 · 구독 (관리자가 늦게 준비될 수 있어 Update에서도 시도)
    {
        if (quests == null && NpcQuestManager.Instance != null)
        {
            quests = NpcQuestManager.Instance;
            quests.QuestsChanged += MarkDirty;
            quests.Notice += ShowNotice;
            quests.QuestCompleted += HandleCompleted;
            isDirty = true;
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();

            if (inventory != null)
            {
                inventory.InventoryChanged += MarkDirty;
            }
        }
    }

    private void MarkDirty() // 다시 그리기 요청
    {
        isDirty = true;
    }

    private void HandleCompleted(NpcQuestBook.Quest quest, NpcCharacterData owner) // 완료 알림
    {
        ShowNotice($"의뢰 완료 : {quest.Title}  ·  {NpcQuestManager.RewardText(quest)}");
    }

    public void ShowNotice(string message) // 알림 표시
    {
        if (noticeText == null)
        {
            return;
        }

        noticeText.text = message;
        noticeText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        noticeHideTime = Time.unscaledTime + noticeSeconds;
        isDirty = true;
    }

    private void Update() // 갱신
    {
        Subscribe();

        if (quests == null)
        {
            if (panel != null && panel.activeSelf) panel.SetActive(false);
            return;
        }

        if (noticeText != null && noticeText.gameObject.activeSelf && Time.unscaledTime >= noticeHideTime)
        {
            noticeText.gameObject.SetActive(false);
            isDirty = true;
        }

        if (shownDay != quests.CurrentDay)
        {
            shownDay = quests.CurrentDay;
            isDirty = true;
        }

        if (isDirty)
        {
            Rebuild();
        }
    }

    private void Rebuild() // 목록 다시 그리기
    {
        isDirty = false;
        bool hasNotice = noticeText != null && noticeText.gameObject.activeSelf;
        bool show = quests.Active.Count > 0 || hasNotice;

        if (panel.activeSelf != show)
        {
            panel.SetActive(show);
        }

        titleText.text = $"의뢰  {quests.Active.Count}/{quests.ActiveLimit}";
        bodyText.gameObject.SetActive(quests.Active.Count > 0);
        StringBuilder text = new StringBuilder();

        foreach (NpcActiveQuest entry in quests.Active)
        {
            NpcCharacterData owner = quests.GetOwner(entry.Book);
            bool ready = quests.IsReady(entry.Quest);
            string state = ready ? "<color=#F2B84B>전달 가능!</color>" : NpcQuestManager.DaysLeftText(quests.DaysLeft(entry));

            if (text.Length > 0)
            {
                text.Append('\n');
            }

            text.Append($"<b>{(owner != null ? owner.DisplayName : entry.OwnerId)}</b>  {entry.Quest.Title}\n<size=82%><color=#B8B3A6>{quests.ProgressText(entry.Quest)}  ·  </color>{state}</size>");
        }

        bodyText.text = text.ToString();
    }
}
