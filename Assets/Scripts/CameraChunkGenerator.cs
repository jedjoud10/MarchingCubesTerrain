using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Generates chunks/ unloads them based on distance to camera
public class CameraChunkGenerator : MonoBehaviour
{
    private List<List<List<MarchingCubeChunk>>> chunks;
    private MarchingCubesTerrainScript terrainScript;
    private Vector3 position;
    int xs, ys, zs;//Number of chunks that can be loaded
    // Start is called before the first frame update
    void Start()
    {
        terrainScript = GameObject.FindObjectOfType<MarchingCubesTerrainScript>();
        GenerateChunks();
    }
    void GenerateChunks() 
    {
        xs = terrainScript.worldSize.x;
        ys = terrainScript.worldSize.y;
        zs = terrainScript.worldSize.z;
        float chunkSize = terrainScript.size * terrainScript.cubeSize;
        GameObject chunk;
        Material mat = terrainScript.material;
        for (int x = 0; x < xs; x++)
        {
            for (int y = 0; y < ys; y++)
            {
                for (int z = 0; z < zs; z++)
                {
                    chunk = new GameObject();
                    chunk.name = "Chunk : " + x + "-" + y + "-" + z;
                    chunk.transform.position = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                    chunk.AddComponent<MeshFilter>();
                    chunk.AddComponent<MeshRenderer>();
                    chunk.GetComponent<MeshRenderer>().material = mat;
                    chunk.AddComponent<MarchingCubeChunk>();
                    chunk.GetComponent<MarchingCubeChunk>().StartChunk(terrainScript);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
