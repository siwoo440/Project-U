using System; // 문자열 비교와 직렬화 기능
using System.Collections.Generic; // 목록과 Dictionary 기능
using System.Text.RegularExpressions; // 콘텐츠 ID 형식 검사 기능
using UnityEngine; // Unity 기본 기능

[CreateAssetMenu( // ScriptableObject 생성 메뉴 설정
    fileName = "GameDataRegistry", // 새 Registry Asset 기본 이름
    menuName = "Project U/Data/Game Data Registry")] // Project 창 생성 메뉴 경로
public sealed class GameDataRegistry : ScriptableObject // 프로젝트의 공통 콘텐츠 데이터를 ID로 관리하는 Registry
{
    private const int CurrentRegistryVersion = 2; // Visual Profile 등록을 포함한 현재 Registry 구조 버전

    [Header("Registry")] // Registry 기본 설정 묶음
    [Tooltip("저장 데이터 호환성과 Registry 변경 추적에 사용할 버전입니다.")] // Inspector Registry 버전 설명
    [SerializeField, Min(CurrentRegistryVersion)] private int registryVersion = CurrentRegistryVersion; // Registry 버전

    [Header("Item Data")] // 아이템 데이터 목록 묶음
    [Tooltip("게임에서 사용할 전체 ItemData 목록입니다.")] // Inspector 아이템 목록 설명
    [SerializeField] private ItemData[] items = Array.Empty<ItemData>(); // 전체 아이템 데이터 목록

    [Header("Crafting Recipe Data")] // 제작법 데이터 목록 묶음
    [Tooltip("게임에서 사용할 전체 CraftingRecipeData 목록입니다.")] // Inspector 제작법 목록 설명
    [SerializeField] private CraftingRecipeData[] craftingRecipes = Array.Empty<CraftingRecipeData>(); // 전체 제작법 데이터 목록

    [Header("Build Recipe Data")] // 건축법 데이터 목록 묶음
    [Tooltip("게임에서 사용할 전체 BuildRecipeData 목록입니다.")] // Inspector 건축법 목록 설명
    [SerializeField] private BuildRecipeData[] buildRecipes = Array.Empty<BuildRecipeData>(); // 전체 건축법 데이터 목록

    [Header("Enemy Combat Data")] // 적 데이터 목록 묶음
    [Tooltip("게임에서 사용할 전체 EnemyCombatData 목록입니다.")] // Inspector 적 데이터 목록 설명
    [SerializeField] private EnemyCombatData[] enemies = Array.Empty<EnemyCombatData>(); // 전체 적 전투 데이터 목록

    [Header("Visual Profile Data")] // Visual Profile 데이터 목록 묶음
    [Tooltip("게임에서 사용할 전체 ContentVisualProfile 목록입니다.")] // Inspector Visual Profile 목록 설명
    [SerializeField] private ContentVisualProfile[] visualProfiles = Array.Empty<ContentVisualProfile>(); // 전체 Visual Profile 목록

    [Header("Runtime - Lookup")] // Registry 실행 상태 묶음
    [Tooltip("현재 정상 등록된 아이템 데이터 수입니다.")] // Inspector 아이템 등록 수 설명
    [SerializeField] private int registeredItemCount; // 정상 등록된 아이템 수

    [Tooltip("현재 정상 등록된 제작법 데이터 수입니다.")] // Inspector 제작법 등록 수 설명
    [SerializeField] private int registeredCraftingRecipeCount; // 정상 등록된 제작법 수

    [Tooltip("현재 정상 등록된 건축법 데이터 수입니다.")] // Inspector 건축법 등록 수 설명
    [SerializeField] private int registeredBuildRecipeCount; // 정상 등록된 건축법 수

    [Tooltip("현재 정상 등록된 적 데이터 수입니다.")] // Inspector 적 등록 수 설명
    [SerializeField] private int registeredEnemyCount; // 정상 등록된 적 수

    [Tooltip("현재 정상 등록된 Visual Profile 수입니다.")] // Inspector Visual Profile 등록 수 설명
    [SerializeField] private int registeredVisualProfileCount; // 정상 등록된 Visual Profile 수

    [Tooltip("같은 데이터 종류 안에서 발견한 중복 ID 수입니다.")] // Inspector 종류별 중복 ID 설명
    [SerializeField] private int duplicateIdCount; // 같은 종류 안의 중복 ID 수

    [Tooltip("서로 다른 데이터 종류 사이에서 발견한 동일 ID 수입니다.")] // Inspector 전체 중복 ID 설명
    [SerializeField] private int crossCategoryDuplicateIdCount; // 서로 다른 종류 사이의 중복 ID 수

    [Tooltip("비어 있거나 규칙에 맞지 않는 ID 수입니다.")] // Inspector 잘못된 ID 설명
    [SerializeField] private int invalidIdCount; // 비어 있거나 잘못된 ID 수

    private static readonly Regex ContentIdPattern = new Regex( // 공통 콘텐츠 ID 정규식 생성
        "^[a-z][a-z0-9]*(?:_[a-z0-9]+)+$", // 소문자와 숫자 및 밑줄 조합 규칙
        RegexOptions.Compiled); // 반복 검사 성능을 위한 정규식 컴파일

    private readonly Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>(StringComparer.Ordinal); // 아이템 ID 검색 Dictionary
    private readonly Dictionary<string, CraftingRecipeData> craftingRecipeLookup = new Dictionary<string, CraftingRecipeData>(StringComparer.Ordinal); // 제작법 ID 검색 Dictionary
    private readonly Dictionary<string, BuildRecipeData> buildRecipeLookup = new Dictionary<string, BuildRecipeData>(StringComparer.Ordinal); // 건축법 ID 검색 Dictionary
    private readonly Dictionary<string, EnemyCombatData> enemyLookup = new Dictionary<string, EnemyCombatData>(StringComparer.Ordinal); // 적 ID 검색 Dictionary
    private readonly Dictionary<string, ContentVisualProfile> visualProfileLookup = new Dictionary<string, ContentVisualProfile>(StringComparer.Ordinal); // Visual Profile ID 검색 Dictionary
    private readonly HashSet<string> allRegisteredIds = new HashSet<string>(StringComparer.Ordinal); // 전체 데이터 종류의 등록 ID 집합
    private bool isLookupReady; // ID 검색 Dictionary 준비 여부

    public int RegistryVersion => Mathf.Max(CurrentRegistryVersion, registryVersion); // Registry 버전 제공
    public IReadOnlyList<ItemData> Items => items; // 전체 아이템 데이터 목록 제공
    public IReadOnlyList<CraftingRecipeData> CraftingRecipes => craftingRecipes; // 전체 제작법 데이터 목록 제공
    public IReadOnlyList<BuildRecipeData> BuildRecipes => buildRecipes; // 전체 건축법 데이터 목록 제공
    public IReadOnlyList<EnemyCombatData> Enemies => enemies; // 전체 적 데이터 목록 제공
    public IReadOnlyList<ContentVisualProfile> VisualProfiles => visualProfiles; // 전체 Visual Profile 목록 제공
    public int RegisteredItemCount => registeredItemCount; // 정상 아이템 등록 수 제공
    public int RegisteredCraftingRecipeCount => registeredCraftingRecipeCount; // 정상 제작법 등록 수 제공
    public int RegisteredBuildRecipeCount => registeredBuildRecipeCount; // 정상 건축법 등록 수 제공
    public int RegisteredEnemyCount => registeredEnemyCount; // 정상 적 등록 수 제공
    public int RegisteredVisualProfileCount => registeredVisualProfileCount; // 정상 Visual Profile 등록 수 제공
    public int DuplicateIdCount => duplicateIdCount; // 종류별 중복 ID 수 제공
    public int CrossCategoryDuplicateIdCount => crossCategoryDuplicateIdCount; // 전체 종류 중복 ID 수 제공
    public int InvalidIdCount => invalidIdCount; // 잘못된 ID 수 제공
    public bool IsLookupReady => isLookupReady; // ID 검색 Dictionary 준비 여부 제공
    public bool HasValidationErrors => duplicateIdCount > 0 || crossCategoryDuplicateIdCount > 0 || invalidIdCount > 0; // Registry 오류 존재 여부 제공

    private void OnEnable() // Registry Asset 활성화 시 ID 검색 Dictionary 구성
    {
        RebuildLookup(false); // Console 출력 없이 Registry 검색 정보 구성
    }

    private void OnValidate() // Inspector 값과 Registry 배열 검증
    {
        registryVersion = Mathf.Max(CurrentRegistryVersion, registryVersion); // Registry 버전 최소값 적용
        items ??= Array.Empty<ItemData>(); // 아이템 배열 누락 시 빈 배열 생성
        craftingRecipes ??= Array.Empty<CraftingRecipeData>(); // 제작법 배열 누락 시 빈 배열 생성
        buildRecipes ??= Array.Empty<BuildRecipeData>(); // 건축법 배열 누락 시 빈 배열 생성
        enemies ??= Array.Empty<EnemyCombatData>(); // 적 배열 누락 시 빈 배열 생성
        visualProfiles ??= Array.Empty<ContentVisualProfile>(); // Visual Profile 배열 누락 시 빈 배열 생성
        RebuildLookup(false); // Inspector 변경 내용을 ID 검색 Dictionary에 반영
    }

    [ContextMenu("Rebuild Lookup")] // Inspector Registry 검색 정보 재구성 메뉴
    public void RebuildLookupFromContextMenu() // Inspector에서 Registry 검색 정보 수동 재구성
    {
        RebuildLookup(true); // Registry를 재구성하고 결과를 Console에 출력
    }

    [ContextMenu("Validate Registry")] // Inspector Registry 전체 검증 메뉴
    public void ValidateRegistryFromContextMenu() // Inspector에서 Registry 유효성 전체 검사
    {
        RebuildLookup(true); // Registry를 재구성하고 전체 오류를 Console에 출력
    }

    public void RebuildLookup(bool logResults) // 전체 데이터 목록을 ID 검색 Dictionary로 재구성
    {
        ClearLookupRuntime(); // 이전 ID 검색 정보와 검증 결과 초기화
        RegisterItems(logResults); // 전체 아이템 데이터 등록
        RegisterCraftingRecipes(logResults); // 전체 제작법 데이터 등록
        RegisterBuildRecipes(logResults); // 전체 건축법 데이터 등록
        RegisterEnemies(logResults); // 전체 적 데이터 등록
        RegisterVisualProfiles(logResults); // 전체 Visual Profile 등록
        isLookupReady = true; // ID 검색 Dictionary 준비 완료 상태 적용

        if (logResults) // Registry 결과 로그 사용 여부 확인
        {
            LogValidationSummary(); // Registry 전체 등록과 검증 결과 출력
        }
    }

    public bool TryGetItem(string itemId, out ItemData itemData) // 아이템 ID로 ItemData 검색
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return itemLookup.TryGetValue(NormalizeId(itemId), out itemData); // 정리된 아이템 ID 검색 결과 반환
    }

    public ItemData GetItemOrNull(string itemId) // 아이템 ID로 ItemData를 검색하고 실패 시 null 반환
    {
        return TryGetItem(itemId, out ItemData itemData) // 아이템 데이터 검색 실행
            ? itemData // 검색 성공 시 ItemData 반환
            : null; // 검색 실패 시 null 반환
    }

    public bool TryGetCraftingRecipe(string recipeId, out CraftingRecipeData recipeData) // 제작법 ID로 CraftingRecipeData 검색
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return craftingRecipeLookup.TryGetValue(NormalizeId(recipeId), out recipeData); // 정리된 제작법 ID 검색 결과 반환
    }

    public CraftingRecipeData GetCraftingRecipeOrNull(string recipeId) // 제작법 ID로 데이터를 검색하고 실패 시 null 반환
    {
        return TryGetCraftingRecipe(recipeId, out CraftingRecipeData recipeData) // 제작법 데이터 검색 실행
            ? recipeData // 검색 성공 시 CraftingRecipeData 반환
            : null; // 검색 실패 시 null 반환
    }

    public bool TryGetBuildRecipe(string recipeId, out BuildRecipeData recipeData) // 건축법 ID로 BuildRecipeData 검색
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return buildRecipeLookup.TryGetValue(NormalizeId(recipeId), out recipeData); // 정리된 건축법 ID 검색 결과 반환
    }

    public BuildRecipeData GetBuildRecipeOrNull(string recipeId) // 건축법 ID로 데이터를 검색하고 실패 시 null 반환
    {
        return TryGetBuildRecipe(recipeId, out BuildRecipeData recipeData) // 건축법 데이터 검색 실행
            ? recipeData // 검색 성공 시 BuildRecipeData 반환
            : null; // 검색 실패 시 null 반환
    }

    public bool TryGetEnemy(string enemyId, out EnemyCombatData enemyData) // 적 ID로 EnemyCombatData 검색
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return enemyLookup.TryGetValue(NormalizeId(enemyId), out enemyData); // 정리된 적 ID 검색 결과 반환
    }

    public EnemyCombatData GetEnemyOrNull(string enemyId) // 적 ID로 데이터를 검색하고 실패 시 null 반환
    {
        return TryGetEnemy(enemyId, out EnemyCombatData enemyData) // 적 데이터 검색 실행
            ? enemyData // 검색 성공 시 EnemyCombatData 반환
            : null; // 검색 실패 시 null 반환
    }

    public bool TryGetVisualProfile(string profileId, out ContentVisualProfile visualProfile) // Profile ID로 ContentVisualProfile 검색
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return visualProfileLookup.TryGetValue(NormalizeId(profileId), out visualProfile); // 정리된 Profile ID 검색 결과 반환
    }

    public ContentVisualProfile GetVisualProfileOrNull(string profileId) // Profile ID로 데이터를 검색하고 실패 시 null 반환
    {
        return TryGetVisualProfile(profileId, out ContentVisualProfile visualProfile) // Visual Profile 검색 실행
            ? visualProfile // 검색 성공 시 ContentVisualProfile 반환
            : null; // 검색 실패 시 null 반환
    }

    public bool ContainsAnyId(string contentId) // 전체 데이터 종류에 지정 ID가 존재하는지 확인
    {
        EnsureLookupReady(); // ID 검색 Dictionary 준비 상태 확인
        return allRegisteredIds.Contains(NormalizeId(contentId)); // 정리된 ID의 전체 Registry 등록 여부 반환
    }

    public static bool IsValidContentId(string contentId) // 공통 콘텐츠 ID가 규칙에 맞는지 검사
    {
        string normalizedId = NormalizeId(contentId); // 검사할 ID 양쪽 공백 제거
        return !string.IsNullOrEmpty(normalizedId) // 비어 있지 않은 ID인지 확인
            && ContentIdPattern.IsMatch(normalizedId); // 소문자 밑줄 ID 규칙 일치 여부 반환
    }

    private void RegisterItems(bool logResults) // 전체 ItemData를 아이템 검색 Dictionary에 등록
    {
        for (int index = 0; index < items.Length; index++) // 전체 아이템 데이터 순회
        {
            ItemData itemData = items[index]; // 현재 아이템 데이터 가져오기

            if (itemData == null) // 아이템 데이터 참조 누락 여부 확인
            {
                LogNullEntry("ItemData", index, logResults); // 누락된 아이템 참조 결과 출력
                continue; // 다음 아이템 데이터로 이동
            }

            if (TryRegisterEntry(itemData.ItemId, itemData, itemLookup, "ItemData", logResults)) // 아이템 ID 등록 시도
            {
                registeredItemCount++; // 정상 아이템 등록 수 증가
            }
        }
    }

    private void RegisterCraftingRecipes(bool logResults) // 전체 CraftingRecipeData를 제작법 검색 Dictionary에 등록
    {
        for (int index = 0; index < craftingRecipes.Length; index++) // 전체 제작법 데이터 순회
        {
            CraftingRecipeData recipeData = craftingRecipes[index]; // 현재 제작법 데이터 가져오기

            if (recipeData == null) // 제작법 데이터 참조 누락 여부 확인
            {
                LogNullEntry("CraftingRecipeData", index, logResults); // 누락된 제작법 참조 결과 출력
                continue; // 다음 제작법 데이터로 이동
            }

            if (TryRegisterEntry(recipeData.RecipeId, recipeData, craftingRecipeLookup, "CraftingRecipeData", logResults)) // 제작법 ID 등록 시도
            {
                registeredCraftingRecipeCount++; // 정상 제작법 등록 수 증가
            }
        }
    }

    private void RegisterBuildRecipes(bool logResults) // 전체 BuildRecipeData를 건축법 검색 Dictionary에 등록
    {
        for (int index = 0; index < buildRecipes.Length; index++) // 전체 건축법 데이터 순회
        {
            BuildRecipeData recipeData = buildRecipes[index]; // 현재 건축법 데이터 가져오기

            if (recipeData == null) // 건축법 데이터 참조 누락 여부 확인
            {
                LogNullEntry("BuildRecipeData", index, logResults); // 누락된 건축법 참조 결과 출력
                continue; // 다음 건축법 데이터로 이동
            }

            if (TryRegisterEntry(recipeData.RecipeId, recipeData, buildRecipeLookup, "BuildRecipeData", logResults)) // 건축법 ID 등록 시도
            {
                registeredBuildRecipeCount++; // 정상 건축법 등록 수 증가
            }
        }
    }

    private void RegisterEnemies(bool logResults) // 전체 EnemyCombatData를 적 검색 Dictionary에 등록
    {
        for (int index = 0; index < enemies.Length; index++) // 전체 적 데이터 순회
        {
            EnemyCombatData enemyData = enemies[index]; // 현재 적 데이터 가져오기

            if (enemyData == null) // 적 데이터 참조 누락 여부 확인
            {
                LogNullEntry("EnemyCombatData", index, logResults); // 누락된 적 참조 결과 출력
                continue; // 다음 적 데이터로 이동
            }

            if (TryRegisterEntry(enemyData.EnemyId, enemyData, enemyLookup, "EnemyCombatData", logResults)) // 적 ID 등록 시도
            {
                registeredEnemyCount++; // 정상 적 등록 수 증가
            }
        }
    }

    private void RegisterVisualProfiles(bool logResults) // 전체 ContentVisualProfile을 Profile 검색 Dictionary에 등록
    {
        for (int index = 0; index < visualProfiles.Length; index++) // 전체 Visual Profile 순회
        {
            ContentVisualProfile visualProfile = visualProfiles[index]; // 현재 Visual Profile 가져오기

            if (visualProfile == null) // Visual Profile 참조 누락 여부 확인
            {
                LogNullEntry("ContentVisualProfile", index, logResults); // 누락된 Visual Profile 참조 결과 출력
                continue; // 다음 Visual Profile로 이동
            }

            if (TryRegisterEntry(visualProfile.ProfileId, visualProfile, visualProfileLookup, "ContentVisualProfile", logResults)) // Profile ID 등록 시도
            {
                registeredVisualProfileCount++; // 정상 Visual Profile 등록 수 증가
            }
        }
    }

    private bool TryRegisterEntry<TData>( // 지정 데이터와 ID를 종류별 검색 Dictionary에 등록
        string contentId, // 등록할 콘텐츠 ID
        TData contentData, // 등록할 콘텐츠 데이터
        Dictionary<string, TData> lookup, // 등록 대상 종류별 Dictionary
        string categoryName, // 오류 출력에 사용할 데이터 종류 이름
        bool logResults) // Console 결과 출력 여부
        where TData : UnityEngine.Object // Unity Asset 데이터 형식 제한
    {
        string normalizedId = NormalizeId(contentId); // 등록할 ID 양쪽 공백 제거

        if (!IsValidContentId(normalizedId)) // ID 공통 규칙 일치 여부 확인
        {
            invalidIdCount++; // 잘못된 ID 수 증가

            if (logResults) // 오류 로그 사용 여부 확인
            {
                Debug.LogError($"{categoryName} ID가 올바르지 않습니다. Asset: {contentData.name} / ID: '{normalizedId}'", contentData); // 잘못된 ID 오류 출력
            }

            return false; // 잘못된 ID 등록 실패 반환
        }

        if (lookup.ContainsKey(normalizedId)) // 같은 데이터 종류 안의 중복 ID 확인
        {
            duplicateIdCount++; // 같은 종류 중복 ID 수 증가

            if (logResults) // 오류 로그 사용 여부 확인
            {
                Debug.LogError($"{categoryName} 중복 ID가 있습니다. ID: {normalizedId} / Asset: {contentData.name}", contentData); // 종류별 중복 ID 오류 출력
            }

            return false; // 중복 ID 등록 실패 반환
        }

        if (!allRegisteredIds.Add(normalizedId)) // 다른 데이터 종류에 같은 ID가 이미 등록되었는지 확인
        {
            crossCategoryDuplicateIdCount++; // 서로 다른 종류 중복 ID 수 증가

            if (logResults) // 오류 로그 사용 여부 확인
            {
                Debug.LogError($"서로 다른 데이터 종류에서 같은 ID를 사용하고 있습니다. ID: {normalizedId} / Asset: {contentData.name}", contentData); // 전체 종류 중복 ID 오류 출력
            }

            return false; // 전체 종류 중복 ID 등록 실패 반환
        }

        lookup.Add(normalizedId, contentData); // 종류별 ID 검색 Dictionary에 데이터 등록
        return true; // 콘텐츠 데이터 정상 등록 반환
    }

    private void EnsureLookupReady() // ID 검색 Dictionary 준비 상태 보장
    {
        if (isLookupReady) // 이미 검색 Dictionary가 준비되었는지 확인
        {
            return; // Registry 재구성 생략
        }

        RebuildLookup(false); // Console 출력 없이 Registry 검색 정보 구성
    }

    private void ClearLookupRuntime() // 이전 Registry 검색 정보와 실행값 초기화
    {
        itemLookup.Clear(); // 아이템 검색 Dictionary 초기화
        craftingRecipeLookup.Clear(); // 제작법 검색 Dictionary 초기화
        buildRecipeLookup.Clear(); // 건축법 검색 Dictionary 초기화
        enemyLookup.Clear(); // 적 검색 Dictionary 초기화
        visualProfileLookup.Clear(); // Visual Profile 검색 Dictionary 초기화
        allRegisteredIds.Clear(); // 전체 등록 ID 집합 초기화
        registeredItemCount = 0; // 정상 아이템 등록 수 초기화
        registeredCraftingRecipeCount = 0; // 정상 제작법 등록 수 초기화
        registeredBuildRecipeCount = 0; // 정상 건축법 등록 수 초기화
        registeredEnemyCount = 0; // 정상 적 등록 수 초기화
        registeredVisualProfileCount = 0; // 정상 Visual Profile 등록 수 초기화
        duplicateIdCount = 0; // 종류별 중복 ID 수 초기화
        crossCategoryDuplicateIdCount = 0; // 전체 종류 중복 ID 수 초기화
        invalidIdCount = 0; // 잘못된 ID 수 초기화
        isLookupReady = false; // 검색 Dictionary 준비 상태 초기화
    }

    private void LogNullEntry(string categoryName, int index, bool logResults) // Registry 배열의 빈 참조 경고 출력
    {
        if (!logResults) // Console 결과 출력 여부 확인
        {
            return; // 빈 참조 경고 출력 생략
        }

        Debug.LogWarning($"{categoryName} 목록의 {index}번 항목이 비어 있습니다.", this); // Registry 빈 참조 경고 출력
    }

    private void LogValidationSummary() // Registry 등록과 검증 결과 요약 출력
    {
        string summary = // Registry 결과 문자열 생성 시작
            $"GameDataRegistry 검증 완료 / " // Registry 검증 완료 안내
            + $"아이템 {registeredItemCount} / " // 정상 아이템 수 추가
            + $"제작법 {registeredCraftingRecipeCount} / " // 정상 제작법 수 추가
            + $"건축법 {registeredBuildRecipeCount} / " // 정상 건축법 수 추가
            + $"적 {registeredEnemyCount} / " // 정상 적 수 추가
            + $"Visual Profile {registeredVisualProfileCount} / " // 정상 Visual Profile 수 추가
            + $"종류별 중복 {duplicateIdCount} / " // 종류별 중복 수 추가
            + $"전체 중복 {crossCategoryDuplicateIdCount} / " // 전체 종류 중복 수 추가
            + $"잘못된 ID {invalidIdCount}"; // 잘못된 ID 수 추가

        if (HasValidationErrors) // Registry 검증 오류 존재 여부 확인
        {
            Debug.LogError(summary, this); // 오류가 포함된 Registry 결과 출력
            return; // 정상 결과 출력 생략
        }

        Debug.Log(summary, this); // 정상 Registry 등록 결과 출력
    }

    private static string NormalizeId(string contentId) // ID 검색과 검사를 위해 양쪽 공백 제거
    {
        return string.IsNullOrWhiteSpace(contentId) // ID 입력 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 반환
            : contentId.Trim(); // 입력이 있으면 양쪽 공백을 제거하여 반환
    }
}
