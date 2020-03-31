using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEditor;
using System.Linq;
using System.Diagnostics;
using Unity.Jobs;
using UnityEngine.Jobs;
//Marching cubes terrain script
public class MarchingCubesTerrainScript : MonoBehaviour
{
    public float threshold;//If density value is bigger than this, there is terrain at that 3D point

    /// <summary>
    /// Before making a chunk, we could optimize this terrain by checking if the position of that chunk itself is terrain, if it is then we can generate that chunk
    /// if it is not, we dont have to generate that chunk because we know there is no terrain. But what if the position of the chunk is somewhere where there is not air, but the chunk is actually filled with terrain ? 
    /// Well then we should have another threshold value that controls if those empty (or not) chunks should be generated
    /// We can turn this value low (-1) to make those more empty spaced chunks to generate
    /// Or we can turn it high (0) to make them not generate (Which might cause chunks to not be generated, so it might corrupt the terrain a little) 
    /// </summary>
    public float chunkThreshold;
    public float cubeSize;//The size of the marched cube
    public bool onValidate;//Should we update the terrain if we change one of the parameters in the editor
    public bool interpolation;//Should we use interpolation for the edges points
    public bool mergeVertices;//Merges close vertices to a single vertex
    public float mergeDistance;//distance to merge closest vertices
    public int size;//Size container for each chunk of the marched cube
    public Vector3Int worldSize;//How much chunks we have in the world
    public Material material;
    private MarchingCubesChunk[,,] chunks;
    private Vector3[] marchingPoints;//3D points where the marching cube is going to march through (Only in one single chunk)

    public float scale, offset, yheight;

    public GameObject chunkPrefab;

    // Start is called before the first frame update
    void Start()
    {
        onValidate = false;
        Destroy(GetComponent<MarchingCubesChunk>());
        Destroy(GetComponent<MeshFilter>());
        Destroy(GetComponent<MeshRenderer>());
        chunks = new MarchingCubesChunk[worldSize.x, worldSize.y, worldSize.z];
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    chunks[x, y, z] = null;
                }
            }
        }
        foreach (var cameras in GameObject.FindObjectsOfType<MarchingCubesCameraScript>())
        {
            cameras.canGenerateChunks = true;
        }

        InitMarchingPoints();
    }
    private void InitMarchingPoints() 
    {
        if(marchingPoints == null || marchingPoints.Length != size) 
        {
            //Set points where the marching cube is going to march through
            marchingPoints = new Vector3[size * size * size];
            for (int x = 0, i = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++, i++)
                    {
                        marchingPoints[i] = new Vector3(x, y, z) * cubeSize;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    //Marches the cube in a x*x*x grid and generates a mesh out of it
    public Mesh MarchCube(Vector3 position)
    {
        MarchedCubeMeshData meshData;//Variable that will be helpful for storing and moving mesh data
        meshData.vertices = new List<Vector3>();
        meshData.triangles = new List<int>();
        //Init of the MarchedCube class

        //Input and output meshes
        Mesh mesh = new Mesh();
        Mesh newmesh;

        List<CombineInstance> meshes = new List<CombineInstance>();
        CombineInstance instance = new CombineInstance();
        instance.transform = transform.localToWorldMatrix;
        int outcase;
        NativeArray<CombineInstance> meshInstances = new NativeArray<CombineInstance>(size * size * size, Allocator.TempJob);
        NativeArray<Vector3> marchingPointsNative = new NativeArray<Vector3>(marchingPoints, Allocator.TempJob);
        NativeArray<MarchedCubeCorner> corners = new NativeArray<MarchedCubeCorner>(8, Allocator.TempJob);
        NativeArray<MarchedCubeEdge> edges = new NativeArray<MarchedCubeEdge>(8, Allocator.TempJob);
        //Init Job
        MarchingCubesMarchJob marchJob = new MarchingCubesMarchJob();
        marchJob.chunkPosition = position;
        marchJob.positions = marchingPointsNative;
        marchJob.interpolation = interpolation;
        marchJob.cubeSize = cubeSize;
        marchJob.threshold = threshold;
        marchJob.outputMeshes = meshInstances;
        marchJob.corners = corners;
        marchJob.edges = edges;
        //Density function parameters
        marchJob.scale = scale;
        marchJob.offset = offset;
        marchJob.yheight = yheight;

        marchJob.Run(size * size * size);

        JobHandle jobHandle = marchJob.Schedule(size * size * size, 64);

        jobHandle.Complete();



        //Create final mesh for this chunk
        meshes = meshInstances.ToList();
        mesh.CombineMeshes(meshes.ToArray(), true);
        if (mergeVertices) AutoWeld(mesh, mergeDistance);
        mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshInstances.Dispose();
        marchingPointsNative.Dispose();
        corners.Dispose();
        edges.Dispose();

        return mesh;
    }
    //Merge vertices that are close to each other https://answers.unity.com/questions/228841/dynamically-combine-verticies-that-share-the-same.html
    private void AutoWeld(Mesh mesh, float threshold)
    {
        Vector3[] verts = mesh.vertices;

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
        int[] tris = mesh.triangles;
        for (int i = 0; i < tris.Length; ++i)
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
        mesh.Clear();
        mesh.vertices = newVerts.ToArray();
        mesh.triangles = tris;
    }
    
    //Generates all the chunks of the terrain
    public void GenerateChunks() 
    {
        if (chunks == null || chunks.GetLength(0) != worldSize.x || chunks.GetLength(1) != worldSize.y || chunks.GetLength(2) != worldSize.z) chunks = new MarchingCubesChunk[worldSize.x, worldSize.y, worldSize.z];
        chunks[0, 0, 0] = GetComponent<MarchingCubesChunk>();
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    GenerateChunk(x, y, z, true);
                }
            }
        }
    }
    //Generate a single chunk
    public void GenerateChunk(int x, int y, int z, bool recalculateChunks) 
    {
        if (x < 0 || y < 0 || z < 0) return;
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return;
        Vector3 chunkPos = TransformCoordinatesChunkToWorld(x, y, z);
        if (chunks[x, y, z] == null)//Generate new chunks if they dont exist yet
        {
            
            GameObject chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
            chunk.name = x + "-" + y + "-" + z;
            MarchingCubesChunk chunkScript = chunk.GetComponent<MarchingCubesChunk>();
            chunkScript.StartChunk(this, x, y, z);
            //if(densityCalculator.Density(chunkPos) < chunkThreshold) return;//This chunk is not filled with terrain
            //chunkScript.UpdateMesh(MarchCube(chunkPos));

            chunks[x, y, z] = chunkScript;
        }
        else if(recalculateChunks)
        {
            MarchingCubesChunk chunkScript = chunks[x, y, z];
            chunkScript.transform.position = chunkPos;
            chunkScript.UpdateMesh(MarchCube(chunkPos));
        }
    }
    //Generate a single chunk using world coordinates
    public void GenerateChunk(Vector3 pos, bool recalculateChunks)
    {
        Vector3Int chunkCoords = TransformCoordinatesWorldToChunk(pos);
        GenerateChunk(chunkCoords.x, chunkCoords.y, chunkCoords.z, recalculateChunks);
    }
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
    public Vector3Int TransformCoordinatesWorldToChunk(Vector3 pos) 
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x / size / cubeSize), Mathf.RoundToInt(pos.y / size / cubeSize), Mathf.RoundToInt(pos.z / size / cubeSize));
    }
    //Set chunk visibility from world coordinates
    public void SetChunkVisibility(Vector3 pos, bool isVisible)
    {
        MarchingCubesChunk chunk = GetChunk(pos);
        if (isVisible) chunk.OnShowChunk();
        else chunk.OnHideChunk();
    }
    //Set chunk visibility
    public void SetChunkVisibility(int x, int y, int z, bool isVisible)
    {
        MarchingCubesChunk chunk = GetChunk(x, y, z);
        if (chunk == null) return;
        if (isVisible) chunk.OnShowChunk();
        else chunk.OnHideChunk();
    }
    //Set chunk visibility with already given chunk
    public void SetChunkVisibility(MarchingCubesChunk chunk, bool isVisible)
    {
        if (chunk == null) return;
        if (isVisible) chunk.OnShowChunk();
        else chunk.OnHideChunk();
    }
    //Set chunk visibilty for all chunks
    public void SetChunksVisibility(bool isVisible) 
    {
        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;
            if (isVisible) chunk.OnShowChunk();
            else chunk.OnHideChunk();
        }
    }
    //Get chunk (Returns null if chunk is outside map bounds)
    public MarchingCubesChunk GetChunk(Vector3 pos) 
    {
        Vector3Int newpos = TransformCoordinatesWorldToChunk(pos);
        int x, y, z;
        x = newpos.x; y = newpos.y; z = newpos.z;
        if (x < 0 || y < 0 || z < 0) return null;
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return null;
        return chunks[x, y, z];
    }
    //Get chunk (Returns null if chunk is outside map bounds)
    public MarchingCubesChunk GetChunk(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0) return null; 
        if (x >= worldSize.x || y >= worldSize.y || z >= worldSize.z) return null; 
        return chunks[x, y, z];
    }
    //Debug stuff
    private void OnValidate()
    {
        OnValidatePublic();
    }
    public void OnValidatePublic() //Make method so when we change other script's data the mesh also gets updated
    {
        if (onValidate)
        {
            InitMarchingPoints();
            GetComponent<MarchingCubesChunk>().StartChunk(this, 0, 0, 0);
            GetComponent<MarchingCubesChunk>().UpdateMesh(MarchCube(transform.position));
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(((new Vector3(size, size, size)) / 2 * cubeSize), (new Vector3(size, size, size)) * cubeSize);        
    }
}
