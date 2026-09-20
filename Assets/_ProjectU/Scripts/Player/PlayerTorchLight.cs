using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class PlayerTorchLight : MonoBehaviour // 116일차: 횃불을 손에 들면 따라다니는 불빛 (동굴 안에서 앞을 밝힘)
{
    [Tooltip("불빛을 켜는 아이템 ID.")]
    [SerializeField] private string torchItemId = "item_torch"; // 횃불 아이템
    [Tooltip("불빛을 붙일 자리 (비어 있으면 플레이어 자신).")]
    [SerializeField] private Transform lightAnchor; // 불빛 자리
    [Tooltip("불빛이 닿는 거리 (m).")]
    [SerializeField, Min(1f)] private float lightRange = 14f; // 불빛 거리
    [Tooltip("불빛 밝기.")]
    [SerializeField, Min(0f)] private float lightIntensity = 3.4f; // 불빛 밝기
    [Tooltip("불빛 색.")]
    [SerializeField] private Color lightColor = new Color(1f, 0.76f, 0.45f, 1f); // 불빛 색

    private PlayerInventory inventory; // 가방
    private Light torchLight; // 불빛

    public bool IsLit => torchLight != null && torchLight.enabled; // 켜져 있는지 (테스트용)

    private void Awake()
    {
        inventory = GetComponentInParent<PlayerInventory>();

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }

        GameObject holder = new GameObject("TorchLight");
        holder.transform.SetParent(lightAnchor != null ? lightAnchor : transform, false);
        holder.transform.localPosition = new Vector3(0.35f, 1.2f, 0.2f);
        torchLight = holder.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.range = lightRange;
        torchLight.intensity = lightIntensity;
        torchLight.color = lightColor;
        torchLight.shadows = LightShadows.None; // 동굴에서 그림자까지 켜면 무거움
        torchLight.enabled = false;
        holder.AddComponent<StylizedFlameFlicker>(); // 불꽃처럼 밝기가 흔들림
    }

    private void Update()
    {
        bool holdingTorch = inventory != null && inventory.SelectedHotbarItem != null && inventory.SelectedHotbarItem.ItemId == torchItemId;

        if (torchLight != null && torchLight.enabled != holdingTorch)
        {
            torchLight.enabled = holdingTorch;
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(Transform anchor) // 생성 도구 전용
    {
        lightAnchor = anchor;
    }
#endif
}
