
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Handles painting over terrain with brushes and such
public class MarchingCubesEditor : MonoBehaviour
{
    public Vector3 hitPoint;//The hit point in space
    public float brushSize;//The size of the brush
    public float brushStrengh;//The strengh of the brush
    [HideInInspector()]
    public bool addDensity;//The mouse button was pressed to add density
    [HideInInspector]
    public bool invertBrush;//Invert the strengh of the brush
    public MarchingCubesEditorBrushType brushType;//The current brush we are using
    public bool autoFixSeams;//auto fix the seams between chunks (Heavier on performance)

    private MarchingCubesTerrainScript terrainScript;//The script that handles the whole marchingcube logic and meshes
    [SerializeField]
    public enum MarchingCubesEditorBrushType 
    {
        SPHERE,
        CUBE
    }
    // Start is called before the first frame updateasdfadf
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmos()
    {
        if (terrainScript == null) terrainScript = GetComponent<MarchingCubesTerrainScript>();
        Gizmos.DrawWireSphere(hitPoint, brushSize);
        GetChunks();
    }
    //Gets the chunk script from the hitCollider form the brush
    private void GetChunks() 
    {
        List<MarchingCubesChunk> outputChunks = new List<MarchingCubesChunk>();
        MarchingCubesTerrainScript.GetChunksInCubeForEach forEachChunk = new MarchingCubesTerrainScript.GetChunksInCubeForEach(ForEachChunk);
        terrainScript.GetChunksInCube(brushSize, autoFixSeams ? 1 : 0, hitPoint, forEachChunk);
    }

    //Callback method from the GetChunksInCube
    public void ForEachChunk(int x, int y, int z, Vector3 worldPosition, MarchingCubesTerrainScript.ChunkData chunk) 
    {
        if (!addDensity) return;
        switch (brushType) 
        {
            case MarchingCubesEditorBrushType.SPHERE:
            {
                    terrainScript.EditChunkDensitiesSphereBrush(x, y, z, hitPoint, brushSize, brushStrengh * (invertBrush ? -1 : 1));//Paint densities using the sphere brush
                    break;
            }
            case MarchingCubesEditorBrushType.CUBE:
            {
                    terrainScript.EditChunkDensitiesCubeBrush(x, y, z, hitPoint, brushSize, brushStrengh * (invertBrush ? -1 : 1));//Paint densities using the cube brush
                    break;
            }
            default: break;
        }
        Debug.DrawRay(worldPosition, Vector3.up / 2);
    }
}
