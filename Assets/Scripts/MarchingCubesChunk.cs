using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof (MeshFilter))]
[RequireComponent(typeof (MeshRenderer))]
[RequireComponent(typeof (MeshCollider))]
[System.Serializable]
public class MarchingCubesChunk : MonoBehaviour
{
    private MeshRenderer chunkRenderer;
    [HideInInspector]
    public int x, y, z;
    // Start is called before the first frame update
    void Start()
    {        

    }
    //Starts generating this chunk
    public void StartChunk(MarchingCubesTerrainScript terrain, int _x, int _y, int _z) 
    {
        chunkRenderer = GetComponent<MeshRenderer>();
        chunkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        chunkRenderer.material = terrain.material;
        x = _x; y = _y; z = _z;
    }
    //Update current mesh
    public void UpdateMesh(Mesh _mesh, bool generateCollisions) 
    {
        if(generateCollisions) GetComponent<MeshCollider>().sharedMesh = _mesh;
        GetComponent<MeshFilter>().sharedMesh = _mesh;        
    }
    //Hide chunk
    public void OnHideChunk() 
    {
        if (chunkRenderer == null) return;
        chunkRenderer.enabled = false;
    }
    //Show chunk
    public void OnShowChunk()
    {
        if (chunkRenderer == null) return;
        chunkRenderer.enabled = true;
    }
}
