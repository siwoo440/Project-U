using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

// 84일차: 핫바 칸이 아이콘만 보여주므로, 선택을 바꾸면 핫바 위에 아이템 이름을 잠깐 띄운다.
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class SelectedItemNameUI : MonoBehaviour
{
    [Header("References")] // 참조 묶음
    [Tooltip("선택 핫바 아이템을 제공하는 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventory playerInventory; // 플레이어 인벤토리

    [Tooltip("투명도로 나타나고 사라지게 할 Canvas Group입니다.")]
    [SerializeField] private CanvasGroup canvasGroup; // 투명도 제어

    [Tooltip("아이템 이름 TMP Text입니다.")]
    [SerializeField] private TMP_Text nameText; // 이름

    [Tooltip("이 오브젝트가 켜져 있으면 이름을 숨깁니다. (같은 자리의 물뿌리개 게이지)")]
    [SerializeField] private GameObject hideWhileActive; // 겹치는 HUD

    [Header("Timing")] // 시간 묶음
    [Tooltip("이름을 보여주는 시간(초)입니다.")]
    [SerializeField, Min(0.1f)] private float showDuration = 1.6f; // 표시 시간

    [Tooltip("사라지는 시간(초)입니다.")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f; // 사라짐 시간

    private ItemData lastItem; // 마지막 선택 아이템
    private int lastIndex = -1; // 마지막 선택 칸
    private float hideTime; // 사라지기 시작하는 시각

    private void Awake() // 시작 시 숨김
    {
        if (playerInventory == null) // 참조 확인
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>(); // Scene에서 검색
        }

        if (canvasGroup != null) // 투명도 확인
        {
            canvasGroup.alpha = 0f; // 숨김
            canvasGroup.blocksRaycasts = false; // 클릭 통과
            canvasGroup.interactable = false; // 입력 없음
        }
    }

    private void OnEnable() // 선택 변경 구독
    {
        if (playerInventory != null) // 참조 확인
        {
            playerInventory.HotbarSelectionChanged += HandleSelectionChanged; // 구독
        }
    }

    private void OnDisable() // 선택 변경 해제
    {
        if (playerInventory != null) // 참조 확인
        {
            playerInventory.HotbarSelectionChanged -= HandleSelectionChanged; // 해제
        }
    }

    private void LateUpdate() // 선택 확인과 투명도 갱신
    {
        if (playerInventory == null || canvasGroup == null || nameText == null) // 참조 확인
        {
            return; // 처리 생략
        }

        InventorySlot slot = playerInventory.GetSlot(playerInventory.SelectedHotbarIndex); // 선택 칸
        ItemData item = slot != null ? slot.ItemData : null; // 선택 아이템

        if (item != lastItem || playerInventory.SelectedHotbarIndex != lastIndex) // 선택 칸 또는 칸의 아이템 변경 확인
        {
            Show(item); // 이름 표시
        }

        float alpha = Mathf.Clamp01((hideTime - Time.unscaledTime) / fadeDuration); // 남은 표시 비율

        if (hideWhileActive != null && hideWhileActive.activeInHierarchy) // 겹치는 HUD 확인
        {
            alpha = 0f; // 숨김
        }

        if (!Mathf.Approximately(canvasGroup.alpha, alpha)) // 변경 확인
        {
            canvasGroup.alpha = alpha; // 투명도 적용
        }
    }

    private void HandleSelectionChanged() // 핫바 선택 변경 알림
    {
        lastIndex = -1; // 다음 LateUpdate에서 다시 표시
    }

    private void Show(ItemData item) // 이름 표시 시작
    {
        lastItem = item; // 아이템 기록
        lastIndex = playerInventory.SelectedHotbarIndex; // 칸 기록

        if (item == null) // 빈 칸 확인
        {
            hideTime = 0f; // 바로 숨김
            return; // 처리 종료
        }

        nameText.SetText(item.DisplayName); // 이름 적용
        hideTime = Time.unscaledTime + showDuration + fadeDuration; // 표시 시간 적용
    }
}
