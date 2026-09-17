using UnityEngine; // Unity 기본 기능

[CreateAssetMenu(fileName = "FishingRodData_New", menuName = "Project U/Fishing/Fishing Rod Data")] // 낚싯대 데이터 생성 메뉴
public sealed class FishingRodData : ScriptableObject // 낚싯대 등급과 낚시 보정값
{
    [Tooltip("이 데이터가 설명하는 낚싯대 아이템입니다. Tool Type이 FishingRod여야 합니다.")]
    [SerializeField] private ItemData rodItem; // 낚싯대 아이템

    [Tooltip("낚싯대 등급입니다. (1 기본, 2 강화, 3 철제, 4 심해)")]
    [SerializeField, Range(1, 4)] private int tier = 1; // 낚싯대 등급

    [Tooltip("플레이어 앞으로 찌를 던지는 거리(m)입니다.")]
    [SerializeField, Min(1f)] private float castDistance = 4f; // 던지는 거리

    [Tooltip("입질 대기 시간 배율입니다. 1보다 작을수록 빨리 입질합니다.")]
    [SerializeField, Min(0.1f)] private float biteTimeMultiplier = 1f; // 입질 대기 배율

    [Tooltip("미니게임 입력 제한 시간에 더하는 여유 시간(초)입니다. (83일차)")]
    [SerializeField, Min(0f)] private float inputTimeBonus; // 입력 여유 시간

    public ItemData RodItem => rodItem; // 낚싯대 아이템 제공
    public int Tier => tier; // 등급 제공
    public float CastDistance => castDistance; // 던지는 거리 제공
    public float BiteTimeMultiplier => biteTimeMultiplier; // 입질 대기 배율 제공
    public float InputTimeBonus => inputTimeBonus; // 입력 여유 시간 제공

    public bool TryValidate(out string errorMessage) // 낚싯대 데이터 검사
    {
        if (rodItem == null || !rodItem.IsTool || rodItem.ToolType != ToolType.FishingRod) // 아이템 확인
        {
            errorMessage = $"{name}: 낚싯대 아이템이 없거나 Tool Type이 FishingRod가 아닙니다."; // 아이템 오류
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 없음
        return true; // 검사 성공
    }
}
