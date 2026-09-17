using System; // 배열 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "FishingRules_New", menuName = "Project U/Fishing/Fishing Rules Data")] // 낚시 규칙 데이터 생성 메뉴
public sealed class FishingRulesData : ScriptableObject // 낚시 시작·대기·입질 공통 규칙
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("낚시 규칙 고유 ID입니다.")]
    [SerializeField] private string rulesId = "fishing_rules_default"; // 규칙 ID

    [Header("Equipment")] // 장비 묶음
    [Tooltip("사용 가능한 낚싯대 목록입니다.")]
    [SerializeField] private FishingRodData[] rods = Array.Empty<FishingRodData>(); // 낚싯대 목록

    [Tooltip("던질 때 인벤토리에 있으면 입질이 빨라지는 미끼 아이템입니다.")]
    [SerializeField] private ItemData baitItem; // 미끼 아이템

    [Tooltip("미끼를 사용할 때의 입질 대기 시간 배율입니다.")]
    [SerializeField, Range(0.1f, 1f)] private float baitBiteTimeMultiplier = 0.6f; // 미끼 대기 배율

    [Header("Timing")] // 시간 묶음
    [Tooltip("입질까지의 최소 대기 시간(초)입니다.")]
    [SerializeField, Min(0.5f)] private float minimumBiteWait = 3f; // 최소 대기

    [Tooltip("입질까지의 최대 대기 시간(초)입니다.")]
    [SerializeField, Min(0.5f)] private float maximumBiteWait = 8f; // 최대 대기

    [Tooltip("입질 후 챔질(F)할 수 있는 시간(초)입니다.")]
    [SerializeField, Min(0.2f)] private float biteWindow = 1.5f; // 챔질 가능 시간

    [Tooltip("찌가 날아가는 시간(초)입니다.")]
    [SerializeField, Min(0.1f)] private float castDuration = 0.6f; // 던지기 시간

    [Header("Rules")] // 기타 규칙 묶음
    [Tooltip("던질 때 소비하는 스태미나입니다.")]
    [SerializeField, Min(0f)] private float castStaminaCost = 3f; // 던지기 스태미나

    [Tooltip("던진 위치에서 이만큼 움직이면 낚시를 취소합니다(m).")]
    [SerializeField, Min(0.05f)] private float cancelMoveDistance = 0.35f; // 취소 이동 거리

    public string RulesId => rulesId; // 규칙 ID 제공
    public ItemData BaitItem => baitItem; // 미끼 제공
    public float BaitBiteTimeMultiplier => baitBiteTimeMultiplier; // 미끼 대기 배율 제공
    public float MinimumBiteWait => minimumBiteWait; // 최소 대기 제공
    public float MaximumBiteWait => Mathf.Max(minimumBiteWait, maximumBiteWait); // 최대 대기 제공
    public float BiteWindow => biteWindow; // 챔질 시간 제공
    public float CastDuration => castDuration; // 던지기 시간 제공
    public float CastStaminaCost => castStaminaCost; // 던지기 스태미나 제공
    public float CancelMoveDistance => cancelMoveDistance; // 취소 거리 제공

    public bool TryGetRod(ItemData item, out FishingRodData rodData) // 아이템에 맞는 낚싯대 데이터 검색
    {
        rodData = null; // 기본 결과

        if (item == null || !item.IsTool || item.ToolType != ToolType.FishingRod) // 낚싯대 여부 확인
        {
            return false; // 검색 실패
        }

        for (int index = 0; index < rods.Length; index++) // 낚싯대 목록 순회
        {
            if (rods[index] != null && rods[index].RodItem == item) // 아이템 일치 확인
            {
                rodData = rods[index]; // 결과 저장
                return true; // 검색 성공
            }
        }

        return false; // 검색 실패
    }

    public bool TryValidate(out string errorMessage) // 규칙 데이터 검사
    {
        if (rods.Length == 0) // 낚싯대 목록 확인
        {
            errorMessage = $"{name}: 낚싯대 목록이 비어 있습니다."; // 목록 오류
            return false; // 검사 실패
        }

        for (int index = 0; index < rods.Length; index++) // 낚싯대 순회
        {
            if (rods[index] == null) // 빈 항목 확인
            {
                errorMessage = $"{name}: {index}번 낚싯대가 비어 있습니다."; // 빈 항목 오류
                return false; // 검사 실패
            }

            if (!rods[index].TryValidate(out errorMessage)) // 낚싯대 검사
            {
                return false; // 검사 실패
            }
        }

        if (baitItem == null) // 미끼 확인
        {
            errorMessage = $"{name}: 미끼 아이템이 연결되지 않았습니다."; // 미끼 오류
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }

    private void OnValidate() // Inspector 값 정리
    {
        rods ??= Array.Empty<FishingRodData>(); // 배열 누락 방지
        maximumBiteWait = Mathf.Max(minimumBiteWait, maximumBiteWait); // 최대값 보정
    }
}
