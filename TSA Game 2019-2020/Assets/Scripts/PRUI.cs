using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrefabReplacer))]
public class PRUI : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabReplacer myScript = (PrefabReplacer)target;
        if (GUILayout.Button("Replace"))
        {
            myScript.Replace();
        }
    }
}
