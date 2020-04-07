using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
//Marching cubes terrain script
public class MarchingCubesTerrainScript : MonoBehaviour
{
    public float threshold;//If density value is bigger than this, there is terrain at that 3D point

    #region Explanation of chunk threshold
    /// <summary>
    /// Before making a chunk, we could optimize this terrain by checking if the position of that chunk itself is terrain, if it is then we can generate that chunk
    /// if it is not, we dont have to generate that chunk because we know there is no terrain. But what if the position of the chunk is somewhere where there is not air, but the chunk is actually filled with terrain ? 
    /// Well then we should have another threshold value that controls if those empty (or not) chunks should be generated
    /// We can turn this value low (-1) to make those more empty spaced chunks to generate
    /// Or we can turn it high (0) to make them not generate (Which might cause chunks to not be generated, so it might corrupt the terrain a little) 
    /// </summary>
    #endregion
    public float chunkThreshold;
    public float cubeSize;//The size of the marched cube
    public bool onValidate;//Should we update the terrain if we change one of the parameters in the editor
    public bool interpolation;//Should we use interpolation for the edges points
    public bool mergeVertices;//Merges close vertices to a single vertex
    public float mergeDistance;//distance to merge closest vertices
    public bool generateAtStart;//Should we generate the whole world at start of the game ?
    public bool generateChunks;//Should we generate the chunks or just leave them empty ? Debug purposes
    public bool generateCollisions;//Applies the new MarchedCube mesh to the collision for the chunk
    public bool visibiltyAtStart;//The visibility of the chunks at the start of the game
    public bool useDensities;//Should we save the densities and reuse them ?
    public int size;//Size container for each chunk of the marched cube
    public Vector3Int worldSize;//How much chunks we have in the world
    public Material material;
    private ChunkData[,,] chunks;
    private MarchingCubesDensityScript densityCalculator;//Calculates density at a 3d point for us
    private List<QueuedChunkMeshData> queuedMeshDataChunks;
    public static int[,] triangulation = new int[256, 16]{
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
    { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
    { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
    { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
    { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
    { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
    { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
    { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
    { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
    { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
    { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
    { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
    { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
    { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
    { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
    { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
    { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
    { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
    { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
    { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
    { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
    { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
    { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
    { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
    { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
    { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
    { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
    { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
    { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
    { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
    { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
    { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
    { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
    { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
    { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
    { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
    { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
    { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
    { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
    { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
    { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
    { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
    { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
    { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
    { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
    { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
    { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
    { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
    { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
    { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
    { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
    { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
    { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
    { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
    { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
    { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
    { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
    { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
    { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
    { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
    { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
    { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
    { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
    { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
    { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
    { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
    { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
    { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
    { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
    { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
    { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
    { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
    { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
    { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
    { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
    { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
    { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
    { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
    { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
    { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
    { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
    { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
    { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
    { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
    { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
    { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
    { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
    { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
    { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
    { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
    { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
    { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
    { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
    { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
    { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
    { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
    { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
    { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
    { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
    { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
    { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
    { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
    { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
    { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
    { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
    { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
    { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
    { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
    { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
    { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
    { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
    };//Triangulation table from https://paulbourke.net/geometry/polygonise/


    public GameObject chunkPrefab;

    // Start is called before the first frame update
    void Start()
    {
        //Setup variables and disactivate the terrain's generator preview chunk
        onValidate = false;
        if (densityCalculator == null) densityCalculator = GetComponent<MarchingCubesDensityScript>();
        Destroy(GetComponent<MarchingCubesChunk>());
        Destroy(GetComponent<MeshFilter>());
        Destroy(GetComponent<MeshRenderer>());


        chunks = new ChunkData[worldSize.x, worldSize.y, worldSize.z];//New chunks
        queuedMeshDataChunks = new List<QueuedChunkMeshData>();
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    chunks[x, y, z].chunkScript = null;
                }
            }
        }//Init chunk scripts
        foreach (var cameras in GameObject.FindObjectsOfType<MarchingCubesChunkLoaderCameraScript>())
        {
            cameras.canGenerateChunks = true;
        }//Init Chunk camera loaders
        queuedMeshDataChunks.Clear();
        if (generateAtStart)
        {
            GenerateChunks(true, false, false);
            SetChunksVisibility(visibiltyAtStart);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (queuedMeshDataChunks != null && queuedMeshDataChunks.Count != 0)
        {
            //Debug.Log("Generate Mesh from MeshData for chunk :" + queuedMeshDataChunks[0].chunk.x + "-" + queuedMeshDataChunks[0].chunk.y + "-" + queuedMeshDataChunks[0].chunk.z);
            QueuedChunkMeshData queuedData = queuedMeshDataChunks[0];
            if (queuedData.chunk == null) return;
            Mesh mesh = GenerateMeshFromData(queuedData.finalMesh);
            queuedData.chunk.UpdateMesh(mesh, generateCollisions);
            chunks[queuedData.x, queuedData.y, queuedData.z].chunkMesh = mesh;

            queuedMeshDataChunks.RemoveAt(0);
        }
    }
    #region Threads
    private struct QueuedChunkMeshData //Struct to be added to queue when thread finished calculating
    {
        public MarchedCubeMeshData finalMesh;
        public MarchingCubesChunk chunk;
        public int x, y, z;
    }
    //Marches the cube in a x*x*x grid and generates a MarchedCubeMeshData out of it in another thread
    private void GenerateMeshDataThread(Vector3 _position, MarchingCubesChunk chunk, MarchedCube marchedCube, int x, int y, int z)
    {
        //Debug.Log("Started new chunk Thread for chunk : " + chunk.x + "-" + chunk.y + "-" + chunk.z);
        QueuedChunkMeshData queuedMeshData = new QueuedChunkMeshData();
        queuedMeshData.finalMesh = MarchCube(_position, marchedCube);
        queuedMeshData.chunk = chunk;//Set chunk so we can call it back when we want to update its mesh
        queuedMeshData.x = x;
        queuedMeshData.y = y;
        queuedMeshData.z = z;
        lock (queuedMeshDataChunks)
        {
            queuedMeshDataChunks.Add(queuedMeshData);//Add to list so it can be processed later        
        }
    }
    #endregion 
    #region Mesh generation
    public struct MarchedCubeMeshData //Information about the mesh
    {
        public List<Vector3> vertices;
        public List<int> triangles;
    }
    //Creates Mesh out of the MarchedCubeMeshData
    private Mesh GenerateMeshFromData(MarchedCubeMeshData meshData)
    {
        //Copy final meshData to real mesh
        Mesh mesh = new Mesh();

        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();

        mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    //Marches the cube in a x*x*x grid and generates a MarchedCubeMeshData out of it
    private MarchedCubeMeshData MarchCube(Vector3 _position, MarchedCube marchedCube)
    {
        MarchedCubeMeshData meshData;//Variable that will be helpful for storing and moving mesh data
        meshData.vertices = new List<Vector3>();
        meshData.triangles = new List<int>();
        //Output meshes
        MarchedCubeMeshData newmesh;

        List<MarchedCubeMeshData> combineMeshesList = new List<MarchedCubeMeshData>();

        int outcase;
        Vector3 posInsideChunk;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    posInsideChunk.x = x * cubeSize; posInsideChunk.y = y * cubeSize; posInsideChunk.z = z * cubeSize;
                    outcase = marchedCube.MarchCube(posInsideChunk + _position, x, y, z);//Get density points at this location
                    meshData = GenerateMeshFromCase(outcase, _position, marchedCube);//Create a small mesh section out of the density points
                    //Update current mesh
                    newmesh = new MarchedCubeMeshData
                    {
                        vertices = meshData.vertices,
                        triangles = meshData.triangles
                    };

                    //Add a new meshData to combine later               
                    combineMeshesList.Add(newmesh);
                }
            }
        }
        MarchedCubeMeshData outputMesh = CombineMeshes(combineMeshesList);
        if (mergeVertices) outputMesh = AutoWeld(outputMesh, mergeDistance);
        return outputMesh;
    }
    //Combine multiple MarchingCubeMeshData together
    private MarchedCubeMeshData CombineMeshes(List<MarchedCubeMeshData> meshDatas)
    {
        MarchedCubeMeshData outputMeshData;
        outputMeshData = new MarchedCubeMeshData();

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();

        MarchedCubeMeshData currentMeshData;

        List<int> tempTriangles;
        for (int i = 0; i < meshDatas.Count; i++)
        {
            currentMeshData = meshDatas[i];
            tempTriangles = currentMeshData.triangles;
            for (int x = 0; x < tempTriangles.Count; x++)
            {
                tempTriangles[x] += vertices.Count;
            }
            triangles.AddRange(tempTriangles);
            vertices.AddRange(currentMeshData.vertices);
        }

        outputMeshData.triangles = triangles;
        outputMeshData.vertices = vertices;

        return outputMeshData;
    }
    //Modified. Merge vertices that are close to each other https://answers.unity.com/questions/228841/dynamically-combine-verticies-that-share-the-same.html
    private MarchedCubeMeshData AutoWeld(MarchedCubeMeshData mesh, float threshold)
    {
        List<Vector3> verts = mesh.vertices;

        // Build new vertex buffer and remove "duplicate" verticies
        // that are within the given threshold.
        List<Vector3> newVerts = new List<Vector3>();

        int k = 0;

        foreach (Vector3 vert in verts)
        {
            // Has vertex already been added to newVerts list?
            foreach (Vector3 newVert in newVerts)
                if (Vector3.Distance(newVert, vert) <= threshold)
                    goto skipToNext;

            // Accept new vertex!
            newVerts.Add(vert);

            skipToNext:;
            ++k;
        }

        // Rebuild triangles using new verticies
        List<int> tris = mesh.triangles;
        for (int i = 0; i < tris.Count; ++i)
        {
            // Find new vertex point from buffer
            for (int j = 0; j < newVerts.Count; ++j)
            {
                if (Vector3.Distance(newVerts[j], verts[tris[i]]) <= threshold)
                {
                    tris[i] = j;
                    break;
                }
            }
        }

        // Update mesh!
        mesh.vertices = newVerts;
        mesh.triangles = tris;
        return mesh;
    }
    //Generate mesh out of the triangulation table and the marchedcube data
    private MarchedCubeMeshData GenerateMeshFromCase(int outcase, Vector3 offsetpos, MarchedCube marchedCube)
    {
        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        int currenttriindex;
        for (int i = 0; i < 16; i++)
        {
            currenttriindex = triangulation[outcase, i];
            if (currenttriindex != -1)
            {
                triangles.Add(currenttriindex);//Make triangle face
            }
        }
        for (int i = 0; i < 12; i++)
        {
            if (triangles.Contains(i))
            {
                vertices.Add((marchedCube.GetEdgePoint(i, interpolation) - offsetpos));//Add vertex at correct position
            }
            else
            {
                vertices.Add(Vector3.zero);//We dont need to make this unused vertex compute its position
            }
        }
        triangles.Reverse();//Invert the normals of the faces
        MarchedCubeMeshData mesh = new MarchedCubeMeshData();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }
    #endregion
    #region Chunk related
    public struct ChunkData //Information about a single chunk
    {
        public MarchedCube cube;
        public MarchingCubesChunk chunkScript;
        public Mesh chunkMesh;
        public int x, y, z;//Chunk coordinates
    }
    //Generates all the chunks of the terrain
    public void GenerateChunks(bool multithreaded, bool update, bool inEditor)
    {
        if (inEditor && chunks == null) chunks = new ChunkData[worldSize.x, worldSize.y, worldSize.z];
        if (chunks.GetLength(0) != worldSize.x || chunks.GetLength(1) != worldSize.y || chunks.GetLength(2) != worldSize.z) chunks = new ChunkData[worldSize.x, worldSize.y, worldSize.z];
        if (inEditor) chunks[0, 0, 0].chunkScript = GetComponent<MarchingCubesChunk>();


        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    GenerateChunk(x, y, z, multithreaded, update);
                }
            }
        }
    }
    //Generate a single chunk
    public void GenerateChunk(int x, int y, int z, bool multithreaded, bool update)
    {
        if (x < 0 || y < 0 || z < 0) return;
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return;
        Vector3 chunkPos = TransformCoordinatesChunkToWorld(x, y, z);
        ChunkData chunk = chunks[x, y, z];
        if (chunk.chunkScript == null)//Generate new chunks if they dont exist yet
        {
            GameObject chunkGameObject;
            chunkGameObject = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);

            chunkGameObject.name = string.Concat(x, "-", y, "-", z);
            MarchingCubesChunk chunkScript = chunkGameObject.GetComponent<MarchingCubesChunk>();
            MarchedCube marchedCube = new MarchedCube();//Creates an Instance of the MarchedCubeClass with all the parameters
            marchedCube.Setup(cubeSize, threshold, densityCalculator, size, useDensities, chunkPos);
            chunkScript.StartChunk(this, x, y, z);
            if (!multithreaded)
            {
                chunk.chunkScript = chunkScript;
                if (densityCalculator.Density(chunkPos) < chunkThreshold) return;//This chunk is not filled with terrain
                if (generateChunks) chunkScript.UpdateMesh(GenerateMeshFromData(MarchCube(chunkScript.transform.position, marchedCube)), generateCollisions);
                chunk.cube = marchedCube;
                chunk.x = x; chunk.y = y; chunk.z = z;
                chunks[x, y, z] = chunk;
            }
            else
            {
                chunk.chunkScript = chunkScript;
                if (densityCalculator.Density(chunkPos) < chunkThreshold) return;//This chunk is not filled with terrain
                if (generateChunks)
                {
                    ThreadPool.QueueUserWorkItem(state => GenerateMeshDataThread(chunkPos, chunkScript, marchedCube, x, y, z));
                }
                chunks[x, y, z] = chunk;

            }
        }
        else if (update)
        {
            MarchingCubesChunk chunkScript = chunk.chunkScript;
            MarchedCube marchedCube = chunk.cube;//Creates an Instance of the MarchedCubeClass with all the parameters
            marchedCube.Setup(cubeSize, threshold, densityCalculator, size, useDensities, chunkPos);
            if (!multithreaded)
            {
                if (generateChunks) chunkScript.UpdateMesh(GenerateMeshFromData(MarchCube(chunkScript.transform.position, marchedCube)), generateCollisions);
                chunk.cube = marchedCube;
                chunks[x, y, z] = chunk;
            }
            else
            {
                if (generateChunks)
                {
                    ThreadPool.QueueUserWorkItem(state => GenerateMeshDataThread(chunkPos, chunkScript, marchedCube, x, y, z));
                }
            }
        }
    }
    //Generate a single chunk using world coordinates
    public void GenerateChunk(Vector3 pos, bool multithreaded, bool update)
    {
        Vector3Int chunkCoords = TransformCoordinatesWorldToChunk(pos);
        GenerateChunk(chunkCoords.x, chunkCoords.y, chunkCoords.z, multithreaded, update);
    }
    #region Coordinates
    //Transform chunk coordinates into world coordinates
    public Vector3 TransformCoordinatesChunkToWorld(int x, int y, int z)
    {
        return new Vector3(x * size * cubeSize, y * size * cubeSize, z * size * cubeSize);
    }
    //Transform chunk coordinates into world coordinates with vector3int
    public Vector3 TransformCoordinatesChunkToWorld(Vector3Int pos)
    {
        return new Vector3(pos.x * size * cubeSize, pos.y * size * cubeSize, pos.z * size * cubeSize);
    }
    //Transform world coordinates into chunk coordinates
    public Vector3Int TransformCoordinatesWorldToChunk(Vector3 pos)
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x / size / cubeSize), Mathf.RoundToInt(pos.y / size / cubeSize), Mathf.RoundToInt(pos.z / size / cubeSize));
    }
    #endregion
    #region Visibility
    //Set chunk visibility from world coordinates
    public void SetChunkVisibility(Vector3 pos, bool isVisible)
    {
        ChunkData chunk = GetChunk(pos);
        if (chunk.chunkScript == null) return;
        if (isVisible) chunk.chunkScript.OnShowChunk();
        else chunk.chunkScript.OnHideChunk();
    }
    //Set chunk visibility
    public void SetChunkVisibility(int x, int y, int z, bool isVisible)
    {
        ChunkData chunk = GetChunk(x, y, z);
        if (chunk.chunkScript == null) return;
        if (isVisible) chunk.chunkScript.OnShowChunk();
        else chunk.chunkScript.OnHideChunk();
    }
    //Set chunk visibility with already given chunk
    public void SetChunkVisibility(ChunkData chunk, bool isVisible)
    {
        if (chunk.chunkScript == null) return;
        if (isVisible) chunk.chunkScript.OnShowChunk();
        else chunk.chunkScript.OnHideChunk();
    }
    //Set chunk visibilty for all chunks
    public void SetChunksVisibility(bool isVisible)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.chunkScript == null) continue;
            if (isVisible) chunk.chunkScript.OnShowChunk();
            else chunk.chunkScript.OnHideChunk();
        }
    }
    #endregion
    #region Getting Chunks
    //Get chunk (Returns null if chunk is outside map bounds)
    public ChunkData GetChunk(Vector3 pos)
    {
        Vector3Int newpos = TransformCoordinatesWorldToChunk(pos);
        int x, y, z;
        x = newpos.x; y = newpos.y; z = newpos.z;
        if (x < 0 || y < 0 || z < 0) return new ChunkData();
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return new ChunkData();
        return chunks[x, y, z];
    }
    //Get chunk (Returns null if chunk is outside map bounds)
    public ChunkData GetChunk(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0) return new ChunkData();
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return new ChunkData();
        return chunks[x, y, z];
    }
    //Delegate to be called back whenever the last loop in GetChunksInCube has run
    public delegate void GetChunksInCubeForEach(int x, int y, int z, Vector3 worldPosition, ChunkData chunk);
    //Get chunks in a cube from chunk data
    public void GetChunksInCube(int cubeSize, int xs, int ys, int zs, GetChunksInCubeForEach delegateFunction) 
    {
        if (chunks == null || chunks.GetLength(0) != worldSize.x || chunks.GetLength(1) != worldSize.y || chunks.GetLength(2) != worldSize.z) chunks = new ChunkData[worldSize.x, worldSize.y, worldSize.z];
        int xc, yc, zc;//Position of the current chunk
        ChunkData chunk;//The current chunk that we are on
        for (int x = -cubeSize; x < cubeSize; x++)
        {
            for (int y = -cubeSize; y < cubeSize; y++)
            {
                for (int z = -cubeSize; z < cubeSize; z++)
                {
                    xc = x + xs;
                    yc = y + ys;
                    zc = z + zs;
                    chunk = GetChunk(xc, yc, zc);
                     delegateFunction(xc, yc, zc, TransformCoordinatesChunkToWorld(xc, yc, zc), chunk);//Call delegate function
                }
            }
        }
    }
    //Gets chunks in a cube from world space data (1/2)
    public void GetChunksInCube(int cubeSize, Vector3 pos, GetChunksInCubeForEach delegateFunction) 
    {
        Vector3Int chunkPos = TransformCoordinatesWorldToChunk(pos);
        GetChunksInCube(cubeSize, chunkPos.x, chunkPos.y, chunkPos.z, delegateFunction);
    }
    //Gets chunks in a cube from world space data (2/2) Adds the offset to the final cube size
    public void GetChunksInCube(float _cubeSize, int offset, Vector3 pos, GetChunksInCubeForEach delegateFunction) 
    {
        GetChunksInCube(Mathf.Max(Mathf.RoundToInt(_cubeSize / size / cubeSize), 1) + offset, pos, delegateFunction);
    }
    #endregion
    #endregion
    #region Editor
    //Makes densities modify their values for the specified chunk using a sphere brush
    public void EditChunkDensitiesSphereBrush(int chunkX, int chunkY, int chunkZ, Vector3 sphereCenter, float falloffDistance, float strenghBrush) 
    {
        if (!useDensities) return;//We dont use densities so no Editor for u haha
        ChunkData chunkData = GetChunk(chunkX, chunkY, chunkZ);
        if (chunkData.chunkScript == null)
        if (chunkData.chunkScript == null) return;//Bruh moment
        MarchedCube marchingCube = chunkData.cube;
        float[,,] densities = marchingCube.densities;
        Vector3 chunkPos = marchingCube.chunkPosition;
        Vector3 worldSpacePosition;//The world space position of the chunk density point
        int _size = size + 1;
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                for (int z = 0; z < _size; z++)
                {
                    worldSpacePosition.x = x * cubeSize + chunkPos.x;
                    worldSpacePosition.y = y * cubeSize + chunkPos.y;
                    worldSpacePosition.z = z * cubeSize + chunkPos.z;
                    densities[x, y, z] += Mathf.Max(falloffDistance - Vector3.Distance(sphereCenter, worldSpacePosition), 0) * strenghBrush;
                    //Debug.DrawRay(worldSpacePosition, Vector3.up * 0.5f, Color.red);
                }
            }
        }

        //Update variables
        //Copy chunk edge densities
        if (chunkX > 0)
        {
            marchingCube.CopyChunkEdgeDensitiesX(chunks[chunkX - 1, chunkY, chunkZ].cube);
        }
        if (chunkY > 0)
        {
            marchingCube.CopyChunkEdgeDensitiesY(chunks[chunkX, chunkY - 1, chunkZ].cube);
        }
        if (chunkZ > 0)
        {
            marchingCube.CopyChunkEdgeDensitiesZ(chunks[chunkX, chunkY, chunkZ - 1].cube);
        }

        marchingCube.readDensities = true;
        marchingCube.densities = densities;
        chunkData.cube = marchingCube;
        chunks[chunkX, chunkY, chunkZ] = chunkData;
        MarchingCubesChunk chunkScript = chunks[chunkX, chunkY, chunkZ].chunkScript;
        chunkScript.UpdateMesh(GenerateMeshFromData(MarchCube(marchingCube.chunkPosition, marchingCube)), generateCollisions);
    }
    //Temporary fix for the seams between the chunks
    public void FixChunkSeams() 
    {
        MarchedCube marchingCube;//The marchingcube that is going to fix up the chunk seams by copying the chunk edge densities
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    if (chunks[x, y, z].chunkScript == null) return;
                    marchingCube = chunks[x, y, z].cube;
                    //Copy chunk edge densities
                    if (x > 0)
                    {
                        marchingCube.CopyChunkEdgeDensitiesX(chunks[x - 1, y, z].cube);
                    }
                    if (y > 0)
                    {
                        marchingCube.CopyChunkEdgeDensitiesY(chunks[x, y - 1, z].cube);
                    }
                    if (z > 0)
                    {
                        marchingCube.CopyChunkEdgeDensitiesZ(chunks[x, y, z - 1].cube);
                    }
                    marchingCube.readDensities = true;
                    chunks[x, y, z].chunkScript.UpdateMesh(GenerateMeshFromData(MarchCube(marchingCube.chunkPosition, marchingCube)), generateCollisions);
                    chunks[x, y, z].cube = marchingCube;
                }
            }
        }
    }
    #endregion
    #region Saving/Loading

    [Serializable]
    struct SavedChunkData//Chunk data that is going to be serialized 
    {
        public float[] densities;
        public MarchingCubesChunk chunkScript;

        public int x, y, z;//Chunk coordinates

        public float cubeSize;
        public int size;//the size of the chunk
        public float threshold;//If density is higher than this, there is terrain at that point
        public bool useDensities;//Should we save densities and reuse them ?
        public MarchingCubesDensityScript densityCalculator;
        public Vector3 chunkPosition;//The position of the chunk in world space

    }
    [SerializeField]
    [HideInInspector]
    SavedChunkData[] chunksData;//1D data of chunks
    //Conversion methods from https://coderwall.com/p/fzni3g/bidirectional-translation-between-1d-and-3d-arrays
    //Saves all the chunks densities to a json file
    public void SaveChunksData() 
    {
        if (chunks.GetLength(0) != worldSize.x || chunks.GetLength(1) != worldSize.y || chunks.GetLength(2) != worldSize.z) Debug.LogError("Outdated chunk array");
        Debug.Log("Saving chunks data...");
        #if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        #endif
        SavedChunkData[] outputArray = new SavedChunkData[worldSize.x * worldSize.y * worldSize.z];
        int i = 0;//Index of chunk in flattened array
        SavedChunkData currentChunkData = new SavedChunkData();
        MarchedCube cube = new MarchedCube();
        foreach (var chunk in chunks)
        {
            currentChunkData.x = chunk.x; currentChunkData.y = chunk.y; currentChunkData.z = chunk.z;
            cube = chunk.cube;

            currentChunkData.densities = ConvertDensitiesArrayToSingle(cube.densities);
            currentChunkData.chunkScript = chunk.chunkScript;
            currentChunkData.cubeSize = cube.cubeSize;
            currentChunkData.threshold = cube.threshold;
            currentChunkData.densityCalculator = cube.densityCalculator;
            currentChunkData.size = cube.size;
            currentChunkData.useDensities = cube.useDensities;
            currentChunkData.chunkPosition = cube.chunkPosition;

            //Why did i do this i could've just made a function aaaaaaaaa
            outputArray[i] = currentChunkData;
            i++;
        }
        chunksData = outputArray;
    }
    //Loads all the chunks densities from a json file
    public void LoadChunksData() 
    {
        Debug.Log(chunksData);
        if(chunksData.Length != worldSize.x * worldSize.y * worldSize.z) 
        {
            Debug.LogWarning("Load was not found");
            SaveChunksData();
        }
        else
        {
            ChunkData[,,] outputArray = new ChunkData[worldSize.x, worldSize.y, worldSize.z];
            SavedChunkData currentChunkData;
            ChunkData currentChunk = new ChunkData();
            for (int i = 0; i < chunksData.Length; i++)
            {
                //Run for every chunk
                currentChunkData = chunksData[i];
                currentChunk.cube.densities = ConvertDensitiesArrayToMulti(currentChunkData.densities);
                currentChunk.chunkScript = currentChunkData.chunkScript;
                currentChunk.cube.SetVariables(currentChunkData.cubeSize, currentChunkData.threshold, currentChunkData.densityCalculator, currentChunkData.size, currentChunkData.useDensities, currentChunkData.chunkPosition);
                currentChunk.cube.readDensities = true;
                currentChunk.x = currentChunkData.x; currentChunk.y = currentChunkData.y; currentChunk.z = currentChunkData.z;
                outputArray[currentChunkData.x, currentChunkData.y, currentChunkData.z] = currentChunk;//Converto hah
                currentChunk.chunkScript.UpdateMesh(GenerateMeshFromData(MarchCube(currentChunk.cube.chunkPosition, currentChunk.cube)), generateCollisions);
            }
            chunks = outputArray;
        }
    }
    //Converts a 3D array into a 1D array. For densities only
    private float[] ConvertDensitiesArrayToSingle(float[,,] array) 
    {
        int _size = size + 1;//Correct array length
        float[] outputArray = new float[_size * _size * _size];
        int index = 0;
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
            {
                for (int z = 0; z < _size; z++)
                {
                    index = x + y * _size + z * _size * _size;
                    outputArray[index] = array[x, y, z];
                }
            }
        }
        return outputArray;
    }
    //Converts a 1D array into a 3D array. For densities only
    private float[,,] ConvertDensitiesArrayToMulti(float[] array) 
    {
        int _size = size + 1;//Correct array length
        float[,,] outputArray = new float[_size, _size, _size];
        int x, y, z;
        for (int i = 0; i < array.Length; i++)
        {
            x = i % _size;
            y = (i / _size) % _size;
            z = i / (_size * _size);
            outputArray[x, y, z] = array[i];
        }
        return outputArray;
    }

    #endregion
    #region Debug Stuff
    //Debug stuff
    private void OnValidate()
    {
        OnValidatePublic();
    }
    public void OnValidatePublic() //Make method so when we change other script's data the mesh also gets updated
    {
        if (onValidate)
        {
            //Set variables
            if (densityCalculator == null) densityCalculator = GetComponent<MarchingCubesDensityScript>();
            MarchedCube marchedCube = new MarchedCube();
            marchedCube.Setup(cubeSize, threshold, densityCalculator, size, useDensities, transform.position);
            GetComponent<MarchingCubesChunk>().StartChunk(this, 0, 0, 0);
            GetComponent<MarchingCubesChunk>().UpdateMesh(GenerateMeshFromData(MarchCube(transform.position, marchedCube)), generateCollisions);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(((new Vector3(size, size, size)) / 2 * cubeSize), (new Vector3(size, size, size)) * cubeSize);        
    }
    #endregion
}

//The cube that we are going to march
//Will be created for each chunk so we can run all the marchingcubes in parralel
public struct MarchedCube
{
    //Each 8 Corners of the cube
    public struct MarchedCubeCorner
    {
        public float density;
        public Vector3 pos;
    }
    //Each 12 Edges of the cube
    public struct MarchedCubeEdge
    {
        public Vector3 position;
        public MarchedCubeCorner vertex0;
        public MarchedCubeCorner vertex1;
    }    
    public float cubeSize;
    public int size;//the size of the chunk
    public float threshold;//If density is higher than this, there is terrain at that point
    public int outcase;//Case for marched cube
    public bool useDensities;//Should we save densities and reuse them ?
    public float[,,] densities;
    public bool readDensities;//Instead of calling densityCalculator , read the densities array and use those as densities for points
    public Vector3 chunkPosition;//The position of the chunk in world space
    public MarchingCubesDensityScript densityCalculator;
    //Instantiate new MarchedCube class
    public void Setup(float _cubeSize, float _threshold, MarchingCubesDensityScript _densityCalculator, int _size, bool _useDensities, Vector3 _chunkPosition) //Initialization of the MarchedCube
    {
        //Setup variables for mesh generation
        SetVariables(_cubeSize, _threshold, _densityCalculator, _size + 1, _useDensities, _chunkPosition);
        if (useDensities) densities = new float[size, size, size];
    }
    
    //Set marching cube variables
    public void SetVariables(float _cubeSize, float _threshold, MarchingCubesDensityScript _densityCalculator, int _size, bool _useDensities, Vector3 _chunkPosition) 
    {
        //Just set variables
        cubeSize = _cubeSize;
        threshold = _threshold;
        densityCalculator = _densityCalculator;
        if (corners == null)
        {
            corners = new MarchedCubeCorner[8];
            edges = new MarchedCubeEdge[12];
        }
        useDensities = _useDensities;
        chunkPosition = _chunkPosition;
        readDensities = false;
        size = _size;
    }
    //Set correct corner points for edges
    private void SetVerticesForEdges() 
    {
        edges[0].vertex0 = corners[0]; edges[0].vertex1 = corners[1];
        edges[1].vertex0 = corners[1]; edges[1].vertex1 = corners[2];
        edges[2].vertex0 = corners[2]; edges[2].vertex1 = corners[3];
        edges[3].vertex0 = corners[3]; edges[3].vertex1 = corners[0];
        edges[4].vertex0 = corners[4]; edges[4].vertex1 = corners[5];
        edges[5].vertex0 = corners[5]; edges[5].vertex1 = corners[6];
        edges[6].vertex0 = corners[6]; edges[6].vertex1 = corners[7];
        edges[7].vertex0 = corners[7]; edges[7].vertex1 = corners[4];
        edges[8].vertex0 = corners[0]; edges[8].vertex1 = corners[4];
        edges[9].vertex0 = corners[1]; edges[9].vertex1 = corners[5];
        edges[10].vertex0 = corners[2]; edges[10].vertex1 = corners[6];
        edges[11].vertex0 = corners[3]; edges[11].vertex1 = corners[7];
    }
    public MarchedCubeCorner[] corners;
    public MarchedCubeEdge[] edges;
    MarchedCubeCorner corner0, corner1;
    float density0, density1, estimatedSurface;
    //Gets the vertex at the current edge
    public Vector3 GetEdgePoint(int edgeIndex, bool smoothed)
    {
        //return edgePoints[edgeIndex];
        corner0 = edges[edgeIndex].vertex0;
        corner1 = edges[edgeIndex].vertex1;
        density0 = corner0.density;
        density1 = corner1.density;

        //float estimatedSurface = (threshold - density0) / (density1 - density0);  
        estimatedSurface = Mathf.InverseLerp(density0, density1, threshold);
        if (smoothed)
            return Vector3.Lerp(corner0.pos, corner1.pos, estimatedSurface);
        else
            return Vector3.Lerp(corner0.pos, corner1.pos, 0.5f);   
    }
    #region Copy chunk edges
    //Copies the chunk edge densities from another amrchingCube (XAxis)
    public void CopyChunkEdgeDensitiesX(MarchedCube otherMarchedCube) 
    {
        float[,,] otherDensities = otherMarchedCube.densities;
        for (int y = 0; y < size; y++)
        {
            for (int z = 0; z < size; z++)
            {
                densities[0, y, z] = otherDensities[size - 1, y, z];
               // Debug.DrawRay(new Vector3(0, y, z) * cubeSize + chunkPosition, Vector3.right * 0.5f, Color.green);
            }
        }        
    }
    //Copies the chunk edge densities from another amrchingCube (YAxis)
    public void CopyChunkEdgeDensitiesY(MarchedCube otherMarchedCube)
    {
        float[,,] otherDensities = otherMarchedCube.densities;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                densities[x, 0, z] = otherDensities[x, size - 1, z];
                //Debug.DrawRay(new Vector3(x, 0, z) * cubeSize + chunkPosition, Vector3.up * 0.5f, Color.green);
            }
        }        
    }
    //Copies the chunk edge densities from another amrchingCube (ZAxis)
    public void CopyChunkEdgeDensitiesZ(MarchedCube otherMarchedCube)
    {
        float[,,] otherDensities = otherMarchedCube.densities;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                densities[x, y, 0] = otherDensities[x, y, size - 1];
                //Debug.DrawRay(new Vector3(x, y, 0) * cubeSize + chunkPosition, Vector3.forward * 0.5f, Color.green);
            }
        }        
    }
    #endregion
    //Gets all 8 vertices' density at a base position
    public int MarchCube(Vector3 newpos, int x, int y, int z)
    {
        outcase = 0;
        //Set corners new position
        corners[0].pos = newpos;
        corners[1].pos = newpos + (new Vector3(0, 1, 0) * cubeSize);
        corners[2].pos = newpos + (new Vector3(1, 1, 0) * cubeSize);
        corners[3].pos = newpos + (new Vector3(1, 0, 0) * cubeSize);
        corners[4].pos = newpos + (new Vector3(0, 0, 1) * cubeSize);
        corners[5].pos = newpos + (new Vector3(0, 1, 1) * cubeSize);
        corners[6].pos = newpos + (new Vector3(1, 1, 1) * cubeSize);
        corners[7].pos = newpos + (new Vector3(1, 0, 1) * cubeSize);

        if (useDensities && readDensities)
        {
            corners[0].density = densities[x, y, z];
            if (x < size && y < size && z < size)
            {
                corners[1].density = densities[x, y + 1, z];
                corners[2].density = densities[x + 1, y + 1, z];
                corners[3].density = densities[x + 1, y, z];
                corners[4].density = densities[x, y, z + 1];
                corners[5].density = densities[x, y + 1, z + 1];
                corners[6].density = densities[x + 1, y + 1, z + 1];
                corners[7].density = densities[x + 1, y, z + 1];
            }
        }
        else 
        {
            corners[0].density = Density(corners[0].pos);
            corners[1].density = Density(corners[1].pos);
            corners[2].density = Density(corners[2].pos);
            corners[3].density = Density(corners[3].pos);
            corners[4].density = Density(corners[4].pos);
            corners[5].density = Density(corners[5].pos);
            corners[6].density = Density(corners[6].pos);
            corners[7].density = Density(corners[7].pos);
        }
        outcase += (corners[0].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 0));
        outcase += (corners[1].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 1));
        outcase += (corners[2].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 2));
        outcase += (corners[3].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 3));
        outcase += (corners[4].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 4));
        outcase += (corners[5].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 5));
        outcase += (corners[6].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 6));
        outcase += (corners[7].density < threshold ? 1 : 0) * Mathf.RoundToInt(Mathf.Pow(2, 7));

        if (useDensities && !readDensities)//Save densities
        {
            densities[x, y, z] = corners[0].density;
            if (x < size && y < size && z < size)
            {
                densities[x, y + 1, z] = corners[1].density;
                densities[x + 1, y + 1, z] = corners[2].density;
                densities[x + 1, y, z] = corners[3].density;
                densities[x, y, z + 1] = corners[4].density;
                densities[x, y + 1, z + 1] = corners[5].density;
                densities[x + 1, y + 1, z + 1] = corners[6].density;
                densities[x + 1, y, z + 1] = corners[7].density;
            }
        }

        SetVerticesForEdges();
        return outcase;        
    }
    //How much terrain density at a current 3d point
    private float Density(Vector3 pos)
    {
        return densityCalculator.Density(pos);
    }
}
