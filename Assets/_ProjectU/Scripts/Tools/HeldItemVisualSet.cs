using System; // 직렬화 기능
using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

// 84일차: 전용 도구 외형이 없는 아이템(음식·씨앗·물고기·재료·장비 등)을 손에 들었을 때 보여줄 외형 목록
[CreateAssetMenu(fileName = "HeldItemVisuals", menuName = "Project U/Items/Held Item Visual Set")] // 생성 메뉴
public sealed class HeldItemVisualSet : ScriptableObject
{
    [Serializable] // Inspector 표시
    public sealed class Entry // 아이템 하나의 손에 든 외형
    {
        [Tooltip("이 외형을 사용할 아이템입니다.")]
        public ItemData item; // 아이템

        [Tooltip("손에 들 모델 Prefab입니다. (저폴리 모델)")]
        public GameObject modelPrefab; // 모델

        [Tooltip("손 기준점(ToolHolder)에서의 위치입니다.")]
        public Vector3 holderLocalPosition; // 기준점 위치

        [Tooltip("손 기준점(ToolHolder)에서의 회전입니다.")]
        public Vector3 holderLocalEuler; // 기준점 회전

        [Tooltip("모델의 위치 보정입니다. (모델 중심 또는 손잡이를 기준점에 맞춤)")]
        public Vector3 modelLocalPosition; // 모델 위치

        [Tooltip("모델 크기입니다.")]
        [Min(0.01f)] public float modelScale = 1f; // 모델 크기
    }

    [SerializeField] private List<Entry> entries = new List<Entry>(); // 외형 목록

    public IReadOnlyList<Entry> Entries => entries; // 목록 제공

    public bool TryGet(ItemData item, out Entry entry) // 아이템 외형 검색
    {
        entry = null; // 기본 결과

        if (item == null) // 아이템 확인
        {
            return false; // 검색 실패
        }

        for (int index = 0; index < entries.Count; index++) // 목록 순회
        {
            Entry candidate = entries[index]; // 현재 항목

            if (candidate != null && candidate.item == item && candidate.modelPrefab != null) // 일치 확인
            {
                entry = candidate; // 결과 저장
                return true; // 검색 성공
            }
        }

        return false; // 검색 실패
    }
}
