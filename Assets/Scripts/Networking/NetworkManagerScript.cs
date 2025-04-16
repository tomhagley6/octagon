using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/*
On every client connection, the server will run the OnClientConnectedCallback
event, and pass the clientId (so, as represented on the server), to subscribers
of this event. 
Here, we write a quick debug to show agreement between the server-side clientId
and the local LocalClientId from NetworkManager 
*/
public class NetworkManagerScript : MonoBehaviour
{

    public GameManager gameManager;


    // It feels messy that I can't use any NetworkBehaviour fields here
    // Consider moving this script to somewhere else?
    public void ConnectionCallbackSubscriptions(bool isReady)
    {   
        try
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        catch (Exception e)
        {
            Debug.Log("gameManager could not be assigned in NetworkManagerScript.cs");
            Debug.Log(e.Message);
        }
        
        // Debug.LogWarning($"gameManager.IsServer is {gameManager.IsServer}, gameManager.IsHost is {gameManager.IsHost}");
        if (gameManager.IsServer && isReady)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += AddConnectedClientServerRPC;
            // Debug.LogWarning("NetworkManager.Singleton.OnClientConnectedCallback += AddConnectedClientServerRPC; ran");
            NetworkManager.Singleton.OnClientDisconnectCallback += RemoveDisconnectedClientServerRPC;
            // Debug.LogWarning($"gameManager.IsServer is {gameManager.IsServer}, adding RPCs to callback");
            // Debug.LogWarning($"gameManager.IsHost is {gameManager.IsHost}, adding RPCs to callback");
        }
        

    }

    // // Think these just need to be ServerRPCs because clients cannot change a NetworkVariable/List
    // void Connected(ulong clientId)
    // {
    //     Debug.Log($"ClientId {clientId} connected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    //     gameManager.connectedClientIds.Add(clientId);
    // }

    // void Disconnected(ulong clientId)
    // {
    //     Debug.Log($"ClientId {clientId} disconnected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    //     gameManager.connectedClientIds.Remove(clientId);
    // }


[ServerRpc(RequireOwnership=false)]
public void AddConnectedClientServerRPC(ulong clientId)
{
    // Append client ID to list
    Debug.Log($"ClientId {clientId} connected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    gameManager.connectedClientIds.Add(clientId);

    // Also initialise a new client's score to 0
    gameManager.scores.Add(0);
    // Debug.LogWarning($"gameManager.scores[0] = {gameManager.scores[0]}");
}


[ServerRpc(RequireOwnership=false)]
public void RemoveDisconnectedClientServerRPC(ulong clientId)
{   
    // Find index of the removed client
    int idx = gameManager.connectedClientIds.IndexOf(clientId);

    // Remove client based on ID
    Debug.Log($"ClientId {clientId} disconnected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    gameManager.connectedClientIds.Remove(clientId);

    // Remove the score of a client that has left, at the correct index
    // Do not remove based on score to avoid bugs when duplicate scores exist
    gameManager.scores.RemoveAt(idx);

}



}
