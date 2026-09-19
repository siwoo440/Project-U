using System.Collections.Generic; // Dictionary 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.Rendering; // 그림자 설정

public sealed class EquippedToolView : MonoBehaviour // 장착 도구 외형 관리 / 97일차: 무기 3인칭 · 1인칭 외형
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

    [Header("Weapon Views")] // 97일차: 무기 외형 설정(Visual Profile)의 3인칭 · 1인칭 외형
    [Tooltip("현재 시점(1인칭 · 3인칭)을 확인할 Camera 관리자입니다.")]
    [SerializeField] private ThirdPersonCameraFollow cameraFollow; // 시점 확인
    [Tooltip("1인칭에서 무기를 붙일 Camera 앞 거치대입니다.")]
    [SerializeField] private Transform firstPersonHolder; // 1인칭 무기 거치대
    [Tooltip("손 외형(Held Views)을 쓰는 무기 외형 설정입니다. 여기 있는 무기는 전용 외형 · 목록 외형보다 먼저 사용합니다.")]
    [SerializeField] private ContentVisualProfile[] weaponProfiles = new ContentVisualProfile[0]; // 무기 외형 설정
    [Tooltip("활을 당길 때 보이는 장전 화살입니다. 활 외형 설정을 쓰면 보이는 활 모델로 옮겨 붙입니다.")]
    [SerializeField] private Transform nockedArrow; // 장전 화살 (97일차)

    private Transform arrowHomeParent; // 장전 화살 원래 부모 (기존 BowVisual)
    private Vector3 arrowHomePosition; // 원래 위치
    private Quaternion arrowHomeRotation; // 원래 회전
    private Vector3 arrowHomeScale; // 원래 크기
    private Matrix4x4 arrowToHolder; // 도구 거치대 기준 장전 화살 위치

    private readonly Dictionary<ItemData, GameObject> heldInstances = new Dictionary<ItemData, GameObject>(); // 만들어 둔 외형
    private readonly Dictionary<ContentVisualProfile, GameObject> thirdPersonInstances = new Dictionary<ContentVisualProfile, GameObject>(); // 만들어 둔 3인칭 무기
    private readonly Dictionary<ContentVisualProfile, GameObject> firstPersonInstances = new Dictionary<ContentVisualProfile, GameObject>(); // 만들어 둔 1인칭 무기
    private GameObject activeHeldInstance; // 지금 보이는 외형
    private GameObject activeThirdPersonWeapon; // 지금 보이는 3인칭 무기
    private GameObject activeFirstPersonWeapon; // 지금 보이는 1인칭 무기
    private ContentVisualProfile activeProfile; // 지금 무기 외형 설정
    private bool lastFirstPerson; // 마지막으로 적용한 시점

    public bool IsFirstPersonView => cameraFollow != null && cameraFollow.IsFirstPerson; // 현재 1인칭 여부 (97일차)
    public ContentVisualProfile ActiveWeaponProfile => activeProfile; // 지금 무기 외형 설정 (테스트용)
    public GameObject ActiveThirdPersonWeapon => activeThirdPersonWeapon; // 지금 3인칭 무기 (테스트용)
    public GameObject ActiveFirstPersonWeapon => activeFirstPersonWeapon; // 지금 1인칭 무기 (테스트용)
    public Transform FirstPersonHolder => firstPersonHolder; // 1인칭 거치대 (테스트용)
    public Transform NockedArrow => nockedArrow; // 장전 화살 (테스트용)

    private void Awake() // 필수 참조 확인
    {
        bool hasMissingReference = playerInventory == null || axeVisual == null || pickaxeVisual == null; // 참조 누락 확인

        if (hasMissingReference) // 참조 누락 여부 확인
        {
            Debug.LogError("EquippedToolView의 참조를 모두 연결해야 합니다.", this); // 참조 누락 오류
            enabled = false; // 도구 표시 기능 비활성화
        }

        if (nockedArrow != null) // 97일차: 장전 화살 원래 자리 기억
        {
            arrowHomeParent = nockedArrow.parent;
            arrowHomePosition = nockedArrow.localPosition;
            arrowHomeRotation = nockedArrow.localRotation;
            arrowHomeScale = nockedArrow.localScale;
            arrowToHolder = transform.worldToLocalMatrix * nockedArrow.localToWorldMatrix;
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
        SetOptionalVisual(activeFirstPersonWeapon, false); // Camera 아래 무기 숨김 (플레이어가 꺼지면 함께 숨김)
    }

    private void LateUpdate() // 97일차: 시점이 바뀌면 무기 외형 전환
    {
        if (IsFirstPersonView != lastFirstPerson) // 시점 변경 확인
        {
            Refresh(); // 외형 다시 적용
        }
    }

    public void Refresh() // 장착 도구 외형 갱신
    {
        ItemData selectedItem = playerInventory.SelectedHotbarItem; // 선택 핫바 아이템 조회
        bool firstPerson = IsFirstPersonView; // 현재 시점
        lastFirstPerson = firstPerson; // 적용한 시점 기록
        ContentVisualProfile profile = ResolveWeaponProfile(selectedItem); // 97일차: 무기 외형 설정
        ToolType selectedToolType = ToolType.None; // 기본 도구 종류 설정
        GameObject held = profile == null ? GetHeldInstance(selectedItem) : null; // 목록 외형
        bool isRanged = profile == null && held == null && selectedItem != null && selectedItem.WeaponAttackType == WeaponAttackType.Ranged; // 활 여부

        if (profile == null && held == null && selectedItem != null && selectedItem.IsTool) // 도구 아이템 선택 확인
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

        // 97일차: 무기 외형 설정이 있으면 3인칭은 몸 옆, 1인칭은 Camera 앞에 하나만 보여 준다
        activeProfile = profile; // 현재 설정 기록
        GameObject third = profile != null && !firstPerson ? GetProfileInstance(profile, false) : null; // 3인칭 무기
        GameObject first = profile != null && firstPerson ? GetProfileInstance(profile, true) : null; // 1인칭 무기
        SwapActive(ref activeThirdPersonWeapon, third); // 3인칭 교체
        SwapActive(ref activeFirstPersonWeapon, first); // 1인칭 교체
        AttachNockedArrow(profile, selectedItem, firstPerson ? first : third); // 장전 화살 따라가기
    }

    private void AttachNockedArrow(ContentVisualProfile profile, ItemData item, GameObject weapon) // 97일차: 장전 화살을 보이는 활 모델로 옮긴다
    {
        if (nockedArrow == null || arrowHomeParent == null) // 장전 화살 없음
        {
            return;
        }

        Transform model = weapon != null ? weapon.transform.Find("Model") : null; // 보이는 활 모델

        if (profile == null || model == null || item == null || item.WeaponAttackType != WeaponAttackType.Ranged) // 활 외형 설정이 아니면 원래 자리로
        {
            if (nockedArrow.parent != arrowHomeParent)
            {
                nockedArrow.SetParent(arrowHomeParent, false);
                nockedArrow.localPosition = arrowHomePosition;
                nockedArrow.localRotation = arrowHomeRotation;
                nockedArrow.localScale = arrowHomeScale;
            }

            return;
        }

        // 3인칭 모델 기준 화살 위치 = (3인칭 모델 위치)⁻¹ × (거치대 기준 화살 위치) → 1인칭 모델에도 같은 상대 위치로 붙인다
        Matrix4x4 thirdModel = Matrix4x4.TRS(profile.ThirdPersonPosition, Quaternion.Euler(profile.ThirdPersonEulerAngles), profile.ThirdPersonScale);
        Matrix4x4 arrowInModel = thirdModel.inverse * arrowToHolder;
        nockedArrow.SetParent(model, false);
        nockedArrow.localPosition = arrowInModel.GetColumn(3);
        nockedArrow.localRotation = Quaternion.LookRotation(arrowInModel.GetColumn(2), arrowInModel.GetColumn(1));
        nockedArrow.localScale = new Vector3(arrowInModel.GetColumn(0).magnitude, arrowInModel.GetColumn(1).magnitude, arrowInModel.GetColumn(2).magnitude);
        SetLayerRecursive(nockedArrow, model.gameObject.layer);
    }

    private ContentVisualProfile ResolveWeaponProfile(ItemData item) // 97일차: 아이템 → 무기 외형 설정 (tool_axe → visual_weapon_axe)
    {
        if (item == null || weaponProfiles == null || weaponProfiles.Length == 0 || !(item.IsTool || item.IsWeapon))
        {
            return null; // 무기 외형 없음
        }

        if (!ContentVisualProfileIdUtility.TryBuildProfileId(ContentVisualIdentityCategory.Weapon, item.ItemId, false, string.Empty, out string profileId, out _))
        {
            return null; // 무기 외형 ID 규칙 밖
        }

        foreach (ContentVisualProfile profile in weaponProfiles) // 설정 검색
        {
            if (profile != null && profile.UseHeldViews && profile.ProfileId == profileId)
            {
                return profile; // 찾음
            }
        }

        return null; // 없음
    }

    private GameObject GetProfileInstance(ContentVisualProfile profile, bool firstPerson) // 97일차: 무기 외형을 처음 들 때 한 번 만든다
    {
        Dictionary<ContentVisualProfile, GameObject> cache = firstPerson ? firstPersonInstances : thirdPersonInstances; // 시점별 기록
        Transform parent = firstPerson ? firstPersonHolder : transform; // 붙일 곳

        if (parent == null) // 1인칭 거치대 없음
        {
            return null; // 외형 없음
        }

        if (cache.TryGetValue(profile, out GameObject existing) && existing != null) // 만들어 둔 외형 확인
        {
            return existing; // 재사용
        }

        GameObject root = new GameObject($"{(firstPerson ? "FirstPerson" : "ThirdPerson")}_{profile.ProfileId}"); // 기준 오브젝트
        root.layer = parent.gameObject.layer; // 레이어 맞춤
        root.transform.SetParent(parent, false); // 거치대 아래
        GameObject model = Instantiate(profile.VisualPrefab, root.transform); // 모델
        model.name = "Model";
        model.transform.localPosition = firstPerson ? profile.FirstPersonPosition : profile.ThirdPersonPosition; // 위치
        model.transform.localRotation = Quaternion.Euler(firstPerson ? profile.FirstPersonEulerAngles : profile.ThirdPersonEulerAngles); // 회전
        model.transform.localScale = firstPerson ? profile.FirstPersonScale : profile.ThirdPersonScale; // 크기
        SetLayerRecursive(model.transform, parent.gameObject.layer); // 레이어 맞춤

        foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>(true)) // 외형 충돌체 제거
        {
            Destroy(modelCollider);
        }

        if (firstPerson) // 1인칭 무기는 그림자를 만들지 않는다 (공중에 뜬 그림자 방지)
        {
            foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        root.SetActive(false); // 처음에는 숨김
        cache[profile] = root; // 기록
        return root; // 외형 반환
    }

    private static void SwapActive(ref GameObject current, GameObject next) // 보이는 외형 교체
    {
        if (current != next) // 변경 확인
        {
            SetOptionalVisual(current, false); // 이전 외형 숨김
            current = next; // 현재 외형 기록
        }

        SetOptionalVisual(current, current != null); // 현재 외형 표시
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

#if UNITY_EDITOR
    public void EditorAssignWeaponViews(ThirdPersonCameraFollow follow, Transform holder, ContentVisualProfile[] profiles, Transform arrow) // 97일차: 생성 도구 전용
    {
        cameraFollow = follow;
        firstPersonHolder = holder;
        weaponProfiles = profiles ?? new ContentVisualProfile[0];
        nockedArrow = arrow;
    }
#endif
}
