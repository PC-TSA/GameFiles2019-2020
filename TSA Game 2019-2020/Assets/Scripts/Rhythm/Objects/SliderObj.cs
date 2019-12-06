using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderObj
{
    public int lane;
    public Vector3 pos;
    public float childY;
    public float height;
    public float colliderSizeY;
    public float colliderCenterY;

    public SliderObj(int laneInt, Vector3 posVector)
    {
        lane = laneInt;
        pos = posVector;
    }

    public SliderObj(int laneInt, Vector3 posVector, float childYFloat, float heightFloat, float colliderSizeYFloat, float colliderCenterYFloat)
    {
        lane = laneInt;
        pos = posVector;
        childY = childYFloat;
        height = heightFloat;
        colliderSizeY = colliderSizeYFloat;
        colliderCenterY = colliderCenterYFloat;
    }

    public SliderObj() { }
}
