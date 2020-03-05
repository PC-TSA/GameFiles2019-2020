using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabReplacer : MonoBehaviour
{
    public Transform parentOfReplacees;
    public Transform newParent;
    public GameObject prefabReplacer;

    public bool randomizeRotation;
    public bool randomizeSize;
    public float sizeMultiplierMin;
    public float sizeMultiplierMax;

    public bool disableInsteadOfDelete;

    public void Replace()
    {
        for (int i = 0; i < parentOfReplacees.childCount; i++)
        {
            Transform temp = parentOfReplacees.GetChild(i);
            GameObject newObj = Instantiate(prefabReplacer, temp.position, temp.rotation, newParent);

            if(randomizeSize)
                newObj.transform.localScale = new Vector3(temp.localScale.x * Random.Range(sizeMultiplierMin, sizeMultiplierMax), temp.localScale.y * Random.Range(sizeMultiplierMin, sizeMultiplierMax), temp.localScale.z * Random.Range(sizeMultiplierMin, sizeMultiplierMax));
            else
                newObj.transform.localScale = temp.localScale;

            if (randomizeRotation)
                newObj.transform.localRotation = Quaternion.Euler(new Vector3(newObj.transform.localRotation.x, Random.Range(0, 360), newObj.transform.localRotation.z));

            newObj.transform.position = temp.position;
        }
        if (disableInsteadOfDelete)
            parentOfReplacees.gameObject.SetActive(false);
        else
            DestroyImmediate(parentOfReplacees.gameObject);
    }
}
