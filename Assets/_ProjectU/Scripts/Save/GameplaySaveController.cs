using System; // 문자열 비교 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 현재 Scene 확인 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class GameplaySaveController : MonoBehaviour // 게임 진행 상태 저장과 불러오기 관리
{
    [Header("Save Slot")] // 저장 슬롯 설정 묶음
    [SerializeField] private string slotId = SaveFileService.DefaultSlotId; // 현재 사용할 저장 슬롯 ID

    [Header("References")] // 외부 참조 설정 묶음
    [SerializeField] private Transform playerTransform; // 플레이어 위치와 회전 대상
    [SerializeField] private ThirdPersonCameraFollow thirdPersonCameraFollow; // 플레이어 추적 카메라
    [SerializeField] private DayNightCycle dayNightCycle; // 날짜와 시간 시스템
    [SerializeField] private InventorySaveBridge inventorySaveBridge; // 인벤토리와 장비 저장 연결
    [SerializeField] private WorldSaveBridge worldSaveBridge; // 월드 상태 저장 연결
    [SerializeField] private PlacedStructureSaveBridge placedStructureSaveBridge; // 설치 건축물 저장 연결
    [SerializeField] private RespawnSaveBridge respawnSaveBridge; // 부활 지점 저장 연결
    [SerializeField] private SleepSystem sleepSystem; // 수면 진행 상태 확인

    private CharacterController characterController; // 플레이어 충돌 이동기
    private PlayerMovement playerMovement; // 플레이어 이동 관리기
    private PlayerHealth playerHealth; // 플레이어 체력 관리기
    private PlayerHunger playerHunger; // 플레이어 허기 관리기
    private PlayerThirst playerThirst; // 플레이어 갈증 관리기
    private PlayerStamina playerStamina; // 플레이어 스태미나 관리기
    private bool isReady; // 저장 기능 준비 상태

    private void Awake() // 저장 기능 참조 초기화
    {
        bool hasPlayerReference = playerTransform != null; // 플레이어 참조 확인
        bool hasCameraReference = thirdPersonCameraFollow != null; // 카메라 참조 확인
        bool hasTimeReference = dayNightCycle != null; // 시간 시스템 참조 확인
        bool hasInventorySaveBridge = inventorySaveBridge != null; // 인벤토리 저장 연결 확인
        bool hasWorldSaveBridge = worldSaveBridge != null; // 월드 저장 연결 확인
        bool hasPlacedStructureSaveBridge = placedStructureSaveBridge != null; // 건축물 저장 연결 확인
        bool hasRespawnSaveBridge = respawnSaveBridge != null; // 부활 지점 저장 연결 확인
        bool hasSleepSystem = sleepSystem != null; // 수면 시스템 참조 확인

        if (!hasPlayerReference || !hasCameraReference
            || !hasTimeReference || !hasInventorySaveBridge
            || !hasWorldSaveBridge || !hasPlacedStructureSaveBridge
            || !hasRespawnSaveBridge || !hasSleepSystem) // 필수 Inspector 참조 확인
        {
            Debug.LogError($"{gameObject.name}의 저장 시스템 참조가 누락되었습니다.", this); // 참조 누락 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        characterController = playerTransform.GetComponent<CharacterController>(); // CharacterController 가져오기
        playerMovement = playerTransform.GetComponent<PlayerMovement>(); // PlayerMovement 가져오기
        playerHealth = playerTransform.GetComponent<PlayerHealth>(); // PlayerHealth 가져오기
        playerHunger = playerTransform.GetComponent<PlayerHunger>(); // PlayerHunger 가져오기
        playerThirst = playerTransform.GetComponent<PlayerThirst>(); // PlayerThirst 가져오기
        playerStamina = playerTransform.GetComponent<PlayerStamina>(); // PlayerStamina 가져오기

        bool hasCharacterController = characterController != null; // 충돌 이동기 확인
        bool hasPlayerMovement = playerMovement != null; // 이동 관리기 확인
        bool hasPlayerHealth = playerHealth != null; // 체력 관리기 확인
        bool hasPlayerHunger = playerHunger != null; // 허기 관리기 확인
        bool hasPlayerThirst = playerThirst != null; // 갈증 관리기 확인
        bool hasPlayerStamina = playerStamina != null; // 스태미나 관리기 확인

        if (!hasCharacterController || !hasPlayerMovement || !hasPlayerHealth || !hasPlayerHunger || !hasPlayerThirst || !hasPlayerStamina) // 플레이어 필수 컴포넌트 확인
        {
            Debug.LogError("플레이어의 저장 대상 컴포넌트가 누락되었습니다.", playerTransform); // 플레이어 구성 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        if (!inventorySaveBridge.TryValidateSetup(out string inventorySetupError)) // 인벤토리 저장 연결 검사
        {
            Debug.LogError($"인벤토리 저장 시스템 초기화 실패\n{inventorySetupError}", this); // 연결 검사 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        if (!worldSaveBridge.TryValidateSetup(out string worldSetupError)) // 월드 저장 연결 검사
        {
            Debug.LogError($"월드 저장 시스템 초기화 실패\n{worldSetupError}", this); // 월드 연결 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        if (!placedStructureSaveBridge.TryValidateSetup(out string structureSetupError)) // 건축물 저장 연결 검사
        {
            Debug.LogError($"건축물 저장 시스템 초기화 실패\n{structureSetupError}", this); // 건축물 연결 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        if (!respawnSaveBridge.TryValidateSetup(out string respawnSetupError)) // 부활 지점 저장 연결 검사
        {
            Debug.LogError($"부활 지점 저장 시스템 초기화 실패\n{respawnSetupError}", this); // 연결 오류 출력
            enabled = false; // 저장 기능 비활성화
            return; // 초기화 중단
        }

        isReady = true; // 저장 기능 준비 완료
    }

    [ContextMenu("Save Current Game")] // Inspector 저장 실행 메뉴
    public void SaveCurrentGame() // 현재 게임 상태 저장
    {
        if (!CanUseSaveSystem()) // 저장 기능 사용 가능 여부 확인
        {
            return; // 저장 중단
        }

        SaveGameData saveData = CaptureCurrentState(); // 현재 게임 상태 수집

        if (!worldSaveBridge.TryCapture(saveData, out string worldCaptureError)) // 월드 상태 저장 데이터 수집
        {
            Debug.LogError($"월드 상태 저장 준비 실패\n{worldCaptureError}", this); // 월드 수집 오류 출력
            return; // 파일 저장 중단
        }

        if (!placedStructureSaveBridge.TryCapture(saveData, out string structureCaptureError)) // 건축물 상태 수집
        {
            Debug.LogError($"건축물 상태 저장 준비 실패\n{structureCaptureError}", this); // 건축물 수집 오류 출력
            return; // 파일 저장 중단
        }

        if (!respawnSaveBridge.TryCapture(saveData, out string respawnCaptureError)) // 부활 지점 상태 수집
        {
            Debug.LogError($"부활 지점 저장 준비 실패\n{respawnCaptureError}", this); // 수집 오류 출력
            return; // 파일 저장 중단
        }

        if (!SaveFileService.TrySave(slotId, saveData, out string resultMessage)) // JSON 파일 저장 실행
        {
            Debug.LogError($"현재 게임 저장 실패\n{resultMessage}", this); // 저장 실패 내용 출력
            return; // 저장 종료
        }

        Debug.Log($"현재 게임 저장 완료\n{resultMessage}", this); // 저장 성공 내용 출력
    }

    [ContextMenu("Load Current Game")] // Inspector 불러오기 실행 메뉴
    public void LoadCurrentGame() // 저장된 게임 상태 불러오기
    {
        if (!CanUseSaveSystem()) // 저장 기능 사용 가능 여부 확인
        {
            return; // 불러오기 중단
        }

        if (!SaveFileService.TryLoad(slotId, out SaveGameData saveData, out bool loadedFromBackup, out string resultMessage)) // JSON 파일 불러오기 실행
        {
            Debug.LogError($"현재 게임 불러오기 실패\n{resultMessage}", this); // 불러오기 실패 내용 출력
            return; // 불러오기 종료
        }

        Scene activeScene = SceneManager.GetActiveScene(); // 현재 Scene 정보 가져오기
        bool isSameScene = string.Equals(saveData.sceneName, activeScene.name, StringComparison.Ordinal); // 저장 Scene 일치 여부 확인

        if (!isSameScene) // 다른 Scene의 저장 파일 확인
        {
            Debug.LogError($"저장 Scene과 현재 Scene이 다릅니다.\n저장: {saveData.sceneName}\n현재: {activeScene.name}", this); // Scene 불일치 출력
            return; // 상태 적용 중단
        }

        bool hasPositionData = saveData.player.position != null; // 위치 데이터 존재 확인
        bool hasRotationData = saveData.player.rotation != null; // 회전 데이터 존재 확인

        if (!hasPositionData || !hasRotationData) // Transform 데이터 누락 확인
        {
            Debug.LogError("저장 파일의 플레이어 위치 또는 회전 데이터가 누락되었습니다.", this); // Transform 데이터 오류 출력
            return; // 상태 적용 중단
        }

        if (!inventorySaveBridge.TryRestore(saveData, out string inventoryRestoreError)) // 인벤토리와 장비 상태 복원
        {
            Debug.LogError($"인벤토리와 장비 불러오기 실패\n{inventoryRestoreError}", this); // 아이템 복원 오류 출력
            return; // 전체 불러오기 중단
        }

        if (!worldSaveBridge.TryRestore(saveData, out string worldRestoreError)) // 월드 아이템과 채집 자원 복원
        {
            Debug.LogError($"월드 상태 불러오기 실패\n{worldRestoreError}", this); // 월드 복원 오류 출력
            return; // 전체 불러오기 중단
        }

        if (!placedStructureSaveBridge.TryRestore(saveData, out string structureRestoreError)) // 건축물 상태 복원
        {
            Debug.LogError($"건축물 상태 불러오기 실패\n{structureRestoreError}", this); // 건축물 복원 오류 출력
            return; // 전체 불러오기 중단
        }

        if (!respawnSaveBridge.TryRestore(saveData, out string respawnRestoreError)) // 부활 지점 상태 복원
        {
            Debug.LogError($"부활 지점 불러오기 실패\n{respawnRestoreError}", this); // 복원 오류 출력
            return; // 전체 불러오기 중단
        }

        ApplyLoadedState(saveData); // 불러온 게임 상태 적용

        string fileSource = loadedFromBackup ? "백업 파일" : "기본 파일"; // 불러온 파일 종류 결정
        Debug.Log($"현재 게임 불러오기 완료\n사용 파일: {fileSource}\n{resultMessage}", this); // 불러오기 성공 내용 출력
    }

    private SaveGameData CaptureCurrentState() // 현재 게임 상태를 저장 데이터로 변환
    {
        Scene activeScene = SceneManager.GetActiveScene(); // 현재 Scene 정보 가져오기
        SaveGameData saveData = SaveGameData.CreateNew(activeScene.name); // 현재 Scene 기준 저장 데이터 생성

        saveData.player.position = SaveVector3Data.FromVector3(playerTransform.position); // 플레이어 위치 저장
        saveData.player.rotation = SaveQuaternionData.FromQuaternion(playerTransform.rotation); // 플레이어 회전 저장
        saveData.player.health = playerHealth.CurrentHealth; // 현재 체력 저장
        saveData.player.hunger = playerHunger.CurrentHunger; // 현재 허기 저장
        saveData.player.thirst = playerThirst.CurrentThirst; // 현재 갈증 저장
        saveData.player.stamina = playerStamina.CurrentStamina; // 현재 스태미나 저장
        saveData.time.currentDay = dayNightCycle.CurrentDay; // 현재 날짜 저장
        saveData.time.currentHour = dayNightCycle.CurrentHour; // 현재 시간 저장
        inventorySaveBridge.Capture(saveData); // 인벤토리와 장비 상태 저장

        return saveData; // 수집된 저장 데이터 반환
    }

    private void ApplyLoadedState(SaveGameData saveData) // 불러온 데이터를 현재 게임에 적용
    {
        Vector3 loadedPosition = saveData.player.position.ToVector3(); // 저장 위치를 Unity 위치로 변환
        Quaternion loadedRotation = saveData.player.rotation.ToQuaternion(); // 저장 회전을 Unity 회전으로 변환
        bool wasControllerEnabled = characterController.enabled; // 기존 CharacterController 활성 상태 저장

        if (wasControllerEnabled) // CharacterController 활성 상태 확인
        {
            characterController.enabled = false; // 안전한 순간 이동을 위한 충돌 이동기 비활성화
        }

        playerTransform.SetPositionAndRotation(loadedPosition, loadedRotation); // 저장 위치와 회전 적용
        thirdPersonCameraFollow.SetYaw(loadedRotation.eulerAngles.y); // 플레이어 방향에 맞춰 카메라 좌우 시점 적용

        if (wasControllerEnabled) // 기존 CharacterController 상태 확인
        {
            characterController.enabled = true; // 기존 충돌 이동기 상태 복구
        }

        playerMovement.ResetMotionState(); // 낙하 속도와 접지 상태 초기화
        playerHealth.SetCurrentHealth(saveData.player.health); // 저장 체력 적용
        playerHunger.SetCurrentHunger(saveData.player.hunger); // 저장 허기 적용
        playerThirst.SetCurrentThirst(saveData.player.thirst); // 저장 갈증 적용
        playerStamina.SetCurrentStamina(saveData.player.stamina); // 저장 스태미나 적용
        dayNightCycle.SetTime(saveData.time.currentDay, saveData.time.currentHour); // 저장 날짜와 시간 적용
    }

    private bool CanUseSaveSystem() // 현재 저장 기능 사용 가능 여부 확인
    {
        if (!Application.isPlaying) // Play Mode 여부 확인
        {
            Debug.LogWarning("저장과 불러오기는 Play Mode에서 실행해야 합니다.", this); // 실행 상태 안내 출력
            return false; // 기능 사용 차단
        }

        if (!isReady) // 초기화 완료 여부 확인
        {
            Debug.LogError("저장 시스템 초기화가 완료되지 않았습니다.", this); // 초기화 오류 출력
            return false; // 기능 사용 차단
        }

        if (sleepSystem.IsSleeping) // 수면 연출 진행 여부 확인
        {
            Debug.LogWarning("수면 연출 중에는 저장과 불러오기를 사용할 수 없습니다.", this); // 사용 제한 안내 출력
            return false; // 저장과 불러오기 차단
        }

        return true; // 저장 기능 사용 허용
    }
}