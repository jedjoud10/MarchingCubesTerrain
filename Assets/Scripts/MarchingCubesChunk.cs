using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof (MeshFilter))]
[RequireComponent(typeof (MeshRenderer))]
[RequireComponent(typeof (MeshCollider))]

public class MarchingCubesChunk : MonoBehaviour
{
    private MeshRenderer renderer;
    [HideInInspector]
    public int x, y, z;
    // Start is called before the first frame update
    void Start()
    {        

    }
    //Starts generating this chunk
    public void StartChunk(MarchingCubesTerrainScript terrain) 
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        renderer.material = terrain.material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //Update current mesh
    public void UpdateMesh(Mesh _mesh) 
    {
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        GetComponent<MeshCollider>().sharedMesh = _mesh;
    }
    //Hide chunk
    public void OnHideChunk() 
    {
        if (renderer == null) return;
        renderer.enabled = false;
    }
    //Show chunk
    public void OnShowChunk()
    {
        if (renderer == null) return;
        renderer.enabled = true;
    }
}
