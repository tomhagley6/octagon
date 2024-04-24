using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ToggleCamera : NetworkBehaviour
{
    public GameObject topDownCam;
    public GameObject firstPersonCam;
    public NetworkManager networkManager;
    public Action switchCamera;
    public bool switchCameraTrigger = false;

    public override void OnNetworkSpawn()
    {
        
        networkManager = FindObjectOfType<NetworkManager>();

        // subscribe to a key-triggered event with mouse lock toggle method
        switchCamera += SwitchCameraTriggerListener;


        StartCoroutine(FindPlayerCam());

    }


    IEnumerator FindPlayerCam()
    {
        Debug.Log("Start FindPlayerCam");

        yield return new WaitForSeconds(2f);
        
        Debug.Log("FindPlayerCam WaitUntil finished");

        foreach (Camera c in Camera.allCameras)
        {
            if (c.name == "PlayerCamera(Clone)")
            {
                firstPersonCam = c.gameObject;
            }

        }

        Debug.Log("Assigned camera for first person");

    }

    // Toggle mouse lock mode between locked and free
    // to allow interaction with QuantumConsole
    public void SwitchCameraTriggerListener()
    {
        
        switchCameraTrigger = !switchCameraTrigger;
        // Cursor.lockState = switchCameraTrigger ? CursorLockMode.None : CursorLockMode.Locked;
        if (switchCameraTrigger)
        {
            firstPersonCam.SetActive(false);
            topDownCam.SetActive(true);
        }
        else
        {
            firstPersonCam.SetActive(true);
            topDownCam.SetActive(false);  
        }
    }

    void Update()
    {
        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(KeyCode.F))
        {
            switchCamera();
        }
    }

}
