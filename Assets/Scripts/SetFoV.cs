
using UnityEngine;
using Globals;

public class SetFoV : MonoBehaviour
{
    public Camera playerCamera;
    [SerializeField] private float verticalFoV;
    [SerializeField] private float aspectRatio;
    [SerializeField] private float horizontalFoV;
    void Start()
    {
        playerCamera = gameObject.GetComponent<Camera>();

        float horizontalFoV = General.horizontalFoV;
        float verticalFoV = playerCamera.fieldOfView;
        float aspectRatio = playerCamera.aspect;

        // Set (default) vertical FoV to the correct value for an aspect ratio
        // of 1.713521 to give intended horizontal FoV.
        // This is currently hard coded based on the aspect ratio given by the 
        // fullscreen-windowed game on the Octagon laptops

        /* // horizontal FoV: 90
        playerCamera.fieldOfView = 60.53513f; */

        /*  // horizontal FoV: 110
        playerCamera.fieldOfView = 79.61958f; */

        // Set vertical FoV based on intended horizontal FoV and the monitor's aspect ratio
        playerCamera.fieldOfView = CalculateVerticalFoV(horizontalFoV, aspectRatio);

    }

    // Use this function to calculate a vertical FoV from a desirved horizontal FoV 
    // and a known aspect ratio
    float CalculateVerticalFoV(float horizontalFoV, float aspectRatio)
    {
        // Equation to calculate vertical FoV from horizontal FoV and aspect ratio
        float verticalFoV = 2 * Mathf.Atan(Mathf.Tan(horizontalFoV * Mathf.Deg2Rad / 2) / aspectRatio) * Mathf.Rad2Deg;

        return verticalFoV;

    }

}
