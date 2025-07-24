using System;
using Unity.Netcode;

[Serializable]
public struct InventoryItem : INetworkSerializable, IEquatable<InventoryItem> // 네트워크 동기화 시 INetworkSerializable 구현
{
    public int itemId; // 어떤 아이템인지 (ScriptableObject 참조)
    public int quantity;      // 몇 개인지 (스택 개수)

    // 생성자 (필수는 아니지만 편리함)
    public InventoryItem(int id, int qty)
    {
        itemId = id;
        quantity = qty;
    }

    public bool IsEmpty => itemId <= 0 || quantity <= 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemId);
        serializer.SerializeValue(ref quantity);
    }


    public bool Equals(InventoryItem other)
    {
        return itemId == other.itemId && quantity == other.quantity;
    }

    public override bool Equals(object obj)
    {
        return obj is InventoryItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(itemId, quantity);
    }
}
