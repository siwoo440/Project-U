using System; // 예외와 날짜 기능
using System.IO; // 파일과 폴더 기능
using System.Text; // UTF-8 인코딩 기능
using UnityEngine; // Unity 저장 경로와 JSON 기능

public static class SaveFileService // 저장 파일 입출력 관리
{
    private const string SaveFolderName = "ProjectU/Saves"; // 저장 폴더 이름
    private const string MainFileExtension = ".json"; // 기본 파일 확장자
    private const string BackupFileExtension = ".backup.json"; // 백업 파일 확장자
    private const string TemporaryFileExtension = ".tmp"; // 임시 파일 확장자
    private const int MaximumSlotIdLength = 32; // 슬롯 ID 최대 길이
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false); // BOM 없는 UTF-8 인코딩

    public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName); // 전체 저장 폴더 경로

    public static string GetMainFilePath(string slotId) // 기본 저장 파일 경로 반환
    {
        return Path.Combine(SaveDirectoryPath, slotId + MainFileExtension); // 기본 파일 경로 조합
    }

    public static string GetBackupFilePath(string slotId) // 백업 저장 파일 경로 반환
    {
        return Path.Combine(SaveDirectoryPath, slotId + BackupFileExtension); // 백업 파일 경로 조합
    }

    public static string GetTemporaryFilePath(string slotId) // 임시 저장 파일 경로 반환
    {
        return Path.Combine(SaveDirectoryPath, slotId + TemporaryFileExtension); // 임시 파일 경로 조합
    }

    public static bool HasSaveFile(string slotId) // 저장 파일 존재 여부 확인
    {
        if (!TryValidateSlotId(slotId, out string validSlotId, out _)) // 슬롯 ID 유효성 확인
        {
            return false; // 잘못된 슬롯 처리
        }

        bool hasMainFile = File.Exists(GetMainFilePath(validSlotId)); // 기본 파일 존재 확인
        bool hasBackupFile = File.Exists(GetBackupFilePath(validSlotId)); // 백업 파일 존재 확인
        return hasMainFile || hasBackupFile; // 하나 이상의 파일 존재 여부 반환
    }

    public static bool TrySave(string slotId, SaveGameData saveData, out string resultMessage) // 저장 파일 생성 시도
    {
        if (!TryValidateSlotId(slotId, out string validSlotId, out resultMessage)) // 슬롯 ID 검사
        {
            return false; // 잘못된 슬롯 처리
        }

        if (saveData == null) // 저장 데이터 존재 확인
        {
            resultMessage = "저장할 데이터가 비어 있습니다."; // 오류 내용 설정
            return false; // 저장 실패
        }

        saveData.saveVersion = SaveVersionPolicy.CurrentVersion; // 현재 저장 버전 적용
        saveData.saveSlotId = validSlotId; // 정상 슬롯 ID 적용
        saveData.savedAtUtc = DateTime.UtcNow.ToString("O"); // UTC 저장 시각 적용

        if (!SaveDataValidator.TryValidate(saveData, out resultMessage)) // 저장 전 데이터 검사
        {
            resultMessage = $"저장 데이터 검사 실패: {resultMessage}"; // 검사 오류 설정
            return false; // 저장 실패
        }

        string mainFilePath = GetMainFilePath(validSlotId); // 기본 파일 경로 생성
        string backupFilePath = GetBackupFilePath(validSlotId); // 백업 파일 경로 생성
        string temporaryFilePath = GetTemporaryFilePath(validSlotId); // 임시 파일 경로 생성

        try // 파일 저장 예외 처리
        {
            Directory.CreateDirectory(SaveDirectoryPath); // 저장 폴더 생성
            DeleteTemporaryFile(temporaryFilePath); // 남은 임시 파일 정리

            string json = JsonUtility.ToJson(saveData, true); // 저장 데이터를 JSON으로 변환
            File.WriteAllText(temporaryFilePath, json, Utf8WithoutBom); // 임시 파일에 JSON 기록

            if (!TryReadAndValidateFile(temporaryFilePath, validSlotId, out _, out string temporaryError)) // 임시 파일 검사
            {
                resultMessage = $"임시 저장 파일 검사 실패: {temporaryError}"; // 임시 파일 오류 설정
                return false; // 저장 중단
            }

            if (File.Exists(mainFilePath)) // 기존 기본 파일 확인
            {
                bool mainFileIsValid = TryReadAndValidateFile(mainFilePath, validSlotId, out _, out _); // 기존 기본 파일 검사

                if (mainFileIsValid) // 기존 기본 파일 정상 여부 확인
                {
                    DeleteFileIfExists(backupFilePath); // 이전 백업 파일 정리
                    File.Replace(temporaryFilePath, mainFilePath, backupFilePath, true); // 기본 파일 교체와 백업 생성
                }
                else // 기존 기본 파일 손상 처리
                {
                    File.Delete(mainFilePath); // 손상된 기본 파일 삭제
                    File.Move(temporaryFilePath, mainFilePath); // 임시 파일을 기본 파일로 이동
                }
            }
            else // 최초 저장 처리
            {
                File.Move(temporaryFilePath, mainFilePath); // 임시 파일을 기본 파일로 이동
            }

            if (!TryReadAndValidateFile(mainFilePath, validSlotId, out _, out string finalError)) // 최종 기본 파일 검사
            {
                RestoreBackupOrRemoveInvalidMain(mainFilePath, backupFilePath); // 백업 복구 또는 손상 파일 정리
                resultMessage = $"최종 저장 파일 검사 실패: {finalError}"; // 최종 검사 오류 설정
                return false; // 저장 실패
            }

            resultMessage = $"저장 완료: {mainFilePath}"; // 저장 성공 내용 설정
            return true; // 저장 성공
        }
        catch (Exception exception) // 파일 처리 예외 확인
        {
            resultMessage = $"저장 파일 생성 실패: {exception.Message}"; // 예외 내용 설정
            return false; // 저장 실패
        }
        finally // 저장 종료 처리
        {
            DeleteTemporaryFile(temporaryFilePath); // 남은 임시 파일 정리
        }
    }

    public static bool TryLoad(string slotId, out SaveGameData saveData, out bool loadedFromBackup, out string resultMessage) // 저장 파일 불러오기 시도
    {
        saveData = null; // 반환 데이터 초기화
        loadedFromBackup = false; // 백업 사용 여부 초기화

        if (!TryValidateSlotId(slotId, out string validSlotId, out resultMessage)) // 슬롯 ID 검사
        {
            return false; // 잘못된 슬롯 처리
        }

        string mainFilePath = GetMainFilePath(validSlotId); // 기본 파일 경로 생성
        string backupFilePath = GetBackupFilePath(validSlotId); // 백업 파일 경로 생성
        string mainError = "기본 저장 파일이 없습니다."; // 기본 파일 오류 초기값
        string backupError = "백업 저장 파일이 없습니다."; // 백업 파일 오류 초기값

        if (File.Exists(mainFilePath)) // 기본 파일 존재 확인
        {
            if (TryReadAndValidateFile(mainFilePath, validSlotId, out saveData, out mainError)) // 기본 파일 읽기
            {
                resultMessage = $"기본 저장 파일 불러오기 완료: {mainFilePath}"; // 기본 파일 성공 내용
                return true; // 불러오기 성공
            }
        }

        if (File.Exists(backupFilePath)) // 백업 파일 존재 확인
        {
            if (TryReadAndValidateFile(backupFilePath, validSlotId, out saveData, out backupError)) // 백업 파일 읽기
            {
                loadedFromBackup = true; // 백업 사용 상태 적용
                resultMessage = $"기본 파일을 불러오지 못해 백업 파일을 사용했습니다: {backupFilePath}"; // 백업 성공 내용
                return true; // 불러오기 성공
            }
        }

        saveData = null; // 실패 데이터 초기화
        resultMessage = $"저장 파일 불러오기 실패\n기본 파일: {mainError}\n백업 파일: {backupError}"; // 전체 오류 내용 설정
        return false; // 불러오기 실패
    }

    private static bool TryReadAndValidateFile(string filePath, string expectedSlotId, out SaveGameData saveData, out string errorMessage) // 단일 저장 파일 검사
    {
        saveData = null; // 반환 데이터 초기화

        try // 파일 읽기 예외 처리
        {
            string json = File.ReadAllText(filePath, Utf8WithoutBom); // JSON 문자열 읽기

            if (string.IsNullOrWhiteSpace(json)) // 빈 파일 확인
            {
                errorMessage = "저장 파일이 비어 있습니다."; // 빈 파일 오류 설정
                return false; // 검사 실패
            }

            saveData = JsonUtility.FromJson<SaveGameData>(json); // JSON을 저장 데이터로 변환

            if (!SaveDataValidator.TryValidate(saveData, out errorMessage)) // 저장 데이터 구조 검사
            {
                saveData = null; // 잘못된 데이터 제거
                return false; // 검사 실패
            }

            if (!string.Equals(saveData.saveSlotId, expectedSlotId, StringComparison.Ordinal)) // 슬롯 ID 일치 확인
            {
                saveData = null; // 다른 슬롯 데이터 제거
                errorMessage = "저장 파일의 슬롯 ID가 파일 이름과 일치하지 않습니다."; // 슬롯 불일치 오류 설정
                return false; // 검사 실패
            }

            errorMessage = string.Empty; // 오류 내용 초기화
            return true; // 검사 성공
        }
        catch (Exception exception) // JSON 또는 파일 예외 확인
        {
            saveData = null; // 실패 데이터 제거
            errorMessage = exception.Message; // 예외 내용 설정
            return false; // 검사 실패
        }
    }

    private static bool TryValidateSlotId(string slotId, out string validSlotId, out string errorMessage) // 슬롯 ID 검사
    {
        validSlotId = string.IsNullOrWhiteSpace(slotId) ? string.Empty : slotId.Trim(); // 슬롯 ID 공백 정리

        if (string.IsNullOrEmpty(validSlotId)) // 빈 슬롯 ID 확인
        {
            errorMessage = "저장 슬롯 ID가 비어 있습니다."; // 빈 슬롯 오류 설정
            return false; // 검사 실패
        }

        if (validSlotId.Length > MaximumSlotIdLength) // 슬롯 ID 길이 확인
        {
            errorMessage = $"저장 슬롯 ID는 {MaximumSlotIdLength}자 이하여야 합니다."; // 길이 오류 설정
            return false; // 검사 실패
        }

        for (int index = 0; index < validSlotId.Length; index++) // 모든 문자 반복
        {
            char currentCharacter = validSlotId[index]; // 현재 문자 가져오기
            bool isLetter = char.IsLetterOrDigit(currentCharacter); // 영문자와 숫자 확인
            bool isAllowedSymbol = currentCharacter == '_' || currentCharacter == '-'; // 허용 기호 확인

            if (!isLetter && !isAllowedSymbol) // 허용되지 않은 문자 확인
            {
                errorMessage = "저장 슬롯 ID에는 문자, 숫자, 밑줄과 하이픈만 사용할 수 있습니다."; // 문자 오류 설정
                return false; // 검사 실패
            }
        }

        errorMessage = string.Empty; // 오류 내용 초기화
        return true; // 검사 성공
    }

    private static void RestoreBackupOrRemoveInvalidMain(string mainFilePath, string backupFilePath) // 저장 실패 후 파일 복구
    {
        try // 복구 예외 처리
        {
            if (File.Exists(backupFilePath)) // 백업 파일 존재 확인
            {
                File.Copy(backupFilePath, mainFilePath, true); // 백업 파일로 기본 파일 복구
                return; // 복구 종료
            }

            DeleteFileIfExists(mainFilePath); // 복구 불가능한 기본 파일 정리
        }
        catch (Exception) // 복구 실패 예외 무시
        {
        }
    }

    private static void DeleteTemporaryFile(string temporaryFilePath) // 임시 파일 안전 정리
    {
        try // 임시 파일 삭제 예외 처리
        {
            DeleteFileIfExists(temporaryFilePath); // 임시 파일 삭제
        }
        catch (Exception) // 정리 실패 예외 무시
        {
        }
    }

    private static void DeleteFileIfExists(string filePath) // 파일 존재 시 삭제
    {
        if (File.Exists(filePath)) // 파일 존재 확인
        {
            File.Delete(filePath); // 파일 삭제
        }
    }
}