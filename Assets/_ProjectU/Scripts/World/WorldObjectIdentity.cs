using System; // GUID 생성 기능
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 ID 컴포넌트 중복 방지
public sealed class WorldObjectIdentity : MonoBehaviour // 월드 오브젝트 고유 식별자
{
    [Header("Identity")] // 식별 정보 묶음
    [Tooltip("저장용 고유 ID입니다. Scene에 배치된 각 오브젝트는 서로 다른 ID를 사용해야 합니다.")] // Inspector 저장 ID 설명
    [SerializeField] private string worldObjectId = string.Empty; // 저장용 고유 ID

    public string WorldObjectId => worldObjectId; // 현재 고유 ID 제공
    public bool HasValidId => !string.IsNullOrWhiteSpace(worldObjectId); // 유효 ID 존재 여부 제공

    public void AssignWorldObjectId(string newWorldObjectId) // 지정된 저장 ID 적용
    {
        worldObjectId = string.IsNullOrWhiteSpace(newWorldObjectId) // 새로운 ID 입력 여부 확인
            ? string.Empty // 비어 있으면 빈 ID 적용
            : newWorldObjectId.Trim(); // 앞뒤 공백을 제거한 ID 적용
    }

    public void GenerateRuntimeId() // 실행 중 생성된 오브젝트에 임시 고유 ID 발급
    {
        worldObjectId = Guid.NewGuid().ToString("N"); // 중복 가능성이 낮은 GUID 생성
    }

    [ContextMenu("Generate New World Object ID")] // Inspector 새 저장 ID 발급 메뉴
    private void GenerateNewWorldObjectId() // Scene 오브젝트에 새로운 영구 저장 ID 발급
    {
#if UNITY_EDITOR
        if (Application.isPlaying) // Play Mode 상태 확인
        {
            Debug.LogWarning("영구 World Object ID는 Edit Mode에서 발급해야 합니다.", this); // Play Mode 변경 경고 출력
            return; // 영구 ID 발급 처리 중단
        }

        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) // Project 창의 원본 Prefab 여부 확인
        {
            Debug.LogWarning( // 원본 Prefab ID 발급 금지 경고 시작
                "원본 Prefab에는 고정 World Object ID를 발급하지 마세요. " // 원본 Prefab 문제 안내
                + "Hierarchy의 Scene 인스턴스를 선택해서 ID를 발급해야 합니다.", // 올바른 발급 위치 안내
                this); // 현재 오브젝트를 Log Context로 지정
            return; // 원본 Prefab ID 발급 처리 중단
        }

        UnityEditor.Undo.RecordObject(this, "Generate World Object ID"); // ID 변경 Undo 기록
#endif

        worldObjectId = Guid.NewGuid().ToString("N"); // 새로운 영구 저장 GUID 생성

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // 변경된 컴포넌트를 저장 대상으로 표시

        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject)) // Scene Prefab 인스턴스 여부 확인
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this); // worldObjectId를 Scene Override로 기록
        }

        if (gameObject.scene.IsValid()) // 유효한 Scene 오브젝트 여부 확인
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene); // 현재 Scene을 변경 상태로 표시
        }
#endif

        Debug.Log($"{gameObject.name}의 새 World Object ID를 발급했습니다: {worldObjectId}", this); // 새 ID 발급 결과 출력
    }

    [ContextMenu("Clear World Object ID")] // Inspector 저장 ID 제거 메뉴
    private void ClearWorldObjectId() // 현재 저장 ID 제거
    {
#if UNITY_EDITOR
        if (Application.isPlaying) // Play Mode 상태 확인
        {
            Debug.LogWarning("World Object ID는 Edit Mode에서 수정해야 합니다.", this); // Play Mode 수정 경고 출력
            return; // ID 제거 처리 중단
        }

        UnityEditor.Undo.RecordObject(this, "Clear World Object ID"); // ID 제거 Undo 기록
#endif

        worldObjectId = string.Empty; // 현재 저장 ID 제거

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // 변경된 컴포넌트를 저장 대상으로 표시

        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject)) // Scene Prefab 인스턴스 여부 확인
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this); // 빈 ID를 Scene Override로 기록
        }

        if (gameObject.scene.IsValid()) // 유효한 Scene 오브젝트 여부 확인
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene); // 현재 Scene을 변경 상태로 표시
        }
#endif
    }
}
