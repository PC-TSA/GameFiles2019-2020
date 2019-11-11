using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceObj
{
    public Vector3 pos;
    public float width;

    public SpaceObj(float w, Vector3 posVector)
    {
        width = w;
        pos = posVector;
    }

    public SpaceObj() { }
}
