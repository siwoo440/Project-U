#if UNITY_EDITOR
using System; // 기본 형식 기능
using System.Collections.Generic; // 목록 기능
using System.IO; // 파일 읽기와 쓰기 기능
using System.Linq; // 목록 검색과 정렬 기능
using System.Text; // 문자열과 UTF-8 인코딩 기능
using System.Text.RegularExpressions; // 필드 선언 분석 기능
using UnityEditor; // Unity Editor 기능
using UnityEngine; // Unity 기본 기능

public static class ProjectUInspectorTooltipApplier // Project U Inspector Tooltip 일괄 적용 도구
{
    private const string RuntimeScriptRoot = "Assets/_ProjectU/Scripts"; // Tooltip 적용 대상 런타임 스크립트 루트
    private const string MenuRoot = "Tools/Project U/Inspector Tooltips/"; // Unity 상단 메뉴 경로
    private const int MaximumDeclarationSearchLines = 80; // 여러 줄 필드 선언 최대 검색 범위

    private static readonly Regex FieldNameRegex = new Regex(
        @"\b(?<name>[A-Za-z_]\w*)\s*(?:=|;)",
        RegexOptions.Compiled); // 필드 이름 검색 정규식

    private static readonly Regex PublicFieldRegex = new Regex(
        @"\bpublic\s+"
        + @"(?:(?:new|unsafe)\s+)*"
        + @"(?<type>[\w\.\<\>\,\[\]\?\s]+?)\s+"
        + @"(?<name>[A-Za-z_]\w*)\s*(?:=|;)",
        RegexOptions.Compiled); // public 직렬화 필드 검색 정규식

    [MenuItem(MenuRoot + "1. Preview Missing Tooltips")] // Tooltip 적용 예상 결과 확인 메뉴
    private static void PreviewMissingTooltips() // 파일 변경 없이 Tooltip 적용 대상 미리보기
    {
        TooltipProcessResult result = ProcessScripts(false); // Tooltip 미리보기 검사 실행
        PrintProcessResult(result, false); // 미리보기 결과 출력
    }

    [MenuItem(MenuRoot + "2. Apply Tooltips From Korean Comments")] // Tooltip 실제 적용 메뉴
    private static void ApplyTooltips() // 직렬화 필드에 Tooltip 일괄 적용
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Project U Inspector Tooltip 적용",
            "Assets/_ProjectU/Scripts 아래의 런타임 스크립트를 전부 검사합니다.\n\n"
            + "이번 버전은 다음 필드를 처리합니다.\n"
            + "• [SerializeField]\n"
            + "• [SerializeReference]\n"
            + "• Inspector에 표시되는 public 필드\n"
            + "• 중첩 [Serializable] 데이터의 public 필드\n\n"
            + "기존 Tooltip은 유지하며 누락된 Tooltip만 추가합니다.",
            "적용",
            "취소"); // Tooltip 적용 확인 창 표시

        if (!confirmed) // 사용자 취소 확인
        {
            return; // Tooltip 적용 중단
        }

        TooltipProcessResult result = ProcessScripts(true); // Tooltip 실제 적용 실행
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 변경된 스크립트 다시 불러오기
        PrintProcessResult(result, true); // 실제 적용 결과 출력
    }

    [MenuItem(MenuRoot + "3. Validate Tooltip Coverage")] // Tooltip 누락 검사 메뉴
    private static void ValidateTooltipCoverage() // 전체 직렬화 필드 Tooltip 적용 여부 검사
    {
        List<string> missingEntries = FindMissingTooltipEntries(); // Tooltip 누락 필드 검색

        if (missingEntries.Count == 0) // 누락 항목 존재 확인
        {
            Debug.Log(
                "[Project U Tooltip] 모든 Inspector 직렬화 필드에 Tooltip이 적용되어 있습니다."); // 검사 성공 출력

            EditorUtility.DisplayDialog(
                "Tooltip 검사 완료",
                "모든 Inspector 직렬화 필드에 Tooltip이 적용되어 있습니다.",
                "확인"); // 검사 성공 창 표시

            return; // 검사 종료
        }

        StringBuilder builder = new StringBuilder(); // 누락 결과 문자열 생성기

        builder.AppendLine(
            $"[Project U Tooltip] Tooltip이 없는 Inspector 직렬화 필드가 {missingEntries.Count}개 있습니다."); // 누락 개수 추가

        for (int index = 0; index < missingEntries.Count; index++) // 전체 누락 항목 순회
        {
            builder.AppendLine(missingEntries[index]); // 현재 누락 항목 추가
        }

        Debug.LogWarning(builder.ToString()); // 누락 결과 출력

        EditorUtility.DisplayDialog(
            "Tooltip 누락 발견",
            $"Tooltip이 없는 Inspector 직렬화 필드가 {missingEntries.Count}개 있습니다.\n"
            + "파일과 줄 번호는 Console에서 확인하십시오.",
            "확인"); // 누락 안내 창 표시
    }

    private static TooltipProcessResult ProcessScripts(bool applyChanges) // 전체 스크립트 Tooltip 처리
    {
        string[] scriptPaths = FindRuntimeScriptPaths(); // 대상 런타임 스크립트 검색
        TooltipProcessResult result = new TooltipProcessResult(); // 전체 처리 결과 생성

        result.ScannedFileCount = scriptPaths.Length; // 검사 파일 수 저장

        try // 진행 표시 안전 처리
        {
            for (int fileIndex = 0; fileIndex < scriptPaths.Length; fileIndex++) // 전체 스크립트 순회
            {
                string scriptPath = scriptPaths[fileIndex]; // 현재 스크립트 경로
                float progress = scriptPaths.Length == 0
                    ? 1f
                    : (float)fileIndex / scriptPaths.Length; // 현재 진행률 계산

                EditorUtility.DisplayProgressBar(
                    "Project U Inspector Tooltip",
                    scriptPath,
                    progress); // 현재 처리 파일 표시

                string originalText = File.ReadAllText(scriptPath); // 현재 스크립트 읽기
                TooltipFileResult fileResult = AddMissingTooltips(
                    scriptPath,
                    originalText); // 현재 파일 Tooltip 추가 결과 생성

                result.SerializedFieldCount += fileResult.SerializedFieldCount; // 직렬화 필드 수 누적
                result.ExistingTooltipCount += fileResult.ExistingTooltipCount; // 기존 Tooltip 수 누적
                result.AddedTooltipCount += fileResult.AddedTooltipCount; // 추가 Tooltip 수 누적
                result.CommentTooltipCount += fileResult.CommentTooltipCount; // 주석 기반 Tooltip 수 누적
                result.FallbackTooltipCount += fileResult.FallbackTooltipCount; // 기본 Tooltip 수 누적
                result.ExplicitSerializedFieldCount += fileResult.ExplicitSerializedFieldCount; // 명시적 직렬화 필드 수 누적
                result.PublicSerializedFieldCount += fileResult.PublicSerializedFieldCount; // public 직렬화 필드 수 누적

                if (!fileResult.Changed) // 현재 파일 변경 여부 확인
                {
                    continue; // 파일 저장 생략
                }

                result.ChangedFiles.Add(scriptPath); // 변경 파일 경로 저장

                if (!applyChanges) // 미리보기 모드 확인
                {
                    continue; // 실제 파일 저장 생략
                }

                string newline = DetectNewline(originalText); // 기존 줄바꿈 형식 확인
                string outputText = string.Join(newline, fileResult.Lines); // 변경 코드 조합

                if (originalText.EndsWith("\n", StringComparison.Ordinal)
                    && !outputText.EndsWith(newline, StringComparison.Ordinal)) // 기존 마지막 줄바꿈 확인
                {
                    outputText += newline; // 마지막 줄바꿈 유지
                }

                File.WriteAllText(
                    scriptPath,
                    outputText,
                    new UTF8Encoding(false)); // UTF-8 BOM 없음 형식으로 저장
            }
        }
        finally // 처리 종료 후 진행 표시 제거
        {
            EditorUtility.ClearProgressBar(); // 진행 표시 닫기
        }

        return result; // 전체 처리 결과 반환
    }

    private static TooltipFileResult AddMissingTooltips(
        string scriptPath,
        string originalText) // 단일 스크립트 Tooltip 추가
    {
        string normalizedText = originalText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n"); // 줄바꿈 형식 통일

        List<string> lines = normalizedText.Split('\n').ToList(); // 줄 단위 코드 목록 생성
        List<SerializedFieldCandidate> candidates = FindSerializedFieldCandidates(
            scriptPath,
            lines); // 현재 파일 직렬화 필드 검색

        TooltipFileResult result = new TooltipFileResult(lines); // 단일 파일 결과 생성

        result.SerializedFieldCount = candidates.Count; // 전체 직렬화 필드 수 저장
        result.ExplicitSerializedFieldCount = candidates.Count(
            candidate => candidate.IsExplicitlySerialized); // 명시적 직렬화 필드 수 저장
        result.PublicSerializedFieldCount = candidates.Count(
            candidate => !candidate.IsExplicitlySerialized); // public 직렬화 필드 수 저장
        result.ExistingTooltipCount = candidates.Count(
            candidate => candidate.HasTooltip); // 기존 Tooltip 수 저장

        for (int candidateIndex = candidates.Count - 1; candidateIndex >= 0; candidateIndex--) // 아래 필드부터 역순 처리
        {
            SerializedFieldCandidate candidate = candidates[candidateIndex]; // 현재 직렬화 필드 조회

            if (candidate.HasTooltip) // 기존 Tooltip 존재 확인
            {
                continue; // 중복 Tooltip 추가 방지
            }

            string tooltipText = ExtractTrailingComment(
                lines,
                candidate.DeclarationStartIndex,
                candidate.DeclarationEndIndex); // 기존 필드 설명 주석 추출

            bool usedFallback = string.IsNullOrWhiteSpace(tooltipText); // 기본 설명 사용 여부 계산

            if (usedFallback) // 기존 설명 주석 없음 확인
            {
                tooltipText = BuildFallbackTooltip(candidate); // 필드 정보 기반 기본 설명 생성
            }

            string indentation = GetIndentation(
                lines[candidate.AttributeStartIndex]); // 필드 속성 또는 선언 줄 들여쓰기 추출

            string escapedTooltip = EscapeTooltipText(tooltipText); // C# 문자열 특수 문자 처리
            string tooltipLine =
                $"{indentation}[Tooltip(\"{escapedTooltip}\")]"; // Tooltip 속성 코드 생성

            lines.Insert(
                candidate.AttributeStartIndex,
                tooltipLine); // 첫 속성 또는 필드 선언 위에 Tooltip 추가

            result.AddedTooltipCount++; // 추가 Tooltip 수 증가

            if (usedFallback) // 기본 설명 사용 여부 확인
            {
                result.FallbackTooltipCount++; // 기본 설명 Tooltip 수 증가
            }
            else // 기존 한국어 주석 사용
            {
                result.CommentTooltipCount++; // 주석 기반 Tooltip 수 증가
            }
        }

        return result; // 단일 파일 처리 결과 반환
    }

    private static List<SerializedFieldCandidate> FindSerializedFieldCandidates(
        string scriptPath,
        List<string> lines) // 현재 스크립트의 Inspector 직렬화 필드 검색
    {
        List<SerializedFieldCandidate> candidates = new List<SerializedFieldCandidate>(); // 검색 결과 목록
        HashSet<int> processedDeclarationEnds = new HashSet<int>(); // 중복 필드 처리 방지 목록

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++) // 전체 코드 줄 순회
        {
            if (!CouldStartSerializedField(lines, lineIndex)) // 직렬화 필드 시작 가능 여부 확인
            {
                continue; // 일반 코드 줄 제외
            }

            int attributeStartIndex = FindAttributeBlockStart(
                lines,
                lineIndex); // 현재 필드의 첫 속성 줄 검색

            int declarationStartIndex = FindDeclarationStart(
                lines,
                attributeStartIndex); // 실제 필드 선언 시작 줄 검색

            if (declarationStartIndex < 0) // 선언 시작 검색 실패 확인
            {
                continue; // 현재 후보 제외
            }

            int declarationEndIndex = FindFieldDeclarationEnd(
                lines,
                declarationStartIndex); // 실제 필드 선언 마지막 줄 검색

            if (declarationEndIndex < declarationStartIndex) // 선언 마지막 줄 검색 실패 확인
            {
                continue; // 현재 후보 제외
            }

            if (!processedDeclarationEnds.Add(declarationEndIndex)) // 이미 처리한 필드 선언 확인
            {
                continue; // 중복 후보 제외
            }

            string attributeText = JoinLines(
                lines,
                attributeStartIndex,
                declarationStartIndex); // 필드 속성 문자열 조합

            string declarationText = JoinDeclarationLines(
                lines,
                declarationStartIndex,
                declarationEndIndex); // 필드 선언 문자열 조합

            bool isExplicitlySerialized = ContainsExplicitSerializationAttribute(
                attributeText,
                declarationText); // SerializeField 또는 SerializeReference 여부 확인

            bool isPublicSerialized = IsPublicSerializableField(
                attributeText,
                declarationText); // public 직렬화 필드 여부 확인

            if (!isExplicitlySerialized && !isPublicSerialized) // Inspector 비노출 필드 확인
            {
                continue; // Tooltip 적용 대상 제외
            }

            if (ContainsAttribute(
                attributeText,
                declarationText,
                "HideInInspector")
                || ContainsAttribute(
                    attributeText,
                    declarationText,
                    "NonSerialized")) // Inspector 숨김 또는 직렬화 제외 속성 확인
            {
                continue; // Tooltip 적용 대상 제외
            }

            if (IsUnsupportedFieldDeclaration(declarationText)) // 직렬화되지 않는 필드 형식 확인
            {
                continue; // Tooltip 적용 대상 제외
            }

            string fieldName = ExtractFieldName(declarationText); // 현재 필드 이름 추출

            if (string.IsNullOrWhiteSpace(fieldName)) // 필드 이름 분석 실패 확인
            {
                Debug.LogWarning(
                    $"[Project U Tooltip] 필드 이름을 분석하지 못했습니다: "
                    + $"{scriptPath}:{declarationStartIndex + 1}"); // 필드 분석 실패 출력

                continue; // 현재 후보 제외
            }

            bool hasTooltip = ContainsAttribute(
                attributeText,
                declarationText,
                "Tooltip"); // 기존 Tooltip 존재 여부 확인

            candidates.Add(
                new SerializedFieldCandidate(
                    attributeStartIndex,
                    declarationStartIndex,
                    declarationEndIndex,
                    fieldName,
                    ExtractFieldType(declarationText, fieldName),
                    isExplicitlySerialized,
                    hasTooltip)); // 직렬화 필드 후보 저장

            lineIndex = declarationEndIndex; // 현재 여러 줄 선언 이후로 이동
        }

        return candidates; // 전체 직렬화 필드 후보 반환
    }

    private static bool CouldStartSerializedField(
        List<string> lines,
        int lineIndex) // 현재 줄의 직렬화 필드 시작 가능 여부 확인
    {
        string trimmedLine = lines[lineIndex].TrimStart(); // 현재 줄 앞 공백 제거

        if (string.IsNullOrWhiteSpace(trimmedLine)) // 빈 줄 확인
        {
            return false; // 필드 시작 아님 반환
        }

        if (trimmedLine.StartsWith("//", StringComparison.Ordinal)
            || trimmedLine.StartsWith("/*", StringComparison.Ordinal)
            || trimmedLine.StartsWith("*", StringComparison.Ordinal)) // 주석 줄 확인
        {
            return false; // 필드 시작 아님 반환
        }

        if (trimmedLine.StartsWith("[", StringComparison.Ordinal)) // 속성 줄 확인
        {
            string attributeBlock = CollectForwardAttributeBlock(
                lines,
                lineIndex); // 현재 위치부터 속성 블록 수집

            return attributeBlock.IndexOf(
                "SerializeField",
                StringComparison.Ordinal) >= 0
                || attributeBlock.IndexOf(
                    "SerializeReference",
                    StringComparison.Ordinal) >= 0
                || CouldLeadToPublicField(
                    lines,
                    lineIndex); // 명시적 직렬화 또는 뒤쪽 public 필드 여부 반환
        }

        return StartsWithPublicMember(trimmedLine); // public 멤버 시작 여부 반환
    }

    private static bool CouldLeadToPublicField(
        List<string> lines,
        int attributeLineIndex) // 속성 블록 뒤 public 필드 여부 확인
    {
        int declarationStartIndex = FindDeclarationStart(
            lines,
            attributeLineIndex); // 속성 뒤 선언 시작 줄 검색

        if (declarationStartIndex < 0) // 선언 검색 실패 확인
        {
            return false; // public 필드 아님 반환
        }

        return StartsWithPublicMember(
            lines[declarationStartIndex].TrimStart()); // public 선언 시작 여부 반환
    }

    private static bool StartsWithPublicMember(string trimmedLine) // public 멤버 시작 여부 확인
    {
        return trimmedLine.StartsWith("public ", StringComparison.Ordinal)
            || trimmedLine.StartsWith("public\t", StringComparison.Ordinal); // public 접근 제한자 여부 반환
    }

    private static int FindAttributeBlockStart(
        List<string> lines,
        int currentLineIndex) // 현재 필드의 첫 속성 줄 검색
    {
        int startIndex = currentLineIndex; // 현재 필드 시작 줄 설정

        while (startIndex > 0) // 현재 필드 바로 위의 독립 속성 줄 검사
        {
            string previousLine = lines[startIndex - 1]; // 바로 이전 원문 줄 조회

            if (!IsAttributeOnlyLine(previousLine)) // 속성만 존재하는 줄인지 확인
            {
                break; // 이전 필드 선언 또는 일반 코드 도달로 검색 종료
            }

            startIndex--; // 현재 필드에 속한 첫 속성 줄로 이동
        }

        return startIndex; // 현재 필드의 첫 속성 또는 선언 줄 반환
    }

    private static int FindDeclarationStart(
        List<string> lines,
        int attributeStartIndex) // 속성 블록 이후 실제 필드 선언 시작 줄 검색
    {
        int maximumIndex = Mathf.Min(
            lines.Count - 1,
            attributeStartIndex + MaximumDeclarationSearchLines); // 최대 검색 줄 계산

        for (int index = attributeStartIndex; index <= maximumIndex; index++) // 속성 시작부터 순회
        {
            string line = lines[index]; // 현재 코드 줄
            string trimmedLine = line.Trim(); // 현재 줄 공백 제거

            if (string.IsNullOrWhiteSpace(trimmedLine)
                || trimmedLine.StartsWith("//", StringComparison.Ordinal)
                || trimmedLine.StartsWith("/*", StringComparison.Ordinal)
                || trimmedLine.StartsWith("*", StringComparison.Ordinal)) // 빈 줄과 주석 줄 확인
            {
                continue; // 다음 줄 검사
            }

            if (trimmedLine.StartsWith("[", StringComparison.Ordinal)) // 속성 시작 줄 확인
            {
                string remainingText = GetTextAfterLeadingAttributes(line); // 모든 선행 속성 뒤 코드 추출

                if (string.IsNullOrWhiteSpace(remainingText)
                    || remainingText.StartsWith("//", StringComparison.Ordinal)) // 속성만 있는 줄 확인
                {
                    continue; // 다음 속성 또는 필드 선언 줄 검사
                }

                return index; // 속성과 필드 선언이 함께 있는 현재 줄 반환
            }

            return index; // 첫 일반 코드 줄을 선언 시작으로 반환
        }

        return -1; // 선언 시작 검색 실패 반환
    }

    private static int FindFieldDeclarationEnd(
        List<string> lines,
        int declarationStartIndex) // 여러 줄 필드 선언 마지막 줄 검색
    {
        int maximumIndex = Mathf.Min(
            lines.Count - 1,
            declarationStartIndex + MaximumDeclarationSearchLines); // 최대 검색 줄 계산

        int parenthesisDepth = 0; // 소괄호 깊이
        int bracketDepth = 0; // 대괄호 깊이
        int braceDepth = 0; // 중괄호 깊이
        bool insideString = false; // 문자열 내부 상태
        bool insideCharacter = false; // 문자 리터럴 내부 상태
        bool escaped = false; // 이스케이프 상태
        bool insideBlockComment = false; // 블록 주석 내부 상태

        for (int lineIndex = declarationStartIndex; lineIndex <= maximumIndex; lineIndex++) // 선언 시작부터 순회
        {
            string line = lines[lineIndex]; // 현재 코드 줄

            for (int characterIndex = 0; characterIndex < line.Length; characterIndex++) // 현재 줄 문자 순회
            {
                char current = line[characterIndex]; // 현재 문자
                char next = characterIndex + 1 < line.Length
                    ? line[characterIndex + 1]
                    : '\0'; // 다음 문자

                if (insideBlockComment) // 블록 주석 내부 확인
                {
                    if (current == '*' && next == '/') // 블록 주석 종료 확인
                    {
                        insideBlockComment = false; // 블록 주석 상태 해제
                        characterIndex++; // 종료 기호 다음 문자 건너뛰기
                    }

                    continue; // 블록 주석 내용 제외
                }

                if (!insideString
                    && !insideCharacter
                    && current == '/'
                    && next == '*') // 블록 주석 시작 확인
                {
                    insideBlockComment = true; // 블록 주석 상태 적용
                    characterIndex++; // 시작 기호 다음 문자 건너뛰기
                    continue; // 다음 문자 검사
                }

                if (!insideString
                    && !insideCharacter
                    && current == '/'
                    && next == '/') // 줄 주석 시작 확인
                {
                    break; // 현재 줄 나머지 문자 제외
                }

                if (escaped) // 이전 문자 이스케이프 확인
                {
                    escaped = false; // 이스케이프 상태 해제
                    continue; // 현재 문자 특수 처리 생략
                }

                if ((insideString || insideCharacter) && current == '\\') // 문자열 내부 이스케이프 확인
                {
                    escaped = true; // 다음 문자 이스케이프 기록
                    continue; // 다음 문자 검사
                }

                if (!insideCharacter && current == '"') // 문자열 따옴표 확인
                {
                    insideString = !insideString; // 문자열 내부 상태 전환
                    continue; // 다음 문자 검사
                }

                if (!insideString && current == '\'') // 문자 리터럴 따옴표 확인
                {
                    insideCharacter = !insideCharacter; // 문자 리터럴 상태 전환
                    continue; // 다음 문자 검사
                }

                if (insideString || insideCharacter) // 문자열 또는 문자 내부 확인
                {
                    continue; // 구조 문자 처리 생략
                }

                switch (current) // 현재 구조 문자 분기
                {
                    case '(': // 소괄호 시작
                        parenthesisDepth++; // 소괄호 깊이 증가
                        break; // 문자 처리 종료

                    case ')': // 소괄호 종료
                        parenthesisDepth = Mathf.Max(0, parenthesisDepth - 1); // 소괄호 깊이 감소
                        break; // 문자 처리 종료

                    case '[': // 대괄호 시작
                        bracketDepth++; // 대괄호 깊이 증가
                        break; // 문자 처리 종료

                    case ']': // 대괄호 종료
                        bracketDepth = Mathf.Max(0, bracketDepth - 1); // 대괄호 깊이 감소
                        break; // 문자 처리 종료

                    case '{': // 중괄호 시작
                        braceDepth++; // 중괄호 깊이 증가
                        break; // 문자 처리 종료

                    case '}': // 중괄호 종료
                        braceDepth = Mathf.Max(0, braceDepth - 1); // 중괄호 깊이 감소
                        break; // 문자 처리 종료

                    case ';': // 필드 선언 종료 가능 문자
                        if (parenthesisDepth == 0
                            && bracketDepth == 0
                            && braceDepth == 0) // 모든 내부 구조 종료 확인
                        {
                            return lineIndex; // 현재 줄을 선언 마지막 줄로 반환
                        }

                        break; // 내부 세미콜론 처리 종료
                }
            }
        }

        return -1; // 선언 마지막 줄 검색 실패 반환
    }

    private static bool ContainsExplicitSerializationAttribute(
        string attributeText,
        string declarationText) // 명시적 Unity 직렬화 속성 포함 여부 확인
    {
        return ContainsAttribute(
            attributeText,
            declarationText,
            "SerializeField")
            || ContainsAttribute(
                attributeText,
                declarationText,
                "SerializeReference"); // 명시적 직렬화 속성 여부 반환
    }

    private static bool IsPublicSerializableField(
        string attributeText,
        string declarationText) // Inspector에 노출되는 public 필드 여부 확인
    {
        string codeText = RemoveAttributes(declarationText).Trim(); // 필드 선언에서 속성 코드 제거

        if (!StartsWithPublicMember(codeText)) // public 접근 제한자 확인
        {
            return false; // public 필드 아님 반환
        }

        if (ContainsAttribute(
            attributeText,
            declarationText,
            "NonSerialized")
            || ContainsAttribute(
                attributeText,
                declarationText,
                "HideInInspector")) // 직렬화 또는 Inspector 제외 속성 확인
        {
            return false; // Inspector 비노출 반환
        }

        if (ContainsModifier(codeText, "static")
            || ContainsModifier(codeText, "const")
            || ContainsModifier(codeText, "readonly")
            || ContainsModifier(codeText, "event")
            || ContainsModifier(codeText, "delegate")) // Unity 직렬화 제외 멤버 확인
        {
            return false; // 직렬화 필드 아님 반환
        }

        if (codeText.IndexOf("=>", StringComparison.Ordinal) >= 0) // 식 본문 프로퍼티 확인
        {
            return false; // 프로퍼티 제외
        }

        int firstSemicolonIndex = codeText.IndexOf(';'); // 첫 세미콜론 위치 검색
        int firstOpeningBraceIndex = codeText.IndexOf('{'); // 첫 중괄호 위치 검색

        if (firstOpeningBraceIndex >= 0
            && (firstSemicolonIndex < 0
                || firstOpeningBraceIndex < firstSemicolonIndex)) // 자동 프로퍼티 본문 여부 확인
        {
            return false; // 프로퍼티 제외
        }

        int firstParenthesisIndex = codeText.IndexOf('('); // 첫 소괄호 위치 검색
        int assignmentIndex = codeText.IndexOf('='); // 값 대입 위치 검색

        if (firstParenthesisIndex >= 0
            && (assignmentIndex < 0
                || firstParenthesisIndex < assignmentIndex)) // 메서드 또는 생성자 선언 여부 확인
        {
            return false; // 메서드 제외
        }

        return PublicFieldRegex.IsMatch(codeText); // public 필드 정규식 결과 반환
    }

    private static bool IsUnsupportedFieldDeclaration(string declarationText) // Unity 직렬화 제외 선언 확인
    {
        string codeText = RemoveAttributes(declarationText).Trim(); // 속성 제거 필드 선언

        if (ContainsModifier(codeText, "static")
            || ContainsModifier(codeText, "const")
            || ContainsModifier(codeText, "readonly")
            || ContainsModifier(codeText, "event")
            || ContainsModifier(codeText, "delegate")) // 지원하지 않는 필드 한정자 확인
        {
            return true; // 직렬화 제외 반환
        }

        if (codeText.IndexOf("=>", StringComparison.Ordinal) >= 0) // 식 본문 프로퍼티 확인
        {
            return true; // 직렬화 제외 반환
        }

        return false; // 일반 필드 반환
    }

    private static bool ContainsModifier(
        string codeText,
        string modifier) // 특정 필드 한정자 포함 여부 확인
    {
        return Regex.IsMatch(
            codeText,
            $@"\b{Regex.Escape(modifier)}\b"); // 단어 단위 한정자 검색 결과 반환
    }

    private static bool ContainsAttribute(
        string attributeText,
        string declarationText,
        string attributeName) // 특정 속성 포함 여부 확인
    {
        string combinedText = attributeText + "\n" + declarationText; // 속성과 선언 문자열 조합

        return Regex.IsMatch(
            combinedText,
            $@"\[\s*(?:field\s*:\s*)?"
            + $@"(?:UnityEngine\.)?{Regex.Escape(attributeName)}"
            + @"(?:Attribute)?\b",
            RegexOptions.IgnoreCase); // 속성 이름 포함 여부 반환
    }

    private static string CollectForwardAttributeBlock(
        List<string> lines,
        int startIndex) // 현재 위치부터 현재 필드의 속성 블록 수집
    {
        StringBuilder builder = new StringBuilder(); // 속성 블록 문자열 생성기
        int maximumIndex = Mathf.Min(
            lines.Count - 1,
            startIndex + MaximumDeclarationSearchLines); // 최대 검색 줄 계산

        for (int index = startIndex; index <= maximumIndex; index++) // 현재 위치부터 순회
        {
            string line = lines[index]; // 현재 원문 줄 조회
            string trimmedLine = line.Trim(); // 현재 줄 공백 제거

            if (string.IsNullOrWhiteSpace(trimmedLine)) // 빈 줄 확인
            {
                continue; // 다음 줄 검사
            }

            if (!trimmedLine.StartsWith("[", StringComparison.Ordinal)) // 속성 시작 줄 여부 확인
            {
                break; // 현재 필드 속성 블록 수집 종료
            }

            builder.AppendLine(trimmedLine); // 현재 속성 또는 속성 포함 선언 줄 추가

            if (!IsAttributeOnlyLine(line)) // 같은 줄에 필드 선언이 함께 있는지 확인
            {
                break; // 다음 필드의 SerializeField 줄까지 수집하지 않도록 종료
            }
        }

        return builder.ToString(); // 현재 필드의 속성 블록 반환
    }

    private static bool IsAttributeOnlyLine(string line) // 현재 줄에 속성만 존재하는지 확인
    {
        if (string.IsNullOrWhiteSpace(line)) // 빈 줄 확인
        {
            return false; // 속성 줄 아님 반환
        }

        string trimmedLine = line.TrimStart(); // 앞쪽 공백 제거

        if (!trimmedLine.StartsWith("[", StringComparison.Ordinal)) // 속성 시작 여부 확인
        {
            return false; // 속성 줄 아님 반환
        }

        string remainingText = GetTextAfterLeadingAttributes(trimmedLine); // 모든 선행 속성 뒤 코드 추출

        return string.IsNullOrWhiteSpace(remainingText)
            || remainingText.StartsWith("//", StringComparison.Ordinal); // 속성 뒤에 주석만 있으면 속성 전용 줄 반환
    }

    private static string GetTextAfterLeadingAttributes(string line) // 한 줄의 모든 선행 속성 뒤 코드 추출
    {
        if (string.IsNullOrWhiteSpace(line)) // 빈 줄 확인
        {
            return string.Empty; // 빈 문자열 반환
        }

        string remainingText = line.TrimStart(); // 앞쪽 공백 제거

        while (remainingText.StartsWith("[", StringComparison.Ordinal)) // 선행 속성이 남아 있는 동안 반복
        {
            int closingBracketIndex = FindMatchingAttributeBracket(
                remainingText,
                0); // 현재 속성의 닫힘 대괄호 검색

            if (closingBracketIndex < 0) // 잘못된 속성 선언 확인
            {
                return remainingText; // 분석 불가 원문 반환
            }

            remainingText = remainingText
                .Substring(closingBracketIndex + 1)
                .TrimStart(); // 현재 속성 제거 후 남은 코드 저장
        }

        return remainingText; // 모든 선행 속성 뒤 코드 반환
    }

    private static int FindMatchingAttributeBracket(
        string line,
        int openingBracketIndex) // 지정 속성의 닫힘 대괄호 검색
    {
        if (string.IsNullOrEmpty(line)
            || openingBracketIndex < 0
            || openingBracketIndex >= line.Length
            || line[openingBracketIndex] != '[') // 시작 위치와 대괄호 확인
        {
            return -1; // 검색 실패 반환
        }

        bool insideString = false; // 문자열 내부 상태
        bool insideCharacter = false; // 문자 리터럴 내부 상태
        bool escaped = false; // 이스케이프 상태
        int bracketDepth = 0; // 대괄호 깊이

        for (int index = openingBracketIndex; index < line.Length; index++) // 시작 대괄호부터 문자 순회
        {
            char current = line[index]; // 현재 문자

            if (escaped) // 이전 문자 이스케이프 확인
            {
                escaped = false; // 이스케이프 상태 해제
                continue; // 현재 문자 특수 처리 생략
            }

            if ((insideString || insideCharacter)
                && current == '\\') // 문자열 또는 문자 내부 이스케이프 확인
            {
                escaped = true; // 다음 문자 이스케이프 기록
                continue; // 다음 문자 검사
            }

            if (!insideCharacter && current == '"') // 문자열 따옴표 확인
            {
                insideString = !insideString; // 문자열 내부 상태 전환
                continue; // 다음 문자 검사
            }

            if (!insideString && current == '\'') // 문자 리터럴 따옴표 확인
            {
                insideCharacter = !insideCharacter; // 문자 리터럴 내부 상태 전환
                continue; // 다음 문자 검사
            }

            if (insideString || insideCharacter) // 문자열 또는 문자 내부 확인
            {
                continue; // 대괄호 구조 검사 생략
            }

            if (current == '[') // 대괄호 시작 확인
            {
                bracketDepth++; // 대괄호 깊이 증가
                continue; // 다음 문자 검사
            }

            if (current != ']') // 대괄호 종료 문자가 아닌지 확인
            {
                continue; // 다음 문자 검사
            }

            bracketDepth--; // 대괄호 깊이 감소

            if (bracketDepth == 0) // 현재 속성 블록 종료 확인
            {
                return index; // 닫힘 대괄호 위치 반환
            }
        }

        return -1; // 닫힘 대괄호 검색 실패 반환
    }

    private static string ExtractTrailingComment(
        List<string> lines,
        int startIndex,
        int endIndex) // 필드 선언의 기존 끝 주석 추출
    {
        if (endIndex < startIndex
            || startIndex < 0
            || endIndex >= lines.Count) // 선언 줄 범위 확인
        {
            return string.Empty; // 빈 설명 반환
        }

        for (int index = endIndex; index >= startIndex; index--) // 선언 마지막 줄부터 역순 검사
        {
            int commentIndex = FindLineCommentOutsideStrings(lines[index]); // 줄 주석 위치 검색

            if (commentIndex < 0) // 현재 줄 주석 없음 확인
            {
                continue; // 이전 줄 검사
            }

            string commentText = lines[index]
                .Substring(commentIndex + 2)
                .Trim(); // 주석 기호 제거와 공백 정리

            if (string.IsNullOrWhiteSpace(commentText)) // 빈 주석 확인
            {
                continue; // 이전 줄 검사
            }

            return NormalizeTooltipSentence(commentText); // Tooltip 문장 반환
        }

        return string.Empty; // 기존 설명 주석 없음 반환
    }

    private static string BuildFallbackTooltip(
        SerializedFieldCandidate candidate) // 주석 없는 필드의 기본 Tooltip 생성
    {
        string readableName = ObjectNames.NicifyVariableName(
            candidate.FieldName); // 변수 이름 읽기 형식 변환

        string fieldType = candidate.FieldType; // 현재 필드 형식

        if (fieldType == "bool"
            || fieldType == "System.Boolean") // bool 형식 확인
        {
            return $"{readableName} 기능의 사용 여부를 설정합니다."; // bool 기본 설명 반환
        }

        if (fieldType == "int"
            || fieldType == "float"
            || fieldType == "double"
            || fieldType == "long"
            || fieldType == "short"
            || fieldType == "uint") // 숫자 형식 확인
        {
            return $"{readableName}에 사용할 수치를 설정합니다."; // 숫자 기본 설명 반환
        }

        if (fieldType == "string"
            || fieldType == "System.String") // 문자열 형식 확인
        {
            return $"{readableName}에 표시하거나 사용할 문자열을 설정합니다."; // 문자열 기본 설명 반환
        }

        if (fieldType.EndsWith("[]", StringComparison.Ordinal)
            || fieldType.IndexOf("List<", StringComparison.Ordinal) >= 0
            || fieldType.IndexOf("IList<", StringComparison.Ordinal) >= 0) // 배열 또는 목록 확인
        {
            return $"{readableName}에 사용할 요소 목록을 설정합니다."; // 목록 기본 설명 반환
        }

        if (LooksLikeObjectReferenceType(fieldType)) // Unity 오브젝트 참조 형식 확인
        {
            return $"{readableName}에 사용할 Scene 오브젝트, 컴포넌트 또는 에셋을 연결합니다."; // 참조 기본 설명 반환
        }

        return $"{readableName}에 사용할 값을 설정합니다."; // 기타 형식 기본 설명 반환
    }

    private static bool LooksLikeObjectReferenceType(string fieldType) // Unity 참조 형식 추정
    {
        string[] referenceTypeKeywords =
        {
            "GameObject",
            "Transform",
            "RectTransform",
            "Camera",
            "Material",
            "Sprite",
            "Texture",
            "AudioClip",
            "Animator",
            "Collider",
            "Rigidbody",
            "LayerMask",
            "Button",
            "Image",
            "Slider",
            "ScrollRect",
            "Canvas",
            "TMP_",
            "TextMeshPro",
            "UI",
            "Manager",
            "Controller",
            "Data",
            "Prefab"
        }; // Unity 참조 가능성이 높은 형식 이름 목록

        for (int index = 0; index < referenceTypeKeywords.Length; index++) // 전체 형식 키워드 순회
        {
            if (fieldType.IndexOf(
                referenceTypeKeywords[index],
                StringComparison.OrdinalIgnoreCase) >= 0) // 형식 키워드 포함 여부 확인
            {
                return true; // Unity 참조 형식 추정 반환
            }
        }

        return false; // 일반 값 형식 반환
    }

    private static string ExtractFieldName(string declarationText) // 필드 선언에서 변수 이름 추출
    {
        string codeText = RemoveAttributes(declarationText); // 속성 코드 제거
        Match fieldMatch = FieldNameRegex.Match(codeText); // 필드 이름 검색

        return fieldMatch.Success
            ? fieldMatch.Groups["name"].Value
            : string.Empty; // 필드 이름 또는 빈 문자열 반환
    }

    private static string ExtractFieldType(
        string declarationText,
        string fieldName) // 필드 선언에서 형식 이름 추출
    {
        if (string.IsNullOrWhiteSpace(fieldName)) // 필드 이름 존재 확인
        {
            return string.Empty; // 빈 형식 반환
        }

        string codeText = RemoveAttributes(declarationText); // 속성 코드 제거
        int fieldNameIndex = codeText.IndexOf(
            fieldName,
            StringComparison.Ordinal); // 필드 이름 위치 검색

        if (fieldNameIndex < 0) // 필드 이름 위치 검색 실패 확인
        {
            return string.Empty; // 빈 형식 반환
        }

        string beforeFieldName = codeText
            .Substring(0, fieldNameIndex)
            .Trim(); // 필드 이름 이전 선언 추출

        string[] removableModifiers =
        {
            "public",
            "private",
            "protected",
            "internal",
            "new",
            "unsafe",
            "static",
            "readonly",
            "const",
            "volatile"
        }; // 제거할 필드 한정자 목록

        for (int index = 0; index < removableModifiers.Length; index++) // 전체 한정자 순회
        {
            beforeFieldName = Regex.Replace(
                beforeFieldName,
                $@"\b{Regex.Escape(removableModifiers[index])}\b",
                string.Empty); // 현재 한정자 제거
        }

        return Regex.Replace(
            beforeFieldName,
            @"\s+",
            " ").Trim(); // 형식 내부 연속 공백 정리
    }

    private static string RemoveAttributes(string declarationText) // 필드 선언에서 속성 코드 제거
    {
        return Regex.Replace(
            declarationText,
            @"\[[^\]]*\]",
            " "); // 대괄호 속성 블록 제거
    }

    private static string JoinLines(
        List<string> lines,
        int startIndex,
        int endIndex) // 지정 코드 줄 문자열 조합
    {
        if (startIndex < 0
            || endIndex < startIndex
            || endIndex >= lines.Count) // 줄 범위 확인
        {
            return string.Empty; // 빈 문자열 반환
        }

        StringBuilder builder = new StringBuilder(); // 코드 문자열 생성기

        for (int index = startIndex; index <= endIndex; index++) // 지정 범위 순회
        {
            builder.AppendLine(lines[index]); // 현재 코드 줄 추가
        }

        return builder.ToString(); // 조합 코드 반환
    }

    private static string JoinDeclarationLines(
        List<string> lines,
        int startIndex,
        int endIndex) // 여러 줄 필드 선언 한 줄 조합
    {
        if (startIndex < 0
            || endIndex < startIndex
            || endIndex >= lines.Count) // 선언 범위 확인
        {
            return string.Empty; // 빈 선언 반환
        }

        StringBuilder builder = new StringBuilder(); // 필드 선언 문자열 생성기

        for (int index = startIndex; index <= endIndex; index++) // 선언 범위 순회
        {
            string line = RemoveLineComment(lines[index]); // 줄 끝 주석 제거
            builder.Append(line); // 현재 선언 코드 추가
            builder.Append(' '); // 줄 구분 공백 추가
        }

        return builder.ToString(); // 조합 필드 선언 반환
    }

    private static string RemoveLineComment(string line) // 문자열 밖 줄 주석 제거
    {
        int commentIndex = FindLineCommentOutsideStrings(line); // 줄 주석 위치 검색

        if (commentIndex < 0) // 주석 없음 확인
        {
            return line; // 기존 줄 반환
        }

        return line.Substring(0, commentIndex); // 주석 이전 코드 반환
    }

    private static int FindLineCommentOutsideStrings(string line) // 문자열 밖 줄 주석 위치 검색
    {
        bool insideString = false; // 문자열 내부 상태
        bool insideCharacter = false; // 문자 리터럴 내부 상태
        bool escaped = false; // 이스케이프 상태

        for (int index = 0; index < line.Length - 1; index++) // 전체 문자 순회
        {
            char current = line[index]; // 현재 문자

            if (escaped) // 이전 문자 이스케이프 확인
            {
                escaped = false; // 이스케이프 상태 해제
                continue; // 현재 문자 특수 처리 생략
            }

            if ((insideString || insideCharacter)
                && current == '\\') // 문자열 내부 이스케이프 확인
            {
                escaped = true; // 다음 문자 이스케이프 기록
                continue; // 다음 문자 검사
            }

            if (!insideCharacter && current == '"') // 문자열 따옴표 확인
            {
                insideString = !insideString; // 문자열 내부 상태 전환
                continue; // 다음 문자 검사
            }

            if (!insideString && current == '\'') // 문자 리터럴 따옴표 확인
            {
                insideCharacter = !insideCharacter; // 문자 내부 상태 전환
                continue; // 다음 문자 검사
            }

            if (!insideString
                && !insideCharacter
                && current == '/'
                && line[index + 1] == '/') // 줄 주석 기호 확인
            {
                return index; // 줄 주석 시작 위치 반환
            }
        }

        return -1; // 줄 주석 없음 반환
    }

    private static string NormalizeTooltipSentence(string text) // Tooltip 문장 형식 정리
    {
        string normalizedText = text.Trim(); // 앞뒤 공백 제거

        while (normalizedText.EndsWith(".", StringComparison.Ordinal)
            || normalizedText.EndsWith("。", StringComparison.Ordinal)) // 기존 마침표 확인
        {
            normalizedText = normalizedText
                .Substring(0, normalizedText.Length - 1)
                .TrimEnd(); // 중복 마침표 제거
        }

        if (string.IsNullOrWhiteSpace(normalizedText)) // 빈 설명 확인
        {
            return "Inspector에서 설정하는 값입니다."; // 일반 설명 반환
        }

        return normalizedText + "."; // Tooltip 문장 마침표 추가
    }

    private static string EscapeTooltipText(string tooltipText) // Tooltip 문자열 특수 문자 처리
    {
        return tooltipText
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", " ")
            .Replace("\n", " "); // C# 문자열용 특수 문자 처리
    }

    private static string GetIndentation(string line) // 코드 줄 들여쓰기 추출
    {
        int index = 0; // 현재 문자 위치

        while (index < line.Length
            && char.IsWhiteSpace(line[index])) // 앞쪽 공백 문자 순회
        {
            index++; // 들여쓰기 길이 증가
        }

        return line.Substring(0, index); // 들여쓰기 문자열 반환
    }

    private static string DetectNewline(string text) // 기존 스크립트 줄바꿈 형식 확인
    {
        return text.Contains("\r\n")
            ? "\r\n"
            : "\n"; // Windows 또는 Unix 줄바꿈 반환
    }

    private static string[] FindRuntimeScriptPaths() // Tooltip 적용 대상 스크립트 검색
    {
        if (!AssetDatabase.IsValidFolder(RuntimeScriptRoot)) // 대상 폴더 존재 확인
        {
            Debug.LogError(
                $"[Project U Tooltip] 대상 폴더를 찾을 수 없습니다: {RuntimeScriptRoot}"); // 대상 폴더 누락 출력

            return Array.Empty<string>(); // 빈 스크립트 목록 반환
        }

        string[] scriptGuids = AssetDatabase.FindAssets(
            "t:MonoScript",
            new[] { RuntimeScriptRoot }); // 런타임 스크립트 에셋 검색

        return scriptGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !IsEditorOnlyPath(path))
            .Distinct()
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray(); // 중복 제거와 경로 정렬
    }

    private static bool IsEditorOnlyPath(string path) // Editor 전용 경로 여부 확인
    {
        string normalizedPath = path.Replace('\\', '/'); // 경로 구분자 통일

        return normalizedPath.IndexOf(
            "/Editor/",
            StringComparison.OrdinalIgnoreCase) >= 0; // Editor 폴더 포함 여부 반환
    }

    private static List<string> FindMissingTooltipEntries() // Tooltip 누락 직렬화 필드 검색
    {
        string[] scriptPaths = FindRuntimeScriptPaths(); // 대상 스크립트 검색
        List<string> missingEntries = new List<string>(); // 누락 결과 목록

        for (int fileIndex = 0; fileIndex < scriptPaths.Length; fileIndex++) // 전체 스크립트 순회
        {
            string scriptPath = scriptPaths[fileIndex]; // 현재 스크립트 경로
            string text = File.ReadAllText(scriptPath)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n"); // 현재 파일 읽기와 줄바꿈 통일

            List<string> lines = text.Split('\n').ToList(); // 줄 단위 코드 목록 생성
            List<SerializedFieldCandidate> candidates = FindSerializedFieldCandidates(
                scriptPath,
                lines); // 현재 파일 직렬화 필드 검색

            for (int index = 0; index < candidates.Count; index++) // 전체 직렬화 필드 순회
            {
                SerializedFieldCandidate candidate = candidates[index]; // 현재 필드 조회

                if (candidate.HasTooltip) // 기존 Tooltip 존재 확인
                {
                    continue; // 누락 목록 제외
                }

                missingEntries.Add(
                    $"- {scriptPath}:{candidate.DeclarationStartIndex + 1}"
                    + $" | {candidate.FieldName}"); // Tooltip 누락 경로 저장
            }
        }

        return missingEntries; // 전체 누락 목록 반환
    }

    private static void PrintProcessResult(
        TooltipProcessResult result,
        bool applied) // Tooltip 처리 결과 출력
    {
        string actionLabel = applied
            ? "적용"
            : "미리보기"; // 현재 처리 종류 문구

        StringBuilder builder = new StringBuilder(); // 결과 문자열 생성기

        builder.AppendLine($"[Project U Tooltip] {actionLabel} 완료"); // 처리 완료 제목 추가
        builder.AppendLine($"검사 스크립트: {result.ScannedFileCount}개"); // 검사 파일 수 추가
        builder.AppendLine($"전체 Inspector 직렬화 필드: {result.SerializedFieldCount}개"); // 전체 필드 수 추가
        builder.AppendLine($"명시적 SerializeField·SerializeReference: {result.ExplicitSerializedFieldCount}개"); // 명시적 필드 수 추가
        builder.AppendLine($"public 직렬화 필드: {result.PublicSerializedFieldCount}개"); // public 필드 수 추가
        builder.AppendLine($"기존 Tooltip: {result.ExistingTooltipCount}개"); // 기존 Tooltip 수 추가
        builder.AppendLine($"추가 Tooltip: {result.AddedTooltipCount}개"); // 추가 Tooltip 수 추가
        builder.AppendLine($"한국어 주석 사용: {result.CommentTooltipCount}개"); // 주석 기반 Tooltip 수 추가
        builder.AppendLine($"기본 설명 생성: {result.FallbackTooltipCount}개"); // 기본 Tooltip 수 추가
        builder.AppendLine($"변경 파일: {result.ChangedFiles.Count}개"); // 변경 파일 수 추가

        for (int index = 0; index < result.ChangedFiles.Count; index++) // 전체 변경 파일 순회
        {
            builder.AppendLine($"- {result.ChangedFiles[index]}"); // 현재 변경 파일 경로 추가
        }

        if (applied) // 실제 적용 완료 확인
        {
            builder.AppendLine(
                "Unity 컴파일 완료 후 3. Validate Tooltip Coverage를 실행하십시오."); // 다음 검사 단계 추가
        }

        Debug.Log(builder.ToString()); // 전체 처리 결과 출력

        EditorUtility.DisplayDialog(
            $"Tooltip {actionLabel} 완료",
            $"검사 스크립트: {result.ScannedFileCount}개\n"
            + $"전체 직렬화 필드: {result.SerializedFieldCount}개\n"
            + $"public 직렬화 필드: {result.PublicSerializedFieldCount}개\n"
            + $"추가 Tooltip: {result.AddedTooltipCount}개\n"
            + $"변경 파일: {result.ChangedFiles.Count}개\n\n"
            + "세부 결과는 Console에서 확인하십시오.",
            "확인"); // 처리 완료 안내 창 표시
    }

    private sealed class SerializedFieldCandidate // Inspector 직렬화 필드 정보
    {
        public SerializedFieldCandidate(
            int attributeStartIndex,
            int declarationStartIndex,
            int declarationEndIndex,
            string fieldName,
            string fieldType,
            bool isExplicitlySerialized,
            bool hasTooltip) // 직렬화 필드 정보 생성
        {
            AttributeStartIndex = attributeStartIndex; // 첫 속성 줄 저장
            DeclarationStartIndex = declarationStartIndex; // 선언 시작 줄 저장
            DeclarationEndIndex = declarationEndIndex; // 선언 마지막 줄 저장
            FieldName = fieldName; // 필드 이름 저장
            FieldType = fieldType; // 필드 형식 저장
            IsExplicitlySerialized = isExplicitlySerialized; // 명시적 직렬화 여부 저장
            HasTooltip = hasTooltip; // 기존 Tooltip 여부 저장
        }

        public int AttributeStartIndex { get; } // 첫 속성 또는 선언 줄
        public int DeclarationStartIndex { get; } // 필드 선언 시작 줄
        public int DeclarationEndIndex { get; } // 필드 선언 마지막 줄
        public string FieldName { get; } // 필드 변수 이름
        public string FieldType { get; } // 필드 형식 이름
        public bool IsExplicitlySerialized { get; } // SerializeField 또는 SerializeReference 여부
        public bool HasTooltip { get; } // 기존 Tooltip 존재 여부
    }

    private sealed class TooltipFileResult // 단일 파일 Tooltip 처리 결과
    {
        public TooltipFileResult(List<string> lines) // 단일 파일 결과 생성
        {
            Lines = lines; // 변경 코드 줄 목록 저장
        }

        public List<string> Lines { get; } // 변경 코드 줄 목록
        public int SerializedFieldCount { get; set; } // 전체 직렬화 필드 수
        public int ExplicitSerializedFieldCount { get; set; } // 명시적 직렬화 필드 수
        public int PublicSerializedFieldCount { get; set; } // public 직렬화 필드 수
        public int ExistingTooltipCount { get; set; } // 기존 Tooltip 수
        public int AddedTooltipCount { get; set; } // 추가 Tooltip 수
        public int CommentTooltipCount { get; set; } // 한국어 주석 기반 Tooltip 수
        public int FallbackTooltipCount { get; set; } // 기본 설명 기반 Tooltip 수
        public bool Changed => AddedTooltipCount > 0; // 현재 파일 변경 여부
    }

    private sealed class TooltipProcessResult // 전체 Tooltip 처리 결과
    {
        public int ScannedFileCount { get; set; } // 전체 검사 스크립트 수
        public int SerializedFieldCount { get; set; } // 전체 직렬화 필드 수
        public int ExplicitSerializedFieldCount { get; set; } // 명시적 직렬화 필드 수
        public int PublicSerializedFieldCount { get; set; } // public 직렬화 필드 수
        public int ExistingTooltipCount { get; set; } // 기존 Tooltip 수
        public int AddedTooltipCount { get; set; } // 전체 추가 Tooltip 수
        public int CommentTooltipCount { get; set; } // 한국어 주석 기반 Tooltip 수
        public int FallbackTooltipCount { get; set; } // 기본 설명 기반 Tooltip 수
        public List<string> ChangedFiles { get; } = new List<string>(); // 변경 파일 경로 목록
    }
}
#endif
