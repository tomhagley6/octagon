using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFoV : MonoBehaviour
{
    public Camera playerCamera;
    // Start is called before the first frame update
    void Start()
    {
        playerCamera = gameObject.GetComponent<Camera>();

        // Set (default) vertical FoV to the correct value for an aspect ratio
        // of 1.713521 to give a horizontal FoV of 90.
        // This is currently hard coded based on the aspect ratio given by the 
        // fullscreen-windowed game on the Octagon laptops
        playerCamera.fieldOfView = 60.53513f;
    }

}
