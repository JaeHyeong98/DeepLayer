using UnityEngine;

[CreateAssetMenu(fileName = "NewItemInfo", menuName = "Inventory/Item Info")]
public class ItemInfo : ScriptableObject
{
    public int itemId; // 아이템의 고유 ID (네트워크 동기화에 유용)
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public int maxStackSize = 1; // 최대 스택 가능 개수 (겹치기 가능한지)
    public ItemType itemType; // 아이템 타입 (장비, 소모품, 재료 등)

    // 아이템 타입 열거형
    public enum ItemType
    {
        General,
        Consumable,
        Equipment,
        Material,
        Weapon,
        Armor
    }

    // 아이템 사용 시 효과 (선택 사항)
    public virtual void UseItem(PlayerController player)
    {
        Debug.Log($"Using {itemName}");
        // 기본적인 사용 로직 (예: 소모품 사용 시 메시지 출력)
    }
}
