using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //Update current mesh from multithreaded method
    public void UpdateMesh(Mesh _mesh) 
    {
        mesh = _mesh;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
