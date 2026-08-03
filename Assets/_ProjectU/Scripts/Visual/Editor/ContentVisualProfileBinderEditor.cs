using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(ContentVisualProfileBinder))] // ContentVisualProfileBinder 전용 Inspector 등록
public sealed class ContentVisualProfileBinderEditor : Editor // Visual Profile 적용과 검증 Editor 도구
{
    public override void OnInspectorGUI() // Binder 기본 Inspector와 관리 버튼 표시
    {
        DrawDefaultInspector(); // Binder 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 Inspector와 관리 도구 사이 간격 추가
        EditorGUILayout.LabelField("Visual Profile Tools", EditorStyles.boldLabel); // Profile 관리 도구 제목 표시
        ContentVisualProfileBinder binder = (ContentVisualProfileBinder)target; // 현재 Inspector 대상 Binder 가져오기

        if (GUILayout.Button("Apply Assigned Profile")) // 연결 Profile 적용 버튼 표시
        {
            Undo.RegisterFullObjectHierarchyUndo(binder.gameObject, "Apply Content Visual Profile"); // Profile 적용 전 전체 계층 Undo 등록
            binder.ApplyAssignedProfile(); // 연결 또는 Registry Profile 적용
            SaveBinderChanges(binder); // Binder와 Scene 변경 내용 저장
        }

        if (GUILayout.Button("Validate Assigned Profile")) // 연결 Profile 검증 버튼 표시
        {
            binder.ValidateAssignedProfile(); // Binder와 Profile 전체 검증
        }
    }

    [MenuItem("Project U/Visual/Apply Profiles To Selected Roots")] // 선택 Root Profile 일괄 적용 메뉴 등록
    private static void ApplyProfilesToSelectedRoots() // 선택한 전체 Root의 Binder Profile 일괄 적용
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("Visual Profile을 적용할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
            return; // 일괄 Profile 적용 처리 종료
        }

        int successCount = 0; // Profile 적용 성공 수
        int failedCount = 0; // Profile 적용 실패 수

        for (int index = 0; index < selectedObjects.Length; index++) // 선택된 전체 Root 순회
        {
            GameObject selectedObject = selectedObjects[index]; // 현재 선택 Root 가져오기
            ContentVisualProfileBinder binder = selectedObject == null // 선택 Root 존재 여부 확인
                ? null // 선택 Root가 없으면 Binder 없음
                : selectedObject.GetComponent<ContentVisualProfileBinder>(); // 현재 Root의 Binder 검색

            if (binder == null) // Binder 존재 여부 확인
            {
                Debug.LogWarning($"{selectedObject?.name ?? "NULL"}에 ContentVisualProfileBinder가 없습니다.", selectedObject); // Binder 누락 경고 출력
                failedCount++; // Profile 적용 실패 수 증가
                continue; // 다음 선택 Root로 이동
            }

            Undo.RegisterFullObjectHierarchyUndo(selectedObject, "Apply Selected Content Visual Profile"); // Profile 적용 전 전체 계층 Undo 등록

            if (binder.ApplyAssignedProfile()) // 현재 Binder Profile 적용 결과 확인
            {
                successCount++; // Profile 적용 성공 수 증가
            }
            else // 현재 Binder Profile 적용 실패 시
            {
                failedCount++; // Profile 적용 실패 수 증가
            }

            SaveBinderChanges(binder); // Binder와 Scene 변경 내용 저장
        }

        Debug.Log($"선택 Visual Profile 적용 완료 / 성공 {successCount} / 실패 {failedCount}"); // 일괄 Profile 적용 결과 출력
    }

    private static void SaveBinderChanges(ContentVisualProfileBinder binder) // Binder와 Visual Root 및 Scene 변경 내용 저장
    {
        if (binder == null) // Binder 참조 존재 여부 확인
        {
            return; // 변경 저장 처리 종료
        }

        EditorUtility.SetDirty(binder); // Binder 변경 상태 표시
        PrefabUtility.RecordPrefabInstancePropertyModifications(binder); // Binder Prefab Instance 변경 기록
        ContentVisualRoot visualRoot = binder.GetComponent<ContentVisualRoot>(); // 현재 Root의 ContentVisualRoot 검색

        if (visualRoot != null) // ContentVisualRoot 존재 여부 확인
        {
            EditorUtility.SetDirty(visualRoot); // ContentVisualRoot 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualRoot); // Visual Root Prefab Instance 변경 기록
        }

        if (binder.gameObject.scene.IsValid() && binder.gameObject.scene.isLoaded) // 열린 Scene 오브젝트 여부 확인
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binder.gameObject.scene); // 현재 Root가 속한 Scene을 변경 상태로 표시
        }
    }
}
