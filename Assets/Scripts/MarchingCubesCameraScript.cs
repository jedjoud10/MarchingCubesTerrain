﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Generates chunk in the distance and hides them is they get too far away
public class MarchingCubesCameraScript : MonoBehaviour
{
    public int chunkDistance;//The maximum distance we can see chunks at
    public bool keepChunksLoaded;//Keeps the generated chunks loaded even if we get far away from them
    private MarchingCubesTerrainScript terrainScript;//Terrain generator
<<<<<<< HEAD
    private HashSet<MarchingCubesChunk> loadedChunks;
    private Vector3Int chunkCoordinates;//Chunk coordinates of the camera
    private Vector3Int oldChunkCoordinates;//Chunk coordinates from last frame
    [HideInInspector]
    public bool canGenerateChunks = false;    
=======
    private List<MarchingCubesChunk> loadedChunks;
    private Vector3Int chunkCoordinates;
>>>>>>> parent of de138c1... Optimized a bit by turning classes into structs
    // Start is called before the first frame update
    void Start()
    {
        terrainScript = GameObject.FindObjectOfType<MarchingCubesTerrainScript>();
        loadedChunks = new HashSet<MarchingCubesChunk>(); 
    }

    // Update is called once per frame
    void Update()
    {
        terrainScript.GenerateChunk(transform.position, false);
        if(!keepChunksLoaded) terrainScript.SetChunksVisibility(false);
        for (int x = -chunkDistance; x < chunkDistance; x++)
        {
<<<<<<< HEAD
            chunkCoordinates = terrainScript.TransformCoordinatesWorldToChunk(transform.position);//Get chunk at our exact position
            if (oldChunkCoordinates != chunkCoordinates)//If we moved from last frame chunk
=======
            for (int y = -chunkDistance; y < chunkDistance; y++)
>>>>>>> parent of de138c1... Optimized a bit by turning classes into structs
            {
                for (int z = -chunkDistance; z < chunkDistance; z++)
                {
                    int xc, yc, zc;
                    chunkCoordinates = terrainScript.TransformCoordinatesWorldToChunk(transform.position);
                    xc = x + chunkCoordinates.x;
                    yc = y + chunkCoordinates.y;
                    zc = z + chunkCoordinates.z;
                    terrainScript.GenerateChunk(xc, yc, zc, false);
                    if (!keepChunksLoaded) terrainScript.SetChunkVisibility(xc, yc, zc, true);
                    MarchingCubesChunk chunk = terrainScript.GetChunk(xc, yc, zc);
                    if (!loadedChunks.Contains(chunk))
                    {
<<<<<<< HEAD
                        for (int z = -chunkDistance; z < chunkDistance; z++)
                        {
                            int xc, yc, zc;
                            xc = x + chunkCoordinates.x;
                            yc = y + chunkCoordinates.y;
                            zc = z + chunkCoordinates.z;
                            terrainScript.GenerateChunk(xc, yc, zc, false);
                            if (!keepChunksLoaded) terrainScript.SetChunkVisibility(xc, yc, zc, true);
                            MarchingCubesChunk chunk = terrainScript.GetChunk(xc, yc, zc);
                            if(!loadedChunks.Contains(chunk)) loadedChunks.Add(chunk);                            
                        }
                    }
=======
                        loadedChunks.Add(chunk);
                    }                    
>>>>>>> parent of de138c1... Optimized a bit by turning classes into structs
                }
            }
        }        
        if(!keepChunksLoaded)loadedChunks.Clear();
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
                if (chunk == null) continue;
                Gizmos.DrawWireSphere(chunk.transform.position, 1);
            }
        }
    }
}
