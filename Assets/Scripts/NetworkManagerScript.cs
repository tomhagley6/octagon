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

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += AddConnectedClientServerRPC;
        NetworkManager.Singleton.OnClientDisconnectCallback += RemoveDisconnectedClientServerRPC;
        try
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        catch (Exception e)
        {
            Debug.Log("gameManager could not be assigned in NetworkManagerScript.cs");
            Debug.Log(e.Message);
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
    Debug.Log($"ClientId {clientId} connected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    gameManager.connectedClientIds.Add(clientId);
}
[ServerRpc(RequireOwnership=false)]
public void RemoveDisconnectedClientServerRPC(ulong clientId)
{
    Debug.Log($"ClientId {clientId} disconnected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    gameManager.connectedClientIds.Remove(clientId);
}



}
