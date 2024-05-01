using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Logging;
using System;
using ActiveWalls = GameManager.ActiveWalls;
using LoggingClasses;
using Newtonsoft.Json;

// Class to implement a DiskLogger, attached to each client's FirstPersonPlayer
public class PlayerLogger : NetworkBehaviour
{


public DiskLogger diskLogger;
public GameObject player;
public Action<bool> playerSpawned;
public GameManager gameManager;
public override void OnNetworkSpawn() {
        
        
       gameManager = FindObjectOfType<GameManager>();
       
       
        // Get this player
        player = transform.parent.gameObject;
        

        
        // Start logging data only for the player in the current client
        // This could be an event that triggers the logger initiation method in GameManager
        // does not even need a player reference passed
        // First line of OnNetworkSpawn can be to call a method in GameManager that subscribes to 
        if (IsLocalPlayer) {
            Debug.Log("Local player; beginning logging");
            diskLogger = FindObjectOfType<DiskLogger>();
            diskLogger.StartLogger();
            StartCoroutine("LogPos");
        }
        else {
            Debug.Log("NOT local player");
        }

        // Subscribe to change in active wall values on the network
        gameManager.activeWalls.OnValueChanged += ActiveWallsHandler_LogSliceOnset;

    }

    IEnumerator LogPos()
    {
        while (true)
        {
            // diskLogger.Log(String.Format(Globals.posFormat, Globals.posTag,
            //                                                 Globals.player, 
            //                                                 player.transform.position.x,
            //                                                 player.transform.position.z));
            
            yield return new WaitForSeconds(0.2f);

        }
    }


    // Method to log slice onset data only for the Server, when ActiveWalls value changes
    // Try to rewrite this to accept an arbitrary number of walls
    private void ActiveWallsHandler_LogSliceOnset(ActiveWalls previousValue, ActiveWalls newValue)
    {
        if (!IsServer) { Debug.Log("Not server, not running ActiveWallsHandler_LogSliceOnset in PlayerLogger");
        return; }

        Debug.Log("Is Server, so running ActiveWallsHandler_LogSliceOnset in PlayerLogger");

        int wall1 = newValue.wall1;
        int wall2 = newValue.wall2;
        Dictionary<string,object> playerInfoDict = new Dictionary<string,object>();
        
        // For each connected client, create a player info class (defined in LoggingClasses)
        // and add this class as the value for this clientId in a dictionary
        // Then, log to JSON format the full slice onset information
        // as defined in LoggingClasses.SliceOnsetLogEvent
        var players = NetworkManager.ConnectedClientsList;
        Debug.Log($"ConnectedClientsList is {players.Count} items long");
        for (int i = 0; i < players.Count; i++)
        {
            int clientId = i;
            NetworkClient networkClient = players[i];
            Vector3 playerPosition = networkClient.PlayerObject.gameObject.transform.position;
            Quaternion playerRotation = networkClient.PlayerObject.gameObject.transform.rotation;

            PlayerInfo thisPlayerInfo = new PlayerInfo(networkClient.ClientId, playerPosition, playerRotation);

            playerInfoDict.Add(networkClient.ClientId.ToString(), thisPlayerInfo);
            Debug.Log($"playerInfoDict is {playerInfoDict.Count} item long");
        }

        // Create the final log class instance
        SliceOnsetLogEvent sliceOnsetLogEvent = new SliceOnsetLogEvent(wall1, wall2, playerInfoDict);
        Debug.Log("SliceOnsetLogEvent created");

        // Serialize the class to JSON
        string logEntry = JsonConvert.SerializeObject(sliceOnsetLogEvent, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        Debug.Log("SliceOnsetLogEvent serialized to JSON string: " + logEntry);

        // Send this string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);
        

    }

    void LogEvent(int increment, int triggerID, string rewardType) {

        // Was event triggered by this player instance
        bool thisPlayer;
        thisPlayer = IsLocalPlayer;

        // diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
        //                                                         Globals.trialNum,
        //                                                         Globals.trialType,
        //                                                         triggerID,
        //                                                         rewardType,
        //                                                         increment,
        //                                                         thisPlayer)); 
    }

}




