using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    float horizontalFoV = 2 * Mathf.Atan(Mathf.Tan(verticalFoV * Mathf.Deg2Rad / 2) * aspectRatio) * Mathf.Rad2Deg;

    Debug.Log("Vertical FoV: " + verticalFoV + ", Horizontal FoV: " + horizontalFoV + ", aspect ratio: " + aspectRatio);
}
}
