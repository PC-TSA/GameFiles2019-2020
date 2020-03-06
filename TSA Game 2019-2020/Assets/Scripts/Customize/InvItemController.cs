using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvItemController : MonoBehaviour
{
    public bool isDragging = false;
    public Vector3 originalPos;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isDragging)
        {
            Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y)); ;
            transform.position = newPos;    
        }
    }

    public void StartDrag()
    {
        originalPos = transform.position;
        isDragging = true;
    }

    public void EndDrag()
    {
        isDragging = false;
    }
}
