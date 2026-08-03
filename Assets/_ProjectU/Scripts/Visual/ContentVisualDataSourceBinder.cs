using UnityEngine; // Unity 기본 기능

public enum ContentVisualDataSourceType // Visual Identity를 제공할 실제 게임 데이터 종류
{
    Auto = 0, // 현재 Root의 구성 요소와 데이터 참조를 자동 탐색
    ItemPickup = 1, // WorldItemPickup의 ItemData 사용
    Enemy = 2, // EnemyHealth의 EnemyCombatData 사용
    BuildObject = 3 // PlacedBuildObject 또는 BuildRecipeData Override 사용
}

[DefaultExecutionOrder(-600)] // ContentVisualProfileBinder보다 먼저 Identity를 동기화
[DisallowMultipleComponent] // 같은 Root의 Data Source Binder 중복 추가 방지
[RequireComponent(typeof(ContentVisualIdentity))] // 콘텐츠 Identity 구성 보장
[RequireComponent(typeof(ContentVisualProfileBinder))] // Visual Profile Binder 구성 보장
public sealed class ContentVisualDataSourceBinder : MonoBehaviour // 실제 게임 데이터에서 콘텐츠 ID를 읽어 Visual Identity를 자동 동기화
{
    [Header("Visual References")] // Visual 연결 구성 요소 묶음
    [Tooltip("실제 데이터에서 읽은 콘텐츠 ID를 저장할 ContentVisualIdentity입니다.")] // Inspector Identity 참조 설명
    [SerializeField] private ContentVisualIdentity visualIdentity; // 동기화 대상 콘텐츠 Identity

    [Tooltip("동기화된 Identity를 이용해 Visual Profile을 적용할 Binder입니다.")] // Inspector Profile Binder 참조 설명
    [SerializeField] private ContentVisualProfileBinder visualProfileBinder; // Profile 적용 대상 Binder

    [Header("Data Source")] // 실제 게임 데이터 원본 설정 묶음
    [Tooltip("콘텐츠 ID를 읽을 게임 데이터 종류입니다. Auto는 현재 Root의 유효한 데이터 원본 하나를 자동 선택합니다.")] // Inspector 데이터 원본 종류 설명
    [SerializeField] private ContentVisualDataSourceType sourceType = ContentVisualDataSourceType.Auto; // 현재 데이터 원본 종류

    [Tooltip("아이템 획득 기능에서 ItemData를 읽을 WorldItemPickup입니다. 비우면 같은 Root에서 자동 검색합니다.")] // Inspector 아이템 원본 설명
    [SerializeField] private WorldItemPickup worldItemPickup; // 아이템 데이터 제공 컴포넌트

    [Tooltip("적 전투 데이터 제공에 사용할 EnemyHealth입니다. 비우면 같은 Root에서 자동 검색합니다.")] // Inspector 적 원본 설명
    [SerializeField] private EnemyHealth enemyHealth; // 적 데이터 제공 컴포넌트

    [Tooltip("설치된 건축 데이터 제공에 사용할 PlacedBuildObject입니다. 비우면 같은 Root에서 자동 검색합니다.")] // Inspector 건축 원본 설명
    [SerializeField] private PlacedBuildObject placedBuildObject; // 건축 데이터 제공 컴포넌트

    [Header("Optional Data Overrides")] // Prefab Mode와 초기 생성 전 사용할 데이터 예외 참조 묶음
    [Tooltip("WorldItemPickup이 없거나 아직 초기화되지 않았을 때 사용할 ItemData입니다.")] // Inspector 아이템 예외 데이터 설명
    [SerializeField] private ItemData itemDataOverride; // 아이템 데이터 예외 참조

    [Tooltip("EnemyHealth가 없거나 아직 초기화되지 않았을 때 사용할 EnemyCombatData입니다.")] // Inspector 적 예외 데이터 설명
    [SerializeField] private EnemyCombatData enemyCombatDataOverride; // 적 데이터 예외 참조

    [Tooltip("PlacedBuildObject가 실행 중 추가되기 전 Prefab Mode에서 사용할 BuildRecipeData입니다.")] // Inspector 건축 예외 데이터 설명
    [SerializeField] private BuildRecipeData buildRecipeDataOverride; // 건축 데이터 예외 참조

    [Header("Synchronization")] // Identity 자동 동기화 규칙 묶음
    [Tooltip("Awake에서 직렬화된 데이터 참조를 이용해 Binder보다 먼저 Identity를 갱신합니다.")] // Inspector Awake 동기화 설명
    [SerializeField] private bool synchronizeOnAwake = true; // Awake Identity 동기화 여부

    [Tooltip("Start에서 생성 직후 Initialize 또는 RestoreFromSave로 바뀐 데이터를 다시 확인합니다.")] // Inspector Start 동기화 설명
    [SerializeField] private bool synchronizeOnStart = true; // Start Identity 동기화 여부

    [Tooltip("실행 중 ItemData 또는 BuildRecipeData가 바뀌면 일정 간격으로 Identity와 외형을 다시 적용합니다.")] // Inspector 실행 중 변경 감시 설명
    [SerializeField] private bool monitorRuntimeSourceChanges = true; // 실행 중 데이터 변경 감시 여부

    [Tooltip("실행 중 데이터 변경을 확인할 시간 간격입니다.")] // Inspector 변경 감시 간격 설명
    [SerializeField, Min(0.05f)] private float runtimeCheckInterval = 0.25f; // 실행 중 데이터 변경 검사 간격

    [Tooltip("Start 또는 실행 중 데이터 변경을 발견하면 계산된 Visual Profile을 즉시 적용합니다.")] // Inspector 변경 시 외형 적용 설명
    [SerializeField] private bool applyVisualWhenSourceChanges = true; // 데이터 변경 시 Visual Profile 적용 여부

    [Tooltip("동기화 성공과 변경 결과를 Console에 출력합니다.")] // Inspector 동기화 로그 설명
    [SerializeField] private bool logSynchronizationResults; // 동기화 결과 로그 출력 여부

    [Header("Runtime")] // 실행 상태 확인 묶음
    [Tooltip("마지막으로 사용한 실제 데이터 원본 종류입니다.")] // Inspector 최종 원본 종류 설명
    [SerializeField] private ContentVisualDataSourceType resolvedSourceType = ContentVisualDataSourceType.Auto; // 마지막 동기화 데이터 원본 종류

    [Tooltip("마지막으로 읽은 실제 콘텐츠 ID입니다.")] // Inspector 최종 콘텐츠 ID 설명
    [SerializeField] private string resolvedContentId = string.Empty; // 마지막 동기화 콘텐츠 ID

    [Tooltip("마지막으로 계산한 Visual Profile ID입니다.")] // Inspector 최종 Profile ID 설명
    [SerializeField] private string resolvedVisualProfileId = string.Empty; // 마지막 계산 Visual Profile ID

    [Tooltip("마지막으로 사용한 ScriptableObject Asset 이름입니다.")] // Inspector 최종 데이터 Asset 이름 설명
    [SerializeField] private string resolvedDataAssetName = string.Empty; // 마지막 데이터 Asset 이름

    [Tooltip("마지막 Identity 및 Visual 동기화 성공 여부입니다.")] // Inspector 최종 동기화 결과 설명
    [SerializeField] private bool lastSynchronizationSucceeded; // 마지막 동기화 성공 여부

    private float nextRuntimeCheckTime; // 다음 실행 중 데이터 변경 검사 시각

    public ContentVisualIdentity VisualIdentity => visualIdentity; // 연결된 ContentVisualIdentity 제공
    public ContentVisualProfileBinder VisualProfileBinder => visualProfileBinder; // 연결된 ContentVisualProfileBinder 제공
    public ContentVisualDataSourceType SourceType => sourceType; // 설정된 데이터 원본 종류 제공
    public ContentVisualDataSourceType ResolvedSourceType => resolvedSourceType; // 마지막으로 사용한 데이터 원본 종류 제공
    public string ResolvedContentId => resolvedContentId; // 마지막으로 읽은 콘텐츠 ID 제공
    public string ResolvedVisualProfileId => resolvedVisualProfileId; // 마지막으로 계산한 Profile ID 제공
    public string ResolvedDataAssetName => resolvedDataAssetName; // 마지막 데이터 Asset 이름 제공
    public bool LastSynchronizationSucceeded => lastSynchronizationSucceeded; // 마지막 동기화 성공 여부 제공

    private void Reset() // 컴포넌트 최초 추가 시 같은 Root의 참조와 현재 데이터 연결 시도
    {
        ResolveComponentReferences(); // 같은 Root의 Visual 및 게임 데이터 구성 요소 검색
        SynchronizeFromDataSource(false, false); // Console 출력과 외형 적용 없이 Identity 미리보기 갱신
    }

    private void OnValidate() // Inspector 변경값 보정과 Edit Mode Identity 미리보기 갱신
    {
        runtimeCheckInterval = Mathf.Max(0.05f, runtimeCheckInterval); // 실행 중 검사 간격 최소값 적용
        ResolveComponentReferences(); // 같은 Root의 Visual 및 게임 데이터 구성 요소 검색
        SynchronizeFromDataSource(false, false); // Inspector 변경 내용을 Identity 계산 결과에 반영
    }

    private void Awake() // Binder Awake보다 먼저 직렬화된 게임 데이터에서 Identity 동기화
    {
        ResolveComponentReferences(); // 같은 Root의 Visual 및 게임 데이터 구성 요소 검색

        if (synchronizeOnAwake) // Awake 동기화 사용 여부 확인
        {
            SynchronizeFromDataSource(false, false); // Binder가 뒤에서 적용할 수 있도록 Identity만 먼저 동기화
        }
    }

    private void Start() // 생성 직후 초기화된 실제 데이터와 Visual Profile을 최종 동기화
    {
        if (synchronizeOnStart) // Start 동기화 사용 여부 확인
        {
            SynchronizeFromDataSource(applyVisualWhenSourceChanges, true); // 실제 데이터와 Identity 및 선택적 외형 적용
        }

        nextRuntimeCheckTime = Time.unscaledTime + runtimeCheckInterval; // 다음 데이터 변경 검사 시각 설정
    }

    private void Update() // 실행 중 실제 데이터 참조 또는 콘텐츠 ID 변경 감시
    {
        if (!monitorRuntimeSourceChanges) // 실행 중 변경 감시 사용 여부 확인
        {
            return; // 데이터 변경 감시 처리 종료
        }

        if (Time.unscaledTime < nextRuntimeCheckTime) // 다음 검사 시각 도달 여부 확인
        {
            return; // 검사 간격이 남아 있으면 처리 종료
        }

        nextRuntimeCheckTime = Time.unscaledTime + runtimeCheckInterval; // 다음 검사 시각 갱신

        if (!TryResolveDataSource( // 현재 실제 데이터 원본 정보 확인 시작
            out ContentVisualDataSourceType currentSourceType, // 현재 원본 종류 반환
            out ContentVisualIdentityCategory currentCategory, // 현재 Identity 분류 반환
            out string currentContentId, // 현재 콘텐츠 ID 반환
            out Object currentDataAsset, // 현재 데이터 Asset 반환
            out _)) // 자동 검사에서는 반복 오류 Log를 막기 위해 오류 내용 생략
        {
            return; // 아직 초기화되지 않은 데이터 원본이면 다음 검사까지 대기
        }

        string currentAssetName = currentDataAsset == null // 현재 데이터 Asset 존재 여부 확인
            ? string.Empty // Asset이 없으면 빈 이름 사용
            : currentDataAsset.name; // Asset이 있으면 Asset 이름 사용

        bool hasSourceChanged = currentSourceType != resolvedSourceType // 데이터 원본 종류 변경 여부 확인
            || currentContentId != resolvedContentId // 콘텐츠 ID 변경 여부 확인
            || currentAssetName != resolvedDataAssetName // 데이터 Asset 변경 여부 확인
            || visualIdentity == null // Identity 참조 누락 여부 확인
            || visualIdentity.Category != currentCategory; // Identity 분류 불일치 여부 확인

        if (!hasSourceChanged) // 실제 데이터 변경 여부 확인
        {
            return; // 변경이 없으면 외형 재적용 생략
        }

        SynchronizeFromDataSource(applyVisualWhenSourceChanges, logSynchronizationResults); // 변경된 데이터로 Identity와 외형 재동기화
    }

    [ContextMenu("Refresh Identity From Data Source")] // Inspector 실제 데이터 기반 Identity 갱신 메뉴
    public bool RefreshIdentityFromDataSource() // 현재 게임 데이터에서 Identity만 다시 계산
    {
        return SynchronizeFromDataSource(false, true); // 결과 Log를 출력하며 Identity 동기화
    }

    [ContextMenu("Refresh Identity And Apply Visual")] // Inspector 실제 데이터 기반 Identity와 Visual 적용 메뉴
    public bool RefreshIdentityAndApplyVisual() // 현재 게임 데이터에서 Identity를 계산하고 Profile 적용
    {
        return SynchronizeFromDataSource(true, true); // 결과 Log를 출력하며 Identity와 외형 동기화
    }

    [ContextMenu("Validate Data Source Visual Link")] // Inspector 게임 데이터와 Identity 및 Profile 연결 검증 메뉴
    public bool ValidateDataSourceVisualLink() // 현재 실제 데이터부터 Visual Profile까지 전체 연결 검사
    {
        ResolveComponentReferences(); // 같은 Root의 필수 구성 요소 참조 보장

        if (!TryResolveDataSource( // 현재 실제 데이터 원본 정보 검사 시작
            out ContentVisualDataSourceType currentSourceType, // 현재 원본 종류 반환
            out ContentVisualIdentityCategory currentCategory, // 현재 Identity 분류 반환
            out string currentContentId, // 현재 콘텐츠 ID 반환
            out Object currentDataAsset, // 현재 데이터 Asset 반환
            out string errorMessage)) // 검사 실패 원인 반환
        {
            Debug.LogError($"{name} Data Source Visual 연결 오류 / {errorMessage}", this); // 실제 데이터 원본 오류 출력
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (visualIdentity == null) // ContentVisualIdentity 존재 여부 확인
        {
            Debug.LogError($"{name}에 ContentVisualIdentity가 없습니다.", this); // Identity 누락 오류 출력
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (visualProfileBinder == null) // ContentVisualProfileBinder 존재 여부 확인
        {
            Debug.LogError($"{name}에 ContentVisualProfileBinder가 없습니다.", this); // Binder 누락 오류 출력
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (!visualIdentity.TryGetVisualProfileId(out string expectedProfileId, out string identityError)) // 현재 Identity Profile ID 계산 성공 여부 확인
        {
            Debug.LogError($"{name} ContentVisualIdentity 오류 / {identityError}", visualIdentity); // Identity 계산 오류 출력
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (visualIdentity.Category != currentCategory // Identity 분류와 실제 데이터 분류 일치 여부 확인
            || visualIdentity.ContentId != currentContentId) // Identity ID와 실제 데이터 ID 일치 여부 확인
        {
            Debug.LogError( // 실제 데이터와 Identity 불일치 오류 출력 시작
                $"{name}의 실제 데이터와 ContentVisualIdentity가 다릅니다. " // 오류 원인 안내
                + $"실제 분류: {currentCategory} / Identity 분류: {visualIdentity.Category} / " // 분류 비교 결과 추가
                + $"실제 ID: {currentContentId} / Identity ID: {visualIdentity.ContentId}", // ID 비교 결과 추가
                this); // 현재 Data Source Binder를 Log Context로 지정
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (visualProfileBinder.VisualProfile == null) // 캐시된 Visual Profile 존재 여부 확인
        {
            Debug.LogError($"{name} Binder에 캐시된 ContentVisualProfile이 없습니다.", visualProfileBinder); // Profile 캐시 누락 오류 출력
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        if (visualProfileBinder.VisualProfile.ProfileId != expectedProfileId) // 캐시 Profile ID와 계산 ID 일치 여부 확인
        {
            Debug.LogError( // 캐시 Profile 불일치 오류 출력 시작
                $"{name}의 계산 Profile ID와 캐시 Profile ID가 다릅니다. " // 오류 원인 안내
                + $"계산 ID: {expectedProfileId} / " // 계산된 Profile ID 추가
                + $"캐시 ID: {visualProfileBinder.VisualProfile.ProfileId}", // 캐시된 Profile ID 추가
                visualProfileBinder); // Binder를 Log Context로 지정
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장
            return false; // 전체 연결 검증 실패 반환
        }

        bool isBinderValid = visualProfileBinder.ValidateAssignedProfile(); // Binder와 Visual Root 전체 검증
        lastSynchronizationSucceeded = isBinderValid; // 마지막 동기화 결과 저장

        if (isBinderValid) // 전체 연결 정상 여부 확인
        {
            Debug.Log( // 실제 데이터 Visual 연결 검증 성공 결과 출력 시작
                $"{name} Data Source Visual 연결 검증 완료 / " // 현재 Root와 검증 완료 안내
                + $"Source: {currentSourceType} / " // 실제 데이터 원본 종류 추가
                + $"Data: {(currentDataAsset == null ? "NONE" : currentDataAsset.name)} / " // 데이터 Asset 이름 추가
                + $"Content ID: {currentContentId} / " // 실제 콘텐츠 ID 추가
                + $"Profile ID: {expectedProfileId}", // 최종 Profile ID 추가
                this); // 현재 Data Source Binder를 Log Context로 지정
        }

        return isBinderValid; // 전체 연결 검증 결과 반환
    }

    public bool SynchronizeFromDataSource(bool applyVisual, bool logResult) // 실제 게임 데이터에서 Identity를 갱신하고 선택적으로 외형 적용
    {
        ResolveComponentReferences(); // 같은 Root의 필수 구성 요소 참조 보장

        if (!TryResolveDataSource( // 현재 실제 데이터 원본 확인 시작
            out ContentVisualDataSourceType currentSourceType, // 현재 원본 종류 반환
            out ContentVisualIdentityCategory currentCategory, // 현재 Identity 분류 반환
            out string currentContentId, // 현재 콘텐츠 ID 반환
            out Object currentDataAsset, // 현재 데이터 Asset 반환
            out string errorMessage)) // 원본 확인 실패 원인 반환
        {
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장

            if (logResult) // 오류 Log 출력 여부 확인
            {
                Debug.LogError($"{name} Data Source Visual 동기화 실패 / {errorMessage}", this); // 데이터 원본 오류 출력
            }

            return false; // 실제 데이터 동기화 실패 반환
        }

        if (visualIdentity == null || visualProfileBinder == null) // Visual 필수 구성 요소 존재 여부 확인
        {
            lastSynchronizationSucceeded = false; // 마지막 동기화 실패 상태 저장

            if (logResult) // 오류 Log 출력 여부 확인
            {
                Debug.LogError($"{name}에 ContentVisualIdentity와 ContentVisualProfileBinder가 필요합니다.", this); // 필수 구성 요소 누락 오류 출력
            }

            return false; // 실제 데이터 동기화 실패 반환
        }

        string previousContentId = visualIdentity.ContentId; // 동기화 전 Identity 콘텐츠 ID 저장
        ContentVisualIdentityCategory previousCategory = visualIdentity.Category; // 동기화 전 Identity 분류 저장
        visualIdentity.Configure(currentCategory, currentContentId); // 실제 데이터의 분류와 ID를 Identity에 적용
        visualProfileBinder.SetContentIdentityMode(true); // Binder를 Identity 기반 자동 연결 방식으로 변경

        bool hasIdentityChanged = previousContentId != visualIdentity.ContentId // Identity 콘텐츠 ID 변경 여부 확인
            || previousCategory != visualIdentity.Category; // Identity 분류 변경 여부 확인
        bool applySucceeded = true; // 외형 적용 결과 기본값 설정
        bool requiresVisualApply = hasIdentityChanged // Identity ID 또는 분류 변경 여부 확인
            || visualProfileBinder.VisualProfile == null // 캐시 Profile 누락 여부 확인
            || visualProfileBinder.VisualProfile.ProfileId != visualIdentity.ResolvedVisualProfileId // 캐시 Profile ID 불일치 여부 확인
            || visualProfileBinder.AppliedProfileId != visualIdentity.ResolvedVisualProfileId // 마지막 적용 Profile ID 불일치 여부 확인
            || !visualProfileBinder.LastApplySucceeded; // 이전 Profile 적용 실패 여부 확인

        if (applyVisual && requiresVisualApply) // 외형 재적용이 실제로 필요한지 확인
        {
            applySucceeded = visualProfileBinder.ApplyAssignedProfile(); // 캐시 또는 Runtime Registry의 Profile 적용
        }

        resolvedSourceType = currentSourceType; // 마지막 실제 데이터 원본 종류 저장
        resolvedContentId = currentContentId; // 마지막 실제 콘텐츠 ID 저장
        resolvedVisualProfileId = visualIdentity.ResolvedVisualProfileId; // 마지막 계산 Visual Profile ID 저장
        resolvedDataAssetName = currentDataAsset == null // 데이터 Asset 존재 여부 확인
            ? string.Empty // 데이터 Asset이 없으면 빈 이름 저장
            : currentDataAsset.name; // 데이터 Asset이 있으면 Asset 이름 저장
        lastSynchronizationSucceeded = visualIdentity.IsIdentityValid && applySucceeded; // Identity와 선택적 외형 적용 전체 결과 저장

        if (logResult && lastSynchronizationSucceeded) // 정상 동기화 Log 출력 여부 확인
        {
            string changeLabel = hasIdentityChanged // Identity 실제 변경 여부 확인
                ? "변경" // ID 또는 분류가 바뀌었으면 변경 표시
                : "확인"; // 기존 값과 같으면 확인 표시
            Debug.Log( // 실제 데이터 Visual 동기화 성공 결과 출력 시작
                $"{name} Data Source Visual 동기화 {changeLabel} 완료 / " // 현재 Root와 결과 안내
                + $"Source: {currentSourceType} / " // 실제 데이터 원본 종류 추가
                + $"Data: {resolvedDataAssetName} / " // 데이터 Asset 이름 추가
                + $"Content ID: {resolvedContentId} / " // 콘텐츠 ID 추가
                + $"Profile ID: {resolvedVisualProfileId}", // 계산 Profile ID 추가
                this); // 현재 Data Source Binder를 Log Context로 지정
        }

        return lastSynchronizationSucceeded; // 실제 데이터 동기화 전체 결과 반환
    }

    private bool TryResolveDataSource( // 설정된 원본 종류에 따라 실제 데이터와 Identity 정보 확인
        out ContentVisualDataSourceType currentSourceType, // 실제 사용한 데이터 원본 종류 반환
        out ContentVisualIdentityCategory currentCategory, // 실제 데이터에 맞는 Identity 분류 반환
        out string currentContentId, // 실제 콘텐츠 ID 반환
        out Object currentDataAsset, // 실제 ScriptableObject 데이터 반환
        out string errorMessage) // 원본 확인 실패 원인 반환
    {
        ResolveComponentReferences(); // 같은 Root의 게임 데이터 구성 요소 최신 상태 검색
        currentSourceType = ContentVisualDataSourceType.Auto; // 원본 종류 기본값 초기화
        currentCategory = ContentVisualIdentityCategory.Other; // Identity 분류 기본값 초기화
        currentContentId = string.Empty; // 콘텐츠 ID 기본값 초기화
        currentDataAsset = null; // 데이터 Asset 기본값 초기화
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (sourceType == ContentVisualDataSourceType.ItemPickup) // 아이템 데이터 원본 명시 설정 확인
        {
            return TryResolveItemSource( // 아이템 데이터 Identity 확인
                out currentSourceType, // 아이템 원본 종류 반환
                out currentCategory, // 아이템 Identity 분류 반환
                out currentContentId, // 아이템 ID 반환
                out currentDataAsset, // ItemData Asset 반환
                out errorMessage); // 아이템 원본 오류 반환
        }

        if (sourceType == ContentVisualDataSourceType.Enemy) // 적 데이터 원본 명시 설정 확인
        {
            return TryResolveEnemySource( // 적 데이터 Identity 확인
                out currentSourceType, // 적 원본 종류 반환
                out currentCategory, // 적 Identity 분류 반환
                out currentContentId, // 적 ID 반환
                out currentDataAsset, // EnemyCombatData Asset 반환
                out errorMessage); // 적 원본 오류 반환
        }

        if (sourceType == ContentVisualDataSourceType.BuildObject) // 건축 데이터 원본 명시 설정 확인
        {
            return TryResolveBuildSource( // 건축 데이터 Identity 확인
                out currentSourceType, // 건축 원본 종류 반환
                out currentCategory, // 건축 Identity 분류 반환
                out currentContentId, // 건축 ID 반환
                out currentDataAsset, // BuildRecipeData Asset 반환
                out errorMessage); // 건축 원본 오류 반환
        }

        int availableSourceCount = 0; // 현재 Root에서 유효한 실제 데이터 원본 수
        ItemData availableItemData = GetCurrentItemData(); // 현재 사용할 수 있는 ItemData 확인
        EnemyCombatData availableEnemyData = GetCurrentEnemyData(); // 현재 사용할 수 있는 EnemyCombatData 확인
        BuildRecipeData availableBuildData = GetCurrentBuildData(); // 현재 사용할 수 있는 BuildRecipeData 확인

        if (availableItemData != null) // 유효한 아이템 데이터 존재 여부 확인
        {
            availableSourceCount++; // 유효한 데이터 원본 수 증가
        }

        if (availableEnemyData != null) // 유효한 적 데이터 존재 여부 확인
        {
            availableSourceCount++; // 유효한 데이터 원본 수 증가
        }

        if (availableBuildData != null) // 유효한 건축 데이터 존재 여부 확인
        {
            availableSourceCount++; // 유효한 데이터 원본 수 증가
        }

        if (availableSourceCount <= 0) // 유효한 데이터 원본 존재 여부 확인
        {
            errorMessage = "ItemData, EnemyCombatData 또는 BuildRecipeData 원본을 찾지 못했습니다."; // 데이터 원본 누락 오류 설정
            return false; // 자동 데이터 원본 확인 실패 반환
        }

        if (availableSourceCount > 1) // 유효한 데이터 원본이 여러 개인지 확인
        {
            errorMessage = "Auto 모드에서 여러 데이터 원본이 발견되었습니다. Data Source를 명시적으로 선택하세요."; // 데이터 원본 모호성 오류 설정
            return false; // 자동 데이터 원본 확인 실패 반환
        }

        if (availableItemData != null) // 자동 선택할 아이템 데이터 존재 여부 확인
        {
            return TryResolveItemSource( // 아이템 데이터 Identity 확인
                out currentSourceType, // 아이템 원본 종류 반환
                out currentCategory, // 아이템 Identity 분류 반환
                out currentContentId, // 아이템 ID 반환
                out currentDataAsset, // ItemData Asset 반환
                out errorMessage); // 아이템 원본 오류 반환
        }

        if (availableEnemyData != null) // 자동 선택할 적 데이터 존재 여부 확인
        {
            return TryResolveEnemySource( // 적 데이터 Identity 확인
                out currentSourceType, // 적 원본 종류 반환
                out currentCategory, // 적 Identity 분류 반환
                out currentContentId, // 적 ID 반환
                out currentDataAsset, // EnemyCombatData Asset 반환
                out errorMessage); // 적 원본 오류 반환
        }

        return TryResolveBuildSource( // 남은 유효한 건축 데이터 Identity 확인
            out currentSourceType, // 건축 원본 종류 반환
            out currentCategory, // 건축 Identity 분류 반환
            out currentContentId, // 건축 ID 반환
            out currentDataAsset, // BuildRecipeData Asset 반환
            out errorMessage); // 건축 원본 오류 반환
    }

    private bool TryResolveItemSource( // WorldItemPickup 또는 Override의 ItemData에서 Identity 정보 확인
        out ContentVisualDataSourceType currentSourceType, // 실제 원본 종류 반환
        out ContentVisualIdentityCategory currentCategory, // 아이템 Identity 분류 반환
        out string currentContentId, // 아이템 ID 반환
        out Object currentDataAsset, // ItemData Asset 반환
        out string errorMessage) // 아이템 원본 오류 반환
    {
        currentSourceType = ContentVisualDataSourceType.ItemPickup; // 실제 원본 종류를 아이템으로 설정
        ItemData itemData = GetCurrentItemData(); // 현재 사용할 ItemData 확인
        currentCategory = ContentVisualIdentityCategory.Item; // 기본 아이템 Identity 분류 설정
        currentContentId = string.Empty; // 아이템 ID 기본값 초기화
        currentDataAsset = itemData; // 현재 ItemData Asset 반환
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (itemData == null) // ItemData 존재 여부 확인
        {
            errorMessage = "WorldItemPickup.ItemData와 Item Data Override가 모두 비어 있습니다."; // ItemData 누락 오류 설정
            return false; // 아이템 원본 확인 실패 반환
        }

        currentCategory = itemData.IsTool || itemData.IsWeapon // 도구 또는 무기 분류 여부 확인
            ? ContentVisualIdentityCategory.Weapon // 도구와 무기는 Weapon Profile 규칙 사용
            : ContentVisualIdentityCategory.Item; // 나머지 아이템은 Item Profile 규칙 사용
        currentContentId = NormalizeId(itemData.ItemId); // ItemData의 정리된 ID 적용

        if (!GameDataRegistry.IsValidContentId(currentContentId)) // ItemData ID 공통 규칙 검사
        {
            errorMessage = $"ItemData ID가 올바르지 않습니다. ID: '{currentContentId}'"; // ItemData ID 오류 설정
            return false; // 아이템 원본 확인 실패 반환
        }

        return true; // 아이템 원본 확인 성공 반환
    }

    private bool TryResolveEnemySource( // EnemyHealth 또는 Override의 EnemyCombatData에서 Identity 정보 확인
        out ContentVisualDataSourceType currentSourceType, // 실제 원본 종류 반환
        out ContentVisualIdentityCategory currentCategory, // 적 Identity 분류 반환
        out string currentContentId, // 적 ID 반환
        out Object currentDataAsset, // EnemyCombatData Asset 반환
        out string errorMessage) // 적 원본 오류 반환
    {
        currentSourceType = ContentVisualDataSourceType.Enemy; // 실제 원본 종류를 적으로 설정
        currentCategory = ContentVisualIdentityCategory.Enemy; // 적 Identity 분류 설정
        EnemyCombatData enemyData = GetCurrentEnemyData(); // 현재 사용할 EnemyCombatData 확인
        currentContentId = string.Empty; // 적 ID 기본값 초기화
        currentDataAsset = enemyData; // 현재 EnemyCombatData Asset 반환
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (enemyData == null) // EnemyCombatData 존재 여부 확인
        {
            errorMessage = "EnemyHealth.CombatData와 Enemy Combat Data Override가 모두 비어 있습니다."; // EnemyCombatData 누락 오류 설정
            return false; // 적 원본 확인 실패 반환
        }

        currentContentId = NormalizeId(enemyData.EnemyId); // EnemyCombatData의 정리된 ID 적용

        if (!GameDataRegistry.IsValidContentId(currentContentId)) // EnemyCombatData ID 공통 규칙 검사
        {
            errorMessage = $"EnemyCombatData ID가 올바르지 않습니다. ID: '{currentContentId}'"; // EnemyCombatData ID 오류 설정
            return false; // 적 원본 확인 실패 반환
        }

        return true; // 적 원본 확인 성공 반환
    }

    private bool TryResolveBuildSource( // PlacedBuildObject 또는 Override의 BuildRecipeData에서 Identity 정보 확인
        out ContentVisualDataSourceType currentSourceType, // 실제 원본 종류 반환
        out ContentVisualIdentityCategory currentCategory, // 건축 Identity 분류 반환
        out string currentContentId, // 건축 ID 반환
        out Object currentDataAsset, // BuildRecipeData Asset 반환
        out string errorMessage) // 건축 원본 오류 반환
    {
        currentSourceType = ContentVisualDataSourceType.BuildObject; // 실제 원본 종류를 건축물로 설정
        currentCategory = ContentVisualIdentityCategory.Buildable; // 건축물 Identity 분류 설정
        BuildRecipeData buildData = GetCurrentBuildData(); // 현재 사용할 BuildRecipeData 확인
        currentContentId = string.Empty; // 건축 ID 기본값 초기화
        currentDataAsset = buildData; // 현재 BuildRecipeData Asset 반환
        errorMessage = string.Empty; // 오류 메시지 기본값 초기화

        if (buildData == null) // BuildRecipeData 존재 여부 확인
        {
            errorMessage = "PlacedBuildObject.RecipeData와 Build Recipe Data Override가 모두 비어 있습니다."; // BuildRecipeData 누락 오류 설정
            return false; // 건축 원본 확인 실패 반환
        }

        currentContentId = NormalizeId(buildData.RecipeId); // BuildRecipeData의 정리된 ID 적용

        if (!GameDataRegistry.IsValidContentId(currentContentId)) // BuildRecipeData ID 공통 규칙 검사
        {
            errorMessage = $"BuildRecipeData ID가 올바르지 않습니다. ID: '{currentContentId}'"; // BuildRecipeData ID 오류 설정
            return false; // 건축 원본 확인 실패 반환
        }

        return true; // 건축 원본 확인 성공 반환
    }

    private ItemData GetCurrentItemData() // 현재 Root에서 사용할 ItemData 결정
    {
        if (worldItemPickup != null && worldItemPickup.ItemData != null) // WorldItemPickup의 실제 데이터 존재 여부 확인
        {
            return worldItemPickup.ItemData; // 실행 중 실제 ItemData 우선 반환
        }

        return itemDataOverride; // 실제 데이터가 없으면 Prefab용 Override 반환
    }

    private EnemyCombatData GetCurrentEnemyData() // 현재 Root에서 사용할 EnemyCombatData 결정
    {
        if (enemyHealth != null && enemyHealth.CombatData != null) // EnemyHealth의 실제 데이터 존재 여부 확인
        {
            return enemyHealth.CombatData; // 실행 중 실제 EnemyCombatData 우선 반환
        }

        return enemyCombatDataOverride; // 실제 데이터가 없으면 Prefab용 Override 반환
    }

    private BuildRecipeData GetCurrentBuildData() // 현재 Root에서 사용할 BuildRecipeData 결정
    {
        if (placedBuildObject != null && placedBuildObject.RecipeData != null) // PlacedBuildObject의 실제 데이터 존재 여부 확인
        {
            return placedBuildObject.RecipeData; // 실행 중 실제 BuildRecipeData 우선 반환
        }

        return buildRecipeDataOverride; // 실제 데이터가 없으면 Prefab용 Override 반환
    }

    private void ResolveComponentReferences() // 같은 Root의 Visual과 실제 게임 데이터 구성 요소 참조 보장
    {
        if (visualIdentity == null) // ContentVisualIdentity 참조 존재 여부 확인
        {
            visualIdentity = GetComponent<ContentVisualIdentity>(); // 같은 Root에서 ContentVisualIdentity 검색
        }

        if (visualProfileBinder == null) // ContentVisualProfileBinder 참조 존재 여부 확인
        {
            visualProfileBinder = GetComponent<ContentVisualProfileBinder>(); // 같은 Root에서 ContentVisualProfileBinder 검색
        }

        if (worldItemPickup == null) // WorldItemPickup 참조 존재 여부 확인
        {
            worldItemPickup = GetComponent<WorldItemPickup>(); // 같은 Root에서 WorldItemPickup 검색
        }

        if (enemyHealth == null) // EnemyHealth 참조 존재 여부 확인
        {
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 Root에서 EnemyHealth 검색
        }

        if (placedBuildObject == null) // PlacedBuildObject 참조 존재 여부 확인
        {
            placedBuildObject = GetComponent<PlacedBuildObject>(); // 같은 Root에서 PlacedBuildObject 검색
        }
    }

    private static string NormalizeId(string value) // 콘텐츠 ID 양쪽 공백 제거
    {
        return string.IsNullOrWhiteSpace(value) // 입력값 존재 여부 확인
            ? string.Empty // 입력이 없으면 빈 문자열 반환
            : value.Trim(); // 입력이 있으면 양쪽 공백 제거 후 반환
    }
}
