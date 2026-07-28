using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

[Serializable] // Inspector 표시 허용
public sealed class CraftingIngredient // 제작 재료 데이터
{
    [SerializeField] private ItemData itemData; // 필요 아이템 데이터
    [SerializeField] private int amount = 1; // 필요 아이템 수량

    public ItemData ItemData => itemData; // 필요 아이템 제공
    public int Amount => Mathf.Max(1, amount); // 보정된 필요 수량 제공
}