using UnityEngine;

[CreateAssetMenu(fileName = "NewItemInfo", menuName = "Inventory/Item Info")]
public class ItemInfo : ScriptableObject
{
    public int itemId; // �������� ���� ID (��Ʈ��ũ ����ȭ�� ����)
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public int maxStackSize = 1; // �ִ� ���� ���� ���� (��ġ�� ��������)
    public ItemType itemType; // ������ Ÿ�� (���, �Ҹ�ǰ, ��� ��)

    // ������ Ÿ�� ������
    public enum ItemType
    {
        General,
        Consumable,
        Equipment,
        Material,
        Weapon,
        Armor
    }

    // ������ ��� �� ȿ�� (���� ����)
    public virtual void UseItem(PlayerController player)
    {
        Debug.Log($"Using {itemName}");
        // �⺻���� ��� ���� (��: �Ҹ�ǰ ��� �� �޽��� ���)
    }
}
