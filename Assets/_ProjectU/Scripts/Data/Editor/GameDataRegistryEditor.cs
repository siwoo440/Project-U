using System.Collections.Generic; // Asset 목록과 ID 집합 기능
using System.Linq; // Asset 정렬 기능
using UnityEditor; // Unity Editor 확장 기능
using UnityEngine; // Unity 기본 기능

[CustomEditor(typeof(GameDataRegistry))] // GameDataRegistry 전용 Inspector 등록
public sealed class GameDataRegistryEditor : Editor // Registry 자동 수집과 검증 Inspector
{
    private const string DefaultRegistryFolder = "Assets/_ProjectU/Data/Registry"; // 기본 Registry Asset 폴더
    private const string DefaultRegistryPath = DefaultRegistryFolder + "/GameDataRegistry.asset"; // 기본 Registry Asset 경로

    public override void OnInspectorGUI() // Registry 기본 Inspector와 관리 버튼 표시
    {
        DrawDefaultInspector(); // GameDataRegistry의 기본 직렬화 필드 표시
        EditorGUILayout.Space(12f); // 기본 Inspector와 관리 영역 사이 간격 추가
        EditorGUILayout.LabelField("Project Data Management", EditorStyles.boldLabel); // Registry 관리 영역 제목 표시

        if (GUILayout.Button("Collect All Project Data")) // 프로젝트 전체 데이터 자동 수집 버튼 표시
        {
            CollectAllProjectData((GameDataRegistry)target); // 현재 Registry Asset에 전체 데이터 자동 등록
        }

        if (GUILayout.Button("Validate Registry")) // Registry 전체 검증 버튼 표시
        {
            ValidateRegistry((GameDataRegistry)target); // 현재 Registry Asset 전체 검증 실행
        }

        if (GUILayout.Button("Collect And Validate")) // 전체 수집 후 검증 버튼 표시
        {
            GameDataRegistry registry = (GameDataRegistry)target; // 현재 Inspector 대상 Registry 가져오기
            CollectAllProjectData(registry); // 프로젝트 전체 데이터 자동 수집
            ValidateRegistry(registry); // 수집 결과 Registry 전체 검증
        }
    }

    [MenuItem("Project U/Data/Create Or Refresh Game Data Registry")] // Unity 상단 Registry 생성 및 갱신 메뉴 등록
    private static void CreateOrRefreshDefaultRegistry() // 기본 경로 Registry Asset 생성 또는 갱신
    {
        EnsureFolderExists(DefaultRegistryFolder); // Registry Asset 기본 폴더 생성 보장
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(DefaultRegistryPath); // 기본 경로 기존 Registry 검색

        if (registry == null) // 기존 Registry Asset 존재 여부 확인
        {
            registry = CreateInstance<GameDataRegistry>(); // 새로운 GameDataRegistry 인스턴스 생성
            AssetDatabase.CreateAsset(registry, DefaultRegistryPath); // 기본 경로에 Registry Asset 저장
            AssetDatabase.SaveAssets(); // 새 Registry Asset 디스크 저장
            Debug.Log($"GameDataRegistry Asset을 생성했습니다. 경로: {DefaultRegistryPath}", registry); // Registry 생성 결과 출력
        }

        CollectAllProjectData(registry); // 프로젝트 전체 데이터 자동 수집
        ValidateRegistry(registry); // 자동 수집된 Registry 전체 검증
        Selection.activeObject = registry; // Project 창에서 Registry Asset 선택
        EditorGUIUtility.PingObject(registry); // 생성 또는 갱신된 Registry Asset 위치 강조
    }

    [MenuItem("Project U/Data/Validate Default Game Data Registry")] // Unity 상단 기본 Registry 검증 메뉴 등록
    private static void ValidateDefaultRegistry() // 기본 경로 Registry Asset 검증
    {
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(DefaultRegistryPath); // 기본 Registry Asset 검색

        if (registry == null) // 기본 Registry Asset 존재 여부 확인
        {
            Debug.LogError($"기본 GameDataRegistry Asset이 없습니다. 먼저 생성 메뉴를 실행하세요. 경로: {DefaultRegistryPath}"); // Registry Asset 누락 오류 출력
            return; // 기본 Registry 검증 중단
        }

        ValidateRegistry(registry); // 기본 Registry Asset 전체 검증
        Selection.activeObject = registry; // Project 창에서 Registry Asset 선택
        EditorGUIUtility.PingObject(registry); // 검증한 Registry Asset 위치 강조
    }

    private static void CollectAllProjectData(GameDataRegistry registry) // 프로젝트의 전체 콘텐츠 Data Asset을 Registry에 자동 등록
    {
        if (registry == null) // Registry Asset 참조 확인
        {
            Debug.LogError("전체 데이터를 등록할 GameDataRegistry Asset이 없습니다."); // Registry Asset 누락 오류 출력
            return; // 프로젝트 데이터 자동 수집 중단
        }

        List<ItemData> itemAssets = FindAssets<ItemData>() // 프로젝트 전체 ItemData 검색
            .OrderBy(itemData => itemData.ItemId) // 아이템 ID 순서로 정렬
            .ThenBy(itemData => itemData.name) // 같은 ID는 Asset 이름 순서로 정렬
            .ToList(); // 정렬 결과를 List로 변환

        List<CraftingRecipeData> craftingRecipeAssets = FindAssets<CraftingRecipeData>() // 프로젝트 전체 CraftingRecipeData 검색
            .OrderBy(recipeData => recipeData.RecipeId) // 제작법 ID 순서로 정렬
            .ThenBy(recipeData => recipeData.name) // 같은 ID는 Asset 이름 순서로 정렬
            .ToList(); // 정렬 결과를 List로 변환

        List<BuildRecipeData> buildRecipeAssets = FindAssets<BuildRecipeData>() // 프로젝트 전체 BuildRecipeData 검색
            .OrderBy(recipeData => recipeData.RecipeId) // 건축법 ID 순서로 정렬
            .ThenBy(recipeData => recipeData.name) // 같은 ID는 Asset 이름 순서로 정렬
            .ToList(); // 정렬 결과를 List로 변환

        List<EnemyCombatData> enemyAssets = FindAssets<EnemyCombatData>() // 프로젝트 전체 EnemyCombatData 검색
            .OrderBy(enemyData => enemyData.EnemyId) // 적 ID 순서로 정렬
            .ThenBy(enemyData => enemyData.name) // 같은 ID는 Asset 이름 순서로 정렬
            .ToList(); // 정렬 결과를 List로 변환

        SerializedObject serializedRegistry = new SerializedObject(registry); // Registry private 배열 수정을 위한 SerializedObject 생성
        serializedRegistry.Update(); // 최신 Registry 직렬화 상태 읽기
        AssignAssetArray(serializedRegistry.FindProperty("items"), itemAssets); // 전체 아이템 Asset 배열 등록
        AssignAssetArray(serializedRegistry.FindProperty("craftingRecipes"), craftingRecipeAssets); // 전체 제작법 Asset 배열 등록
        AssignAssetArray(serializedRegistry.FindProperty("buildRecipes"), buildRecipeAssets); // 전체 건축법 Asset 배열 등록
        AssignAssetArray(serializedRegistry.FindProperty("enemies"), enemyAssets); // 전체 적 Asset 배열 등록
        serializedRegistry.ApplyModifiedProperties(); // Registry 배열 변경 내용 적용
        EditorUtility.SetDirty(registry); // Registry Asset 변경 상태 표시
        AssetDatabase.SaveAssets(); // Registry 변경 내용 디스크 저장
        registry.RebuildLookup(false); // 수집된 데이터로 Registry 검색 정보 재구성

        Debug.Log( // 프로젝트 데이터 자동 수집 결과 출력 시작
            $"GameDataRegistry 자동 수집 완료 / " // 수집 완료 안내
            + $"아이템 {itemAssets.Count} / " // 수집 아이템 수 추가
            + $"제작법 {craftingRecipeAssets.Count} / " // 수집 제작법 수 추가
            + $"건축법 {buildRecipeAssets.Count} / " // 수집 건축법 수 추가
            + $"적 {enemyAssets.Count}", // 수집 적 수 추가
            registry); // Registry Asset을 Log Context로 지정
    }

    private static void ValidateRegistry(GameDataRegistry registry) // Registry 전체 등록 정보와 ID 검증
    {
        if (registry == null) // Registry Asset 참조 확인
        {
            Debug.LogError("검증할 GameDataRegistry Asset이 없습니다."); // Registry Asset 누락 오류 출력
            return; // Registry 검증 중단
        }

        registry.RebuildLookup(true); // Registry 자체 중복과 잘못된 ID 전체 검사
        ValidateRecommendedPrefixes(registry); // 데이터 종류별 권장 ID 접두사 검사
        ValidateCraftingResultRegistration(registry); // 제작 결과 아이템 Registry 등록 여부 검사
        EditorUtility.SetDirty(registry); // Registry Runtime 검증값 변경 상태 표시
        AssetDatabase.SaveAssets(); // Registry 검증 실행값 디스크 저장
    }

    private static void ValidateRecommendedPrefixes(GameDataRegistry registry) // 데이터 종류별 권장 ID 접두사 검사
    {
        ValidatePrefix( // 아이템 ID 접두사 검사 시작
            registry.Items, // 전체 아이템 데이터 목록
            itemData => itemData.ItemId, // ItemData에서 ID를 가져오는 함수
            "item_", // 아이템 권장 접두사
            "ItemData"); // 오류 출력용 데이터 종류 이름

        ValidatePrefix( // 제작법 ID 접두사 검사 시작
            registry.CraftingRecipes, // 전체 제작법 데이터 목록
            recipeData => recipeData.RecipeId, // CraftingRecipeData에서 ID를 가져오는 함수
            "recipe_", // 제작법 권장 접두사
            "CraftingRecipeData"); // 오류 출력용 데이터 종류 이름

        ValidatePrefix( // 건축법 ID 접두사 검사 시작
            registry.BuildRecipes, // 전체 건축법 데이터 목록
            recipeData => recipeData.RecipeId, // BuildRecipeData에서 ID를 가져오는 함수
            "structure_", // 건축법 권장 접두사
            "BuildRecipeData"); // 오류 출력용 데이터 종류 이름

        ValidatePrefix( // 적 ID 접두사 검사 시작
            registry.Enemies, // 전체 적 데이터 목록
            enemyData => enemyData.EnemyId, // EnemyCombatData에서 ID를 가져오는 함수
            "enemy_", // 적 권장 접두사
            "EnemyCombatData"); // 오류 출력용 데이터 종류 이름
    }

    private static void ValidateCraftingResultRegistration(GameDataRegistry registry) // 제작법 결과 아이템이 Registry에 등록되었는지 검사
    {
        for (int index = 0; index < registry.CraftingRecipes.Count; index++) // 전체 제작법 데이터 순회
        {
            CraftingRecipeData recipeData = registry.CraftingRecipes[index]; // 현재 제작법 데이터 가져오기

            if (recipeData == null || recipeData.ResultItem == null) // 제작법 또는 결과 아이템 참조 누락 여부 확인
            {
                if (recipeData != null) // 제작법 Asset 자체는 존재하는지 확인
                {
                    Debug.LogError($"제작법 결과 아이템이 연결되지 않았습니다. 제작법: {recipeData.name}", recipeData); // 결과 아이템 누락 오류 출력
                }

                continue; // 다음 제작법 데이터로 이동
            }

            if (registry.TryGetItem(recipeData.ResultItem.ItemId, out ItemData registeredItem) // 결과 아이템 ID가 Registry에 존재하는지 확인
                && registeredItem == recipeData.ResultItem) // Registry의 실제 Asset 참조도 같은지 확인
            {
                continue; // 정상 등록된 제작 결과 아이템 검사 완료
            }

            Debug.LogError( // 제작 결과 아이템 Registry 누락 오류 출력 시작
                $"제작법 결과 아이템이 GameDataRegistry에 등록되지 않았습니다. " // 오류 원인 안내
                + $"제작법: {recipeData.name} / 아이템: {recipeData.ResultItem.name}", // 누락된 제작법과 아이템 이름 추가
                recipeData); // 현재 제작법 Asset을 Log Context로 지정
        }
    }

    private static List<TData> FindAssets<TData>() // 프로젝트에서 지정 ScriptableObject 형식의 모든 Asset 검색
        where TData : UnityEngine.Object // Unity Asset 형식으로 제한
    {
        string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(TData).Name}"); // 지정 형식 Asset GUID 전체 검색
        List<TData> foundAssets = new List<TData>(assetGuids.Length); // 검색 결과 저장 List 생성

        for (int index = 0; index < assetGuids.Length; index++) // 검색된 전체 Asset GUID 순회
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[index]); // 현재 GUID의 프로젝트 경로 조회
            TData asset = AssetDatabase.LoadAssetAtPath<TData>(assetPath); // 현재 경로에서 지정 형식 Asset 불러오기

            if (asset != null) // Asset 불러오기 성공 여부 확인
            {
                foundAssets.Add(asset); // 자동 수집 결과에 Asset 추가
            }
        }

        return foundAssets; // 검색된 전체 Asset 목록 반환
    }

    private static void AssignAssetArray<TData>(SerializedProperty arrayProperty, IReadOnlyList<TData> assets) // SerializedProperty 배열에 Asset 참조 일괄 등록
        where TData : UnityEngine.Object // Unity Asset 형식으로 제한
    {
        if (arrayProperty == null) // Registry 배열 SerializedProperty 검색 결과 확인
        {
            Debug.LogError($"GameDataRegistry 배열 Property를 찾지 못했습니다. 형식: {typeof(TData).Name}"); // Registry 필드 검색 실패 오류 출력
            return; // Asset 배열 등록 중단
        }

        arrayProperty.arraySize = assets.Count; // Registry 배열 크기를 검색 Asset 수로 설정

        for (int index = 0; index < assets.Count; index++) // 전체 검색 Asset 순회
        {
            SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(index); // 현재 Registry 배열 항목 가져오기
            elementProperty.objectReferenceValue = assets[index]; // 현재 검색 Asset 참조 등록
        }
    }

    private static void ValidatePrefix<TData>( // 데이터 종류별 권장 ID 접두사 검사
        IReadOnlyList<TData> assets, // 검사할 데이터 Asset 목록
        System.Func<TData, string> idSelector, // 데이터에서 ID를 가져오는 함수
        string recommendedPrefix, // 데이터 종류별 권장 접두사
        string categoryName) // 오류 출력용 데이터 종류 이름
        where TData : UnityEngine.Object // Unity Asset 형식으로 제한
    {
        for (int index = 0; index < assets.Count; index++) // 전체 데이터 Asset 순회
        {
            TData asset = assets[index]; // 현재 검사할 Asset 가져오기

            if (asset == null) // 현재 Asset 참조 누락 여부 확인
            {
                continue; // 누락 Asset 접두사 검사 생략
            }

            string contentId = idSelector(asset); // 현재 Asset 고유 ID 조회

            if (string.IsNullOrEmpty(contentId) // ID가 비어 있는지 확인
                || contentId.StartsWith(recommendedPrefix, System.StringComparison.Ordinal)) // 권장 접두사를 사용하는지 확인
            {
                continue; // 정상 또는 별도 Registry 오류 대상이면 접두사 경고 생략
            }

            Debug.LogWarning( // 권장 ID 접두사 불일치 경고 출력 시작
                $"{categoryName} ID는 '{recommendedPrefix}' 접두사를 권장합니다. " // 권장 접두사 안내
                + $"Asset: {asset.name} / ID: {contentId}", // 현재 Asset과 ID 정보 추가
                asset); // 현재 데이터 Asset을 Log Context로 지정
        }
    }

    private static void EnsureFolderExists(string folderPath) // 중첩된 Unity Asset 폴더 생성 보장
    {
        string[] folderParts = folderPath.Split('/'); // 전체 Asset 경로를 폴더 이름으로 분리
        string currentPath = folderParts[0]; // 최상위 Assets 폴더를 시작 경로로 설정

        for (int index = 1; index < folderParts.Length; index++) // Assets 아래 전체 하위 폴더 순회
        {
            string nextPath = currentPath + "/" + folderParts[index]; // 현재 생성할 하위 폴더 전체 경로 구성

            if (!AssetDatabase.IsValidFolder(nextPath)) // 현재 하위 폴더 존재 여부 확인
            {
                AssetDatabase.CreateFolder(currentPath, folderParts[index]); // 현재 경로 아래에 하위 폴더 생성
            }

            currentPath = nextPath; // 다음 하위 폴더 검사를 위해 현재 경로 갱신
        }
    }
}
