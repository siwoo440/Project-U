using System; // 이벤트 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(PlacedBuildObject))] // 설치 건축물 정보 요구
public sealed class AnimalPen : InteractableBase, IBuildRemovalGuard // 86일차: 닭장·외양간 (동물 목록·먹이·생산물)
{
    [Header("Pen")] // 우리 설정
    [Tooltip("우리 표시 이름.")]
    [SerializeField] private string penDisplayName = "CHICKEN COOP"; // 표시 이름
    [Tooltip("이 우리에서 기를 수 있는 동물.")]
    [SerializeField] private AnimalData acceptedAnimal; // 동물 종류
    [Tooltip("최대 마리 수.")]
    [SerializeField, Range(1, 6)] private int capacity = 3; // 최대 마리 수

    [Header("Area")] // 돌아다니는 영역
    [Tooltip("동물을 만들 부모. 비어 있으면 우리 자신을 사용합니다.")]
    [SerializeField] private Transform animalRoot; // 동물 부모
    [Tooltip("돌아다니는 영역 중심 (우리 기준).")]
    [SerializeField] private Vector3 wanderCenter = new Vector3(0f, 0f, -0.3f); // 영역 중심
    [Tooltip("돌아다니는 영역 크기 (우리 기준 X·Z).")]
    [SerializeField] private Vector2 wanderSize = new Vector2(1.8f, 1.2f); // 영역 크기

    [Header("Visual")] // 외형
    [Tooltip("오늘 먹이를 준 동물이 있으면 켜는 먹이통 사료.")]
    [SerializeField] private GameObject feedVisual; // 사료 외형
    [Tooltip("생산물이 있으면 켜는 외형 (둥지의 달걀·우유 통).")]
    [SerializeField] private GameObject productVisual; // 생산물 외형

    [Header("Runtime")] // 실행 상태
    [Tooltip("우리 안 동물 목록.")]
    [SerializeField] private List<PenAnimal> animals = new List<PenAnimal>(); // 동물 목록
    [Tooltip("마지막으로 하루 처리를 마친 날짜.")]
    [SerializeField] private int lastProcessedDay; // 마지막 처리 날짜
    [Tooltip("날짜 기록 여부.")]
    [SerializeField] private bool hasDayRecord; // 날짜 기록 여부

    private readonly List<FarmAnimal> visuals = new List<FarmAnimal>(); // 동물 외형
    private PlacedBuildObject placedBuildObject; // 설치 건축물 정보
    private GameUIManager gameUIManager; // 팝업 관리자
    private System.Random random; // 추가 생산 판정

    public event Action StateChanged; // 상태 변경 알림

    public string PenDisplayName => penDisplayName; // 이름 제공
    public AnimalData AcceptedAnimal => acceptedAnimal; // 동물 종류 제공
    public int Capacity => Mathf.Max(1, capacity); // 최대 마리 제공
    public IReadOnlyList<PenAnimal> Animals => animals; // 동물 제공
    public IReadOnlyList<FarmAnimal> AnimalVisuals => visuals; // 동물 외형 제공
    public int LastProcessedDay => lastProcessedDay; // 처리 날짜 제공
    public string StructureId => placedBuildObject != null ? placedBuildObject.StructureId : string.Empty; // 저장 ID 제공
    public bool IsFull => animals.Count >= Capacity; // 가득 참 여부
    public int HungryCount => Count(animal => !animal.FedToday); // 오늘 굶은 동물 수
    public int ProductCount { get { int total = 0; foreach (PenAnimal animal in animals) total += animal.ProductReady; return total; } } // 생산물 합계
    public int FeedNeededToday => HungryCount * (acceptedAnimal != null ? acceptedAnimal.FeedPerDay : 1); // 오늘 필요한 먹이
    public bool CanRemove => animals.Count == 0; // 동물이 없을 때만 철거
    public string RemovalBlockedMessage => animals.Count > 0 ? "RELEASE THE ANIMALS FIRST" : string.Empty; // 철거 차단 문구

    public override string PromptMessage // 안내 문구
    {
        get
        {
            if (acceptedAnimal == null) // 설정 확인
            {
                return "PEN DATA ERROR"; // 오류
            }

            string header = $"F - {penDisplayName} ({animals.Count}/{Capacity})"; // 기본 문구
            int products = ProductCount; // 생산물

            if (products > 0) // 생산물 있음
            {
                return $"{header} | {products} {acceptedAnimal.ProductItem.DisplayName} READY"; // 생산물 안내
            }

            int hungry = HungryCount; // 굶은 동물

            if (hungry > 0) // 먹이 필요
            {
                return $"{header} | {hungry} HUNGRY"; // 먹이 안내
            }

            return animals.Count == 0 ? $"{header} | EMPTY" : header; // 기본
        }
    }

    private void Awake() // 참조 준비
    {
        placedBuildObject = GetComponent<PlacedBuildObject>(); // 건축물 정보
        random = new System.Random(GetInstanceID()); // 판정용 난수

        if (animalRoot == null) // 부모 확인
        {
            animalRoot = transform; // 우리 자신
        }

        if (acceptedAnimal == null) // 데이터 확인
        {
            Debug.LogError($"{gameObject.name}의 동물 데이터가 없습니다. Tools > Project U > Build Content > 5. Livestock를 실행하세요.", this); // 오류
        }
    }

    private void OnEnable() // 등록
    {
        LivestockManager.Register(this); // 관리자 등록
        LivestockManager manager = LivestockManager.Instance; // 관리자

        if (!hasDayRecord && manager != null) // 새로 지은 우리
        {
            lastProcessedDay = manager.CurrentDay; // 오늘부터 시작
            hasDayRecord = true; // 기록
        }

        RebuildVisuals(); // 외형
    }

    private void OnDisable() // 해제
    {
        LivestockManager.Unregister(this); // 관리자 해제
    }

    public override void Interact(GameObject interactor) // 상호작용
    {
        if (acceptedAnimal == null || interactor == null) // 설정 확인
        {
            return; // 중단
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>(); // 인벤토리

        if (gameUIManager == null) // 관리자 확인
        {
            gameUIManager = FindFirstObjectByType<GameUIManager>(); // Scene 검색
        }

        if (gameUIManager != null && gameUIManager.OpenAnimalPen(this)) // 우리 창 열기
        {
            return; // 완료
        }

        // 우리 창이 없는 Scene : 생산물 꺼내기 → 먹이 주기 순서로 바로 처리
        if (inventory != null && CollectAll(inventory) == 0) // 꺼낼 것이 없으면
        {
            FeedAll(inventory); // 먹이 주기
        }
    }

    // ------------------------------------------------------------ 동물

    public bool TryAttract(PlayerInventory inventory, out string message) // 먹이로 새 동물 불러오기
    {
        if (acceptedAnimal == null || inventory == null) // 요청 확인
        {
            message = "PEN DATA ERROR"; // 문구
            return false; // 실패
        }

        if (IsFull) // 가득 참
        {
            message = $"{penDisplayName} IS FULL"; // 문구
            return false; // 실패
        }

        int cost = acceptedAnimal.AttractFeedCost; // 비용

        if (!inventory.HasItem(acceptedAnimal.FeedItem, cost)) // 먹이 확인
        {
            message = $"NEED {cost} {acceptedAnimal.FeedItem.DisplayName}"; // 문구
            return false; // 실패
        }

        if (inventory.RemoveItem(acceptedAnimal.FeedItem, cost) != cost) // 먹이 사용
        {
            message = "COULD NOT USE FEED"; // 문구
            return false; // 실패
        }

        PenAnimal animal = new PenAnimal(acceptedAnimal, NextNameNumber()); // 새 동물
        animals.Add(animal); // 추가
        RebuildVisuals(); // 외형
        NotifyChanged(); // 알림
        message = $"A {acceptedAnimal.DisplayName} MOVED IN!"; // 문구
        SpawnText($"+1 {acceptedAnimal.DisplayName}", new Color(1f, 0.86f, 0.35f, 1f)); // 월드 알림
        return true; // 성공
    }

    public bool Release(int index) // 동물 내보내기
    {
        if (index < 0 || index >= animals.Count) // 범위 확인
        {
            return false; // 실패
        }

        animals.RemoveAt(index); // 제거
        RebuildVisuals(); // 외형
        NotifyChanged(); // 알림
        return true; // 성공
    }

    public bool TryFeed(int index, PlayerInventory inventory, out string message) // 한 마리 먹이 주기
    {
        if (index < 0 || index >= animals.Count || inventory == null) // 요청 확인
        {
            message = string.Empty; // 문구
            return false; // 실패
        }

        PenAnimal animal = animals[index]; // 동물

        if (animal.FedToday) // 이미 먹음
        {
            message = $"{animal.DisplayName} IS ALREADY FED"; // 문구
            return false; // 실패
        }

        int amount = acceptedAnimal.FeedPerDay; // 먹이 수량

        if (!inventory.HasItem(acceptedAnimal.FeedItem, amount) || inventory.RemoveItem(acceptedAnimal.FeedItem, amount) != amount) // 먹이 사용
        {
            message = $"NEED {amount} {acceptedAnimal.FeedItem.DisplayName}"; // 문구
            return false; // 실패
        }

        animal.MarkFed(); // 먹음 기록
        RefreshPenVisuals(); // 외형
        NotifyChanged(); // 알림
        message = $"FED {animal.DisplayName}"; // 문구
        return true; // 성공
    }

    public int FeedAll(PlayerInventory inventory) // 굶은 동물 모두 먹이 주기, 먹인 수 반환
    {
        int fed = 0; // 결과

        for (int index = 0; index < animals.Count; index++) // 순회
        {
            if (!animals[index].FedToday && TryFeed(index, inventory, out _)) // 먹이 주기
            {
                fed++; // 증가
            }
        }

        return fed; // 결과 반환
    }

    public bool TryPet(int index, out string message) // 쓰다듬기
    {
        if (index < 0 || index >= animals.Count) // 범위 확인
        {
            message = string.Empty; // 문구
            return false; // 실패
        }

        PenAnimal animal = animals[index]; // 동물

        if (!animal.TryPet()) // 하루 한 번
        {
            message = $"{animal.DisplayName} WAS ALREADY PETTED TODAY"; // 문구
            return false; // 실패
        }

        if (index < visuals.Count && visuals[index] != null) // 외형 반응
        {
            visuals[index].PlayHappy(); // 폴짝
        }

        NotifyChanged(); // 알림
        message = $"{animal.DisplayName} LOOKS HAPPY"; // 문구
        return true; // 성공
    }

    public int TryCollect(int index, PlayerInventory inventory) // 한 마리 생산물 꺼내기, 꺼낸 수 반환
    {
        if (index < 0 || index >= animals.Count || inventory == null || !animals[index].HasProduct) // 요청 확인
        {
            return 0; // 없음
        }

        PenAnimal animal = animals[index]; // 동물
        ItemData product = acceptedAnimal.ProductItem; // 생산물
        int ready = animal.ProductReady; // 수량
        int left = inventory.AddItem(product, ready); // 가방에 넣기
        int added = ready - left; // 넣은 수

        if (added <= 0) // 가방 가득 참
        {
            return 0; // 실패
        }

        animal.TakeProduct(added); // 차감
        RefreshPenVisuals(); // 외형
        NotifyChanged(); // 알림
        SpawnText($"+{added} {product.DisplayName}", new Color(1f, 0.86f, 0.35f, 1f)); // 월드 알림
        return added; // 결과 반환
    }

    public int CollectAll(PlayerInventory inventory) // 모든 생산물 꺼내기, 꺼낸 수 반환
    {
        int total = 0; // 결과

        for (int index = 0; index < animals.Count; index++) // 순회
        {
            total += TryCollect(index, inventory); // 꺼내기
        }

        return total; // 결과 반환
    }

    // ------------------------------------------------------------ 날짜

    public void ProcessDays(int today) // 지난 날짜 처리
    {
        if (!hasDayRecord) // 기록 없음
        {
            lastProcessedDay = today; // 오늘부터
            hasDayRecord = true; // 기록
            return; // 완료
        }

        if (today <= lastProcessedDay) // 처리할 날 없음
        {
            return; // 생략
        }

        int days = Mathf.Min(today - lastProcessedDay, 30); // 처리 일수 (너무 긴 공백은 30일로 제한)
        int produced = 0; // 새 생산물

        for (int day = 0; day < days; day++) // 날짜 순회
        {
            foreach (PenAnimal animal in animals) // 동물 순회
            {
                produced += animal.ProcessDay(day == 0, (float)random.NextDouble()); // 하루 처리
            }
        }

        lastProcessedDay = today; // 기록
        RefreshPenVisuals(); // 외형
        NotifyChanged(); // 알림

        if (produced > 0 && acceptedAnimal != null) // 생산 알림
        {
            SpawnText($"{produced} {acceptedAnimal.ProductItem.DisplayName} READY", new Color(0.31f, 0.76f, 0.69f, 1f)); // 월드 알림
        }
    }

    // ------------------------------------------------------------ 저장

    public AnimalPenSaveData CaptureSaveData() // 저장 데이터 생성
    {
        AnimalPenSaveData data = new AnimalPenSaveData { structureId = StructureId, lastProcessedDay = lastProcessedDay }; // 기본

        foreach (PenAnimal animal in animals) // 동물 순회
        {
            data.animals.Add(animal.ToSave()); // 추가
        }

        return data; // 결과 반환
    }

    public void ApplySaveData(AnimalPenSaveData data, LivestockManager manager) // 저장 데이터 적용
    {
        animals.Clear(); // 초기화

        foreach (PenAnimalSaveData saved in data.animals) // 동물 순회
        {
            if (manager.TryGetAnimal(saved.animalId, out AnimalData animal) && animals.Count < Capacity) // 종류 확인
            {
                animals.Add(PenAnimal.FromSave(animal, saved)); // 추가
            }
        }

        lastProcessedDay = data.lastProcessedDay; // 날짜
        hasDayRecord = true; // 기록
        RebuildVisuals(); // 외형
        NotifyChanged(); // 알림
    }

    public void ResetForLoad(int today) // 저장 데이터가 없는 우리를 빈 상태로 초기화
    {
        animals.Clear(); // 초기화
        lastProcessedDay = today; // 오늘부터
        hasDayRecord = true; // 기록
        RebuildVisuals(); // 외형
        NotifyChanged(); // 알림
    }

    public void DebugAddAnimal() // 테스트용 : 먹이 없이 동물 추가
    {
        if (acceptedAnimal == null || IsFull) // 확인
        {
            return; // 생략
        }

        animals.Add(new PenAnimal(acceptedAnimal, NextNameNumber())); // 추가
        RebuildVisuals(); // 외형
        NotifyChanged(); // 알림
    }

    // ------------------------------------------------------------ 외형

    public Vector3 GetWanderPoint(float normalizedX, float normalizedZ) // 영역 안의 월드 위치
    {
        Vector3 local = wanderCenter + new Vector3((normalizedX - 0.5f) * wanderSize.x, 0f, (normalizedZ - 0.5f) * wanderSize.y); // 우리 기준
        return transform.TransformPoint(local); // 월드 위치
    }

    public bool IsInsideWanderArea(Vector3 worldPoint) // 영역 안인지 확인
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint) - wanderCenter; // 우리 기준
        return Mathf.Abs(local.x) <= wanderSize.x * 0.5f + 0.01f && Mathf.Abs(local.z) <= wanderSize.y * 0.5f + 0.01f; // 결과
    }

    private void RebuildVisuals() // 동물 외형 맞추기
    {
        for (int index = visuals.Count - 1; index >= 0; index--) // 남는 외형 제거
        {
            if (visuals[index] == null || index >= animals.Count || visuals[index].Data != animals[index].Data) // 조건
            {
                if (visuals[index] != null)
                {
                    Destroy(visuals[index].gameObject); // 제거
                }

                visuals.RemoveAt(index); // 목록 제거
            }
        }

        for (int index = visuals.Count; index < animals.Count; index++) // 부족한 외형 추가
        {
            AnimalData data = animals[index].Data; // 종류

            if (data == null || data.ModelPrefab == null) // 모델 확인
            {
                visuals.Add(null); // 자리만
                continue; // 다음
            }

            float x = 0.2f + 0.6f * ((index * 0.37f) % 1f); // 시작 위치
            float z = 0.25f + 0.5f * ((index * 0.61f + 0.3f) % 1f); // 시작 위치
            GameObject instance = Instantiate(data.ModelPrefab, GetWanderPoint(x, z), transform.rotation * Quaternion.Euler(0f, index * 97f, 0f), animalRoot); // 생성
            instance.name = $"Animal_{index}_{data.AnimalId}"; // 이름
            SetLayerRecursive(instance.transform, gameObject.layer); // 레이어
            FarmAnimal farmAnimal = instance.AddComponent<FarmAnimal>(); // 움직임
            farmAnimal.Setup(this, data, index); // 설정
            visuals.Add(farmAnimal); // 추가
        }

        for (int index = 0; index < visuals.Count; index++) // 번호 갱신
        {
            if (visuals[index] != null)
            {
                visuals[index].SetIndex(index); // 번호
            }
        }

        RefreshPenVisuals(); // 사료·생산물 외형
    }

    private void RefreshPenVisuals() // 사료·생산물 외형
    {
        if (feedVisual != null) // 사료 확인
        {
            feedVisual.SetActive(animals.Count > 0 && animals.Exists(animal => animal.FedToday)); // 먹은 동물이 있으면 표시
        }

        if (productVisual != null) // 생산물 확인
        {
            productVisual.SetActive(ProductCount > 0); // 생산물 있으면 표시
        }
    }

    private static void SetLayerRecursive(Transform target, int layer) // 하위 레이어 적용
    {
        target.gameObject.layer = layer; // 적용

        foreach (Transform child in target) // 하위 순회
        {
            SetLayerRecursive(child, layer); // 재귀
        }
    }

    private int NextNameNumber() // 비어 있는 가장 작은 개체 번호
    {
        for (int number = 1; number < 100; number++) // 번호 순회
        {
            if (!animals.Exists(animal => animal.NameNumber == number)) // 사용 확인
            {
                return number; // 결과
            }
        }

        return animals.Count + 1; // 예비
    }

    private int Count(Predicate<PenAnimal> match) // 조건에 맞는 동물 수
    {
        int count = 0; // 결과

        foreach (PenAnimal animal in animals) // 순회
        {
            if (match(animal)) count++; // 증가
        }

        return count; // 결과 반환
    }

    private void SpawnText(string text, Color color) // 우리 위 알림
    {
        CombatDamagePopup.SpawnText(transform.position + Vector3.up * 1.6f, text, color, 2.2f); // 알림
    }

    private void NotifyChanged() // 상태 변경 알림
    {
        StateChanged?.Invoke(); // 알림
    }

    private void OnDrawGizmosSelected() // 영역 표시
    {
        Gizmos.color = new Color(0.4f, 0.9f, 0.5f, 0.8f); // 색
        Gizmos.matrix = transform.localToWorldMatrix; // 우리 기준
        Gizmos.DrawWireCube(wanderCenter + Vector3.up * 0.05f, new Vector3(wanderSize.x, 0.1f, wanderSize.y)); // 영역
    }
}
