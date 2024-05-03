using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using LoggingClasses;
using Newtonsoft.Json;
using Unity.Collections;

public class LoggingEvents : NetworkBehaviour
{

    public GameManager gameManager;
    public DiskLogger diskLogger;
    public TrialHandler trialHandler;

    public override void OnNetworkSpawn()
    {
        gameManager = FindObjectOfType<GameManager>();
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();

        // Subscribe to the slice onset event that is triggered by running ColourWalls 
        trialHandler.sliceOnset += SliceOnsetHandler_SliceOnsetLog;
    }


    // Write a trial start log event when the TrialActive NetworkVariable has it's value changed 
    // to true
    public void TrialActiveHandler_TrialStartLog()
    {
        // TODO
    }

    // Write a slice onset log event when ColourWalls is run on the server (host)
    /* Be careful with timing of these logs. Slice onset should ideally be when the new walls 
       become visible, and not when the activeWalls are changed. Must consider that when the
       colourWalls is run on the server is not necessarily when it is run on all clients
    */
    /* Also, it may be that when I convert my code to using a dedicated fully-authoritative server
       I will need to have my server run trial start logic and trigger ColourWalls on all clients.
       In that case, I will still have to consider how much lag there is between the server command
       and the client GameObject rendering change */
    public void SliceOnsetHandler_SliceOnsetLog()
    {
        if (!IsServer) { Debug.Log("Not server, not running SliceOnsetHandler_SliceOnsetLog in Gamemanager");
         return; }

        Debug.Log("Is Server, so running SliceOnsetHandler_SliceOnsetLog in GameManager");

        int wall1 = gameManager.activeWalls.Value.wall1;
        int wall2 = gameManager.activeWalls.Value.wall2;
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
            PlayerPosition playerPosition2 = new PlayerPosition(playerPosition.x, playerPosition.y, playerPosition.z);

            Quaternion playerRotation = networkClient.PlayerObject.gameObject.transform.rotation;
            float camXAxisRotation = Camera.main.transform.rotation.eulerAngles.x;
            float camZAxisRotation = Camera.main.transform.rotation.eulerAngles.z;
            PlayerRotation playerRotation2 = new PlayerRotation(camXAxisRotation, playerRotation.eulerAngles.y, camZAxisRotation);

            PlayerInfo thisPlayerInfo = new PlayerInfo(networkClient.ClientId, playerPosition2, playerRotation2);

            playerInfoDict.Add(networkClient.ClientId.ToString(), thisPlayerInfo);
            Debug.Log($"playerInfoDict is {playerInfoDict.Count} item long");
        }

        // Create the final log class instance
        SliceOnsetLogEvent sliceOnsetLogEvent = new SliceOnsetLogEvent(wall1, wall2, playerInfoDict);
        Debug.Log("SliceOnsetLogEvent created");

        // Serialize the class to JSON
        string logEntry = JsonConvert.SerializeObject(sliceOnsetLogEvent, new JsonSerializerSettings
        {
            // This ensures that Unity Quaternions can serialize correctly
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        Debug.Log("SliceOnsetLogEvent serialized to JSON string: " + logEntry);

        // Send this string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);
    }

    // Write a trial end log event when the TrialActive NetworkVariable has it's value changed 
    // to false
    public void TrialActiveHandler_TrialEndLog()
    {
        // TODO
    }

}
