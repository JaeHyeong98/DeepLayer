using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemInfo info;

    public ItemInfo Gathering(PlayerController player)
    {
        Debug.Log("Gathering");
        return info;
    }
}
