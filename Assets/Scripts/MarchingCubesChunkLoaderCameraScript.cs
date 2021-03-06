﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Generates chunk in the distance and hides them is they get too far away
public class MarchingCubesChunkLoaderCameraScript : MonoBehaviour
{
    public int updateTime;//How much to update the chunks. Lower value means more chunk updates, which means more chunk lag
    public int chunkDistance;//The maximum distance we can see chunks at
    private MarchingCubesTerrainScript terrainScript;//Terrain generator
    private Dictionary<int, MarchingCubesTerrainScript.ChunkData> loadedChunks;
    private Vector3Int chunkCoordinates;//Chunk coordinates of the camera
    private Vector3Int oldChunkCoordinates;//Chunk coordinates from last frame
    private float distanceFromCamera;//The distance from camera so we can position the chunk generation origin perfectly in front of the camera without having to draw chunks behind the camera
    [HideInInspector]
    public bool canGenerateChunks = false;
    public float distanceFromCameraOffset;//Offset to add to camera distance
    MarchingCubesTerrainScript.GetChunksInCubeForEach forEachChunk;
    // Start is called before the first frame update
    void Start()
    { 
        terrainScript = GameObject.FindObjectOfType<MarchingCubesTerrainScript>();
        loadedChunks = new Dictionary<int, MarchingCubesTerrainScript.ChunkData>();
        distanceFromCamera = terrainScript.TransformCoordinatesChunkToWorld(chunkDistance, 0, 0).x;
        forEachChunk = new MarchingCubesTerrainScript.GetChunksInCubeForEach(ForEachChunk);

        
    }

    // Update is called once per frame
    void Update()
    {
        if (canGenerateChunks && Time.frameCount % updateTime == 0)
        {
            chunkCoordinates = terrainScript.TransformCoordinatesWorldToChunk(transform.position + transform.forward * (distanceFromCamera + distanceFromCameraOffset));
            terrainScript.GenerateChunk(transform.position, true, false);//The chunk we are currently on has the highest priority         
            if (oldChunkCoordinates != chunkCoordinates)
            {
                foreach (var chunkLoopVar in loadedChunks)
                {
                   terrainScript.SetChunkVisibility(chunkLoopVar.Value, false);
                }
                loadedChunks.Clear();
                
                terrainScript.GetChunksInCube(chunkDistance, chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z, forEachChunk);            
            }
            oldChunkCoordinates = chunkCoordinates;
            
        }
    }
    //Delegate function for each chunk in the GetChunksInCube
    public void ForEachChunk(int x, int y, int z, Vector3 worldPosition, MarchingCubesTerrainScript.ChunkData chunk) 
    {
        terrainScript.GenerateChunk(x, y, z, true, false);
        terrainScript.SetChunkVisibility(x, y, z, true);
        MarchingCubesTerrainScript.ChunkData _chunk = terrainScript.GetChunk(x, y, z);
        loadedChunks.Add(loadedChunks.Count, _chunk);
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
