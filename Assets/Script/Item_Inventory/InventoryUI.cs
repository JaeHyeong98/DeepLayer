using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel; // 인벤토리 전체를 감싸는 UI Panel
    public Transform slotsParent;    // InventorySlotUI 프리팹들이 들어갈 부모 오브젝트 (GridLayoutGroup이 붙은)
    public GameObject inventorySlotPrefab; // InventorySlotUI 스크립트가 붙은 프리팹

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    // 싱글톤 패턴 (로컬 클라이언트의 UI 접근 용이)
    public static InventoryUI Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요시
        }

        InitializeInventoryUI(20); // 예시로 20슬롯
    }

    public void InitializeInventoryUI(int numSlots)
    {
        for (int i = 0; i < numSlots; i++) // slotParents 하위에 Slot 추가
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.slotIndex = i; // 각 슬롯의 인덱스 할당
            slotUIs.Add(slotUI);
        }
    }

    // PlayerInventory 스크립트에서 호출될 UI 갱신 함수
    public void RefreshUI(NetworkList<InventoryItem> inventoryItems)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inventoryItems.Count)
            {
                slotUIs[i].SetItem(inventoryItems[i]);
            }
            else
            {
                // 혹시 슬롯 UI가 inventoryItems.Count보다 많을 경우를 대비 (빈 슬롯 처리)
                slotUIs[i].SetItem(new InventoryItem(-1, 0));
            }
        }
    }

    // 인벤토리 패널 토글 함수 (PlayerInfo.ToggleInventory()에서 호출)
    public void ToggleInventoryPanel()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        // 커서 lock 상태 변경 등 UI 표시/숨김에 따른 부가 로직
    }
}
