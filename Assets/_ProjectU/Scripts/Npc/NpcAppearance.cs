using UnityEngine; // Unity 기본 기능

[ExecuteAlways] // Scene 창에서도 색 표시
[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class NpcAppearance : MonoBehaviour // 90일차: 저폴리 NPC 모델의 옷·머리 색을 캐릭터 시트 색으로 덮어쓰기
{
    private const string OutfitMaterial = "M_LP_NpcOutfit"; // 옷 색 칸
    private const string AccentMaterial = "M_LP_NpcAccent"; // 보조 색 칸
    private const string HairMaterial = "M_LP_NpcHair"; // 머리 색 칸
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 색
    private static readonly int ColorId = Shader.PropertyToID("_Color"); // 기본 셰이더 색

    [Tooltip("색을 바꿀 모델 Renderer.")]
    [SerializeField] private Renderer[] renderers = new Renderer[0]; // 모델
    [Tooltip("옷 색 (캐릭터 시트 대표 색).")]
    [SerializeField] private Color outfitColor = Color.white; // 옷 색
    [Tooltip("보조 색 (캐릭터 시트 두 번째 색).")]
    [SerializeField] private Color accentColor = Color.gray; // 보조 색
    [Tooltip("머리 색.")]
    [SerializeField] private Color hairColor = new Color(0.35f, 0.23f, 0.15f); // 머리 색

    private MaterialPropertyBlock block; // 색 덮어쓰기

    public Color OutfitColor => outfitColor; // 옷 색 제공
    public Color HairColor => hairColor; // 머리 색 제공

    private void OnEnable() // 켜질 때 색 적용
    {
        Apply();
    }

    private void OnValidate() // Inspector 변경 시 색 적용
    {
        Apply();
    }

    public void SetColors(Color outfit, Color accent, Color hair) // 색 지정
    {
        outfitColor = outfit;
        accentColor = accent;
        hairColor = hair;
        Apply();
    }

#if UNITY_EDITOR
    public void EditorAssignRenderers(Renderer[] targets) // 생성 도구 전용
    {
        renderers = targets ?? new Renderer[0];
    }
#endif

    public void Apply() // 모든 Renderer의 색 칸에 적용
    {
        block ??= new MaterialPropertyBlock();

        foreach (Renderer target in renderers)
        {
            if (target == null)
            {
                continue;
            }

            Material[] materials = target.sharedMaterials;

            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];

                if (material == null || !TryGetColor(material.name, out Color color))
                {
                    continue;
                }

                target.GetPropertyBlock(block, index);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                target.SetPropertyBlock(block, index);
            }
        }
    }

    private bool TryGetColor(string materialName, out Color color) // Material 이름 → 색
    {
        switch (materialName)
        {
            case OutfitMaterial:
                color = outfitColor;
                return true;
            case AccentMaterial:
                color = accentColor;
                return true;
            case HairMaterial:
                color = hairColor;
                return true;
            default:
                color = Color.white;
                return false;
        }
    }
}
