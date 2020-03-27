using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingCubesTerrainScript))]
public class ObjectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MarchingCubesTerrainScript myScript = (MarchingCubesTerrainScript)target;
        if (GUILayout.Button("Generate chunks"))
        {
            myScript.GenerateChunks();
        }
    }
}