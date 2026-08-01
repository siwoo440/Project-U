using System; // 문자열 비교 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class RespawnSaveBridge : MonoBehaviour // 침낭 부활 지점 저장 연결
{
    [Header("References")] // 외부 참조 묶음
    [Tooltip("플레이어 부활 시스템.")]
    [SerializeField] private PlayerRespawnSystem playerRespawnSystem; // 플레이어 부활 시스템

    public bool TryValidateSetup(out string errorMessage) // 저장 연결 설정 검사
    {
        if (playerRespawnSystem == null) // 부활 시스템 참조 확인
        {
            errorMessage = "Player Respawn System 참조가 누락되었습니다."; // 참조 오류 저장
            return false; // 검사 실패
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    public bool TryCapture(SaveGameData saveData, out string errorMessage) // 현재 부활 지점 수집
    {
        if (saveData == null || saveData.respawn == null || saveData.world == null) // 저장 데이터 존재 확인
        {
            errorMessage = "부활 지점 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 수집 실패
        }

        if (saveData.world.placedStructures == null) // 건축물 저장 목록 확인
        {
            errorMessage = "설치 건축물 저장 목록이 누락되었습니다."; // 목록 오류 저장
            return false; // 수집 실패
        }

        saveData.respawn.hasRegisteredPoint = false; // 기본 미등록 상태 적용
        saveData.respawn.registeredStructureId = string.Empty; // 기본 침낭 ID 초기화

        Transform registeredPoint = playerRespawnSystem.RegisteredRespawnPoint; // 현재 등록 위치 조회

        if (registeredPoint == null) // 등록된 침낭 없음 확인
        {
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 미등록 상태 저장 성공
        }

        SleepingBagInteractable sleepingBag = registeredPoint.GetComponentInParent<SleepingBagInteractable>(); // 소속 침낭 검색
        PlacedBuildObject placedStructure = registeredPoint.GetComponentInParent<PlacedBuildObject>(); // 소속 건축물 검색

        if (sleepingBag == null || placedStructure == null) // 침낭과 건축물 구성 확인
        {
            errorMessage = "등록된 부활 지점이 설치 침낭에 속하지 않습니다."; // 구성 오류 저장
            return false; // 수집 실패
        }

        if (sleepingBag.RespawnPoint != registeredPoint) // 침낭 등록 위치 일치 확인
        {
            errorMessage = "등록된 위치가 침낭의 Respawn Point와 일치하지 않습니다."; // 위치 오류 저장
            return false; // 수집 실패
        }

        string structureId = placedStructure.StructureId; // 침낭 건축물 ID 조회

        if (string.IsNullOrWhiteSpace(structureId)) // 건축물 ID 존재 확인
        {
            errorMessage = "등록된 침낭의 Structure ID가 비어 있습니다."; // ID 오류 저장
            return false; // 수집 실패
        }

        if (!ContainsSavedStructure(saveData, structureId)) // 저장 건축물 목록 포함 여부 확인
        {
            errorMessage = $"등록 침낭이 건축물 저장 목록에 없습니다: {structureId}"; // 목록 불일치 저장
            return false; // 수집 실패
        }

        saveData.respawn.hasRegisteredPoint = true; // 침낭 등록 상태 저장
        saveData.respawn.registeredStructureId = structureId; // 침낭 건축물 ID 저장
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 수집 성공
    }

    public bool TryRestore(SaveGameData saveData, out string errorMessage) // 저장 부활 지점 복원
    {
        if (saveData == null || saveData.respawn == null) // 저장 데이터 존재 확인
        {
            errorMessage = "부활 지점 저장 데이터가 누락되었습니다."; // 저장 데이터 오류
            return false; // 복원 실패
        }

        if (!saveData.respawn.hasRegisteredPoint) // 등록된 침낭 없음 확인
        {
            playerRespawnSystem.ClearRegisteredRespawnPoint(); // 현재 등록 지점 해제
            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 미등록 상태 복원 성공
        }

        string registeredStructureId = saveData.respawn.registeredStructureId; // 저장 침낭 ID 조회

        if (string.IsNullOrWhiteSpace(registeredStructureId)) // 침낭 ID 존재 확인
        {
            errorMessage = "등록된 침낭의 Structure ID가 비어 있습니다."; // ID 오류 저장
            return false; // 복원 실패
        }

        PlacedBuildObject[] foundStructures = UnityEngine.Object.FindObjectsByType<PlacedBuildObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None); // 활성 설치 건축물 검색

        PlacedBuildObject targetStructure = null; // 복원 대상 침낭 초기화

        for (int index = 0; index < foundStructures.Length; index++) // 활성 건축물 순회
        {
            PlacedBuildObject currentStructure = foundStructures[index]; // 현재 건축물 조회

            if (!string.Equals(
                currentStructure.StructureId,
                registeredStructureId,
                StringComparison.Ordinal)) // 저장 ID 일치 여부 확인
            {
                continue; // 다른 건축물 건너뛰기
            }

            if (targetStructure != null) // 동일 ID 대상 존재 확인
            {
                errorMessage = $"활성 건축물에 중복 Structure ID가 있습니다: {registeredStructureId}"; // 중복 오류 저장
                return false; // 복원 실패
            }

            targetStructure = currentStructure; // 복원 대상 저장
        }

        if (targetStructure == null) // 저장 침낭 검색 결과 확인
        {
            errorMessage = $"저장된 부활 침낭을 찾을 수 없습니다: {registeredStructureId}"; // 누락 오류 저장
            return false; // 복원 실패
        }

        SleepingBagInteractable sleepingBag = targetStructure.GetComponentInChildren<SleepingBagInteractable>(true); // 침낭 기능 검색

        if (sleepingBag == null || sleepingBag.RespawnPoint == null) // 침낭 부활 위치 확인
        {
            errorMessage = $"저장 건축물에 침낭 부활 위치가 없습니다: {registeredStructureId}"; // 프리팹 오류 저장
            return false; // 복원 실패
        }

        playerRespawnSystem.RegisterRespawnPoint(sleepingBag.RespawnPoint); // 복원된 침낭 위치 등록
        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 복원 성공
    }

    private bool ContainsSavedStructure(SaveGameData saveData, string structureId) // 저장 목록의 건축물 ID 확인
    {
        for (int index = 0; index < saveData.world.placedStructures.Count; index++) // 저장 건축물 순회
        {
            PlacedStructureSaveData structureSaveData = saveData.world.placedStructures[index]; // 현재 저장 항목 조회

            if (structureSaveData == null) // 빈 저장 항목 확인
            {
                continue; // 빈 항목 건너뛰기
            }

            if (string.Equals(
                structureSaveData.structureId,
                structureId,
                StringComparison.Ordinal)) // 건축물 ID 일치 확인
            {
                return true; // 일치 항목 존재 반환
            }
        }

        return false; // 일치 항목 없음 반환
    }
}