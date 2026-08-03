using System.Linq; // Registry Asset 경로 정렬 기능
using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(ContentVisualProfileBinder))] // ContentVisualProfileBinder 전용 Inspector 등록
public sealed class ContentVisualProfileBinderEditor : Editor // Visual Profile 자동 검색과 적용 및 검증 Editor 도구
{
    public override void OnInspectorGUI() // Binder 기본 Inspector와 관리 버튼 표시
    {
        DrawDefaultInspector(); // Binder 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 Inspector와 관리 도구 사이 간격 추가
        EditorGUILayout.LabelField("Visual Profile Tools", EditorStyles.boldLabel); // Profile 관리 도구 제목 표시
        ContentVisualProfileBinder binder = (ContentVisualProfileBinder)target; // 현재 Inspector 대상 Binder 가져오기

        if (binder.ResolveFromContentIdentity) // Identity 자동 연결 모드 여부 확인
        {
            DrawIdentityResolveStatus(binder); // 계산 Profile ID와 캐시 상태 표시

            if (GUILayout.Button("Resolve Identity Profile From Registry")) // Identity Profile Registry 검색 버튼 표시
            {
                ResolveIdentityProfileFromRegistry(binder, false); // Profile을 검색하여 Binder에 캐시
            }

            if (GUILayout.Button("Resolve Identity Profile And Apply")) // Identity Profile 검색과 적용 버튼 표시
            {
                ResolveIdentityProfileFromRegistry(binder, true); // Profile 검색 후 Visual에 즉시 적용
            }
        }

        if (GUILayout.Button("Apply Assigned Profile")) // 현재 연결 방식 Profile 적용 버튼 표시
        {
            Undo.RegisterFullObjectHierarchyUndo(binder.gameObject, "Apply Content Visual Profile"); // Profile 적용 전 전체 계층 Undo 등록
            binder.ApplyAssignedProfile(); // 직접 참조, Registry ID 또는 Identity Profile 적용
            SaveBinderChanges(binder); // Binder와 Scene 변경 내용 저장
        }

        if (GUILayout.Button("Validate Assigned Profile")) // 연결 Profile 검증 버튼 표시
        {
            binder.ValidateAssignedProfile(); // Identity와 Profile 및 Visual Root 전체 검증
        }
    }

    [MenuItem("Project U/Visual/Add Content Identity To Selected Roots")] // 선택 Root에 Identity 구조 추가 메뉴 등록
    private static void AddContentIdentityToSelectedRoots() // 선택 Root에 Identity와 Binder 및 Visual Root 구성
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("Content Identity를 추가할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
            return; // Identity 추가 처리 종료
        }

        int addedCount = 0; // Identity 추가 Root 수

        for (int index = 0; index < selectedObjects.Length; index++) // 선택된 전체 Root 순회
        {
            GameObject selectedObject = selectedObjects[index]; // 현재 선택 Root 가져오기

            if (selectedObject == null) // 선택 Root 존재 여부 확인
            {
                continue; // 다음 선택 Root로 이동
            }

            ContentVisualRoot visualRoot = selectedObject.GetComponent<ContentVisualRoot>(); // 현재 Root의 Visual Root 검색

            if (visualRoot == null) // ContentVisualRoot 존재 여부 확인
            {
                visualRoot = Undo.AddComponent<ContentVisualRoot>(selectedObject); // Undo 지원 방식으로 ContentVisualRoot 추가
            }

            ContentVisualProfileBinder binder = selectedObject.GetComponent<ContentVisualProfileBinder>(); // 현재 Root의 Binder 검색

            if (binder == null) // Binder 존재 여부 확인
            {
                binder = Undo.AddComponent<ContentVisualProfileBinder>(selectedObject); // Undo 지원 방식으로 Binder 추가
            }

            ContentVisualIdentity identity = selectedObject.GetComponent<ContentVisualIdentity>(); // 현재 Root의 Identity 검색

            if (identity == null) // Identity 존재 여부 확인
            {
                identity = Undo.AddComponent<ContentVisualIdentity>(selectedObject); // Undo 지원 방식으로 Identity 추가
                addedCount++; // Identity 추가 Root 수 증가
            }

            identity.RefreshResolvedProfileId(); // 현재 Identity 설정으로 Profile ID 계산 상태 갱신

            if (identity.IsIdentityValid) // Identity 설정이 정상인지 확인
            {
                binder.SetContentIdentityMode(true); // 정상 Identity만 자동 연결 모드 활성화
            }
            else // 새로 추가되어 Identity 내용이 비어 있는 경우
            {
                binder.SetContentIdentityMode(false); // 잘못된 자동 적용을 막기 위해 Identity 모드 비활성화

                Debug.LogWarning( // Identity 설정 필요 경고 출력
                    $"{selectedObject.name}에 ContentVisualIdentity를 추가했습니다. " // 현재 Root 안내
                    + "Category와 Content Id를 설정한 뒤 Identity Profile을 적용하세요.", // 후속 작업 안내
                    selectedObject); // 현재 Root를 Log Context로 지정
            }

            EditorUtility.SetDirty(visualRoot); // Visual Root 변경 상태 표시
            EditorUtility.SetDirty(binder); // Binder 변경 상태 표시
            EditorUtility.SetDirty(identity); // Identity 변경 상태 표시
            SaveBinderChanges(binder); // Prefab 또는 Scene 변경 내용 저장
        }

        Debug.Log($"선택 Root Content Identity 구성 완료 / 새 Identity {addedCount}개"); // Identity 구성 결과 출력
    }

    [MenuItem("Project U/Visual/Resolve And Apply Identity Profiles To Selected Roots")] // 선택 Root Identity Profile 일괄 적용 메뉴 등록
    private static void ResolveAndApplyIdentityProfilesToSelectedRoots() // 선택 Root의 Identity Profile을 Registry에서 검색 후 일괄 적용
    {
        GameObject[] selectedObjects = Selection.gameObjects; // 현재 선택된 전체 GameObject 가져오기

        if (selectedObjects == null || selectedObjects.Length <= 0) // 선택 오브젝트 존재 여부 확인
        {
            Debug.LogWarning("Identity Profile을 적용할 Root GameObject를 선택해야 합니다."); // 선택 누락 경고 출력
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

            if (ResolveIdentityProfileFromRegistry(binder, true)) // Identity Profile 검색과 적용 결과 확인
            {
                successCount++; // Profile 적용 성공 수 증가
            }
            else // Identity Profile 검색 또는 적용 실패 시
            {
                failedCount++; // Profile 적용 실패 수 증가
            }
        }

        Debug.Log($"선택 Identity Profile 적용 완료 / 성공 {successCount} / 실패 {failedCount}"); // 일괄 Identity Profile 적용 결과 출력
    }

    [MenuItem("Project U/Visual/Apply Profiles To Selected Roots")] // 선택 Root 현재 Profile 일괄 적용 메뉴 등록
    private static void ApplyProfilesToSelectedRoots() // 선택한 전체 Root의 현재 Binder Profile 일괄 적용
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

    private static void DrawIdentityResolveStatus(ContentVisualProfileBinder binder) // Binder Identity 계산과 캐시 Profile 상태 표시
    {
        if (!binder.TryGetRequestedProfileId(out string requestedProfileId, out string errorMessage)) // Identity Profile ID 계산 성공 여부 확인
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error); // Identity 계산 실패 원인 표시
            return; // Identity 상태 표시 종료
        }

        bool isCachedProfileMatching = binder.VisualProfile != null // 캐시 Profile 존재 여부 확인
            && binder.VisualProfile.ProfileId == requestedProfileId; // 캐시 Profile ID 일치 여부 확인
        string cacheStatus = isCachedProfileMatching // 캐시 Profile 일치 여부 확인
            ? $"Cached Profile: {binder.VisualProfile.name}" // 일치 시 캐시 Profile 이름 표시
            : "Cached Profile: Missing or Mismatched"; // 불일치 시 캐시 오류 표시
        MessageType messageType = isCachedProfileMatching // 캐시 상태에 맞는 도움말 형식 선택
            ? MessageType.Info // 정상 캐시는 정보 표시
            : MessageType.Warning; // 누락 또는 불일치는 경고 표시
        EditorGUILayout.HelpBox( // Identity 계산 상태 도움말 표시 시작
            $"Resolved ID: {requestedProfileId}\n{cacheStatus}", // 계산 ID와 캐시 상태 표시
            messageType); // 선택된 도움말 형식 사용
    }

    private static bool ResolveIdentityProfileFromRegistry( // Binder Identity Profile을 프로젝트 Registry에서 검색
        ContentVisualProfileBinder binder, // 검색과 적용 대상 Binder
        bool applyImmediately) // 검색 후 즉시 외형 적용 여부
    {
        if (binder == null) // Binder 참조 존재 여부 확인
        {
            return false; // Identity Profile 검색 실패 반환
        }

        binder.SetContentIdentityMode(true); // Binder의 Identity 자동 연결 모드 활성화

        if (!binder.TryGetRequestedProfileId(out string requestedProfileId, out string errorMessage)) // Identity Profile ID 계산 성공 여부 확인
        {
            Debug.LogError($"{binder.name} Identity Profile ID 계산 실패 / {errorMessage}", binder); // Identity 계산 오류 출력
            return false; // Identity Profile 검색 실패 반환
        }

        GameDataRegistry registry = FindProjectRegistry(); // 프로젝트 GameDataRegistry Asset 검색

        if (registry == null) // Registry Asset 존재 여부 확인
        {
            return false; // Identity Profile 검색 실패 반환
        }

        registry.RebuildLookup(false); // 최신 Registry 배열로 검색 Dictionary 구성

        if (!registry.TryGetVisualProfile(requestedProfileId, out ContentVisualProfile resolvedProfile)) // 계산 ID의 Profile 검색 성공 여부 확인
        {
            Debug.LogError( // Identity Profile Registry 검색 실패 출력 시작
                $"{binder.name}의 Identity가 요구하는 Visual Profile을 Registry에서 찾지 못했습니다. " // 현재 Root와 오류 안내
                + $"ID: {requestedProfileId}", // 누락된 Profile ID 추가
                binder); // 현재 Binder를 Log Context로 지정
            return false; // Identity Profile 검색 실패 반환
        }

        Undo.RegisterFullObjectHierarchyUndo(binder.gameObject, "Resolve Content Identity Profile"); // Profile 캐시와 적용 전 계층 Undo 등록
        binder.CacheResolvedProfile(resolvedProfile); // Registry에서 찾은 Profile을 Binder에 캐시

        if (applyImmediately && !binder.ApplyAssignedProfile()) // 즉시 적용 설정과 적용 성공 여부 확인
        {
            SaveBinderChanges(binder); // 실패 상태도 Editor에 저장
            return false; // Identity Profile 적용 실패 반환
        }

        SaveBinderChanges(binder); // Binder와 Visual Root 변경 내용 저장
        Debug.Log( // Identity Profile 검색 성공 결과 출력 시작
            $"{binder.name} Identity Profile 검색 완료 / " // 현재 Root와 검색 완료 안내
            + $"ID: {requestedProfileId} / " // 계산 Profile ID 추가
            + $"Profile: {resolvedProfile.name}", // 검색 Profile Asset 이름 추가
            binder); // 현재 Binder를 Log Context로 지정
        return true; // Identity Profile 검색 또는 적용 성공 반환
    }

    private static GameDataRegistry FindProjectRegistry() // 프로젝트의 GameDataRegistry Asset 검색
    {
        string[] registryGuids = AssetDatabase.FindAssets("t:GameDataRegistry"); // 프로젝트 전체 Registry Asset GUID 검색

        if (registryGuids == null || registryGuids.Length <= 0) // Registry Asset 존재 여부 확인
        {
            Debug.LogError("프로젝트에서 GameDataRegistry Asset을 찾지 못했습니다."); // Registry 누락 오류 출력
            return null; // Registry 검색 실패 반환
        }

        string[] registryPaths = registryGuids // 검색된 Registry GUID 배열 가져오기
            .Select(AssetDatabase.GUIDToAssetPath) // 각 GUID를 Asset 경로로 변환
            .OrderBy(path => path) // 재현 가능한 결과를 위해 경로 순서 정렬
            .ToArray(); // 정렬 결과 배열 생성

        if (registryPaths.Length > 1) // Registry Asset이 여러 개인지 확인
        {
            Debug.LogWarning( // 다중 Registry 경고 출력 시작
                $"GameDataRegistry Asset이 {registryPaths.Length}개 있습니다. " // 발견 개수 안내
                + $"첫 번째 Asset을 사용합니다. 경로: {registryPaths[0]}"); // 실제 사용 경로 안내
        }

        return AssetDatabase.LoadAssetAtPath<GameDataRegistry>(registryPaths[0]); // 첫 번째 Registry Asset 불러오기
    }

    private static void SaveBinderChanges(ContentVisualProfileBinder binder) // Binder와 Visual Root 및 Scene 변경 내용 저장
    {
        if (binder == null) // Binder 참조 존재 여부 확인
        {
            return; // 변경 저장 처리 종료
        }

        EditorUtility.SetDirty(binder); // Binder 변경 상태 표시
        PrefabUtility.RecordPrefabInstancePropertyModifications(binder); // Binder Prefab Instance 변경 기록
        ContentVisualIdentity identity = binder.GetComponent<ContentVisualIdentity>(); // 현재 Root의 Identity 검색

        if (identity != null) // Content Identity 존재 여부 확인
        {
            EditorUtility.SetDirty(identity); // Identity 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(identity); // Identity Prefab Instance 변경 기록
        }

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

        AssetDatabase.SaveAssets(); // Prefab Mode와 Asset 변경 내용 디스크 저장
    }
}
