using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    // ScriptableObject 기반 싱글톤 (빌드 후에도 유지)
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Resources 폴더에서 ItemDatabase를 로드
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("ItemDatabase not found in Resources folder. Please create one at Resources/ItemDatabase.asset");
                }
            }
            return _instance;
        }
    }

    public List<ItemInfo> allItems; // 모든 ItemInfo ScriptableObject들을 여기에 할당

    // Awake에서 초기화 (에디터에서 사용될 때)
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
        // LINQ 사용 (성능에 민감하다면 Dictionary로 캐싱하는 것이 좋음)
        return allItems.FirstOrDefault(item => item.itemId == id);
    }
}