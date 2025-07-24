using UnityEngine;
using Unity.Netcode;

public class Inventory : NetworkBehaviour
{
    public NetworkList<InventoryItem> inventorySlots;

    [Header("Inventory Settings")]
    public int inventorySize = 20; // 인벤토리 슬롯 개수

    // NetworkList는 Awake에서 초기화해야 합니다.
    void Awake()
    {
        inventorySlots = new NetworkList<InventoryItem>();
        // 사이즈를 미리 정해두고 초기화 (모든 슬롯을 비어있는 아이템으로)
        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventoryItem(-1, -1)); // 빈 슬롯
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (inventorySlots.Count == 0) // 이미 초기화되지 않았는지 확인 (재활성화 시 중복 초기화 방지)
            {
                for (int i = 0; i < inventorySize; i++)
                {
                    inventorySlots.Add(new InventoryItem(-1, -1)); // 빈 슬롯 추가 (itemId=0)
                }
                Debug.Log($"Server: PlayerInventory initialized with {inventorySize} empty slots.");
            }
        }

        if (IsOwner) // 클라이언트 측에서만 UI 업데이트를 위해 이벤트 구독
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

    // 인벤토리 목록이 변경될 때 호출되는 콜백 (클라이언트에서 UI 업데이트 용도)
    private void OnInventoryListChanged(NetworkListEvent<InventoryItem> changeEvent)
    {
        Debug.Log($"Inventory changed: Index {changeEvent.Index}, Type {changeEvent.Type}");
        UpdateInventoryUI(); // UI 업데이트 함수 호출
    }

    // 아이템 추가
    [ServerRpc] // 서버에서만 실행되도록 요청
    public void AddItemServerRpc(int itemId, int quantity)
    {
        // 서버에서만 ItemInfo를 통해 실제 아이템 정보를 얻음
        ItemInfo infoToAdd = ItemDatabase.Instance.GetItemInfo(itemId);
        if (infoToAdd == null)
        {
            Debug.LogError($"Server: ItemInfo with ID {itemId} not found!");
            return;
        }

        // 아이템 추가 로직 (스택 가능 여부, 빈 슬롯 찾기 등)
        // 이 부분은 좀 더 복잡하게 구현될 수 있습니다. (TODO: 실제 추가 로직 구현)
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventoryItem currentItem = inventorySlots[i];
            // 같은 아이템이고 스택 가능하며, 최대 스택 사이즈를 넘지 않을 경우
            if (currentItem.itemId == infoToAdd.itemId && infoToAdd.maxStackSize > 1 && currentItem.quantity < infoToAdd.maxStackSize)
            {
                int remainingSpace = infoToAdd.maxStackSize - currentItem.quantity;
                int actualAdd = Mathf.Min(quantity, remainingSpace);

                currentItem.quantity += actualAdd;
                inventorySlots[i] = currentItem; // NetworkList는 구조체 변경 시 전체를 다시 할당해야 감지됨
                quantity -= actualAdd;

                if (quantity <= 0) break; // 모두 추가했으면 종료
            }
            else if (currentItem.itemId <= 0) // 빈 슬롯 발견
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
            // 남은 아이템 처리 (바닥에 드롭 등)
        }
    }

    // 아이템 제거
    [ServerRpc]
    public void RemoveItemServerRpc(int slotIndex, int quantity)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        InventoryItem itemToRemove = inventorySlots[slotIndex];
        if (itemToRemove.itemId <= 0 || itemToRemove.quantity == 0) return;

        itemToRemove.quantity -= quantity;
        if (itemToRemove.quantity <= 0)
        {
            inventorySlots[slotIndex] = new InventoryItem(-1, -1); // 슬롯 비움
        }
        else
        {
            inventorySlots[slotIndex] = itemToRemove;
        }
    }

    // 아이템 사용 (클라이언트 요청 -> 서버 처리)
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

    // 인벤토리 UI를 업데이트하는 클라이언트 전용 함수 (UI 스크립트에서 호출)
    // 이 함수는 ClientRpc나 NetworkVariable.OnValueChanged 콜백 등에서 호출되어야 합니다.
    [ClientRpc] // 또는 OnListChanged 콜백에서 직접 호출
    public void UpdateInventoryUIClientRpc()
    {
        // Debug.Log("Client received UI update request.");
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        // 실제 인벤토리 UI (GameObject, Image, Text 등)를 업데이트하는 로직을 여기에 구현합니다.
        // 예를 들어, InventoryUI.Instance.RefreshUI(inventorySlots); 와 같이 호출.
        Debug.Log("Inventory UI Refreshed.");
        // 각 슬롯의 UI를 순회하며 inventorySlots의 데이터를 표시
    }
}
