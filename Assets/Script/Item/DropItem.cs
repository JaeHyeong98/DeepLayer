using UnityEngine;

public class DropItem : Item
{
    [SerializeField]
    public GameObject obj;
    public void Gathering()
    {
        obj.SetActive(false);
    }
}
