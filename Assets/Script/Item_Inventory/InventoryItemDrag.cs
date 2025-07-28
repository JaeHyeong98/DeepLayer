using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItemDrag : MonoBehaviour,IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public InventoryItem inventoryItemInfo;

    public static GameObject obj;

    Vector3 startPosition;

    [SerializeField]
    Transform onDargParent;

    [HideInInspector]
    public Transform startParent;

    public void OnBeginDrag(PointerEventData eventData)
    {
        obj = gameObject;

        startPosition = transform.position;
        startParent = transform.parent;

        GetComponent<CanvasGroup>().blocksRaycasts = false;

        transform.SetParent(onDargParent);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        obj = null;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        if(transform.parent = onDargParent)
        {
            transform.position = startPosition;
            transform.SetParent(startParent);
        }
    }
}
