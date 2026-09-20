using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class CavePortal : InteractableBase // 116일차: 동굴 입구(지상)와 출구(동굴 안). F를 누르면 짝이 되는 곳으로 옮겨 준다.
{
    [Tooltip("입구 ID (지상과 동굴 안 짝이 같은 ID를 씁니다).")]
    [SerializeField] private string portalId = string.Empty; // 입구 ID
    [Tooltip("표시 이름 (설산 광굴 등).")]
    [SerializeField] private string displayName = "동굴"; // 표시 이름
    [Tooltip("동굴 안 구역 ID (들어갔을 때 어느 구역인지).")]
    [SerializeField] private string areaId = "cave_main"; // 동굴 구역
    [Tooltip("체크하면 동굴 안 출구(밖으로 나감), 해제하면 지상 입구(안으로 들어감).")]
    [SerializeField] private bool isExit; // 출구 여부
    [Tooltip("옮겨 갈 자리 (짝이 되는 쪽의 도착 지점).")]
    [SerializeField] private Transform destination; // 도착 지점

    public string PortalId => portalId; // 입구 ID 제공
    public string DisplayName => displayName; // 표시 이름 제공
    public string AreaId => areaId; // 동굴 구역 제공
    public bool IsExit => isExit; // 출구 여부 제공
    public Transform Destination => destination; // 도착 지점 제공

    public override string PromptMessage => isExit ? "F - 밖으로 나가기" : $"F - {displayName} 들어가기"; // 안내 문구

    public override void Interact(GameObject interactor) // 드나들기
    {
        CaveManager manager = CaveManager.Instance;

        if (manager == null || !manager.Travel(this))
        {
            Debug.LogWarning($"{name} : 동굴을 드나들지 못했습니다 (관리자 또는 도착 지점 없음).", this);
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(string id, string label, string area, bool exit, Transform target) // 생성 도구 전용
    {
        portalId = id;
        displayName = label;
        areaId = area;
        isExit = exit;
        destination = target;
    }
#endif
}
