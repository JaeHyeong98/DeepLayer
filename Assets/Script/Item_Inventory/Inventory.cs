using UnityEngine;
using Unity.Netcode;

public class Inventory : NetworkBehaviour
{
    public NetworkList<InventoryItem> inventorySlots;

    [Header("Inventory Settings")]
    public int inventorySize = 20; // �κ��丮 ���� ����

    // NetworkList�� Awake���� �ʱ�ȭ�ؾ� �մϴ�.
    void Awake()
    {
        inventorySlots = new NetworkList<InventoryItem>();
        // ����� �̸� ���صΰ� �ʱ�ȭ (��� ������ ����ִ� ����������)
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventoryItem(-1, -1)); // �� ����
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (inventorySlots.Count == 0) // �̹� �ʱ�ȭ���� �ʾҴ��� Ȯ�� (��Ȱ��ȭ �� �ߺ� �ʱ�ȭ ����)
            {
                for (int i = 0; i < inventorySize; i++)
                {
                    inventorySlots.Add(new InventoryItem(-1, -1)); // �� ���� �߰� (itemId=0)
                }
                Debug.Log($"Server: PlayerInventory initialized with {inventorySize} empty slots.");
            }
        }

        if (IsOwner) // Ŭ���̾�Ʈ �������� UI ������Ʈ�� ���� �̺�Ʈ ����
        {
            inventorySlots.OnListChanged += OnInventoryListChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            inventorySlots.OnListChanged -= OnInventoryListChanged;
        }
    }

    // �κ��丮 ����� ����� �� ȣ��Ǵ� �ݹ� (Ŭ���̾�Ʈ���� UI ������Ʈ �뵵)
    private void OnInventoryListChanged(NetworkListEvent<InventoryItem> changeEvent)
    {
        Debug.Log($"Inventory changed: Index {changeEvent.Index}, Type {changeEvent.Type}");
        UpdateInventoryUI(); // UI ������Ʈ �Լ� ȣ��
    }

    // ������ �߰�
    [ServerRpc] // ���������� ����ǵ��� ��û
    public void AddItemServerRpc(int itemId, int quantity)
    {
        // ���������� ItemInfo�� ���� ���� ������ ������ ����
        ItemInfo infoToAdd = ItemDatabase.Instance.GetItemInfo(itemId);
        if (infoToAdd == null)
        {
            Debug.LogError($"Server: ItemInfo with ID {itemId} not found!");
            return;
        }

        // ������ �߰� ���� (���� ���� ����, �� ���� ã�� ��)
        // �� �κ��� �� �� �����ϰ� ������ �� �ֽ��ϴ�. (TODO: ���� �߰� ���� ����)
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventoryItem currentItem = inventorySlots[i];
            // ���� �������̰� ���� �����ϸ�, �ִ� ���� ����� ���� ���� ���
            if (currentItem.itemId == infoToAdd.itemId && infoToAdd.maxStackSize > 1 && currentItem.quantity < infoToAdd.maxStackSize)
            {
                int remainingSpace = infoToAdd.maxStackSize - currentItem.quantity;
                int actualAdd = Mathf.Min(quantity, remainingSpace);

                currentItem.quantity += actualAdd;
                inventorySlots[i] = currentItem; // NetworkList�� ����ü ���� �� ��ü�� �ٽ� �Ҵ��ؾ� ������
                quantity -= actualAdd;

                if (quantity <= 0) break; // ��� �߰������� ����
            }
            else if (currentItem.itemId <= 0) // �� ���� �߰�
            {
                currentItem = new InventoryItem(infoToAdd.itemId, Mathf.Min(quantity, infoToAdd.maxStackSize));
                inventorySlots[i] = currentItem;
                quantity -= Mathf.Min(quantity, infoToAdd.maxStackSize);
                if (quantity <= 0) break;
            }
        }
        if (quantity > 0)
        {
            Debug.LogWarning($"Could not add all items. {quantity} left over.");
            // ���� ������ ó�� (�ٴڿ� ��� ��)
        }
    }

    // ������ ����
    [ServerRpc]
    public void RemoveItemServerRpc(int slotIndex, int quantity)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        InventoryItem itemToRemove = inventorySlots[slotIndex];
        if (itemToRemove.itemId <= 0 || itemToRemove.quantity == 0) return;

        itemToRemove.quantity -= quantity;
        if (itemToRemove.quantity <= 0)
        {
            inventorySlots[slotIndex] = new InventoryItem(-1, -1); // ���� ���
        }
        else
        {
            inventorySlots[slotIndex] = itemToRemove;
        }
    }

    // ������ ��� (Ŭ���̾�Ʈ ��û -> ���� ó��)
    [ServerRpc]
    public void UseItemServerRpc(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        InventoryItem itemToUse = inventorySlots[slotIndex];
        if (itemToUse.IsEmpty) return;

        ItemInfo actualItemInfo = ItemDatabase.Instance.GetItemInfo(itemToUse.itemId);
        if (actualItemInfo != null)
        {
            actualItemInfo.UseItem(this.GetComponent<PlayerController>());
            RemoveItemServerRpc(slotIndex, 1);
        }
        else
        {
            Debug.LogError($"Server: Attempted to use invalid item with ID {itemToUse.itemId}");
        }
    }

    // �κ��丮 UI�� ������Ʈ�ϴ� Ŭ���̾�Ʈ ���� �Լ� (UI ��ũ��Ʈ���� ȣ��)
    // �� �Լ��� ClientRpc�� NetworkVariable.OnValueChanged �ݹ� ��� ȣ��Ǿ�� �մϴ�.
    [ClientRpc] // �Ǵ� OnListChanged �ݹ鿡�� ���� ȣ��
    public void UpdateInventoryUIClientRpc()
    {
        // Debug.Log("Client received UI update request.");
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // ���� �κ��丮 UI (GameObject, Image, Text ��)�� ������Ʈ�ϴ� ������ ���⿡ �����մϴ�.
        // ���� ���, InventoryUI.Instance.RefreshUI(inventorySlots); �� ���� ȣ��.
        Debug.Log("Inventory UI Refreshed.");
        // �� ������ UI�� ��ȸ�ϸ� inventorySlots�� �����͸� ǥ��
    }
}
