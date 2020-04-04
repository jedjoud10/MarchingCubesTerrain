using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesDensityScript : MonoBehaviour
{
    public bool flatTerrain;//Make flat terrain instead of terrain with height
    public float scale;
    public float noiseScale;
    public float noiseScale2;
    public float height;
    public float height2;
    public float height3;
    public float offset;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //How much terrain density at a current 3d point
    public float Density(Vector3 pos)
    {
        if (flatTerrain) return -pos.y * height3 + offset;
        pos.x *= scale; pos.z *= scale;
        float x, y, z;
        x = pos.x; y = pos.y; z = pos.z;
        float ground = -y * height3 + offset;//Create ground plane
        float terrain = ground + PerlinNoise3D(pos * noiseScale) * height - Noise(pos * noiseScale2) * height2;
        return terrain;
    }
    //Density at 3d point with flattened terrain
    public float DensityFlatTerrain(Vector3 pos) 
    {
        return -pos.y * height3 + offset;//Create flat ground terrain plane
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
    private void OnValidate()
    {
        GetComponent<MarchingCubesTerrainScript>().OnValidatePublic();//Update mesh since we changed noise settings here
    }
}
