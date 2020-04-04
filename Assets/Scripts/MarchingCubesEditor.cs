
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Handles painting over terrain with brushes and such
public class MarchingCubesEditor : MonoBehaviour
{
    public Vector3 hitPoint;//The hit point in space
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Debug.Log(hitPoint);
        Gizmos.DrawSphere(hitPoint, 3);        
    }
}
