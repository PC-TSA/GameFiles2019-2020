using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotContorller : MonoBehaviour
{
    public int slotType; //0 = helmet, 1 = shirt, 2 = pants, 3 = shoes

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "InventoryItem")
            collision.GetComponent<InvItemController>().slot = gameObject;
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.tag == "InventoryItem" && collision.GetComponent<InvItemController>().slot == gameObject)
            collision.GetComponent<InvItemController>().slot = null;
    }
}
