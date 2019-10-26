using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    //public float timestamp;
    public Vector3 pos;
    public int lane;

    public Note(int laneInt, Vector3 posVector)
    {
        lane = laneInt;
        pos = posVector;
    }

    public Note()
    {

    }
}
