using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class FoodBuffHUD : MonoBehaviour // 85일차: 생존 게이지 아래 음식 효과 알약
{
    [Tooltip("효과 관리자. 비어 있으면 현재 플레이어 관리자를 사용합니다.")]
    [SerializeField] private FoodBuffController controller; // 효과 관리자
    [Tooltip("효과 알약 (위에서부터 순서대로 사용).")]
    [SerializeField] private CookingChipUI[] chips = new CookingChipUI[0]; // 알약
    [Tooltip("효과 아이콘 묶음.")]
    [SerializeField] private FoodEffectIconSet iconSet; // 아이콘
    [Tooltip("남은 시간이 이 값보다 적으면 깜빡입니다 (초).")]
    [SerializeField, Min(0f)] private float blinkBelowSeconds = 30f; // 깜빡임 기준
    [Tooltip("문구 갱신 간격 (초).")]
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.2f; // 갱신 간격

    private float nextRefreshTime; // 다음 갱신 시각

    private void Start() // 시작 상태
    {
        Refresh(); // 그리기
    }

    private void Update() // 주기 갱신
    {
        if (controller == null) // 관리자 확인
        {
            controller = FoodBuffController.Local; // 현재 플레이어
        }

        if (Time.unscaledTime < nextRefreshTime) // 간격 확인
        {
            return; // 대기
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval; // 다음 시각
        Refresh(); // 그리기
    }

    private void Refresh() // 알약 그리기
    {
        int count = controller != null ? controller.Buffs.Count : 0; // 효과 수

        for (int index = 0; index < chips.Length; index++) // 알약 순회
        {
            CookingChipUI chip = chips[index]; // 알약

            if (chip == null) // 빈 참조
            {
                continue; // 제외
            }

            if (index >= count) // 사용하지 않는 알약
            {
                if (chip.gameObject.activeSelf) chip.gameObject.SetActive(false); // 숨김
                continue; // 다음
            }

            FoodBuffController.ActiveBuff buff = controller.Buffs[index]; // 효과
            string text = $"{FoodBuffUtility.GetShortLabel(buff.type, buff.strength)}  {FoodBuffUtility.FormatTime(buff.remainingSeconds)}"; // 문구
            chip.Bind(text, FoodBuffUtility.GetColor(buff.type), iconSet != null ? iconSet.Get(buff.type) : null); // 표시
            bool blink = buff.remainingSeconds <= blinkBelowSeconds; // 깜빡임 여부
            chip.SetAlpha(blink ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) : 1f); // 투명도
        }
    }
}
