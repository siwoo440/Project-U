using System; // GUID 생성 기능
using System.Collections.Generic; // 중복 검사 목록 기능
using UnityEditor; // Unity Editor 기능
using UnityEditor.SceneManagement; // Scene 변경 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 현재 Scene 기능

public static class WorldObjectIdValidator // 월드 오브젝트 ID 발급 도구
{
    [MenuItem("Tools/Project U/Assign And Validate World Object IDs")] // ID 발급 메뉴
    private static void AssignAndValidateWorldObjectIds() // 현재 Scene ID 발급과 중복 수정
    {
        HashSet<GameObject> targetObjects = new HashSet<GameObject>(); // ID 발급 대상 목록

        WorldItemPickup[] worldItems = UnityEngine.Object.FindObjectsByType<WorldItemPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 비활성화 포함 월드 아이템 검색

        for (int index = 0; index < worldItems.Length; index++) // 월드 아이템 순회
        {
            targetObjects.Add(worldItems[index].gameObject); // ID 발급 대상 추가
        }

        GatherableResource[] resources = UnityEngine.Object.FindObjectsByType<GatherableResource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None); // 비활성화 포함 채집 자원 검색

        for (int index = 0; index < resources.Length; index++) // 채집 자원 순회
        {
            targetObjects.Add(resources[index].gameObject); // ID 발급 대상 추가
        }

        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal); // 중복 확인 ID 목록
        int generatedIdCount = 0; // 새 ID 발급 수량

        foreach (GameObject targetObject in targetObjects) // 전체 대상 순회
        {
            WorldObjectIdentity identity = targetObject.GetComponent<WorldObjectIdentity>(); // 기존 ID 컴포넌트 검색

            if (identity == null) // ID 컴포넌트 없음 확인
            {
                identity = Undo.AddComponent<WorldObjectIdentity>(targetObject); // 실행 취소 가능한 컴포넌트 추가
            }

            string currentId = identity.WorldObjectId; // 현재 ID 조회
            bool needsNewId = string.IsNullOrWhiteSpace(currentId) || !usedIds.Add(currentId); // 빈 ID 또는 중복 ID 확인

            if (!needsNewId) // 기존 ID 사용 가능 확인
            {
                continue; // 새 ID 발급 생략
            }

            Undo.RecordObject(identity, "Assign World Object ID"); // ID 변경 실행 취소 기록
            string newId = Guid.NewGuid().ToString("N"); // 새로운 GUID 생성
            identity.AssignWorldObjectId(newId); // 새 ID 적용
            usedIds.Add(newId); // 사용 ID 목록 등록
            EditorUtility.SetDirty(identity); // 컴포넌트 변경 표시
            generatedIdCount++; // 발급 수량 증가
        }

        if (generatedIdCount > 0) // Scene 변경 여부 확인
        {
            Scene activeScene = SceneManager.GetActiveScene(); // 현재 Scene 조회
            EditorSceneManager.MarkSceneDirty(activeScene); // Scene 저장 필요 상태 표시
        }

        Debug.Log($"월드 오브젝트 ID 검사 완료 / 대상 {targetObjects.Count}개 / 새 ID {generatedIdCount}개"); // 검사 결과 출력
    }
}