using System.Linq; // Registry Asset 경로 정렬 기능
using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(ContentVisualDataSourceBinder))] // ContentVisualDataSourceBinder 전용 Inspector 등록
public sealed class ContentVisualDataSourceBinderEditor : Editor // 실제 데이터에서 Identity와 Visual Profile을 연결하는 Editor 도구
{
    public override void OnInspectorGUI() // 기본 Inspector와 데이터 연결 관리 도구 표시
    {
        DrawDefaultInspector(); // Data Source Binder 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 필드와 도구 사이 간격 추가
        EditorGUILayout.LabelField("Data Source Visual Tools", EditorStyles.boldLabel); // 데이터 연결 도구 제목 표시
        ContentVisualDataSourceBinder dataSourceBinder = (ContentVisualDataSourceBinder)target; // 현재 Inspector 대상 가져오기
        DrawRuntimeStatus(dataSourceBinder); // 현재 데이터 연결 계산 결과 표시

        if (GUILayout.Button("Refresh Identity From Data Source")) // 실제 데이터 기반 Identity 갱신 버튼 표시
        {
            Undo.RegisterCompleteObjectUndo(dataSourceBinder.gameObject, "Refresh Visual Identity From Data Source"); // Identity 변경 전 Undo 등록
            dataSourceBinder.RefreshIdentityFromDataSource(); // 실제 데이터에서 Identity 갱신
            SaveChanges(dataSourceBinder); // Prefab 또는 Scene 변경 내용 저장
        }

        if (GUILayout.Button("Resolve Profile From Registry And Apply")) // Registry Profile 검색 및 적용 버튼 표시
        {
            ResolveProfileFromRegistryAndApply(dataSourceBinder); // 실제 데이터 ID의 Profile을 검색하고 외형 적용
        }

        if (GUILayout.Button("Validate Data Source Visual Link")) // 실제 데이터부터 Visual까지 전체 검증 버튼 표시
        {
            dataSourceBinder.ValidateDataSourceVisualLink(); // 실제 데이터 연결 전체 검증
        }
    }

    private static void DrawRuntimeStatus(ContentVisualDataSourceBinder dataSourceBinder) // 현재 실제 데이터 연결 결과를 Inspector에 표시
    {
        if (dataSourceBinder == null) // Data Source Binder 존재 여부 확인
        {
            return; // 상태 표시 처리 종료
        }

        string contentId = string.IsNullOrWhiteSpace(dataSourceBinder.ResolvedContentId) // 계산된 콘텐츠 ID 존재 여부 확인
            ? "NOT RESOLVED" // 계산 결과가 없으면 미해결 표시
            : dataSourceBinder.ResolvedContentId; // 계산 결과가 있으면 콘텐츠 ID 표시
        string profileId = string.IsNullOrWhiteSpace(dataSourceBinder.ResolvedVisualProfileId) // 계산된 Profile ID 존재 여부 확인
            ? "NOT RESOLVED" // 계산 결과가 없으면 미해결 표시
            : dataSourceBinder.ResolvedVisualProfileId; // 계산 결과가 있으면 Profile ID 표시
        string dataName = string.IsNullOrWhiteSpace(dataSourceBinder.ResolvedDataAssetName) // 데이터 Asset 이름 존재 여부 확인
            ? "NOT RESOLVED" // 데이터 Asset을 찾지 못했으면 미해결 표시
            : dataSourceBinder.ResolvedDataAssetName; // 데이터 Asset이 있으면 이름 표시
        MessageType messageType = dataSourceBinder.LastSynchronizationSucceeded // 마지막 동기화 성공 여부 확인
            ? MessageType.Info // 성공 상태는 정보 형식 사용
            : MessageType.Warning; // 미실행 또는 실패 상태는 경고 형식 사용
        EditorGUILayout.HelpBox( // 현재 연결 상태 도움말 상자 표시 시작
            $"Resolved Source: {dataSourceBinder.ResolvedSourceType}\n" // 실제 데이터 원본 종류 표시
            + $"Data Asset: {dataName}\n" // 실제 데이터 Asset 이름 표시
            + $"Content ID: {contentId}\n" // 실제 콘텐츠 ID 표시
            + $"Visual Profile ID: {profileId}", // 계산 Visual Profile ID 표시
            messageType); // 선택된 도움말 형식 사용
    }

    private static bool ResolveProfileFromRegistryAndApply(ContentVisualDataSourceBinder dataSourceBinder) // 실제 데이터 ID의 Profile을 Registry에서 검색하고 적용
    {
        if (dataSourceBinder == null) // Data Source Binder 존재 여부 확인
        {
            return false; // Registry Profile 적용 실패 반환
        }

        Undo.RegisterFullObjectHierarchyUndo(dataSourceBinder.gameObject, "Resolve Data Source Visual Profile"); // Identity와 외형 변경 전 계층 Undo 등록

        if (!dataSourceBinder.RefreshIdentityFromDataSource()) // 실제 데이터에서 Identity 갱신 성공 여부 확인
        {
            SaveChanges(dataSourceBinder); // 실패 상태도 저장
            return false; // Registry Profile 적용 실패 반환
        }

        ContentVisualIdentity visualIdentity = dataSourceBinder.VisualIdentity; // 동기화된 ContentVisualIdentity 가져오기
        ContentVisualProfileBinder visualProfileBinder = dataSourceBinder.VisualProfileBinder; // 동기화된 ContentVisualProfileBinder 가져오기

        if (visualIdentity == null || visualProfileBinder == null) // 필수 Visual 구성 요소 존재 여부 확인
        {
            Debug.LogError($"{dataSourceBinder.name}에 ContentVisualIdentity와 ContentVisualProfileBinder가 필요합니다.", dataSourceBinder); // 필수 구성 누락 오류 출력
            SaveChanges(dataSourceBinder); // 실패 상태 저장
            return false; // Registry Profile 적용 실패 반환
        }

        if (!visualIdentity.TryGetVisualProfileId(out string requestedProfileId, out string errorMessage)) // Identity Profile ID 계산 성공 여부 확인
        {
            Debug.LogError($"{dataSourceBinder.name} Visual Profile ID 계산 실패 / {errorMessage}", visualIdentity); // Identity 계산 오류 출력
            SaveChanges(dataSourceBinder); // 실패 상태 저장
            return false; // Registry Profile 적용 실패 반환
        }

        GameDataRegistry registry = FindProjectRegistry(); // 프로젝트 GameDataRegistry Asset 검색

        if (registry == null) // Registry Asset 존재 여부 확인
        {
            SaveChanges(dataSourceBinder); // 실패 상태 저장
            return false; // Registry Profile 적용 실패 반환
        }

        registry.RebuildLookup(false); // 최신 Registry 배열로 검색 Dictionary 구성

        if (!registry.TryGetVisualProfile(requestedProfileId, out ContentVisualProfile resolvedProfile)) // 계산 ID의 Profile 검색 성공 여부 확인
        {
            Debug.LogError( // Registry Profile 누락 오류 출력 시작
                $"{dataSourceBinder.name}의 실제 데이터가 요구하는 Visual Profile을 찾지 못했습니다. " // 현재 Root와 오류 안내
                + $"ID: {requestedProfileId}", // 누락된 Profile ID 추가
                dataSourceBinder); // 현재 Data Source Binder를 Log Context로 지정
            SaveChanges(dataSourceBinder); // 실패 상태 저장
            return false; // Registry Profile 적용 실패 반환
        }

        visualProfileBinder.CacheResolvedProfile(resolvedProfile); // 검색된 Profile Asset을 Binder에 캐시
        visualProfileBinder.SetContentIdentityMode(true); // Binder의 Identity 자동 연결 모드 활성화
        bool applySucceeded = visualProfileBinder.ApplyAssignedProfile(); // 캐시된 Profile로 ContentVisualRoot 외형 적용
        SaveChanges(dataSourceBinder); // Binder와 Visual Root 및 Prefab 변경 내용 저장

        if (applySucceeded) // Profile 적용 성공 여부 확인
        {
            Debug.Log( // 실제 데이터 Profile 적용 성공 결과 출력 시작
                $"{dataSourceBinder.name} Data Source Profile 적용 완료 / " // 현재 Root와 적용 완료 안내
                + $"Content ID: {dataSourceBinder.ResolvedContentId} / " // 실제 콘텐츠 ID 추가
                + $"Profile ID: {requestedProfileId} / " // 계산된 Profile ID 추가
                + $"Profile: {resolvedProfile.name}", // 검색된 Profile Asset 이름 추가
                dataSourceBinder); // 현재 Data Source Binder를 Log Context로 지정
        }

        return applySucceeded; // Registry Profile 적용 결과 반환
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
                + $"첫 번째 Asset을 사용합니다. 경로: {registryPaths[0]}"); // 실제 사용 Registry 경로 안내
        }

        return AssetDatabase.LoadAssetAtPath<GameDataRegistry>(registryPaths[0]); // 첫 번째 Registry Asset 불러오기
    }

    private static void SaveChanges(ContentVisualDataSourceBinder dataSourceBinder) // Data Source Binder와 관련 Visual 구성 변경 내용 저장
    {
        if (dataSourceBinder == null) // Data Source Binder 존재 여부 확인
        {
            return; // 변경 저장 처리 종료
        }

        EditorUtility.SetDirty(dataSourceBinder); // Data Source Binder 변경 상태 표시
        PrefabUtility.RecordPrefabInstancePropertyModifications(dataSourceBinder); // Prefab Instance 변경 기록
        ContentVisualIdentity visualIdentity = dataSourceBinder.VisualIdentity; // 연결된 ContentVisualIdentity 가져오기

        if (visualIdentity != null) // ContentVisualIdentity 존재 여부 확인
        {
            EditorUtility.SetDirty(visualIdentity); // Identity 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualIdentity); // Identity Prefab Instance 변경 기록
        }

        ContentVisualProfileBinder visualProfileBinder = dataSourceBinder.VisualProfileBinder; // 연결된 ContentVisualProfileBinder 가져오기

        if (visualProfileBinder != null) // ContentVisualProfileBinder 존재 여부 확인
        {
            EditorUtility.SetDirty(visualProfileBinder); // Profile Binder 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualProfileBinder); // Profile Binder Prefab Instance 변경 기록
        }

        ContentVisualRoot visualRoot = dataSourceBinder.GetComponent<ContentVisualRoot>(); // 같은 Root의 ContentVisualRoot 검색

        if (visualRoot != null) // ContentVisualRoot 존재 여부 확인
        {
            EditorUtility.SetDirty(visualRoot); // Visual Root 변경 상태 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualRoot); // Visual Root Prefab Instance 변경 기록
        }

        if (dataSourceBinder.gameObject.scene.IsValid() && dataSourceBinder.gameObject.scene.isLoaded) // 열린 Scene 오브젝트 여부 확인
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dataSourceBinder.gameObject.scene); // 현재 Scene 변경 상태 표시
        }

        AssetDatabase.SaveAssets(); // Asset과 Prefab 변경 내용 디스크 저장
    }
}
