using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof (MeshFilter))]
[RequireComponent(typeof (MeshRenderer))]
[RequireComponent(typeof (MeshCollider))]

public class MarchingCubeChunk : MonoBehaviour
{
    private MarchingCubesTerrainScript terrainGenerator;
    private Mesh mesh; 
    // Start is called before the first frame update
    void Start()
    {
        
    }
    //Starts generating this chunk
    public void StartChunk(MarchingCubesTerrainScript terrain) 
    {
        terrainGenerator = terrain;
        terrainGenerator.MarchCube(transform.position, this);
        GetComponent<MeshRenderer>().material = terrain.material;
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //Update current mesh
    public void UpdateMesh(Mesh _mesh) 
    {
        mesh = _mesh;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
