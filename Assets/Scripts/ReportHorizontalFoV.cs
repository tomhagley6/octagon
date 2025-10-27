using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Class to report the horizontal field of view (FoV) of the player camera
based on the vertical FoV and aspect ratio (Unity uses vertical FoV by default) */
/* This script's component on PlayerCamera is currently disabled. Re-enable
to check FoV in the future */
public class ReportHorizontalFoV : MonoBehaviour
{
    public Camera playerCamera;


    public void Start()
    {
        playerCamera = gameObject.GetComponent<Camera>();
    }

    public void Update()
    {
        CalculateEffectiveFoVs();
    }

    public void CalculateEffectiveFoVs()
    {
        float verticalFoV = playerCamera.fieldOfView;
        float aspectRatio = playerCamera.aspect;

        // Equation to calculate horizontal FoV from vertical FoV and aspect ratio
        float horizontalFoV = 2 * Mathf.Atan(Mathf.Tan(verticalFoV * Mathf.Deg2Rad / 2) * aspectRatio) * Mathf.Rad2Deg;

        Debug.LogWarning("Vertical FoV: " + verticalFoV + ", Horizontal FoV: " + horizontalFoV + ", aspect ratio: " + aspectRatio);
    }
}
