using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjDragRotate : MonoBehaviour
{
    public float rotSpeed = 20;
    public bool x;
    public bool y;


    void OnMouseDrag()
    {
        float rotX = Input.GetAxis("Mouse X") * rotSpeed * Mathf.Deg2Rad;
        float rotY = Input.GetAxis("Mouse Y") * rotSpeed * Mathf.Deg2Rad;

        if(x)
            transform.RotateAround(Vector3.up, -rotX);
        if(y)
            transform.RotateAround(Vector3.right, rotY);
    }
}
