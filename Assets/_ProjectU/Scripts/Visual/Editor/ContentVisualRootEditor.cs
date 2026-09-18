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
