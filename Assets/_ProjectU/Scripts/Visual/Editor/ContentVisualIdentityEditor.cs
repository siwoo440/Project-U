using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(ContentVisualIdentity))] // ContentVisualIdentity 전용 Inspector 등록
public sealed class ContentVisualIdentityEditor : Editor // 콘텐츠 ID와 계산 Profile ID 확인 Editor
{
    public override void OnInspectorGUI() // Identity 기본 Inspector와 계산 결과 도구 표시
    {
        DrawDefaultInspector(); // Identity 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 Inspector와 도구 사이 간격 추가
        EditorGUILayout.LabelField("Identity Tools", EditorStyles.boldLabel); // Identity 도구 제목 표시
        ContentVisualIdentity identity = (ContentVisualIdentity)target; // 현재 Inspector 대상 Identity 가져오기

        if (identity.TryGetVisualProfileId(out string profileId, out string errorMessage)) // 현재 Profile ID 계산 성공 여부 확인
        {
            EditorGUILayout.HelpBox( // 계산 성공 안내 상자 표시 시작
                $"Resolved Visual Profile ID\n{profileId}", // 계산된 Profile ID 표시
                MessageType.Info); // 정보 형식 도움말 상자 사용
        }
        else // 현재 Profile ID 계산 실패 시
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error); // 계산 실패 원인 표시
        }

        if (GUILayout.Button("Refresh Resolved Profile ID")) // Profile ID 미리보기 갱신 버튼 표시
        {
            Undo.RecordObject(identity, "Refresh Content Visual Identity"); // Identity 변경 전 Undo 등록
            identity.RefreshResolvedProfileId(); // 현재 설정으로 Profile ID 다시 계산
            EditorUtility.SetDirty(identity); // Identity 변경 상태 표시
        }

        if (GUILayout.Button("Validate Content Visual Identity")) // Identity 검증 버튼 표시
        {
            identity.ValidateIdentity(); // 현재 Identity 설정 전체 검증
        }
    }
}
