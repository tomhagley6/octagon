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


    public override void OnNetworkSpawn()
    {   
        // Instantiate the camera prefab
        networkManager = FindObjectOfType<NetworkManager>();
        player = networkManager.LocalClient.PlayerObject.transform;
        Debug.Log(player);
        camera = Resources.Load<GameObject>(prefabName);

        // find clientID for this client
        clientId = NetworkManager.Singleton.LocalClientId;
        
        // Check if the prefab was found
        if (camera != null)
            {
                camInstance = Instantiate(camera);
                // NetworkObject camInstanceNetworkObject = camInstance.GetComponent<NetworkObject>();

                // // Make sure this belongs to the client
                // // IS THIS NECESSARY? Can maybe make camera client-side only
                // camInstanceNetworkObject.SpawnWithOwnership(clientId);
                // Debug.Log("Current client ID is " + clientId);
                
                
            }
        else 
            {
                Debug.Log("Camera prefab was returned as null");
            }
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
            // Debug.Log("player returned as null in AssignCamera.cs");
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

