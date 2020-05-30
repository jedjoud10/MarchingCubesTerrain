using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounterScript : MonoBehaviour
{
    private float currentFPS;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 30 == 0)
        {
            currentFPS = Mathf.Lerp(1.0f / Time.unscaledDeltaTime, currentFPS, 5 * Time.deltaTime);
        }
    }
    //GUI
    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 100, 100), "FPS : " + currentFPS);
    }
}
