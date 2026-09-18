using UnityEngine; // Unity 기본 기능

public enum NpcLocationAnchor // 위치가 따라가는 건축물
{
    Fixed = 0, // 이 표시 지점 그대로
    MarketStall = 1, // 플레이어가 세운 상인 가판대가 있으면 그 뒤 (상인 자리)
    AnimalPen = 2 // 플레이어가 세운 가축 우리가 있으면 가장 가까운 우리 앞
}

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcLocationPoint : MonoBehaviour // 90일차: NPC 일정 위치 (NpcDatabase 위치 ID와 연결)
{
    [Tooltip("위치 ID (NpcLocations.csv).")]
    [SerializeField] private string locationId; // 위치 ID
    [Tooltip("표시 이름.")]
    [SerializeField] private string displayName; // 표시 이름
    [Tooltip("집이면 밤(잠자는 시간)에 NPC가 안으로 들어가 보이지 않습니다.")]
    [SerializeField] private bool isHome; // 집 여부
    [Tooltip("건축물을 따라가는 위치 (없으면 이 지점을 씁니다).")]
    [SerializeField] private NpcLocationAnchor anchor = NpcLocationAnchor.Fixed; // 따라갈 건축물
    [Tooltip("여러 NPC가 같은 곳에 있을 때 옆으로 벌리는 간격.")]
    [SerializeField, Min(0.5f)] private float slotSpacing = 1.2f; // 자리 간격

    public string LocationId => locationId; // ID 제공
    public string DisplayName => displayName; // 이름 제공
    public bool IsHome => isHome; // 집 여부 제공
    public NpcLocationAnchor Anchor => anchor; // 따라갈 건축물 제공
    public float SlotSpacing => slotSpacing; // 자리 간격 제공

    public Vector3 GetStandPosition(int slot) // 자리 번호별 서는 위치 (0 = 가운데, 1 = 오른쪽, 2 = 왼쪽 ...)
    {
        return transform.position + GetSlotOffset(transform.right, slot, slotSpacing);
    }

    public static Vector3 GetSlotOffset(Vector3 right, int slot, float spacing) // 자리 번호 → 옆 간격
    {
        if (slot <= 0)
        {
            return Vector3.zero;
        }

        int step = (slot + 1) / 2;
        return right * (step * spacing * (slot % 2 == 1 ? 1f : -1f));
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, string label, bool home, NpcLocationAnchor follow) // 생성 도구 전용
    {
        locationId = id;
        displayName = label;
        isHome = home;
        anchor = follow;
    }

    private void OnDrawGizmos() // Scene 창 표시
    {
        Gizmos.color = isHome ? new Color(1f, 0.75f, 0.3f, 0.9f) : new Color(0.35f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.45f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + transform.forward * 0.8f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, string.IsNullOrEmpty(displayName) ? locationId : displayName);
    }
#endif
}
