using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvItemController : MonoBehaviour
{
    public int id;
    public int type; //0 = helmet, 1 = shirt, 2 = pants, 3 = shoes
    public string itemName;
    public string rarity;

    public bool isDragging = false;
    public Vector3 screenPoint;
    public Vector3 offset;
    public Vector3 originalPos;

    public GameObject slot;

    public Transform originalParent;
    public Transform beingDraggedParent;

    public void InitializeItem(int id, string itemName, string rarity)
    {
        this.id = id;
        this.itemName = itemName;
        this.rarity = rarity;
    }

    private void Start()
    {
        originalPos = transform.position;
        originalParent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        if(isDragging)
        {
            Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
            transform.position = cursorPosition;
        }
    }

    public void StartDrag()
    {
        transform.SetParent(beingDraggedParent);
        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        isDragging = true;
    }

    public void EndDrag()
    {
        isDragging = false;
        if (slot != null && slot.GetComponent<SlotContorller>().slotType == type)
            transform.position = slot.transform.position;
        else
        {
            transform.SetParent(originalParent);
            transform.position = originalPos;
        }
    }
}
