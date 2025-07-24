using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용시

public class InventorySlotUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    public int slotIndex;

    public void SetItem(InventoryItem item)
    {
        if (!item.IsEmpty)
        {
            // ItemDatabase에서 itemId를 통해 실제 ItemInfo를 찾아옵니다.
            ItemInfo actualItemInfo = ItemDatabase.Instance.GetItemInfo(item.itemId);
            if (actualItemInfo != null) // 찾아온 ItemInfo가 유효한지 다시 확인 (구조체이므로)
            {
                itemIcon.sprite = actualItemInfo.itemIcon;
                itemIcon.enabled = true;
                quantityText.text = (actualItemInfo.maxStackSize > 1) ? item.quantity.ToString() : "";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                // 유효하지 않은 ItemInfo가 들어왔을 때 빈 슬롯 처리
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
        Inventory localPlayerInventory = GSC.player.GetComponent<Inventory>(); // 실제 게임에서는 참조를 미리 가지고 있어야 함.
        if (localPlayerInventory != null && localPlayerInventory.IsOwner)
        {
            // 이 슬롯의 실제 InventoryItem 데이터를 가져와 유효한지 확인 후 사용 요청
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
