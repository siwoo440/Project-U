using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class QuestBoard : InteractableBase // 93일차: 광장 게시판 (F로 게시판 창 열기)
{
    [Tooltip("게시판 표시 이름.")]
    [SerializeField] private string boardName = "마을 게시판"; // 이름

    private GameUIManager gameUIManager; // 팝업 관리자

    public string BoardName => boardName; // 이름 제공

    public override string PromptMessage // 안내 문구
    {
        get
        {
            NpcQuestManager quests = NpcQuestManager.Instance;

            if (quests == null)
            {
                return $"{boardName} | 의뢰 없음";
            }

            string active = quests.Active.Count > 0 ? $" · 진행 중 {quests.Active.Count}/{quests.ActiveLimit}" : string.Empty;
            return quests.BoardIds.Count > 0 ? $"F - {boardName} | 새 의뢰 {quests.BoardIds.Count}{active}" : $"F - {boardName} | 오늘 의뢰 없음{active}";
        }
    }

    public override void Interact(GameObject interactor) // 게시판 창 열기
    {
        if (interactor == null)
        {
            return;
        }

        if (gameUIManager == null)
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>();
        }

        if (gameUIManager == null || !gameUIManager.OpenQuestBoard(this))
        {
            CombatDamagePopup.SpawnText(transform.position + Vector3.up * 2.4f, "게시판 창이 없어요", ProjectUUIPalette.Danger, 2f);
        }
    }
}
