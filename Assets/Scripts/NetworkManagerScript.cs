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
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Connected;
        
    }

    void Connected(ulong clientId)
    {
        Debug.Log($"ClientId {clientId} connected. LocalClientId is {NetworkManager.Singleton.LocalClientId}");
    }

}
