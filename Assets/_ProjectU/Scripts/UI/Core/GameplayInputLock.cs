using System.Collections.Generic; // 컬렉션 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
public sealed class GameplayInputLock : MonoBehaviour // 게임 플레이 입력 잠금 관리자
{
    [Header("Behaviours")] // 비활성화 대상 설정 묶음
    [Tooltip("UI 사용 중 정지할 컴포넌트 목록.")]
    [SerializeField] private Behaviour[] behavioursToDisable = new Behaviour[0]; // UI 사용 중 정지할 컴포넌트 목록

    [Header("HUD Objects")] // 숨김 대상 설정 묶음
    [Tooltip("UI 사용 중 숨길 HUD 오브젝트 목록.")]
    [SerializeField] private GameObject[] objectsToHide = new GameObject[0]; // UI 사용 중 숨길 HUD 오브젝트 목록

    private readonly HashSet<string> activeLockIds = new HashSet<string>(); // 현재 활성 입력 잠금 ID 목록
    private readonly Dictionary<Behaviour, bool> previousBehaviourStates = new Dictionary<Behaviour, bool>(); // 컴포넌트 기존 활성 상태 목록
    private readonly Dictionary<GameObject, bool> previousObjectStates = new Dictionary<GameObject, bool>(); // HUD 기존 활성 상태 목록
    private CursorLockMode previousCursorLockMode; // 잠금 전 커서 고정 상태
    private bool previousCursorVisible; // 잠금 전 커서 표시 상태
    private bool hasCapturedState; // 기존 상태 저장 여부

    public bool IsLocked => activeLockIds.Count > 0; // 현재 입력 잠금 여부 제공
    public int ActiveLockCount => activeLockIds.Count; // 현재 입력 잠금 개수 제공

    public bool Acquire(string lockId) // 지정 ID의 입력 잠금 획득
    {
        if (string.IsNullOrWhiteSpace(lockId)) // 입력 잠금 ID 확인
        {
            Debug.LogError("GameplayInputLock의 Lock ID가 비어 있습니다.", this); // 입력 잠금 ID 오류 출력
            return false; // 입력 잠금 실패 반환
        }

        if (!activeLockIds.Add(lockId)) // 동일 입력 잠금 존재 확인
        {
            return false; // 중복 입력 잠금 제외
        }

        if (activeLockIds.Count == 1) // 최초 입력 잠금 확인
        {
            CaptureAndApplyLock(); // 현재 상태 저장 후 입력 잠금 적용
        }

        return true; // 입력 잠금 성공 반환
    }

    public bool Release(string lockId) // 지정 ID의 입력 잠금 해제
    {
        if (string.IsNullOrWhiteSpace(lockId)) // 입력 잠금 ID 확인
        {
            return false; // 입력 잠금 해제 실패 반환
        }

        if (!activeLockIds.Remove(lockId)) // 등록된 입력 잠금 확인
        {
            return false; // 존재하지 않는 입력 잠금 제외
        }

        if (activeLockIds.Count == 0) // 모든 입력 잠금 해제 확인
        {
            RestorePreviousStates(); // 기존 게임 상태 복구
        }

        return true; // 입력 잠금 해제 성공 반환
    }

    public bool Contains(string lockId) // 지정 ID의 입력 잠금 보유 여부 확인
    {
        if (string.IsNullOrWhiteSpace(lockId)) // 입력 잠금 ID 확인
        {
            return false; // 입력 잠금 없음 반환
        }

        return activeLockIds.Contains(lockId); // 입력 잠금 보유 여부 반환
    }

    public void ReleaseAll() // 모든 입력 잠금 강제 해제
    {
        activeLockIds.Clear(); // 전체 입력 잠금 ID 제거
        RestorePreviousStates(); // 기존 게임 상태 복구
    }

    private void CaptureAndApplyLock() // 현재 상태 저장 후 입력 잠금 적용
    {
        previousBehaviourStates.Clear(); // 이전 컴포넌트 상태 초기화
        previousObjectStates.Clear(); // 이전 HUD 상태 초기화
        previousCursorLockMode = Cursor.lockState; // 기존 커서 고정 상태 저장
        previousCursorVisible = Cursor.visible; // 기존 커서 표시 상태 저장
        hasCapturedState = true; // 기존 상태 저장 완료 기록

        if (behavioursToDisable != null) // 비활성화 목록 존재 확인
        {
            for (int index = 0; index < behavioursToDisable.Length; index++) // 전체 비활성화 대상 순회
            {
                Behaviour targetBehaviour = behavioursToDisable[index]; // 현재 비활성화 대상 조회

                if (targetBehaviour == null || targetBehaviour == this) // 대상 존재와 자기 참조 확인
                {
                    continue; // 잘못된 대상 제외
                }

                if (previousBehaviourStates.ContainsKey(targetBehaviour)) // 중복 컴포넌트 확인
                {
                    continue; // 중복 컴포넌트 제외
                }

                previousBehaviourStates.Add(targetBehaviour, targetBehaviour.enabled); // 기존 컴포넌트 활성 상태 저장
                targetBehaviour.enabled = false; // 대상 컴포넌트 비활성화
            }
        }

        if (objectsToHide != null) // 숨김 목록 존재 확인
        {
            for (int index = 0; index < objectsToHide.Length; index++) // 전체 숨김 대상 순회
            {
                GameObject targetObject = objectsToHide[index]; // 현재 숨김 대상 조회

                if (targetObject == null || targetObject == gameObject) // 대상 존재와 자기 참조 확인
                {
                    continue; // 잘못된 대상 제외
                }

                if (previousObjectStates.ContainsKey(targetObject)) // 중복 HUD 오브젝트 확인
                {
                    continue; // 중복 HUD 오브젝트 제외
                }

                previousObjectStates.Add(targetObject, targetObject.activeSelf); // 기존 HUD 활성 상태 저장
                targetObject.SetActive(false); // 대상 HUD 오브젝트 숨김
            }
        }

        Cursor.lockState = CursorLockMode.None; // 커서 고정 해제
        Cursor.visible = true; // 커서 표시
    }

    private void RestorePreviousStates() // 저장된 게임 상태 복구
    {
        if (!hasCapturedState) // 기존 상태 저장 여부 확인
        {
            return; // 복구 처리 생략
        }

        foreach (KeyValuePair<Behaviour, bool> statePair in previousBehaviourStates) // 저장된 컴포넌트 상태 순회
        {
            if (statePair.Key == null) // 대상 컴포넌트 존재 확인
            {
                continue; // 제거된 컴포넌트 제외
            }

            statePair.Key.enabled = statePair.Value; // 기존 컴포넌트 활성 상태 복구
        }

        foreach (KeyValuePair<GameObject, bool> statePair in previousObjectStates) // 저장된 HUD 상태 순회
        {
            if (statePair.Key == null) // 대상 HUD 존재 확인
            {
                continue; // 제거된 HUD 제외
            }

            statePair.Key.SetActive(statePair.Value); // 기존 HUD 활성 상태 복구
        }

        previousBehaviourStates.Clear(); // 저장된 컴포넌트 상태 제거
        previousObjectStates.Clear(); // 저장된 HUD 상태 제거
        Cursor.lockState = previousCursorLockMode; // 기존 커서 고정 상태 복구
        Cursor.visible = previousCursorVisible; // 기존 커서 표시 상태 복구
        hasCapturedState = false; // 기존 상태 저장 기록 해제
    }

    private void OnDisable() // 입력 잠금 관리자 비활성화 정리
    {
        ReleaseAll(); // 모든 입력 잠금 강제 해제
    }
}
