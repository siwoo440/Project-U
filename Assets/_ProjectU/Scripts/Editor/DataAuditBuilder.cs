using System.Text;
using UnityEditor;
using UnityEngine;

// 96일차: 데이터 점검에서 찾은 연결 문제 고치기
// 1. 도끼 외형 설정 : ID를 도구 아이템(tool_axe) 규칙에 맞게 visual_weapon_axe로 바꾸고 돌도끼 모델을 연결한다
// 2. GameDataRegistry 등록 목록을 다시 모은다
// 3. 데이터 점검을 하고 Docs/Audit/DataAudit.md 표를 만든다
// 여러 번 실행해도 같은 값이 된다.
public static class DataAuditBuilder
{
    private const string DialogTitle = "Project U 데이터 점검";
    private const string WeaponProfileFolder = "Assets/_ProjectU/Data/VisualProfiles/Weapons/";
    public const string AxeProfileId = "visual_weapon_axe";
    private const string AxeProfileName = "VP_Weapon_Axe";
    private const string OldAxeProfileName = "VP_Weapon_StoneAxe";
    private const string AxeModelId = "tool_stone_axe";

    // ---------------------------------------------------------------- 전체 적용

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[데이터 점검 수정]\n");

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "도끼 외형 설정", 0.2f);
            FixAxeProfile(report);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "등록 목록", 0.4f);
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            report.AppendLine("GameDataRegistry 등록 목록 다시 모음");
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "데이터 점검", 0.6f);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        DataAudit.Result result = DataAudit.Analyze();
        report.AppendLine($"점검 표 : {DataAudit.WriteMarkdown(result)}");
        report.Append(DataAudit.Summary(result));
        return report.ToString();
    }

    private static void FixAxeProfile(StringBuilder report)
    {
        string path = WeaponProfileFolder + AxeProfileName + ".asset";
        ContentVisualProfile profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(path);

        if (profile == null)
        {
            string oldPath = WeaponProfileFolder + OldAxeProfileName + ".asset";

            if (AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(oldPath) == null)
            {
                report.AppendLine($"✗ 도끼 외형 설정({OldAxeProfileName} · {AxeProfileName})이 없습니다.");
                return;
            }

            string error = AssetDatabase.RenameAsset(oldPath, AxeProfileName); // GUID는 그대로 (등록 목록 연결 유지)

            if (!string.IsNullOrEmpty(error))
            {
                report.AppendLine($"✗ 도끼 외형 설정 이름을 바꾸지 못했습니다 : {error}");
                return;
            }

            profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(path);
            report.AppendLine($"{OldAxeProfileName} → {AxeProfileName} 이름 변경");
        }

        GameObject model = StylizedArtAssetFactory.LoadModelPrefab(AxeModelId);
        SerializedObject serialized = new SerializedObject(profile);
        serialized.FindProperty("profileId").stringValue = AxeProfileId;
        serialized.FindProperty("displayName").stringValue = "AXE VISUAL";
        serialized.FindProperty("category").intValue = (int)ContentVisualCategory.Weapon;

        if (model != null)
        {
            serialized.FindProperty("visualPrefab").objectReferenceValue = model;
            serialized.FindProperty("visualLocalScale").vector3Value = Vector3.one; // 임시 모양 크기 대신 모델 원래 크기
        }

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(profile);
        }

        report.AppendLine($"도끼 외형 설정 : {AxeProfileId} · 모델 {(model != null ? model.name : "없음 (임시 모양)")}");
    }

    // ---------------------------------------------------------------- 검사

    public static string Validate(out int errorCount)
    {
        string text = DataAudit.Validate(out int auditErrors);
        ContentVisualProfile axe = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(WeaponProfileFolder + AxeProfileName + ".asset");

        if (axe == null || axe.ProfileId != AxeProfileId)
        {
            errorCount = auditErrors + 1;
            return text + $"✗ 도끼 외형 설정이 {AxeProfileId} 가 아닙니다. 14번 메뉴를 실행하세요.\n결과 : 오류 {errorCount}개\n";
        }

        errorCount = auditErrors;
        return text;
    }
}
