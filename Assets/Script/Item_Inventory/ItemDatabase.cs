using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    // ScriptableObject ��� �̱��� (���� �Ŀ��� ����)
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Resources �������� ItemDatabase�� �ε�
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("ItemDatabase not found in Resources folder. Please create one at Resources/ItemDatabase.asset");
                }
            }
            return _instance;
        }
    }

    public List<ItemInfo> allItems; // ��� ItemInfo ScriptableObject���� ���⿡ �Ҵ�

    // Awake���� �ʱ�ȭ (�����Ϳ��� ���� ��)
    void OnEnable()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Duplicate ItemDatabase instance found. Destroying new one.");
        }
    }

    public ItemInfo GetItemInfo(int id)
    {
        if (allItems == null)
        {
            Debug.LogError("ItemDatabase has no items assigned!");
            return null;
        }
        // LINQ ��� (���ɿ� �ΰ��ϴٸ� Dictionary�� ĳ���ϴ� ���� ����)
        return allItems.FirstOrDefault(item => item.itemId == id);
    }
}