using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
public struct MeshData //Information about the mesh
{
    public List<Vector3> vertices;
    public List<int> triangles;
}

//Called on each "center-point" in the marched cube bound
public struct MarchingCubesMarchJob : IJobParallelFor
{
    public NativeArray<MeshData> outputMesh;//The output mesh data
    public Vector3 position;//The position of the current marched mesh

    public float scale, offset, yheight;
       
    public void Execute(int index)
    {
        //Reverse engineer the index to give us x, y and z coordinates
        //Get 8 corner pieces
        //Calculate densities
        //Return single marched cube mesh
    }
    //How much terrain density at a current 3d point
    public float Density(Vector3 pos)
    {
        pos.x *= scale; pos.z *= scale;
        float x, y, z;
        x = pos.x; y = pos.y; z = pos.z;
        float ground = -y * yheight + offset;//Create ground plane
        return ground;
    }
    //3D perlin noise
    private float PerlinNoise3D(Vector3 pos)
    {
        FastNoise noise = new FastNoise();
        noise.SetNoiseType(FastNoise.NoiseType.Simplex);
        return noise.GetSimplex(pos.x, pos.y, pos.z);
    }
    //Test noise
    private float Noise(Vector3 pos)
    {
        FastNoise noise = new FastNoise();
        noise.SetNoiseType(FastNoise.NoiseType.Cellular);
        noise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
        noise.SetCellularReturnType(FastNoise.CellularReturnType.Distance);
        return noise.GetCellular(pos.x, pos.y, pos.z) - 0.5f;
    }
}

