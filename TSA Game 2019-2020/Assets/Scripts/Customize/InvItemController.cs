using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvItemController : MonoBehaviour
{
    public bool isDragging = false;
    public Vector3 screenPoint;
    public Vector3 offset;
    public Vector3 originalPos;
    public bool shouldBeDropped = false; //If true, snap to a slot position instead of returning to original pos

    // Start is called before the first frame update
    void Start()
    {
        
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
        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        originalPos = transform.position;
        isDragging = true;
        shouldBeDropped = false;
    }

    public void EndDrag()
    {
        isDragging = false;
        if(!shouldBeDropped)
            transform.position = originalPos;
    }
}
