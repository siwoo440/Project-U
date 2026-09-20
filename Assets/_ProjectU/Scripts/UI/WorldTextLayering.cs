using System.Collections.Generic; // 목록
using TMPro; // TextMeshPro 기능
using UnityEngine; // Unity 기본 기능

// 115일차: 3D 글자(말풍선 · 표지판 · 이름표)에서 한글이 <mark> 바탕 뒤로 숨지 않게
// 한글 글꼴은 글자판(Atlas)이 여러 장이라 글자판마다 따로 그려진다 (TMP_SubMesh).
// 바탕(<mark>)은 기본 글자 묶음에 들어 있어, 그리는 순서가 섞이면 일부 한글이 바탕에 덮여 흐리게 보이거나 사라졌다.
// 글자가 바뀔 때마다 한글 묶음을 바탕보다 한 칸 뒤(나중)에 그리게 한다.
public static class WorldTextLayering
{
    private static readonly List<TMP_SubMesh> SubMeshes = new List<TMP_SubMesh>(); // 재사용 목록

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook() // 게임 시작 시 한 번 연결
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
    }

    private static void HandleTextChanged(Object changed) // TMP가 글자 모양을 다시 만들 때마다
    {
        if (changed is TextMeshPro text)
        {
            RaiseSubMeshes(text);
        }
    }

    public static void RaiseSubMeshes(TextMeshPro text) // 한글 묶음을 바탕보다 나중에 그림
    {
        if (text == null || text.renderer == null)
        {
            return;
        }

        int layer = text.renderer.sortingLayerID;
        int order = text.renderer.sortingOrder + 1;
        text.GetComponentsInChildren(true, SubMeshes);

        foreach (TMP_SubMesh subMesh in SubMeshes)
        {
            Renderer renderer = subMesh != null ? subMesh.renderer : null;

            if (renderer != null && (renderer.sortingOrder != order || renderer.sortingLayerID != layer))
            {
                renderer.sortingLayerID = layer;
                renderer.sortingOrder = order;
            }
        }

        SubMeshes.Clear();
    }
}
