using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderObj
{
    //public float timestamp;
    public Vector3 pos;
    public float height;
    public int lane;

    public SliderObj(int laneInt, Vector3 posVector)
    {
        lane = laneInt;
        pos = posVector;
    }

    public SliderObj() { }
}
