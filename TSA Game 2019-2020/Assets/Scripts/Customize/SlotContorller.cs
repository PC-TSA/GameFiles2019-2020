using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotContorller : MonoBehaviour
{
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
