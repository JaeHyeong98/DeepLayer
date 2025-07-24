using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel; // �κ��丮 ��ü�� ���δ� UI Panel
    public Transform slotsParent;    // InventorySlotUI �����յ��� �� �θ� ������Ʈ (GridLayoutGroup�� ����)
    public GameObject inventorySlotPrefab; // InventorySlotUI ��ũ��Ʈ�� ���� ������

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    // �̱��� ���� (���� Ŭ���̾�Ʈ�� UI ���� ����)
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
            // DontDestroyOnLoad(gameObject); // �ʿ��
        }

        InitializeInventoryUI(20); // ���÷� 20����
    }

    public void InitializeInventoryUI(int numSlots)
    {
        for (int i = 0; i < numSlots; i++) // slotParents ������ Slot �߰�
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.slotIndex = i; // �� ������ �ε��� �Ҵ�
            slotUIs.Add(slotUI);
        }
    }

    // PlayerInventory ��ũ��Ʈ���� ȣ��� UI ���� �Լ�
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
                // Ȥ�� ���� UI�� inventoryItems.Count���� ���� ��츦 ��� (�� ���� ó��)
                slotUIs[i].SetItem(new InventoryItem(-1, 0));
            }
        }
    }

    // �κ��丮 �г� ��� �Լ� (PlayerInfo.ToggleInventory()���� ȣ��)
    public void ToggleInventoryPanel()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        // Ŀ�� lock ���� ���� �� UI ǥ��/���迡 ���� �ΰ� ����
    }
}
