using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(MarchingCubesEditor))]
public class MarchingCubesEditorCustomEditor : Editor
{
    private RaycastHit hit;
    void OnSceneGUI()
    {
        if (Event.current.type == EventType.MouseMove)
        {
            if(Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out hit))
            {
                MarchingCubesEditor editorScript = (MarchingCubesEditor)target;
                editorScript.hitPoint = hit.point; 
            }
        }
    }
}
