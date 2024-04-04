using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Mono.CSharp;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

// 
public class AssignCamera : NetworkBehaviour
{
    public Transform player;
    public GameObject camera; 
    public NetworkManager networkManager;
    public string prefabName = "prefabs/playercamera";
    public GameObject camInstance;
    public ulong clientId;
    private bool playerLoaded = false;

    public override void OnNetworkSpawn()
    {   
        // Instantiate the camera prefab
        networkManager = FindObjectOfType<NetworkManager>();

        // Coroutine will not execute body until the LocalPlayer returns true
        // This avoids attempting to assign a value to the player transform before
        // the player has spawned
        StartCoroutine(CreateCamera());

        /* camera = Resources.Load<GameObject>(prefabName);
        // BUG
        // player is being set as null
        // Camera can instantiate, MouseLook.cs works fine, and even seems to assign itself
        // the player correct from 'networkManager.LocalClient.PlayerObject.transform' in MouseLook.cs
        // However, that same code will not run here and therefore camera is not assigned to player location

        camInstance = Instantiate(camera);
        Debug.Log("NetworkManager successfully assigned in AssignCamera");
        
        if (networkManager != null)
        {
            Debug.Log("NetworkManager is NOT null in AssignCamera.cs OnNetworkSpawn()");
            
            if (networkManager.LocalClient != null)
            {
                Debug.Log("And neither is networkManager.LocalClient");
                if (networkManager.LocalClient.PlayerObject != null)
                {
                    Debug.Log("And neither is networkManager.LocalClient.PlayerObject");
                    if (networkManager.LocalClient.PlayerObject.transform != null)
                    {
                        Debug.Log("And neither is networkManager.LocalClient.PlayerObject.transform");
                        player = networkManager.LocalClient.PlayerObject.transform;
                    }
                    else
                    {
                        Debug.Log("However, player = networkManager.LocalClient.PlayerObject.transform IS null");
                    }
                }
                else
                {
                    Debug.Log("However, networkManager.LocalClient.PlayerObject IS null");
                }

            }
            else
            {
                Debug.Log("However, networkManager.LocalClient IS null");
            }

            
        }       
        else
        {
            Debug.Log("NetworkManager is null in AssignCamera.cs OnNetworkSpawn()");
        }
        Debug.Log(player);


        // find clientID for this client
        clientId = NetworkManager.Singleton.LocalClientId;
        
        // Check if the prefab was found
        if (camera != null)
            {
                camInstance = Instantiate(camera);
                Debug.Log("Camera has been instantiated");
                // NetworkObject camInstanceNetworkObject = camInstance.GetComponent<NetworkObject>();

                // // Make sure this belongs to the client
                // // IS THIS NECESSARY? Can maybe make camera client-side only
                // camInstanceNetworkObject.SpawnWithOwnership(clientId);
                // Debug.Log("Current client ID is " + clientId);
                
                
            }
        else 
            {
                Debug.Log("Camera prefab was returned as null");
            } */
    }

    IEnumerator CreateCamera()
    {
        Debug.Log("Start CreateCamera");

        yield return new WaitUntil(() => networkManager.LocalClient.PlayerObject != null);
        
        Debug.Log("CreateCamera WaitUntil finished");

        camera = Resources.Load<GameObject>(prefabName);
        // Create client-side instance of PlayerCamera
        if (camera != null) camInstance = Instantiate(camera);
        else 
        {
            Debug.Log("Camera prefab was returned as null");
        }
        // Set the player for the camera to follow
        player = networkManager.LocalClient.PlayerObject.transform;

        Debug.Log("CreateCamera ran successfully");

    }


    public void LateUpdate()
    {
        if (player != null)
        {
            // Debug.Log("Player no longer null. Running AssignToPlayer()");
            AssignToPlayer();
        }
        else
        {
            Debug.Log("player returned as null in AssignCamera.cs");
        }
    }

    public void AssignToPlayer()
    {
        // New camera position per frame is the player's current position,
        // offset positively in y
        Vector3 currentPositionPlayer = player.position;
        Vector3 newPositionCam = new Vector3(currentPositionPlayer.x, 3.75f, 
                                            currentPositionPlayer.z);
        camInstance.transform.position = newPositionCam;
        
        // New camera rotation per frame is the camera's current rotation (around x-axis)
        // plus the player's current rotation (around y-axis)
        Quaternion currentRotationCam = camInstance.transform.rotation;
        Quaternion currentRotationPlayer = player.transform.rotation;
        Quaternion newRotationCam = Quaternion.Euler(currentRotationCam.eulerAngles.x,
                                                    currentRotationPlayer.eulerAngles.y,
                                                    currentRotationCam.eulerAngles.z);
        
        camInstance.transform.rotation = newRotationCam;
    }

}

