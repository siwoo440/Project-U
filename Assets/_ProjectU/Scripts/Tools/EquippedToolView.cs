using System.Collections.Generic; // Dictionary 기능
using UnityEngine; // Unity 기본 기능

public sealed class EquippedToolView : MonoBehaviour // 장착 도구 외형 관리
{
    [Tooltip("확인할 플레이어 인벤토리.")]
    [SerializeField] private PlayerInventory playerInventory; // 확인할 플레이어 인벤토리
    [Tooltip("도끼 외형.")]
    [SerializeField] private GameObject axeVisual; // 도끼 외형
    [Tooltip("곡괭이 외형.")]
    [SerializeField] private GameObject pickaxeVisual; // 곡괭이 외형
    [Tooltip("괭이 외형. 비어 있으면 표시하지 않습니다.")]
    [SerializeField] private GameObject hoeVisual; // 괭이 외형
    [Tooltip("물뿌리개 외형. 비어 있으면 표시하지 않습니다.")]
    [SerializeField] private GameObject wateringCanVisual; // 물뿌리개 외형
    [Tooltip("낚싯대 외형. 비어 있으면 표시하지 않습니다.")]
    [SerializeField] private GameObject fishingRodVisual; // 낚싯대 외형
    [Tooltip("활(원거리 무기) 외형. 비어 있으면 표시하지 않습니다. (84일차)")]
    [SerializeField] private GameObject bowVisual; // 활 외형

    [Header("Held Items")] // 그 밖의 아이템 묶음 (84일차)
    [Tooltip("전용 외형이 없는 아이템을 손에 들었을 때 보여줄 외형 목록입니다. 목록에 있는 아이템은 전용 외형보다 먼저 사용합니다.")]
    [SerializeField] private HeldItemVisualSet heldItemVisuals; // 아이템별 외형 목록

    private readonly Dictionary<ItemData, GameObject> heldInstances = new Dictionary<ItemData, GameObject>(); // 만들어 둔 외형
    private GameObject activeHeldInstance; // 지금 보이는 외형

    private void Awake() // 필수 참조 확인
    {
        bool hasMissingReference = playerInventory == null || axeVisual == null || pickaxeVisual == null; // 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("EquippedToolView의 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 도구 표시 기능 비활성화
        }
    }

    private void OnEnable() // 인벤토리 이벤트 연결
    {
        if (playerInventory == null) // 인벤토리 존재 확인
        {
            return; // 이벤트 연결 중단
        }

        playerInventory.HotbarSelectionChanged += Refresh; // 핫바 선택 이벤트 구독
        playerInventory.InventoryChanged += Refresh; // 슬롯 변경 이벤트 구독
        Refresh(); // 현재 장착 상태 표시
    }

    private void OnDisable() // 인벤토리 이벤트 해제
    {
        if (playerInventory == null) // 인벤토리 존재 확인
        {
            return; // 이벤트 해제 중단
        }

        playerInventory.HotbarSelectionChanged -= Refresh; // 핫바 선택 이벤트 해제
        playerInventory.InventoryChanged -= Refresh; // 슬롯 변경 이벤트 해제
    }

    private void Refresh() // 장착 도구 외형 갱신
    {
        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 선택 핫바 아이템 조회
        ToolType selectedToolType = ToolType.None; // 기본 도구 종류 설정
        GameObject held = GetHeldInstance(selectedItem); // 목록 외형
        bool isRanged = held == null && selectedItem != null && selectedItem.WeaponAttackType == WeaponAttackType.Ranged; // 활 여부

        if (held == null && selectedItem != null && selectedItem.IsTool) // 도구 아이템 선택 확인
        {
            selectedToolType = selectedItem.ToolType; // 선택 도구 종류 저장
        }

        axeVisual.SetActive(selectedToolType == ToolType.Axe); // 도끼 외형 상태 적용
        pickaxeVisual.SetActive(selectedToolType == ToolType.Pickaxe); // 곡괭이 외형 상태 적용
        SetOptionalVisual(hoeVisual, selectedToolType == ToolType.Hoe); // 괭이 외형 상태 적용
        SetOptionalVisual(wateringCanVisual, selectedToolType == ToolType.WateringCan); // 물뿌리개 외형 상태 적용
        SetOptionalVisual(fishingRodVisual, selectedToolType == ToolType.FishingRod); // 낚싯대 외형 상태 적용
        SetOptionalVisual(bowVisual, isRanged); // 활 외형 상태 적용

        if (activeHeldInstance != held) // 목록 외형 변경 확인
        {
            SetOptionalVisual(activeHeldInstance, false); // 이전 외형 숨김
            activeHeldInstance = held; // 현재 외형 기록
        }

        SetOptionalVisual(activeHeldInstance, activeHeldInstance != null); // 현재 외형 표시
    }

    private GameObject GetHeldInstance(ItemData item) // 목록 외형을 처음 들 때 한 번 만든다
    {
        if (item == null || heldItemVisuals == null) // 요청 확인
        {
            return null; // 외형 없음
        }

        if (heldInstances.TryGetValue(item, out GameObject existing)) // 만들어 둔 외형 확인
        {
            return existing; // 재사용
        }

        if (!heldItemVisuals.TryGet(item, out HeldItemVisualSet.Entry entry)) // 목록 확인
        {
            return null; // 외형 없음
        }

        GameObject root = new GameObject($"Held_{item.ItemId}"); // 기준 오브젝트
        root.layer = gameObject.layer; // 레이어 맞춤
        root.transform.SetParent(transform, false); // 손 기준점 아래
        root.transform.localPosition = entry.holderLocalPosition; // 위치
        root.transform.localRotation = Quaternion.Euler(entry.holderLocalEuler); // 회전
        GameObject model = Instantiate(entry.modelPrefab, root.transform); // 모델
        model.name = "Model";
        model.transform.localPosition = entry.modelLocalPosition; // 모델 위치
        model.transform.localRotation = Quaternion.identity; // 모델 회전
        model.transform.localScale = Vector3.one * entry.modelScale; // 모델 크기
        SetLayerRecursive(model.transform, gameObject.layer); // 레이어 맞춤
        root.SetActive(false); // 처음에는 숨김
        heldInstances.Add(item, root); // 기록
        return root; // 외형 반환
    }

    private static void SetLayerRecursive(Transform target, int layer) // 하위 레이어 적용
    {
        target.gameObject.layer = layer; // 레이어 적용

        foreach (Transform child in target) // 하위 순회
        {
            SetLayerRecursive(child, layer); // 재귀 적용
        }
    }

    private static void SetOptionalVisual(GameObject visual, bool isVisible) // 연결된 선택 외형만 상태 적용
    {
        if (visual != null && visual.activeSelf != isVisible) // 외형 연결과 변경 확인
        {
            visual.SetActive(isVisible); // 표시 상태 적용
        }
    }
}
