using UnityEngine; // Unity 기본 기능

public enum ContentVisualCategory // Visual Profile 적용 대상 분류
{
    Other = 0, // 기타 콘텐츠
    Item = 1, // 월드 아이템
    Weapon = 2, // 장착 무기와 도구
    Enemy = 3, // 적 캐릭터
    Buildable = 4, // 건축물
    Resource = 5 // 채집 자원
}

[CreateAssetMenu( // ScriptableObject 생성 메뉴 설정
    fileName = "VisualProfile_New", // 새 Visual Profile 기본 파일 이름
    menuName = "Project U/Visual/Content Visual Profile")] // Project 창 생성 메뉴 경로
public sealed class ContentVisualProfile : ScriptableObject // 콘텐츠 외형과 임시 표시 설정을 보관하는 공통 데이터
{
    [Header("Identity")] // Profile 식별 정보 묶음
    [Tooltip("Registry 검색과 저장 연결에 사용할 Visual Profile 고유 ID입니다.")] // Inspector Profile ID 설명
    [SerializeField] private string profileId = "visual_new"; // Visual Profile 고유 ID

    [Tooltip("Inspector와 Debug Log에 표시할 Profile 이름입니다.")] // Inspector 표시 이름 설명
    [SerializeField] private string displayName = "NEW VISUAL PROFILE"; // Visual Profile 표시 이름

    [Tooltip("이 Profile을 사용하는 콘텐츠 종류입니다.")] // Inspector 콘텐츠 분류 설명
    [SerializeField] private ContentVisualCategory category = ContentVisualCategory.Other; // Visual Profile 적용 대상 분류

    [Header("Presentation")] // 화면 표시 Asset 묶음
    [Tooltip("VisualInstance 아래에 생성할 실제 모델 또는 임시 Prefab입니다.")] // Inspector Visual Prefab 설명
    [SerializeField] private GameObject visualPrefab; // 실제 외형 Prefab

    [Tooltip("인벤토리, 제작 목록과 UI에서 사용할 대표 아이콘입니다.")] // Inspector 아이콘 설명
    [SerializeField] private Sprite icon; // 대표 UI 아이콘

    [Tooltip("실제 Visual Prefab의 모든 Renderer에 선택적으로 적용할 Material입니다.")] // Inspector Material Override 설명
    [SerializeField] private Material materialOverride; // 실제 외형 Material Override

    [Tooltip("실제 Visual Prefab에 Animator가 있을 때 적용할 Animator Controller입니다.")] // Inspector Animator Controller 설명
    [SerializeField] private RuntimeAnimatorController animatorController; // 외형 Animator Controller

    [Header("Placeholder")] // 실제 모델 누락 시 임시 외형 설정 묶음
    [Tooltip("Visual Prefab이 없을 때 Unity Primitive를 임시 외형으로 생성합니다.")] // Inspector 임시 외형 사용 설명
    [SerializeField] private bool createPlaceholderWhenPrefabMissing = true; // Prefab 누락 시 임시 외형 생성 여부

    [Tooltip("Visual Prefab이 없을 때 생성할 Unity Primitive 종류입니다.")] // Inspector Primitive 설명
    [SerializeField] private PrimitiveType placeholderPrimitive = PrimitiveType.Cube; // 임시 외형 Primitive 종류

    [Tooltip("임시 Primitive에 우선 적용할 Material입니다.")] // Inspector 임시 Material 설명
    [SerializeField] private Material placeholderMaterial; // 임시 외형 Material

    [Tooltip("임시 Primitive에 MaterialPropertyBlock으로 적용할 기본 색상입니다.")] // Inspector 임시 색상 설명
    [SerializeField] private Color placeholderColor = Color.white; // 임시 외형 기본 색상

    [Tooltip("임시 Primitive에 기본 색상을 적용합니다.")] // Inspector 임시 색상 사용 설명
    [SerializeField] private bool usePlaceholderColor = true; // 임시 외형 색상 적용 여부

    [Header("Visual Transform")] // 생성 외형 Transform 설정 묶음
    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 위치입니다.")] // Inspector 외형 위치 설명
    [SerializeField] private Vector3 visualLocalPosition = Vector3.zero; // 생성 외형 로컬 위치

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 회전입니다.")] // Inspector 외형 회전 설명
    [SerializeField] private Vector3 visualLocalEulerAngles = Vector3.zero; // 생성 외형 로컬 회전

    [Tooltip("VisualInstance 아래에 생성된 외형의 로컬 크기입니다.")] // Inspector 외형 크기 설명
    [SerializeField] private Vector3 visualLocalScale = Vector3.one; // 생성 외형 로컬 크기

    [Header("Anchors")] // 표준 기준점 위치 묶음
    [Tooltip("InteractionPoint에 적용할 로컬 위치입니다.")] // Inspector 상호작용 기준 위치 설명
    [SerializeField] private Vector3 interactionPointPosition = new Vector3(0f, 1f, 0f); // 상호작용 기준점 위치

    [Tooltip("EffectOrigin에 적용할 로컬 위치입니다.")] // Inspector 효과 기준 위치 설명
    [SerializeField] private Vector3 effectOriginPosition = new Vector3(0f, 1f, 0f); // 효과 기준점 위치

    [Tooltip("UIAnchor에 적용할 로컬 위치입니다.")] // Inspector UI 기준 위치 설명
    [SerializeField] private Vector3 uiAnchorPosition = new Vector3(0f, 2f, 0f); // UI 기준점 위치

    [Header("Visual Rules")] // 외형 생성 규칙 묶음
    [Tooltip("생성된 외형의 Layer를 기능 Root의 Layer와 동일하게 적용합니다.")] // Inspector Layer 상속 설명
    [SerializeField] private bool inheritRootLayer = true; // Root Layer 상속 여부

    [Tooltip("생성된 외형 Prefab 내부 Collider를 제거합니다.")] // Inspector Collider 제거 설명
    [SerializeField] private bool removeVisualColliders = true; // 외형 Collider 제거 여부

    [Tooltip("Material Override가 있으면 실제 Visual Prefab Renderer에 적용합니다.")] // Inspector Material Override 적용 설명
    [SerializeField] private bool applyMaterialOverrideToVisualPrefab = true; // 실제 Prefab Material Override 적용 여부

    [Header("Future Asset References")] // 이후 시스템에서 사용할 Asset 참조 묶음
    [Tooltip("향후 상호작용 또는 기본 동작에 연결할 대표 AudioClip입니다.")] // Inspector 대표 Audio 설명
    [SerializeField] private AudioClip primaryAudioClip; // 대표 Audio Asset

    [Tooltip("향후 EffectOrigin에 생성할 대표 VFX Prefab입니다.")] // Inspector 대표 VFX 설명
    [SerializeField] private GameObject primaryVfxPrefab; // 대표 VFX Prefab

    public string ProfileId => profileId; // Visual Profile ID 제공
    public string DisplayName => displayName; // Visual Profile 표시 이름 제공
    public ContentVisualCategory Category => category; // 적용 대상 분류 제공
    public GameObject VisualPrefab => visualPrefab; // 실제 외형 Prefab 제공
    public Sprite Icon => icon; // 대표 UI 아이콘 제공
    public Material MaterialOverride => materialOverride; // 실제 외형 Material Override 제공
    public RuntimeAnimatorController AnimatorController => animatorController; // Animator Controller 제공
    public bool CreatePlaceholderWhenPrefabMissing => createPlaceholderWhenPrefabMissing; // 임시 외형 생성 여부 제공
    public PrimitiveType PlaceholderPrimitive => placeholderPrimitive; // 임시 Primitive 종류 제공
    public Material PlaceholderMaterial => placeholderMaterial; // 임시 외형 Material 제공
    public Color PlaceholderColor => placeholderColor; // 임시 외형 색상 제공
    public bool UsePlaceholderColor => usePlaceholderColor; // 임시 외형 색상 적용 여부 제공
    public Vector3 VisualLocalPosition => visualLocalPosition; // 외형 로컬 위치 제공
    public Vector3 VisualLocalEulerAngles => visualLocalEulerAngles; // 외형 로컬 회전 제공
    public Vector3 VisualLocalScale => visualLocalScale; // 외형 로컬 크기 제공
    public Vector3 InteractionPointPosition => interactionPointPosition; // 상호작용 기준점 위치 제공
    public Vector3 EffectOriginPosition => effectOriginPosition; // 효과 기준점 위치 제공
    public Vector3 UiAnchorPosition => uiAnchorPosition; // UI 기준점 위치 제공
    public bool InheritRootLayer => inheritRootLayer; // Root Layer 상속 여부 제공
    public bool RemoveVisualColliders => removeVisualColliders; // 외형 Collider 제거 여부 제공
    public bool ApplyMaterialOverrideToVisualPrefab => applyMaterialOverrideToVisualPrefab; // 실제 Prefab Material Override 적용 여부 제공
    public AudioClip PrimaryAudioClip => primaryAudioClip; // 대표 Audio Asset 제공
    public GameObject PrimaryVfxPrefab => primaryVfxPrefab; // 대표 VFX Prefab 제공
    public bool HasVisualSource => visualPrefab != null || createPlaceholderWhenPrefabMissing; // 실제 또는 임시 외형 생성 가능 여부 제공

    private void OnValidate() // Inspector 입력값 검증
    {
        profileId = string.IsNullOrWhiteSpace(profileId) // Profile ID 입력 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 적용
            : profileId.Trim(); // 입력이 있으면 양쪽 공백 제거

        displayName = string.IsNullOrWhiteSpace(displayName) // 표시 이름 입력 여부 확인
            ? name // 입력이 없으면 Asset 이름 사용
            : displayName.Trim(); // 입력이 있으면 양쪽 공백 제거

        visualLocalScale.x = Mathf.Max(0.01f, visualLocalScale.x); // 외형 X 크기 최소값 적용
        visualLocalScale.y = Mathf.Max(0.01f, visualLocalScale.y); // 외형 Y 크기 최소값 적용
        visualLocalScale.z = Mathf.Max(0.01f, visualLocalScale.z); // 외형 Z 크기 최소값 적용
    }
}
