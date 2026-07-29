using System.IO; // 파일 검사 기능
using System.Text; // UTF-8 인코딩 기능
using UnityEditor; // Unity Editor 메뉴 기능
using UnityEngine; // Unity 로그와 수치 기능

public static class SaveFileSystemValidator // 저장 파일 시스템 검증
{
    private const string TestSlotId = "slot_day32_test"; // 자동 검증 슬롯 ID
    private const string SampleSlotId = "slot_day32_sample"; // 예제 파일 슬롯 ID
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false); // BOM 없는 UTF-8 인코딩

    [MenuItem("Tools/Project U/Validate Save File System")] // 저장 파일 시스템 검증 메뉴
    private static void ValidateSaveFileSystem() // 저장·읽기·백업 복구 자동 검증
    {
        DeleteTestFiles(TestSlotId); // 이전 테스트 파일 정리

        try // 전체 검증 예외 처리
        {
            SaveGameData firstSaveData = SaveGameData.CreateNew("20_Gameplay"); // 첫 번째 예제 데이터 생성
            firstSaveData.player.health = 90f; // 첫 번째 체력 적용
            firstSaveData.time.currentDay = 2; // 첫 번째 날짜 적용
            firstSaveData.time.currentHour = 10f; // 첫 번째 시각 적용

            if (!SaveFileService.TrySave(TestSlotId, firstSaveData, out string firstSaveMessage)) // 첫 번째 저장 실행
            {
                Debug.LogError($"저장 파일 시스템 검사 실패: {firstSaveMessage}"); // 첫 번째 저장 오류 출력
                return; // 검사 중단
            }

            SaveGameData secondSaveData = SaveGameData.CreateNew("20_Gameplay"); // 두 번째 예제 데이터 생성
            secondSaveData.player.health = 65f; // 두 번째 체력 적용
            secondSaveData.time.currentDay = 3; // 두 번째 날짜 적용
            secondSaveData.time.currentHour = 14.5f; // 두 번째 시각 적용

            if (!SaveFileService.TrySave(TestSlotId, secondSaveData, out string secondSaveMessage)) // 두 번째 저장 실행
            {
                Debug.LogError($"저장 파일 시스템 검사 실패: {secondSaveMessage}"); // 두 번째 저장 오류 출력
                return; // 검사 중단
            }

            string backupFilePath = SaveFileService.GetBackupFilePath(TestSlotId); // 백업 파일 경로 가져오기

            if (!File.Exists(backupFilePath)) // 백업 파일 생성 확인
            {
                Debug.LogError("저장 파일 시스템 검사 실패: 백업 파일이 생성되지 않았습니다."); // 백업 생성 오류 출력
                return; // 검사 중단
            }

            if (!SaveFileService.TryLoad(TestSlotId, out SaveGameData mainSaveData, out bool mainLoadedFromBackup, out string mainLoadMessage)) // 기본 파일 불러오기
            {
                Debug.LogError($"저장 파일 시스템 검사 실패: {mainLoadMessage}"); // 기본 파일 불러오기 오류 출력
                return; // 검사 중단
            }

            bool mainDataIsValid = !mainLoadedFromBackup && Mathf.Abs(mainSaveData.player.health - 65f) < 0.001f; // 기본 파일 데이터 확인

            if (!mainDataIsValid) // 기본 파일 결과 확인
            {
                Debug.LogError("저장 파일 시스템 검사 실패: 최신 기본 파일 데이터가 일치하지 않습니다."); // 기본 데이터 오류 출력
                return; // 검사 중단
            }

            string mainFilePath = SaveFileService.GetMainFilePath(TestSlotId); // 기본 파일 경로 가져오기
            File.WriteAllText(mainFilePath, "{ broken json", Utf8WithoutBom); // 기본 파일을 의도적으로 손상

            if (!SaveFileService.TryLoad(TestSlotId, out SaveGameData backupSaveData, out bool loadedFromBackup, out string backupLoadMessage)) // 백업 파일 불러오기
            {
                Debug.LogError($"저장 파일 시스템 검사 실패: {backupLoadMessage}"); // 백업 불러오기 오류 출력
                return; // 검사 중단
            }

            bool backupDataIsValid = loadedFromBackup && Mathf.Abs(backupSaveData.player.health - 90f) < 0.001f; // 백업 데이터 확인

            if (!backupDataIsValid) // 백업 복구 결과 확인
            {
                Debug.LogError("저장 파일 시스템 검사 실패: 백업 데이터가 올바르지 않습니다."); // 백업 데이터 오류 출력
                return; // 검사 중단
            }

            Debug.Log($"저장 파일 생성·읽기·백업 복구 검사 완료\n{SaveFileService.SaveDirectoryPath}"); // 전체 검사 성공 출력
        }
        finally // 검증 종료 처리
        {
            DeleteTestFiles(TestSlotId); // 테스트 전용 파일 정리
        }
    }

    [MenuItem("Tools/Project U/Create Sample Save File")] // 예제 저장 파일 생성 메뉴
    private static void CreateSampleSaveFile() // 확인용 JSON 파일 생성
    {
        SaveGameData sampleSaveData = SaveGameData.CreateNew("20_Gameplay"); // 예제 저장 데이터 생성
        sampleSaveData.player.health = 75f; // 예제 체력 적용
        sampleSaveData.player.hunger = 60f; // 예제 허기 적용
        sampleSaveData.player.thirst = 55f; // 예제 갈증 적용
        sampleSaveData.time.currentDay = 4; // 예제 날짜 적용
        sampleSaveData.time.currentHour = 16.25f; // 예제 시각 적용

        if (!SaveFileService.TrySave(SampleSlotId, sampleSaveData, out string resultMessage)) // 예제 파일 저장
        {
            Debug.LogError($"예제 저장 파일 생성 실패: {resultMessage}"); // 예제 저장 오류 출력
            return; // 생성 중단
        }

        Debug.Log($"예제 저장 파일 생성 완료\n{SaveFileService.GetMainFilePath(SampleSlotId)}"); // 생성 결과 출력
        EditorUtility.RevealInFinder(SaveFileService.SaveDirectoryPath); // 저장 폴더 열기
    }

    [MenuItem("Tools/Project U/Open Save Folder")] // 저장 폴더 열기 메뉴
    private static void OpenSaveFolder() // 저장 폴더 탐색기 표시
    {
        Directory.CreateDirectory(SaveFileService.SaveDirectoryPath); // 저장 폴더 생성
        EditorUtility.RevealInFinder(SaveFileService.SaveDirectoryPath); // 운영체제 탐색기로 폴더 열기
    }

    private static void DeleteTestFiles(string slotId) // 테스트 슬롯 파일 정리
    {
        DeleteFileIfExists(SaveFileService.GetMainFilePath(slotId)); // 기본 테스트 파일 삭제
        DeleteFileIfExists(SaveFileService.GetBackupFilePath(slotId)); // 백업 테스트 파일 삭제
        DeleteFileIfExists(SaveFileService.GetTemporaryFilePath(slotId)); // 임시 테스트 파일 삭제
    }

    private static void DeleteFileIfExists(string filePath) // 파일 존재 시 삭제
    {
        if (File.Exists(filePath)) // 파일 존재 확인
        {
            File.Delete(filePath); // 파일 삭제
        }
    }
}