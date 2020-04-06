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
            myScript.GenerateChunks(false, true, true);
        }
        if(GUILayout.Button("Fix Chunk Seams")) 
        {
            myScript.FixChunkSeams();
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Chunks"))
        {
            myScript.SaveChunksData();
        }
        if (GUILayout.Button("Load Chunks"))
        {
            myScript.LoadChunksData();
        }
        GUILayout.EndHorizontal();
    }
}