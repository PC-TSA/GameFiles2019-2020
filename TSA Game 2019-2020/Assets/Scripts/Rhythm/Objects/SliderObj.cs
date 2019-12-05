using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderObj
{
    //public float timestamp;
    public Vector3 pos;
    public float childY;
    public float height;
    public float colliderSizeY;
    public int lane;

    public SliderObj(int laneInt, Vector3 posVector)
    {
        lane = laneInt;
        pos = posVector;
    }

    public SliderObj(int laneInt, Vector3 posVector, float childYFloat, float heightFloat, float colliderSizeYFloat)
    {
        lane = laneInt;
        pos = posVector;
        childY = childYFloat;
        height = heightFloat;
        colliderSizeY = colliderSizeYFloat;
    }

    public SliderObj() { }
}
