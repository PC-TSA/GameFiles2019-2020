using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabReplacer : MonoBehaviour
{
    public Transform parentOfReplacees;
    public Transform newParent;
    public GameObject prefabReplacer;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
            Replace();
    }

    void Replace()
    {
        for (int i = 0; i < parentOfReplacees.childCount; i++)
        {
            Transform temp = parentOfReplacees.GetChild(i);
            GameObject newObj = Instantiate(prefabReplacer, temp.position, temp.rotation, newParent);
            newObj.transform.localScale = temp.localScale;
        }
        Destroy(parentOfReplacees.gameObject);
    }
}
