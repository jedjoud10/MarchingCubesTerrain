using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingCubesEditor))]
public class MarchingCubesEditorCustomEditor : Editor
{
    void OnSceneGUI()
    {
        MarchingCubesEditor editorScript = (MarchingCubesEditor)target;

        Vector2 mousePos = Event.current.mousePosition;
        bool mouseButton = Event.current.button != 0;
        bool invertBrush = Event.current.shift;
        mousePos.y = Camera.current.pixelHeight - mousePos.y;

        Vector3 worldPosition = Vector3.zero;
        RaycastHit hit;

        if (Physics.Raycast(Camera.current.ScreenPointToRay(mousePos), out hit))
        {
            worldPosition = hit.point;
            editorScript.hitPoint = worldPosition;
            editorScript.addDensity = mouseButton;
            editorScript.invertBrush = invertBrush;
        }
        else
        {
            editorScript.addDensity = false;//Disable brush because we are not currently painting on the terrain
        }
    }
}
