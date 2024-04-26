using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Toggle the camera between first-person and top-down
/* This allows for video recording of Octagon multiplayer 
   sessions using only the .exes. Have 3 clients, with the
   third using a top-down camera and invisible player */
public class ToggleCamera : NetworkBehaviour
{
    public GameObject topDownCamGO; // assigned in Inspector
    public Camera topDownCam; 
    public Camera firstPersonCam; // accessed as the main camera
    public NetworkManager networkManager;
    public Action toggleCamera;
    public bool toggleCameraTrigger = false;

    public override void OnNetworkSpawn()
    {
        
        networkManager = FindObjectOfType<NetworkManager>();
        topDownCam = topDownCamGO.GetComponent<Camera>();

        // subscribe to a key-triggered event with camera toggle method
        toggleCamera += toggleCameraTriggerListener;

        StartCoroutine(FindPlayerCam());

    }


    // Wait for the player and cam to spawn, then identify the player cam in the scene
    IEnumerator FindPlayerCam()
    {
        yield return new WaitForSeconds(2f);
        
        firstPersonCam = Camera.main;

    }


    /* Toggle camera between first-person and top-down
    Do this by switching depth, but maintain topDownCam
    on a separate display when it is not the prioritsed camera */
    public void toggleCameraTriggerListener()
    {
        
        toggleCameraTrigger = !toggleCameraTrigger;
        if (toggleCameraTrigger)
        {
            topDownCam.depth = Camera.main.depth - 1;
            topDownCam.targetDisplay = 0;
            Camera.main.targetDisplay = 1;

        }
        else
        {
            topDownCam.depth = Camera.main.depth + 1;
            topDownCam.targetDisplay = 1;
            Camera.main.targetDisplay = 0;
        }   
    }


    void Update()
    {
        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(KeyCode.F))
        {
            toggleCamera();
        }
    }

}
