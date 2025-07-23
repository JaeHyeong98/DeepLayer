using System;
using UnityEngine;

public enum ItemType
{
    General,    // 일반
    Consumable, // 사용품
    Equipment,  // 장비
    Material,   // 재료
    Weapon,     // 무기
    Armor       // 방어구
}

[Serializable]
public struct ItemInfo
{
    public int itemId; // 아이템의 고유 ID (네트워크 동기화에 유용)
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public int maxStackSize; // 최대 스택 가능 개수 (겹치기 가능한지)
    public ItemType itemType; // 아이템 타입 (장비, 소모품, 재료 등)
}

[CreateAssetMenu(fileName = "NewItemInfo", menuName = "Inventory/Item Info")]
public class Item : ScriptableObject
{
    [SerializeField]
    public ItemInfo info;

    public void Init(ItemInfo info)
    {
        this.info = info;
    }

    public virtual void UseItem(PlayerController player)
    {
        Debug.Log($"Using {info.itemName}");
        // 기본적인 사용 로직 (예: 소모품 사용 시 메시지 출력)
    }
}
