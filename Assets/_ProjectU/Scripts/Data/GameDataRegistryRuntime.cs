using UnityEngine; // Unity 기본 기능

[DefaultExecutionOrder(-1000)] // 다른 게임 시스템보다 먼저 Registry 초기화
[DisallowMultipleComponent] // 동일 Registry Runtime 컴포넌트 중복 방지
public sealed class GameDataRegistryRuntime : MonoBehaviour // Scene에서 공통 Registry를 제공하는 런타임 관리자
{
    [Header("Registry")] // Registry 참조 설정 묶음
    [Tooltip("게임 전체에서 사용할 GameDataRegistry Asset입니다.")] // Inspector Registry Asset 설명
    [SerializeField] private GameDataRegistry registry; // 게임 전체 공통 데이터 Registry

    [Tooltip("Scene이 변경되어도 Registry Runtime 오브젝트를 유지합니다.")] // Inspector Scene 유지 설명
    [SerializeField] private bool persistBetweenScenes = true; // Scene 전환 시 오브젝트 유지 여부

    [Header("Validation")] // Registry 시작 검증 설정 묶음
    [Tooltip("Play Mode 시작 시 Registry 전체 검증 결과를 Console에 출력합니다.")] // Inspector 시작 검증 설명
    [SerializeField] private bool validateOnAwake = true; // Play Mode 시작 시 Registry 검증 여부

    [Header("Runtime")] // Registry Runtime 실행 상태 묶음
    [Tooltip("현재 이 컴포넌트가 전역 Registry Runtime으로 등록되었는지 표시합니다.")] // Inspector 전역 등록 상태 설명
    [SerializeField] private bool isPrimaryInstance; // 현재 전역 Registry Runtime 여부

    private static GameDataRegistryRuntime instance; // 현재 전역 Registry Runtime 인스턴스

    public static GameDataRegistryRuntime Instance => instance; // 현재 전역 Registry Runtime 제공
    public static bool HasInstance => instance != null; // 전역 Registry Runtime 존재 여부 제공
    public GameDataRegistry Registry => registry; // 연결된 GameDataRegistry Asset 제공
    public bool IsReady => isPrimaryInstance && registry != null && registry.IsLookupReady; // Registry Runtime 사용 준비 여부 제공

    private void Reset() // 컴포넌트 최초 추가 시 기본값 설정
    {
        persistBetweenScenes = true; // Scene 전환 유지 기본 활성화
        validateOnAwake = true; // 시작 Registry 검증 기본 활성화
    }

    private void Awake() // 전역 Registry Runtime 초기화
    {
        if (instance != null && instance != this) // 이미 다른 전역 Registry Runtime이 존재하는지 확인
        {
            Debug.LogWarning("중복 GameDataRegistryRuntime을 제거합니다.", this); // 중복 Registry Runtime 경고 출력
            Destroy(gameObject); // 현재 중복 Registry Runtime 오브젝트 제거
            return; // 현재 Registry Runtime 초기화 중단
        }

        instance = this; // 현재 컴포넌트를 전역 Registry Runtime으로 등록
        isPrimaryInstance = true; // 전역 Registry Runtime 상태 적용

        if (persistBetweenScenes) // Scene 전환 유지 설정 확인
        {
            DontDestroyOnLoad(gameObject); // Registry Runtime 오브젝트를 Scene 전환 후에도 유지
        }

        if (registry == null) // GameDataRegistry Asset 연결 여부 확인
        {
            Debug.LogError("GameDataRegistryRuntime에 GameDataRegistry Asset을 연결해야 합니다.", this); // Registry Asset 누락 오류 출력
            enabled = false; // Registry Runtime 기능 비활성화
            return; // Registry Runtime 초기화 중단
        }

        registry.RebuildLookup(validateOnAwake); // Registry ID 검색 Dictionary 구성과 선택적 검증 실행
    }

    private void OnDestroy() // Registry Runtime 오브젝트 제거 시 전역 참조 정리
    {
        if (instance != this) // 현재 전역 Registry Runtime인지 확인
        {
            return; // 다른 인스턴스의 전역 참조는 유지
        }

        instance = null; // 전역 Registry Runtime 참조 초기화
        isPrimaryInstance = false; // 전역 Registry Runtime 상태 해제
    }

    [ContextMenu("Validate Connected Registry")] // Inspector 연결 Registry 검증 메뉴
    private void ValidateConnectedRegistry() // 연결된 Registry를 수동 검증
    {
        if (registry == null) // Registry Asset 연결 여부 확인
        {
            Debug.LogError("검증할 GameDataRegistry Asset이 연결되지 않았습니다.", this); // Registry Asset 누락 오류 출력
            return; // Registry 검증 중단
        }

        registry.RebuildLookup(true); // Registry 전체 검색 정보 재구성과 결과 출력
    }

    [ContextMenu("Test First Registered Entries")] // Inspector 첫 등록 데이터 검색 테스트 메뉴
    private void TestFirstRegisteredEntries() // Registry 각 종류의 첫 번째 등록 데이터 검색 결과 확인
    {
        if (!Application.isPlaying) // Play Mode 실행 여부 확인
        {
            Debug.LogWarning("Registry ID 검색 테스트는 Play Mode에서 실행해야 합니다.", this); // Edit Mode 테스트 경고 출력
            return; // ID 검색 테스트 중단
        }

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            Debug.LogError("검색 테스트를 실행할 GameDataRegistry Asset이 없습니다.", this); // Registry Asset 누락 오류 출력
            return; // ID 검색 테스트 중단
        }

        if (registry.Items.Count > 0 && registry.Items[0] != null) // 첫 번째 아이템 데이터 존재 여부 확인
        {
            TestItemId(registry.Items[0].ItemId); // 첫 번째 등록 아이템 ID 검색 테스트
        }

        if (registry.CraftingRecipes.Count > 0 && registry.CraftingRecipes[0] != null) // 첫 번째 제작법 데이터 존재 여부 확인
        {
            TestCraftingRecipeId(registry.CraftingRecipes[0].RecipeId); // 첫 번째 등록 제작법 ID 검색 테스트
        }

        if (registry.BuildRecipes.Count > 0 && registry.BuildRecipes[0] != null) // 첫 번째 건축법 데이터 존재 여부 확인
        {
            TestBuildRecipeId(registry.BuildRecipes[0].RecipeId); // 첫 번째 등록 건축법 ID 검색 테스트
        }

        if (registry.Enemies.Count > 0 && registry.Enemies[0] != null) // 첫 번째 적 데이터 존재 여부 확인
        {
            TestEnemyId(registry.Enemies[0].EnemyId); // 첫 번째 등록 적 ID 검색 테스트
        }

        if (registry.VisualProfiles.Count > 0 && registry.VisualProfiles[0] != null) // 첫 번째 Visual Profile 존재 여부 확인
        {
            TestVisualProfileId(registry.VisualProfiles[0].ProfileId); // 첫 번째 Visual Profile ID 검색 테스트
        }
    }

    public bool TryGetItem(string itemId, out ItemData itemData) // 전역 Registry에서 아이템 ID 검색
    {
        itemData = null; // 검색 실패 기본 반환값 설정

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            return false; // Registry가 없으면 아이템 검색 실패 반환
        }

        return registry.TryGetItem(itemId, out itemData); // GameDataRegistry 아이템 검색 결과 반환
    }

    public bool TryGetCraftingRecipe(string recipeId, out CraftingRecipeData recipeData) // 전역 Registry에서 제작법 ID 검색
    {
        recipeData = null; // 검색 실패 기본 반환값 설정

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            return false; // Registry가 없으면 제작법 검색 실패 반환
        }

        return registry.TryGetCraftingRecipe(recipeId, out recipeData); // GameDataRegistry 제작법 검색 결과 반환
    }

    public bool TryGetBuildRecipe(string recipeId, out BuildRecipeData recipeData) // 전역 Registry에서 건축법 ID 검색
    {
        recipeData = null; // 검색 실패 기본 반환값 설정

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            return false; // Registry가 없으면 건축법 검색 실패 반환
        }

        return registry.TryGetBuildRecipe(recipeId, out recipeData); // GameDataRegistry 건축법 검색 결과 반환
    }

    public bool TryGetEnemy(string enemyId, out EnemyCombatData enemyData) // 전역 Registry에서 적 ID 검색
    {
        enemyData = null; // 검색 실패 기본 반환값 설정

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            return false; // Registry가 없으면 적 검색 실패 반환
        }

        return registry.TryGetEnemy(enemyId, out enemyData); // GameDataRegistry 적 검색 결과 반환
    }

    public bool TryGetVisualProfile(string profileId, out ContentVisualProfile visualProfile) // 전역 Registry에서 Visual Profile ID 검색
    {
        visualProfile = null; // 검색 실패 기본 반환값 설정

        if (registry == null) // Registry Asset 연결 여부 확인
        {
            return false; // Registry가 없으면 Visual Profile 검색 실패 반환
        }

        return registry.TryGetVisualProfile(profileId, out visualProfile); // GameDataRegistry Visual Profile 검색 결과 반환
    }

    private void TestItemId(string itemId) // 지정 아이템 ID 검색 결과 출력
    {
        if (TryGetItem(itemId, out ItemData itemData)) // 아이템 ID 검색 성공 여부 확인
        {
            Debug.Log($"아이템 Registry 검색 성공 / ID: {itemId} / Asset: {itemData.name}", itemData); // 아이템 검색 성공 결과 출력
            return; // 아이템 검색 실패 로그 생략
        }

        Debug.LogWarning($"아이템 Registry 검색 실패 / ID: {itemId}", this); // 아이템 검색 실패 결과 출력
    }

    private void TestCraftingRecipeId(string recipeId) // 지정 제작법 ID 검색 결과 출력
    {
        if (TryGetCraftingRecipe(recipeId, out CraftingRecipeData recipeData)) // 제작법 ID 검색 성공 여부 확인
        {
            Debug.Log($"제작법 Registry 검색 성공 / ID: {recipeId} / Asset: {recipeData.name}", recipeData); // 제작법 검색 성공 결과 출력
            return; // 제작법 검색 실패 로그 생략
        }

        Debug.LogWarning($"제작법 Registry 검색 실패 / ID: {recipeId}", this); // 제작법 검색 실패 결과 출력
    }

    private void TestBuildRecipeId(string recipeId) // 지정 건축법 ID 검색 결과 출력
    {
        if (TryGetBuildRecipe(recipeId, out BuildRecipeData recipeData)) // 건축법 ID 검색 성공 여부 확인
        {
            Debug.Log($"건축법 Registry 검색 성공 / ID: {recipeId} / Asset: {recipeData.name}", recipeData); // 건축법 검색 성공 결과 출력
            return; // 건축법 검색 실패 로그 생략
        }

        Debug.LogWarning($"건축법 Registry 검색 실패 / ID: {recipeId}", this); // 건축법 검색 실패 결과 출력
    }

    private void TestEnemyId(string enemyId) // 지정 적 ID 검색 결과 출력
    {
        if (TryGetEnemy(enemyId, out EnemyCombatData enemyData)) // 적 ID 검색 성공 여부 확인
        {
            Debug.Log($"적 Registry 검색 성공 / ID: {enemyId} / Asset: {enemyData.name}", enemyData); // 적 검색 성공 결과 출력
            return; // 적 검색 실패 로그 생략
        }

        Debug.LogWarning($"적 Registry 검색 실패 / ID: {enemyId}", this); // 적 검색 실패 결과 출력
    }

    private void TestVisualProfileId(string profileId) // 지정 Visual Profile ID 검색 결과 출력
    {
        if (TryGetVisualProfile(profileId, out ContentVisualProfile visualProfile)) // Visual Profile ID 검색 성공 여부 확인
        {
            Debug.Log($"Visual Profile Registry 검색 성공 / ID: {profileId} / Asset: {visualProfile.name}", visualProfile); // Visual Profile 검색 성공 결과 출력
            return; // Visual Profile 검색 실패 로그 생략
        }

        Debug.LogWarning($"Visual Profile Registry 검색 실패 / ID: {profileId}", this); // Visual Profile 검색 실패 결과 출력
    }
}
