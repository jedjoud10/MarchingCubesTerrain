using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Generates chunk in the distance and hides them is they get too far away
public class MarchingCubesChunkLoaderCameraScript : MonoBehaviour
{
    public int updateTime;//How much to update the chunks. Lower value means more chunk updates, which means more chunk lag
    public int chunkDistance;//The maximum distance we can see chunks at
    public bool keepChunksLoaded;//Keeps the generated chunks loaded even if we get far away from them
    private MarchingCubesTerrainScript terrainScript;//Terrain generator
    private Dictionary<int, MarchingCubesTerrainScript.ChunkData> loadedChunks;
    private Vector3Int chunkCoordinates;//Chunk coordinates of the camera
    private Vector3Int oldChunkCoordinates;//Chunk coordinates from last frame
    [HideInInspector]
    public bool canGenerateChunks = false;
    // Start is called before the first frame update
    void Start()
    {
        terrainScript = GameObject.FindObjectOfType<MarchingCubesTerrainScript>();
        loadedChunks = new Dictionary<int, MarchingCubesTerrainScript.ChunkData>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (canGenerateChunks && Time.frameCount % updateTime == 0)
        {
            chunkCoordinates = terrainScript.TransformCoordinatesWorldToChunk(transform.position + transform.forward * 10);
            if (oldChunkCoordinates != chunkCoordinates)
            {
                terrainScript.GenerateChunk(transform.position, true, false);
                if (!keepChunksLoaded) 
                {
                    foreach (var chunk in loadedChunks) 
                    {
                        terrainScript.SetChunkVisibility(chunk.Value, false);
                    }
                    loadedChunks.Clear();
                }
                for (int x = -chunkDistance; x < chunkDistance; x++)
                {
                    for (int y = -chunkDistance; y < chunkDistance; y++)
                    {
                        for (int z = -chunkDistance; z < chunkDistance; z++)
                        {
                            int xc, yc, zc;
                            xc = x + chunkCoordinates.x;
                            yc = y + chunkCoordinates.y;
                            zc = z + chunkCoordinates.z;
                            terrainScript.GenerateChunk(xc, yc, zc, true, false);
                            if (!keepChunksLoaded) terrainScript.SetChunkVisibility(xc, yc, zc, true);
                            MarchingCubesTerrainScript.ChunkData chunk = terrainScript.GetChunk(xc, yc, zc);
                            if (!loadedChunks.ContainsValue(chunk))
                            {
                                loadedChunks.Add(loadedChunks.Count, chunk);
                            }
                        }
                    }
                }
            }
            oldChunkCoordinates = chunkCoordinates;
        }
    }
    private void OnDrawGizmos()
    {
        if(terrainScript == null) terrainScript = GameObject.FindObjectOfType<MarchingCubesTerrainScript>();
        Gizmos.DrawWireSphere(terrainScript.TransformCoordinatesChunkToWorld(chunkCoordinates), 1);
        if (loadedChunks != null && loadedChunks.Count != 0)
        {
            foreach (var chunk in loadedChunks)
            {
                Gizmos.color = Color.green;
                if (chunk.Value.chunkScript == null) continue;
                Gizmos.DrawWireSphere(chunk.Value.chunkScript.transform.position, 1);
            }
        }
    }
}
