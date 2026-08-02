using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(ContentVisualRoot))] // ContentVisualRoot 전용 Inspector 등록
public sealed class ContentVisualRootEditor : Editor // Visual 표준 구조 생성과 검증 Editor 도구
{
    public override void OnInspectorGUI() // 기본 Inspector와 Visual 관리 버튼 표시
    {
        DrawDefaultInspector(); // ContentVisualRoot 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 Inspector와 관리 도구 사이 간격 추가
        EditorGUILayout.LabelField("Visual Structure Tools", EditorStyles.boldLabel); // Visual 관리 도구 제목 표시
        ContentVisualRoot visualRoot = (ContentVisualRoot)target; // 현재 Inspector 대상 ContentVisualRoot 가져오기

        if (GUILayout.Button("Ensure Standard Structure")) // 표준 자식 구조 생성 버튼 표시
        {
            RegisterHierarchyUndo(visualRoot, "Ensure Standard Visual Structure"); // 구조 생성 전 전체 계층 Undo 등록
            visualRoot.EnsureStandardStructure(); // 표준 Visual과 기준점 자식 생성
            SaveVisualRootChanges(visualRoot); // 컴포넌트와 Prefab 변경 상태 저장
        }

        if (GUILayout.Button("Rebuild Configured Visual")) // 설정 외형 재생성 버튼 표시
        {
            RegisterHierarchyUndo(visualRoot, "Rebuild Configured Visual"); // 외형 재생성 전 전체 계층 Undo 등록
            visualRoot.RebuildConfiguredVisual(); // 설정 Prefab 또는 임시 Primitive 생성
            SaveVisualRootChanges(visualRoot); // 컴포넌트와 Prefab 변경 상태 저장
        }

        if (GUILayout.Button("Apply Current Visual Transform")) // 현재 외형 Transform 적용 버튼 표시
        {
            RegisterHierarchyUndo(visualRoot, "Apply Visual Transform"); // 외형 Transform 변경 전 전체 계층 Undo 등록
            visualRoot.ApplyCurrentVisualTransform(); // Inspector 외형 Transform 설정 적용
            SaveVisualRootChanges(visualRoot); // 컴포넌트와 Prefab 변경 상태 저장
        }

        if (GUILayout.Button("Disable Legacy Root Renderers")) // 기존 Root Renderer 비활성화 버튼 표시
        {
            RegisterHierarchyUndo(visualRoot, "Disable Legacy Root Renderers"); // Renderer 변경 전 전체 계층 Undo 등록
            visualRoot.DisableLegacyRootRenderers(); // Root에 남은 기존 Renderer 비활성화
            SaveVisualRootChanges(visualRoot); // 컴포넌트와 Prefab 변경 상태 저장
        }

        if (GUILayout.Button("Enable Legacy Root Renderers")) // 기존 Root Renderer 활성화 버튼 표시
        {
            RegisterHierarchyUndo(visualRoot, "Enable Legacy Root Renderers"); // Renderer 변경 전 전체 계층 Undo 등록
            visualRoot.EnableLegacyRootRenderers(); // Root에 남은 기존 Renderer 다시 활성화
            SaveVisualRootChanges(visualRoot); // 컴포넌트와 Prefab 변경 상태 저장
        }

        if (GUILayout.Button("Validate Visual Structure")) // Visual 구조 검증 버튼 표시
        {
            visualRoot.ValidateVisualStructure(); // 현재 Root 표준 Visual 구조 검증
        }
    }

    [MenuItem("Project U/Visual/Add Standard Visual Root To Selection")] // 선택 오브젝트에 표준 Visual 구조 추가 메뉴 등록
    private static void AddStandardVisualRootToSelection() // 선택한 전체 GameObject에 ContentVisualRoot와 표준 자식 구조 추가
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("표준 Visual 구조를 추가할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
            return; // Visual 구조 추가 처리 종료
        }

        int addedComponentCount = 0; // 새로 추가한 ContentVisualRoot 수
        int processedObjectCount = 0; // 표준 구조를 처리한 Root 수

        for (int index = 0; index < selectedObjects.Length; index++) // 선택된 전체 Root 오브젝트 순회
        {
            GameObject selectedObject = selectedObjects[index]; // 현재 선택 Root 가져오기

            if (selectedObject == null) // 선택 Root 참조 존재 여부 확인
            {
                continue; // 다음 선택 오브젝트로 이동
            }

            Undo.RegisterFullObjectHierarchyUndo(selectedObject, "Add Standard Visual Root"); // 선택 Root 전체 계층 Undo 등록
            ContentVisualRoot visualRoot = selectedObject.GetComponent<ContentVisualRoot>(); // 기존 ContentVisualRoot 검색

            if (visualRoot == null) // 기존 ContentVisualRoot 존재 여부 확인
            {
                visualRoot = Undo.AddComponent<ContentVisualRoot>(selectedObject); // Undo 지원 방식으로 ContentVisualRoot 추가
                addedComponentCount++; // 신규 컴포넌트 추가 수 증가
            }

            visualRoot.EnsureStandardStructure(); // 선택 Root의 표준 Visual과 기준점 구조 생성
            EditorUtility.SetDirty(visualRoot); // ContentVisualRoot 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualRoot); // Prefab Instance 변경 내용 기록
            processedObjectCount++; // 처리 Root 수 증가
        }

        Debug.Log( // 표준 Visual 구조 일괄 추가 결과 출력 시작
            $"표준 Visual 구조 적용 완료 / Root {processedObjectCount}개 / " // 처리 Root 수 안내
            + $"신규 ContentVisualRoot {addedComponentCount}개"); // 신규 컴포넌트 수 안내
    }

    [MenuItem("Project U/Visual/Add Standard Visual Root To Selection", true)] // 선택 상태에 따른 메뉴 활성 조건 등록
    private static bool ValidateAddStandardVisualRootToSelection() // 선택 GameObject가 있을 때만 Visual 추가 메뉴 활성화
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0; // 선택 GameObject 존재 여부 반환
    }

    [MenuItem("Project U/Visual/Rebuild Selected Visuals")] // 선택 ContentVisualRoot 외형 일괄 재생성 메뉴 등록
    private static void RebuildSelectedVisuals() // 선택한 Root의 설정 외형을 일괄 재생성
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("외형을 재생성할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
            return; // 선택 외형 재생성 처리 종료
        }

        int rebuiltCount = 0; // 외형 재생성 완료 수

        for (int index = 0; index < selectedObjects.Length; index++) // 선택된 전체 GameObject 순회
        {
            GameObject selectedObject = selectedObjects[index]; // 현재 선택 GameObject 가져오기

            if (selectedObject == null) // 선택 GameObject 참조 존재 여부 확인
            {
                continue; // 다음 선택 오브젝트로 이동
            }

            ContentVisualRoot visualRoot = selectedObject.GetComponent<ContentVisualRoot>(); // 현재 Root의 ContentVisualRoot 검색

            if (visualRoot == null) // ContentVisualRoot 존재 여부 확인
            {
                Debug.LogWarning($"{selectedObject.name}에 ContentVisualRoot가 없습니다.", selectedObject); // 컴포넌트 누락 경고 출력
                continue; // 다음 선택 오브젝트로 이동
            }

            RegisterHierarchyUndo(visualRoot, "Rebuild Selected Visual"); // 외형 재생성 전 전체 계층 Undo 등록
            visualRoot.RebuildConfiguredVisual(); // 현재 설정으로 외형 재생성
            SaveVisualRootChanges(visualRoot); // 변경 내용 저장
            rebuiltCount++; // 외형 재생성 완료 수 증가
        }

        Debug.Log($"선택 Visual 재생성 완료 / {rebuiltCount}개"); // 선택 외형 재생성 결과 출력
    }

    [MenuItem("Project U/Visual/Validate Selected Visual Roots")] // 선택 ContentVisualRoot 일괄 검증 메뉴 등록
    private static void ValidateSelectedVisualRoots() // 선택한 Root의 표준 Visual 구조 일괄 검증
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("Visual 구조를 검증할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
            return; // 선택 Visual 검증 처리 종료
        }

        int validCount = 0; // 정상 Visual 구조 수
        int invalidCount = 0; // 잘못된 Visual 구조 수

        for (int index = 0; index < selectedObjects.Length; index++) // 선택된 전체 GameObject 순회
        {
            GameObject selectedObject = selectedObjects[index]; // 현재 선택 GameObject 가져오기

            if (selectedObject == null) // 선택 GameObject 참조 존재 여부 확인
            {
                continue; // 다음 선택 오브젝트로 이동
            }

            ContentVisualRoot visualRoot = selectedObject.GetComponent<ContentVisualRoot>(); // 현재 Root의 ContentVisualRoot 검색

            if (visualRoot == null) // ContentVisualRoot 존재 여부 확인
            {
                Debug.LogWarning($"{selectedObject.name}에 ContentVisualRoot가 없습니다.", selectedObject); // 컴포넌트 누락 경고 출력
                invalidCount++; // 잘못된 Visual 구조 수 증가
                continue; // 다음 선택 오브젝트로 이동
            }

            if (visualRoot.ValidateVisualStructure()) // 현재 Root Visual 구조 검증 결과 확인
            {
                validCount++; // 정상 Visual 구조 수 증가
            }
            else // 현재 Root Visual 구조가 잘못된 경우
            {
                invalidCount++; // 잘못된 Visual 구조 수 증가
            }
        }

        Debug.Log($"선택 Visual 구조 검증 완료 / 정상 {validCount} / 오류 {invalidCount}"); // 선택 Visual 구조 검증 요약 출력
    }

    private static void RegisterHierarchyUndo(ContentVisualRoot visualRoot, string undoName) // ContentVisualRoot 전체 계층 변경 Undo 등록
    {
        if (visualRoot == null) // ContentVisualRoot 참조 존재 여부 확인
        {
            return; // Undo 등록 처리 종료
        }

        Undo.RegisterFullObjectHierarchyUndo(visualRoot.gameObject, undoName); // 현재 Root 전체 계층 Undo 등록
    }

    private static void SaveVisualRootChanges(ContentVisualRoot visualRoot) // ContentVisualRoot와 Prefab Instance 변경 내용을 저장
    {
        if (visualRoot == null) // ContentVisualRoot 참조 존재 여부 확인
        {
            return; // 변경 저장 처리 종료
        }

        EditorUtility.SetDirty(visualRoot); // ContentVisualRoot 변경 상태 표시
        PrefabUtility.RecordPrefabInstancePropertyModifications(visualRoot); // Prefab Instance 직렬화 변경 기록
        ContentVisualEditorSceneBridge.MarkCurrentSceneDirty(visualRoot.gameObject); // Scene 오브젝트 변경 시 현재 Scene 저장 필요 상태 적용
    }
}

public static class ContentVisualEditorSceneBridge // Scene Asset 참조 없이 현재 Scene 변경 상태를 처리하는 Editor 보조 클래스
{
    public static void MarkCurrentSceneDirty(GameObject targetObject) // 대상이 Scene 오브젝트이면 현재 Scene을 변경 상태로 표시
    {
        if (targetObject == null) // 대상 GameObject 존재 여부 확인
        {
            return; // Scene 변경 상태 처리 종료
        }

        if (!targetObject.scene.IsValid() || !targetObject.scene.isLoaded) // 대상이 열린 Scene 오브젝트인지 확인
        {
            return; // Prefab Asset 등 Scene 외부 대상 처리 생략
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetObject.scene); // 대상 오브젝트가 속한 Scene을 변경 상태로 표시
    }
}
