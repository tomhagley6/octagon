using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCoordinateInView : MonoBehaviour
{
public Camera playerCamera;
public Vector3 wall8Centre;

public void Start()
{
    playerCamera = gameObject.GetComponent<Camera>();

    wall8Centre = new Vector3(-14.1000004f,3.69000006f,14.1499996f);

}

public void Update()
{
    bool isInHorizontalBounds = IsInView(wall8Centre);
    Debug.Log(isInHorizontalBounds);
}

// Method to return a bool that identifies whether a coordinate in the scene is currently in the camera view
// If the viewportPos value for any given axis lies within [0,1], the object is in view in this axis
public bool IsInView(Vector3 objectPosition)
{
    // Convert the object position to viewport space
    Vector3 viewportPos = playerCamera.WorldToViewportPoint(objectPosition);

    // Check if the object is within the viewport bounds
    bool isInHorizontalBounds = viewportPos.x >= 0 && viewportPos.x <= 1;
    bool isInVerticalBounds = viewportPos.y >= 0 && viewportPos.y <= 1;
    bool isInFrontOfCamera = viewportPos.z > 0;

    return isInHorizontalBounds && isInVerticalBounds && isInFrontOfCamera;
    // return viewportPos.z;
}
}
