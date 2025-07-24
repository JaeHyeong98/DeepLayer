using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro ����

public class InventorySlotUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    public int slotIndex;

    public void SetItem(InventoryItem item)
    {
        if (!item.IsEmpty)
        {
            // ItemDatabase���� itemId�� ���� ���� ItemInfo�� ã�ƿɴϴ�.
            ItemInfo actualItemInfo = ItemDatabase.Instance.GetItemInfo(item.itemId);
            if (actualItemInfo != null) // ã�ƿ� ItemInfo�� ��ȿ���� �ٽ� Ȯ�� (����ü�̹Ƿ�)
            {
                itemIcon.sprite = actualItemInfo.itemIcon;
                itemIcon.enabled = true;
                quantityText.text = (actualItemInfo.maxStackSize > 1) ? item.quantity.ToString() : "";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                // ��ȿ���� ���� ItemInfo�� ������ �� �� ���� ó��
                SetEmpty();
            }
        }
        else
        {
            SetEmpty();
        }
    }

    private void SetEmpty()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        quantityText.text = "";
        quantityText.gameObject.SetActive(false);
    }

    public void OnSlotClicked()
    {
        Inventory localPlayerInventory = GSC.player.GetComponent<Inventory>(); // ���� ���ӿ����� ������ �̸� ������ �־�� ��.
        if (localPlayerInventory != null && localPlayerInventory.IsOwner)
        {
            // �� ������ ���� InventoryItem �����͸� ������ ��ȿ���� Ȯ�� �� ��� ��û
            InventoryItem currentItemInSlot = localPlayerInventory.inventorySlots[slotIndex];
            if (!currentItemInSlot.IsEmpty)
            {
                localPlayerInventory.UseItemServerRpc(slotIndex);
            }
            else
            {
                Debug.Log($"Slot {slotIndex} is empty.");
            }
        }
        Debug.Log($"Slot {slotIndex} clicked.");
    }
}
