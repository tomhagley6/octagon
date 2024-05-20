using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using LoggingClasses;
using Newtonsoft.Json;
using Unity.Collections;
using TriggerActivation = GameManager.TriggerActivation;
using System;
using Globals;


//// Refactor repetitive sections as separate methods
/* Script containing all log events that are triggered to write game data to the log file.
Any writer is implemented by the Logger
Classes used to represent the data in each of the log events are found in Logger/LogClasses.cs */
public class LoggingEvents : NetworkBehaviour
{

    public GameManager gameManager;
    public DiskLogger diskLogger;
    public TrialHandler trialHandler;
    public event Action loggingEventsSubscribed;

    /* Here subscribe to events that trigger each of the logging events
    The only exception are the 'start' and 'end' log events which are called directly
    when starting end ending the logger */
    public override void OnNetworkSpawn() // Could this be Start() instead of OnNetworkSpawn()?
    {
        gameManager = FindObjectOfType<GameManager>();
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();

        // Subscribe to the slice onset event that is triggered by running ColourWalls 
        trialHandler.sliceOnset += SliceOnsetHandler_SliceOnsetLog;

        // Both StartTrial and EndTrial logging events are handled by a change in the
        // value of the TrialActive NetworkVariable
        gameManager.trialActive.OnValueChanged += TrialActiveHandler;

        // Trigger activation log events are triggered by a change in the TriggerActivation 
        // NetworkVariable
        gameManager.triggerActivation.OnValueChanged += TriggerActivationHandler_TriggerActivationLog;

        // start and end events triggered when the DiskLogger starts or ends for this session
        diskLogger.loggingStarted += LoggingStartedHandler_StartLogging;
        diskLogger.loggingEnded += LoggingEndedHandler_EndLogging;

        // time-triggered logging also begins when DiskLogger starts for this session
        diskLogger.loggingStarted += LoggingStartedHandler_TimeTriggeredLog;

        // Start the logger once subscriptions are finished
        loggingEventsSubscribed?.Invoke();

    }


    // Write a logging start log event when logging first begins
    public void LoggingStartedHandler_StartLogging()
    {

        // Create a StartLoggingLogEvent object
        StartLoggingLogEvent startLoggingLogEvent = new StartLoggingLogEvent();

        // Serialize log event object to JSON formatted string
        string toLog = JsonConvert.SerializeObject(startLoggingLogEvent);

        // Write string to file 
        diskLogger.Log(toLog);
    }


    // Write a trial start log event when the TrialActive NetworkVariable has it's value changed 
    // to true
    public void TrialActiveHandler_TrialStartLog()
    {
        // TODO
        if (!IsServer) {Debug.Log("Not server, not running TrialActiveHandler_TrialStartLog()");
         return; }

        Debug.Log("Is Server, so running TrialActiveHandler_TrialStartLog in LoggingEvents");

        // increment the trial number (this could be done in a better place than the logging method)
        // probably GameManager
        bool trialActive = gameManager.trialActive.Value;
        if (trialActive == true)
        {
            gameManager.trialNum.Value++; 
        }
        

        Dictionary<string,object> playerPosDict = new Dictionary<string,object>();


        // Assemble data to log from each network client
        Debug.Log($"ConnectedClientsList is {NetworkManager.ConnectedClientsList.Count} items long");
        foreach(var networkClient in NetworkManager.ConnectedClientsList)
        {
            PlayerLocation playerLocation = new PlayerLocation(
                                                networkClient.PlayerObject.transform.position.x,
                                                networkClient.PlayerObject.transform.position.y,
                                                networkClient.PlayerObject.transform.position.z
                                                );

            PlayerRotation playerRotation = new PlayerRotation(
                                                Camera.main.transform.rotation.eulerAngles.x,
                                                networkClient.PlayerObject.transform.rotation.eulerAngles.y,
                                                Camera.main.transform.rotation.eulerAngles.z
                                                );

            PlayerPosition thisPlayerPosition = new PlayerPosition(networkClient.ClientId, playerLocation, playerRotation);
            playerPosDict.Add(networkClient.ClientId.ToString(), thisPlayerPosition);
        }

        // create the full log event
        TrialStartLogEvent trialStartLogEvent = new TrialStartLogEvent(gameManager.trialNum.Value, gameManager.trialType.Value, playerPosDict);

        // JSON serialize the log object to string
        string logEntry = JsonConvert.SerializeObject(trialStartLogEvent);

        // Send string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);

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
        if (!IsServer) { Debug.Log("Not server, not running SliceOnsetHandler_SliceOnsetLog in LoggingEvents");
         return; }

        Debug.Log("Is Server, so running SliceOnsetHandler_SliceOnsetLog in LoggingEvents");

        int wall1 = gameManager.activeWalls.Value.wall1;
        int wall2 = gameManager.activeWalls.Value.wall2;
        Dictionary<string,object> playerPosDict = new Dictionary<string,object>();
        
        // For each connected client, create a player info class (defined in LoggingClasses)
        // and add this class as the value for this clientId in a dictionary
        // Then, log to JSON format the full slice onset information
        // as defined in LoggingClasses.SliceOnsetLogEvent
        var players = NetworkManager.ConnectedClientsList;
        // Debug.Log($"ConnectedClientsList is {players.Count} items long");
        for (int i = 0; i < players.Count; i++)
        {
            int clientId = i;
            NetworkClient networkClient = players[i];
            Vector3 playerLocation = networkClient.PlayerObject.gameObject.transform.position;
            PlayerLocation playerLocation2 = new PlayerLocation(playerLocation.x, playerLocation.y, playerLocation.z);

            Quaternion playerRotation = networkClient.PlayerObject.gameObject.transform.rotation;
            float camXAxisRotation = Camera.main.transform.rotation.eulerAngles.x;
            float camZAxisRotation = Camera.main.transform.rotation.eulerAngles.z;
            PlayerRotation playerRotation2 = new PlayerRotation(camXAxisRotation, playerRotation.eulerAngles.y, camZAxisRotation);

            PlayerPosition thisPlayerPosition = new PlayerPosition(networkClient.ClientId, playerLocation2, playerRotation2);

            playerPosDict.Add(networkClient.ClientId.ToString(), thisPlayerPosition);
            // Debug.Log($"playerPosDict is {playerPosDict.Count} item long");
        }

        // Create the final log class instance
        SliceOnsetLogEvent sliceOnsetLogEvent = new SliceOnsetLogEvent(wall1, wall2, playerPosDict);
        // Debug.Log("SliceOnsetLogEvent created");

        // Serialize the class to JSON
        string logEntry = JsonConvert.SerializeObject(sliceOnsetLogEvent, new JsonSerializerSettings
        {
            // This ensures that Unity Quaternions can serialize correctly
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        // Debug.Log("SliceOnsetLogEvent serialized to JSON string: " + logEntry);

        // Send this string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);
    }

    
    // Write a trigger activation log event when the value of NetworkVariable TriggerActivation
    // changes
    public void TriggerActivationHandler_TriggerActivationLog(TriggerActivation prevVal, TriggerActivation newVal)
    {
        
        if (newVal.triggerID == 0) {return; }
        if (!IsServer) { Debug.Log("Not server, not running TriggerActivationHandler_TriggerActivationLog in LoggingEvents");
         return; }

        Debug.Log("Is Server, so running TriggerActivationHandler_TriggerActivationLog in LoggingEvents");

        int wall1 = gameManager.activeWalls.Value.wall1;
        int wall2 = gameManager.activeWalls.Value.wall2;
        int wallTriggered = gameManager.triggerActivation.Value.triggerID;
        ulong triggerClientId = gameManager.triggerActivation.Value.activatorClientId;
        Dictionary<string,object> playerPosDict = new Dictionary<string,object>();

        // Assemble data to log from each network client
        // Debug.Log($"ConnectedClientsList is {NetworkManager.ConnectedClientsList.Count} items long");
        foreach(var networkClient in NetworkManager.ConnectedClientsList)
        {
            PlayerLocation playerLocation = new PlayerLocation(
                                                networkClient.PlayerObject.transform.position.x,
                                                networkClient.PlayerObject.transform.position.y,
                                                networkClient.PlayerObject.transform.position.z
                                                );

            PlayerRotation playerRotation = new PlayerRotation(
                                                Camera.main.transform.rotation.eulerAngles.x,
                                                networkClient.PlayerObject.transform.rotation.eulerAngles.y,
                                                Camera.main.transform.rotation.eulerAngles.z
                                                );

            PlayerPosition thisPlayerPosition = new PlayerPosition(networkClient.ClientId, playerLocation, playerRotation);
            playerPosDict.Add(networkClient.ClientId.ToString(), thisPlayerPosition);
        }

        // Create the final log class instance
        TriggerActivationLogEvent triggerActivationLogEvent = new TriggerActivationLogEvent(wall1, wall2, wallTriggered,
                                                                                             triggerClientId, playerPosDict);
        // Debug.Log("triggerActivationLogEvent created");

        // Serialize the class to JSON
        string logEntry = JsonConvert.SerializeObject(triggerActivationLogEvent, new JsonSerializerSettings
        {
            // This ensures that Unity Quaternions can serialize correctly
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        // Debug.Log("triggerActivationLogEvent serialized to JSON string: " + logEntry);

        // Send this string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);

    }
    
    // Write a trial end log event when the TrialActive NetworkVariable has it's value changed 
    // to false
    public void TrialActiveHandler_TrialEndLog()
    {
        if (!IsServer) { Debug.Log("Not server, not running TrialActiveHandler_TrialEndLog in LoggingEvents");
         return; }

        Debug.Log("Is Server, so running TrialActiveHandler_TrialEndLog in LoggingEvents");

        Dictionary<string,object> playerPosDict = new Dictionary<string, object>();
        Dictionary<string,object> playerScoresDict = new Dictionary<string, object>();

        // Assemble data to log from each network client
        Debug.Log($"ConnectedClientsList is {NetworkManager.ConnectedClientsList.Count} items long");
        int i = 0;
        foreach (var networkClient in NetworkManager.ConnectedClientsList)
        {
            
            PlayerLocation playerLocation = new PlayerLocation(
                                    networkClient.PlayerObject.transform.position.x,
                                    networkClient.PlayerObject.transform.position.y,
                                    networkClient.PlayerObject.transform.position.z
                                    );

            PlayerRotation playerRotation = new PlayerRotation(
                                                Camera.main.transform.rotation.eulerAngles.x,
                                                networkClient.PlayerObject.transform.rotation.eulerAngles.y,
                                                Camera.main.transform.rotation.eulerAngles.z
                                                );

            PlayerPosition thisPlayerPosition = new PlayerPosition(networkClient.ClientId, playerLocation, playerRotation);
            
            // Add entry to dictionary for this networkClient
            playerPosDict.Add(networkClient.ClientId.ToString(), thisPlayerPosition);
            // Debug.LogWarning($"Logging Events sees gameManager.scores[0] as {gameManager.scores[0]}");
            // Debug.LogWarning($"Logging Events sees gameManager.connectedClientIds[0] as {gameManager.connectedClientIds[0]}");
            playerScoresDict.Add(i.ToString(), gameManager.scores[i]);

            i++;
        }

        // create the full log event
        TrialEndLogEvent trialEndLogEvent = new TrialEndLogEvent(gameManager.trialNum.Value, playerPosDict, playerScoresDict);

        // JSON serialize the log object to string
        string logEntry = JsonConvert.SerializeObject(trialEndLogEvent);

        // Send string to the active diskLogger to be logged to file
        diskLogger.Log(logEntry);
    }

    // Write a time-triggered log at regular time intervals
    public void LoggingStartedHandler_TimeTriggeredLog()
    {
        if (!IsServer) { Debug.Log("Not server, not running LoggingStartedHandler_TimeTriggeredLog in LoggingEvents");
         return; }

        Debug.Log("Is Server, so running LoggingStartedHandler_TimeTriggeredLog in LoggingEvents");
        
        StartCoroutine(Coroutine_TimeTriggeredLog());
    }


    public IEnumerator Coroutine_TimeTriggeredLog()
    {
        while (true)
        {
            yield return new WaitForSeconds(Logging.loggingFrequency); 

            // create position dictionary
            Dictionary<string,object> playerPosDict = new Dictionary<string, object>();

            // Assemble data to log from each network client
            // Debug.Log($"ConnectedClientsList is {NetworkManager.ConnectedClientsList.Count} items long");
            foreach (var networkClient in NetworkManager.ConnectedClientsList)
            {
                
                PlayerLocation playerLocation = new PlayerLocation(
                                        networkClient.PlayerObject.transform.position.x,
                                        networkClient.PlayerObject.transform.position.y,
                                        networkClient.PlayerObject.transform.position.z
                                        );

                PlayerRotation playerRotation = new PlayerRotation(
                                                    Camera.main.transform.rotation.eulerAngles.x,
                                                    networkClient.PlayerObject.transform.rotation.eulerAngles.y,
                                                    Camera.main.transform.rotation.eulerAngles.z
                                                    );

                PlayerPosition thisPlayerPosition = new PlayerPosition(networkClient.ClientId, playerLocation, playerRotation);
                
                // Add entry to dictionary for this networkClient
                playerPosDict.Add(networkClient.ClientId.ToString(), thisPlayerPosition);
                // Debug.Log("PlayerPosDict should have received a new entry here ");

            }

            // Debug.LogWarning($"Logging Events sees gameManager.connectecClientIds[0] as {gameManager.connectedClientIds[0]}");

            // create the time-triggered log event
            TimeTriggeredLogEvent timeTriggeredLogEvent = new TimeTriggeredLogEvent(playerPosDict);

            // serialize to JSON format
            string logEntry = JsonConvert.SerializeObject(timeTriggeredLogEvent);
            
            // send string to logger
            diskLogger.Log(logEntry);
        }
    }


public void LoggingEndedHandler_EndLogging()
{
    // Create a StopLoggingLogEvent object
    StopLoggingLogEvent stopLoggingLogEvent = new StopLoggingLogEvent();

    // Serialize log event object to JSON formatted string
    string toLog = JsonConvert.SerializeObject(stopLoggingLogEvent);

    // Write string to file 
    diskLogger.Log(toLog);
}
    public void TrialActiveHandler(bool prevVal, bool newVal)
    {
        // if trial start
        if (newVal == true)
        {
            TrialActiveHandler_TrialStartLog();
        }
        // if trial end
        else
        {
            TrialActiveHandler_TrialEndLog();
        }
    }
}
