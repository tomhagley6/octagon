// Script from 250804
// Archiving GameManager to try and reduce bloat comments and outdated code
// in the working copy. 
// This is the most up-to-date archive of these cut comment sections

using System;
using System.Collections;
using System.Collections.Generic;
using Globals;
using Unity.Netcode;
using UnityEngine;
using Random=UnityEngine.Random;
using KaimiraGames;
using Unity.Collections;


/* Class to control generation and updating of NetworkVariable
values for trials
NB: GameManager does not contain or trigger StartTrial or EndTrial
methods */
public class GameManagerArchived : SingletonNetwork<GameManager>
{
    // //  Variables
    public DiskLogger diskLogger;
    public TrialHandler trialHandler;
    public IdentityManager identityManager;

    public bool isReady = false;
    public int score;
    public int wallID1;
    public int wallID2;
    List<int> walls;
    public List<int> wallIDs;
    public List<GameObject> triggers; // Keep a handle on all triggers
    public event Action<bool> OnReadyStateChanged; // event for checking GameManager startup has run
    public event Action<int> OnTriggerEntered; // delegate to subscribe to when OnTriggerEnter is called
    public static event Action toggleOverlay;  // General overlay toggle 


    // // NetworkVariables 
    
    // Winning player should update the server following trigger entry
    // Create new NetworkVariable triggerActivation
    // Remember that we need to initialise [a NetworkVariable] at declaration
    public NetworkVariable<TriggerActivation> triggerActivation = new NetworkVariable<TriggerActivation>(

            new TriggerActivation
            {
                triggerID = 777,
                activatorClientId = 777
            }

    );
    
    public struct TriggerActivation : INetworkSerializable {
        public int triggerID;
        public ulong activatorClientId;

        // NetworkVariables implement the INetworkSerializable Interface, and therefore need
        // to have a definition for NetworkSerialize<T>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref triggerID);
            serializer.SerializeValue(ref activatorClientId);
        }
    }


    // Create a server-authoritative version of TriggerActivation to address race conditions
    // This NetworkVariable responds to TriggerActivations only if they end the trial
    public NetworkVariable<TriggerActivationAuthorised> triggerActivationAuthorised = new NetworkVariable<TriggerActivationAuthorised>(

            new TriggerActivationAuthorised {
                triggerID = 777,
                activatorClientId = 777
            }

    );

    public struct TriggerActivationAuthorised : INetworkSerializable {
        public int triggerID;
        public ulong activatorClientId;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref triggerID);
            serializer.SerializeValue(ref activatorClientId);
        } 
    }

    // Another server-authoritative version of TriggerActivation
    // But this time for all triggers that are NOT the end of a trial 
    public NetworkVariable<TriggerActivationIrrelevant> triggerActivationIrrelevant = new NetworkVariable<TriggerActivationIrrelevant>(
            new TriggerActivationIrrelevant {
                triggerID = 777,
                activatorClientId = 777
            }
    );

    public struct TriggerActivationIrrelevant: INetworkSerializable {
        public int triggerID;
        public ulong activatorClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref triggerID);
            serializer.SerializeValue(ref activatorClientId);
        }
    }


    /* trialNum int to act as a trigger for events to run on each trial start
    Instead of relying on activeWalls changing value for all of my logic, define logic based on epoch boundaries
    Create events for e.g. trial start, slice onset (which could be activeWalls)
    This will be initially useful for implementing my variable trial start to slice onset time
    // public NetworkVariable<int> trialNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); */

    // Current trial walls must be synced across clients
    // Create new NetworkVariable activeWalls
    public NetworkVariable<ActiveWalls> activeWalls = new NetworkVariable<ActiveWalls>(
        new ActiveWalls {
            wall1 = 0,
            wall2 = 0
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public struct ActiveWalls : INetworkSerializable {
        public int wall1;
        public int wall2;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref wall1);
            serializer.SerializeValue(ref wall2);
        }
    }

    public NetworkVariable<bool> trialActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<ushort> trialNum = new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> firstTriggerActivationThisTrial = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> trialType = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkList<ulong> connectedClientIds;
    public NetworkList<int> scores;


    // // Methods


    public void Awake()
    {
        connectedClientIds = new NetworkList<ulong>(new List<ulong>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        scores = new NetworkList<int>(new List<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        Debug.Log("GameManager Awake finished running");
    }


    public override void OnNetworkSpawn() {
        /* Debug: Subscribe to the OnValueChanged delegate with a lambda expression that fulfills the
        delegate by giving the correct signature (previous and new value), and in the body
        we can put a Debug statement to ensure we only log the value when it changes */
        
        /* trialNum.OnValueChanged += (int previousValue, int newValue) => {
            if (newValue == 0) return;
            Debug.Log($"Trial number: {newValue}");

        }; */

        // access other logic GameObjects in the scene 
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();
        identityManager = FindObjectOfType<IdentityManager>(); 
        // Debug.Log($"identityManager exists and its reference is {identityManager}");
        // Order identityManager's (populated) dictionary
        identityManager.OrderDictionary();  
        // Debug.Log("identityManager.OrderDictionary ran without errors");
        

        /* Here Invoking all subscribed methods of OnReadyStateChanged now that isReady is true
        Invoke called as a method on an event will trigger all methods subscribed to the event
        and passes them isReady as an input */
        isReady = true;
        Debug.Log($"GameManager.isReady is set to true: {isReady}");
        OnReadyStateChanged?.Invoke(isReady);

        // // Subscribe to changes in the triggerID NetworkVariable value
        // triggerID.OnValueChanged += TriggerIDHandler_DeactivateWalls;
        // triggerID.OnValueChanged += TriggerIDHandler_TriggerEntry;

        
        /// WALL NETWORKVARIABLE
        
        // REMOVED
        // Having TriggerActivationAuthorised should remove the need to manually activate and
        // deactivate WallTriggers on each trial
        // Subscribe to the change in value for activeWalls NetworkVariable with a method 
        // which will update our class variables for the current active wall1 and wall2
        activeWalls.OnValueChanged += ActiveWallsHandler_OnWallChange;

        // Read the wallID, for the case that this OnNetworkSpawn runs after the first trial starts
        wallID1 = activeWalls.Value.wall1;
        wallID2 = activeWalls.Value.wall2;

        // REMOVED
        // Seems to be a duplicate of the method in TrialLogic? Didn't realise I had 2
        /* Subscribe OnTriggerEntered Action with a callback method that 
        deactivates the walls on this trial
        To prevent re-entry of relevant walls within the same trial */
        OnTriggerEntered += DeactivateWall;

        /* Subscribe triggerActivation with a callback method that runs server-side 
        client owner authority logic and decides which TriggerActivation call to ratify */
        triggerActivation.OnValueChanged += TriggerActivationHandler_TriggerEntry;
        
        // /* Subscribe triggerActivation with a callback method that runs the trial
        // logic for a wall interaction */
        // triggerActivation.OnValueChanged += TriggerActivationHandler_TriggerEntry;

        /* Subscribe triggerActivationAuthorised with a callback method that executes 
        trial logic on the client, given that permission has been received from the server
        */
        triggerActivationAuthorised.OnValueChanged += TriggerActivationAuthorisedHandler_EnactServerTriggerDecision;

        // Subscribe connectedClientIds with a callback ServerRPC that updates
        // Actually we don't need to do this if we're checking the NetworkList when it's needed

        // // Subscribe to the slice onset event that is triggered by running ColourWalls 
        // trialHandler.sliceOnset += SliceOnsetHandler_SliceOnsetLogTrigger;
        
        // Subscribe to a change in trial active state

        // // is GameManager recognising as host or server?
        // Debug.LogWarning($"gameManager.IsServer is {IsServer}, gameManager.IsHost is {IsHost}");
    }


    void Start()
    {
        // Populate a list of all trigger GameObjects at Start time
        foreach (GameObject trigger in GameObject.FindGameObjectsWithTag("WallTrigger"))
        {
            triggers.Add(trigger);
        }

        // This could be moved to a different script? 
        Application.targetFrameRate = 144;
    }


    // Think I should be only doing this when the activated wall is relevant (further down end-trial logic)
    // // Walls should be deactivated for all clients upon triggerActivation NetworkVariable update
    // public void TriggerActivationHandler_DeactivateWalls(TriggerActivation prevValue, TriggerActivation newValue)
    // {
    //     DeactivateWall(activeWalls.Value.wall1);
    //     DeactivateWall(activeWalls.Value.wall2);
    // }

    // End trial logic should run for all clients upon triggerActivation NetworkVaraible update
    // This will vary for the trigger-activating client vs other clients
    public void TriggerActivationHandler_TriggerEntry_SUPERSEDED(TriggerActivation prevValue, TriggerActivation newValue)
    {   
        Debug.Log($"triggerActivation value received as triggerID {newValue.triggerID} and clientID {newValue.activatorClientId}");
        if (newValue.triggerID == 0) return;

        bool isTrialEnderClient;
        int triggerID;

        // Check client ids to see if this client ended the current trial
        isTrialEnderClient = newValue.activatorClientId == NetworkManager.Singleton.LocalClientId ? true : false;
        Debug.Log($"isTrialEnderClient returns as {isTrialEnderClient} on this client");
        // Debug.Log($"LocalClientId returns as {NetworkManager.Singleton.LocalClientId} on this client");
        triggerID = newValue.triggerID;

        // General game logic for interaction with a wall trigger
        WallInteraction(wallID1, wallID2, triggerID, isTrialEnderClient); // will not carry out actions if wall not active

        // switch (trialType.Value)
        // {
        //     case "HighLow":
        //         Debug.Log($"List at HighLowTrial execution is: {string.Join(",", wallID1, wallID2)}");
        //         HighLowTrial(wallID1, wallID2, triggerID, isTrialEnderClient);
        //         break;

        //     case "Forced":
        //         Debug.Log($"List at ForcedTrial execution is {string,Join(",", wallID1, wallID2)}");

        //     default:
        //         Debug.Log("Trial type not currently implemented");
        //         break;
        // }
    }

    /* Method to handle all changes to TriggerActivation NetworkVariable. 
       These changes happen whenever a player avatar interacts with an active trigger (code in WallTrigger.cs)
       Two different Coroutines are used here. One for the first relevant trigger of each trial, and one for all the rest
    */
    public void TriggerActivationHandler_TriggerEntry(TriggerActivation prevValue, TriggerActivation newValue)
    {
        // Only run trigger handling on the server (for security and to avoid replication)
        if (!IsServer) return;

        // ignore refresh of TriggerActivation values that happens between trials
        if (newValue.triggerID == 0) return;

        Debug.Log($"Server receives triggerActivation value as triggerID {newValue.triggerID} and clientID {newValue.activatorClientId}");

        // // If this is not the first call of this method on the server, break
        // if (!firstTriggerActivationThisTrial.Value)
        // {
        //     Debug.Log($"firstTriggerActivationThisTrial returns as {firstTriggerActivationThisTrial}");
        //     return;
        // }
        
        // if this is the first trigger activation of relevant trigger, run the TriggerEntryAuthorised coroutine
        if (firstTriggerActivationThisTrial.Value && wallIDs.Contains(newValue.triggerID)) {StartCoroutine(TriggerActivationHandler_TriggerEntryAuthorisedCoroutine(newValue));}

        // for all other triggers activations, run the TriggerEntryIrrelevant coroutine
        else {StartCoroutine(TriggerActivationHandler_TriggerEntryIrrelevantCoroutine(newValue));}

        // // Should be able to update NetworkVariable value here without ServerRPC, as already running on server
        // triggerActivationAuthorised.Value = new TriggerActivationAuthorised {
        //     triggerID = newValue.triggerID,
        //     activatorClientId = newValue.activatorClientId
        // };

        // // prevent further call of this method on the server
        // firstTriggerActivationThisTrial = false;

        // // reset values to 0 to account for next trial's TriggerActivation values being identical to the first
        // triggerActivationAuthorised.Value = new TriggerActivationAuthorised {
        //     triggerID = 0,
        //     activatorClientId = 0
        // };

    }

    private IEnumerator TriggerActivationHandler_TriggerEntryAuthorisedCoroutine(TriggerActivation newValue)
    {
        // pre-reset code

        // prevent further call of this method on the server
        // again, no need to write a ServerRPC for changing this value, as we are on the server here
        // Also, keeping this as a direct server change may be faster and help prevent the race condition
        firstTriggerActivationThisTrial.Value = false;
        // UpdateFirstTriggerActivationThisTrialServerRPC(true);

        // Should be able to update NetworkVariable value here without ServerRPC, as already running on server
        triggerActivationAuthorised.Value = new TriggerActivationAuthorised {
            triggerID = newValue.triggerID,
            activatorClientId = newValue.activatorClientId
        };

        yield return new WaitForSeconds(0.5f);

        // post-reset code

        // reset TriggerActivationAuthorised's values to 0 
        // to account for next trial's TriggerActivation values being identical to the first
        triggerActivationAuthorised.Value = new TriggerActivationAuthorised {
            triggerID = 0,
            activatorClientId = 0
        };

        // reset TriggerActivation's values to 0 to allow for subsequent irrelevant trigger activations
        triggerActivation.Value = new TriggerActivation {
            triggerID = 0,
            activatorClientId = 0
        };
        
        Debug.LogWarning($"triggerActivationAuthorised value has been changed to {triggerActivationAuthorised.Value.triggerID} and {triggerActivationAuthorised.Value.activatorClientId}");
    }

    private IEnumerator TriggerActivationHandler_TriggerEntryIrrelevantCoroutine(TriggerActivation newValue)
    {

        Debug.LogWarning($"Went to irrelevant because wall IDs are {wallIDs[0]} and {wallIDs[1]}, and firstTriggerActivationThisTrial is {firstTriggerActivationThisTrial.Value}");
        
        // Update NetworkVariable value here directly without ServerRPC, as already running on server (and this is faster)
        triggerActivationIrrelevant.Value = new TriggerActivationIrrelevant {
            triggerID = newValue.triggerID,
            activatorClientId = newValue.activatorClientId
        };

        Debug.LogWarning($"triggerActivationIrrelevant value has been changed to {triggerActivationIrrelevant.Value.triggerID} and {triggerActivationIrrelevant.Value.activatorClientId}");


        // Wait a short time before resetting the NetworkVariable value
        yield return new WaitForSeconds(0.02f);

        // Reset TriggerActivationIrrelevant's values to 0 
        // to account for next irrelevant trigger activation being identical to the last
        triggerActivationIrrelevant.Value = new TriggerActivationIrrelevant {
            triggerID = 0,
            activatorClientId = 0
        };

        // Reset TriggerActivation's values to 0
        // to allow for subsequent identical irrelevant trigger activations
        // And also for an irrelevant trigger activation followed immediately by a relevant 
        // trigger activation due to a new trial starting while player is within the 
        // newly-relevant trigger
        triggerActivation.Value = new TriggerActivation {
            triggerID = 0,
            activatorClientId = 0
        };

        Debug.LogWarning($"triggerActivationIrrelevant value has been changed to {triggerActivationIrrelevant.Value.triggerID} and {triggerActivationIrrelevant.Value.activatorClientId}");
    }
    

    /* Method to run game logic for the activation of a relevant trigger
       Only run by the client who was responsible for ending this trial.
       For server-authoritative architecture, could have this call a serverRPC
       that is security-checked server-side 
    */
    public void TriggerActivationAuthorisedHandler_EnactServerTriggerDecision(TriggerActivationAuthorised prevValue, TriggerActivationAuthorised newValue)
    {
        Debug.Log($"triggerActivationAuthorised value received as triggerID {newValue.triggerID} and clientID {newValue.activatorClientId}");
        if (newValue.triggerID == 0) return;

        bool isTrialEnderClient;
        int triggerID;

        // Check client ids to see if this client ended the current trial
        isTrialEnderClient = newValue.activatorClientId == NetworkManager.Singleton.LocalClientId ? true : false;
        Debug.Log($"isTrialEnderClient returns as {isTrialEnderClient} on this client");
        // Debug.Log($"LocalClientId returns as {NetworkManager.Singleton.LocalClientId} on this client");
        triggerID = newValue.triggerID;

        // General game logic for interaction with a wall trigger
        WallInteraction(wallID1, wallID2, triggerID, isTrialEnderClient); // will not carry out actions if wall not active

        Debug.Log($"isLocalPlayer returns as {IsLocalPlayer}");

    }


    // Subscriber method for activeWall NetworkVariable value change
    // Update local class fields with new wall values and activate the colliders for these walls
    private void ActiveWallsHandler_OnWallChange(ActiveWalls previousValue, ActiveWalls newValue) {
        if (newValue.wall1 == 0) return;
        
        wallID1 = newValue.wall1;
        wallID2 = newValue.wall2;
        // Debug.Log($"WallTrigger.cs has updated the values of local fields to match new wall values {wallID1} and {wallID2}");
        wallIDs = new List<int>(){wallID1, wallID2};
        // Debug.Log($"WallIDs list contains values: {String.Join(",", wallIDs)}");


        /* Set all walls active as triggers at the beginning of a trial (change of walls)
        (reactivate the triggers disabled in the last trial)
        Any triggers which are irrelevant for the current trial will not take any action
        within OnTriggerEnter */

        if (triggers != null)
        {
            foreach(GameObject trigger in triggers)
            {
                Collider collider = trigger.GetComponent<Collider>();
                if (collider.enabled == false)
                {
                    // collider.enabled = true;
                    // Debug.Log($"Collider for wall {trigger.GetComponent<WallTrigger>().triggerID} re-enabled");
                }
            }
        }
        else
        {
            Debug.Log("Triggers array in GameManager is null at time of ActiveWallsHandler_OnWallChange. Therefore, cannot change collider status");
        }
    }


    // // Trigger a slice onset log event when ColourWalls is run on the server (host)
    // /* Be careful with timing of these logs. Slice onset should ideally be when the new walls 
    //    become visible, and not when the activeWalls are changed. Must consider that when the
    //    colourWalls is run on the server is not necessarily when it is run on all clients
    // */
    // /* Also, it may be that when I convert my code to using a dedicated fully-authoritative server
    //    I will need to have my server run trial start logic and trigger ColourWalls on all clients.
    //    In that case, I will still have to consider how much lag there is between the server command
    //    and the client GameObject rendering change */
    // public void SliceOnsetHandler_SliceOnsetLogTrigger()
    // {
    //     // TODO
    //     if (!IsServer) { Debug.Log("Not server, not running SliceOnsetHandler_SliceOnsetLogTrigger in Gamemanager");
    //      return; }

    //     Debug.Log("Is Server, so running SliceOnsetHandler_SliceOnsetLogTrigger in GameManager");

    //     int wall1 = activeWalls.Value.wall1;
    //     int wall2 = activeWalls.Value.wall2;
    //     Dictionary<string,object> playerInfoDict = new Dictionary<string,object>();
        
    //     // For each connected client, create a player info class (defined in LoggingClasses)
    //     // and add this class as the value for this clientId in a dictionary
    //     // Then, log to JSON format the full slice onset information
    //     // as defined in LoggingClasses.SliceOnsetLogEvent
    //     var players = NetworkManager.ConnectedClientsList;
    //     Debug.Log($"ConnectedClientsList is {players.Count} items long");
    //     for (int i = 0; i < players.Count; i++)
    //     {
    //         int clientId = i;
    //         NetworkClient networkClient = players[i];
    //         Vector3 playerPosition = networkClient.PlayerObject.gameObject.transform.position;
    //         Quaternion playerRotation = networkClient.PlayerObject.gameObject.transform.rotation;

    //         PlayerInfo thisPlayerInfo = new PlayerInfo(networkClient.ClientId, playerPosition, playerRotation);

    //         playerInfoDict.Add(networkClient.ClientId.ToString(), thisPlayerInfo);
    //         Debug.Log($"playerInfoDict is {playerInfoDict.Count} item long");
    //     }

    //     // Create the final log class instance
    //     SliceOnsetLogEvent sliceOnsetLogEvent = new SliceOnsetLogEvent(wall1, wall2, playerInfoDict);
    //     Debug.Log("SliceOnsetLogEvent created");

    //     // Serialize the class to JSON
    //     string logEntry = JsonConvert.SerializeObject(sliceOnsetLogEvent, new JsonSerializerSettings
    //     {
    //         // This ensures that Unity Quaternions can serialize correctly
    //         ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    //     });
    //     Debug.Log("SliceOnsetLogEvent serialized to JSON string: " + logEntry);

    //     // Send this string to the active diskLogger to be logged to file
    //     diskLogger.Log(logEntry);
    // }

    // // Method to log slice onset data only for the Server, when ActiveWalls value changes
    // // Try to rewrite this to accept an arbitrary number of walls
    // private void ActiveWallsHandler_LogSliceOnset(ActiveWalls previousValue, ActiveWalls newValue)
    // {
    //     if (!IsServer) { return; }

    //     int wall1 = newValue.wall1;
    //     int wall2 = newValue.wall2;
    //     Dictionary<string,object> playerInfoDict = new Dictionary<string,object>();
        
    //     // For each connected client, create a player info class (defined in LoggingClasses)
    //     // and add this class as the value for this clientId in a dictionary
    //     // Then, log to JSON format the full slice onset information
    //     // as defined in LoggingClasses.SliceOnsetLogEvent
    //     var players = NetworkManager.ConnectedClientsList;
    //     for (int i = 0; i < players.Count; i++)
    //     {
    //         int clientId = i;
    //         NetworkClient networkClient = players[i];
    //         Vector3 playerPosition = networkClient.PlayerObject.gameObject.transform.position;
    //         Quaternion playerRotation = networkClient.PlayerObject.gameObject.transform.rotation;

    //         PlayerInfo thisPlayerInfo = new PlayerInfo(networkClient.ClientId, playerPosition, playerRotation);

    //         playerInfoDict.Add(networkClient.ClientId.ToString(), thisPlayerInfo);
    //     }

    //     // Create the final log class instance
    //     SliceOnsetLogEvent sliceOnsetLogEvent = new SliceOnsetLogEvent(wall1, wall2, playerInfoDict);

    //     // Serialize the class to JSON
    //     string toLog = JsonConvert.SerializeObject(sliceOnsetLogEvent);

    //     // Send this string to the active diskLogger to be logged to file
        

    // }

    
    public string SelectTrial()
    {
        // Create weighted list of trial types to draw from 
        WeightedList<string> trialTypeDist = new();
        for (int i = 0; i < General.trialTypes.Count; i++)
        {
            trialTypeDist.Add(General.trialTypes[i], General.trialTypeProbabilities[i]);
        }
        
        // Return trial type for this trial
        return trialTypeDist.Next();
    }


    // Basic handling of wall interaction prior to specific trial-type handling
    void WallInteraction(int wallID1, int wallID2, int triggerID, bool isTrialEnderClient)
    {
        // Debug.Log("WallInteraction running");
        
        // int highWallTriggerID = wallIDHigh;
        // int lowWallTriggerID = wallIDLow;

        Debug.Log("Values WallInteraction receives for wall1 and wall2 are: "
        + $"{wallID1} and {wallID2}");
        
        // If this is a relevant wall for the current trial
        if (triggerID == wallID1 || triggerID == wallID2)
        {
            // Invoke the callbacks on OnTriggerEntered Action for each wall currently active
            // Would it be cleaner to only use local variables instead of the wallIDs field? 
            for (int i = 0; i < wallIDs.Count; i++)
                {
                    OnTriggerEntered?.Invoke(wallIDs[i]);
                    Debug.Log($"Invoked OnTriggerEntered's subscribed method DeactivateWall" 
                    + $" for wall number {wallIDs[i]}");
                }

            // Handle the wall interaction for this trial and trial type
            TrialInteraction(triggerID, wallID1, wallID2, isTrialEnderClient);
        }
        else Debug.Log("No conditions met for WallInteraction");
    }


    void TrialInteraction(int triggerID, int highWallTriggerID,
                                int lowWallTriggerID, bool isTrialEnderClient)
    {
        
        Debug.Log("Entered TrialInteraction");

        // LVs
        int score = 0;
        string rewardType = "";

        switch (trialType.Value)
        {
            case var value when value == General.highLow: // No one knows why this works, but cannot directly use FixedString value
            
            score = triggerID == highWallTriggerID ? General.highScore : General.lowScore;
            rewardType = triggerID == highWallTriggerID ? General.highScoreRewardType : General.lowScoreRewardType;
        
           break;

            case var value when value == General.forcedHigh:
            
            score = General.highScore;
            rewardType = General.highScoreRewardType;

            break;

            case var value when value == General.forcedLow:

            score = General.lowScore;
            rewardType = General.lowScoreRewardType;

            break;
        }

        // Debug statement
        Debug.Log($"End of trial values: {score}, {wallID1}, {wallID2}"
        + $" {triggerID}, {rewardType}, {isTrialEnderClient}");

        // All clients wash their own walls, making use of the wall number NetworkObject
        // Bugs that prevent triggers triggering on other clients will prevent this code from running
        trialHandler.WashWalls(wallID1, wallID2);

        // Only call EndTrial if this client is the one that ended the trial
        // to prevent multiple calls in multiplayer
        if (isTrialEnderClient) {
            // Debug.Log($"EndTrial inputs: {score}, {highWallTriggerID}, {lowWallTriggerID}"
            //             + $" {triggerID}, {rewardType}, {isTrialEnderClient}");
            trialHandler.EndTrial(score, isTrialEnderClient);
        }

        Debug.Log($"{rewardType} score ({score}) triggered");
    }


    void DeactivateWall(int wallID) 
    {
        // Look into each trigger and find the one with the matching wallID to deactivate
        foreach(GameObject trigger in triggers)
        {
            WallTrigger wallTrigger = trigger.GetComponent<WallTrigger>();
            if (wallTrigger != null && wallTrigger.triggerID == wallID)
            { 
                // wallTrigger.collider.enabled = false;
            }
        }
    }
    

    public List<int> SelectNewWalls() {
        Debug.Log("NEW TRIAL");

        // Generate wall trigger IDs for a new trial
        walls = identityManager.ListCustomIDs();

        // Choose a random anchor wall to reference the trial to 
        int anchorWallIndex = Random.Range(0, walls.Count);
        // Debug.Log($"anchor walls is {anchorWallIndex}");

        // Replaced the below with a weighted list to bias towards wallSep=1 trials
        // // Randomly choose a wall separation value for this trial
        // int i = General.wallSeparations[Random.Range(0, General.wallSeparations.Count)];


        // Create weighted list of wall separation values to draw from 
        WeightedList<int> wallSeparationsWeighted = new();
        for (int i = 0; i < General.wallSeparations.Count; i++)
        {
            wallSeparationsWeighted.Add(General.wallSeparations[i], General.wallSeparationsProbabilities[i]);
        }
        // Query the weighted list for this trial's wall separation
        int wallSeparation = wallSeparationsWeighted.Next();
        

        // choose a random second wall that is consistent with anchor wall for this trial type
        int wallIndexDiff = new List<int>{-wallSeparation, wallSeparation}[Random.Range(0, 2)];
        // Debug.Log($"wallIndexDiff = {wallIndexDiff}");
        int dependentWallIndex = anchorWallIndex + wallIndexDiff;
        // Debug.Log($"naive dependent wall is walls is {dependentWallIndex}");
        
        // Account for circular octagon structure
        if (dependentWallIndex < 0)
        {
            dependentWallIndex += walls.Count;
            // Debug.Log($" dependent wall < 0, so corrected to {dependentWallIndex}");

        }
        else if (dependentWallIndex >= walls.Count)
        {
            dependentWallIndex -= walls.Count;
            // Debug.Log($" dependent wall >= walls.Count - 1, so corrected to {dependentWallIndex}");
        }
        
        // assign high and low walls with the generated indexes
        // Debug.Log($"chosen walls are {anchorWallIndex}, {dependentWallIndex}");
        int highWallTriggerID = walls[anchorWallIndex];
        int lowWallTriggerID = walls[dependentWallIndex];   

        return new List<int>(new int[] {highWallTriggerID, lowWallTriggerID});
    }
    

    // Limit sessions to a defined number of trials (preserves leaderboard integrity)
    public void EndCurrentSession()
    {
        // Not implemented
    }

    // Toggle UI overlay (move this to a different script?)
    public static void ToggleOverlay()
    {
        toggleOverlay();
    }


    // // Server RPCs


    public void UpdateActiveWalls(List<int> wallList)
    {
        
        // Update activeWalls with new wall values
        UpdateWallsServerRPC(wallList[0], wallList[1]);

        Debug.Log($"activeWalls value is set with the values {wallList[0]} and {wallList[1]}");
    }


    // RPC to update activeWalls on the server and not the client
    [ServerRpc(RequireOwnership=false)]
    public void UpdateWallsServerRPC(int _wall1, int _wall2)
    {
        // This will cause a change over the network
        // and ultimately invoke `OnValueChanged` on all receivers
        activeWalls.Value = new ActiveWalls {
            wall1 = _wall1,
            wall2 = _wall2
        };
    }
    

    /* Accessed directly from OnTriggerEntry method in WallTrigger, which identifies client-side
       whether this client is the one that entered the trigger zone. 
       OnTriggerEnter passes the activator client ID here, if it is indeed the trial-ending client
    */
    public void UpdateTriggerActivation(int triggerID, ulong activatorClientId)
    {
        
        // Update triggerActivation with new wall values
        // as long as the triggerID matches one of the active walls or is 0 (for resetting)
        // if (wallIDs.Contains(triggerID) || triggerID == 0)
        // {
        UpdateTriggerActivationServerRPC(triggerID, activatorClientId);
        Debug.Log("This client just updated the triggerActivation NetworkVariable");
        // }

    }


    // RPC to update triggerActivation on the server and not the client
    [ServerRpc(RequireOwnership=false)]
    public void UpdateTriggerActivationServerRPC(int _triggerID, ulong _activatorClientId)
    {
        // This will cause a change over the network
        // and ultimately invoke `OnValueChanged` on all receivers
        triggerActivation.Value = new TriggerActivation {
          triggerID = _triggerID,
          activatorClientId = _activatorClientId  
        };
        // Debug.Log($"triggerActivation value is set with the triggerID {_triggerID} and clientID {_activatorClientId}");

    
    }


    // RPC to update trialStart on the server and not the client
    [ServerRpc(RequireOwnership=false)]
    public void ToggleTrialActiveServerRPC()
    {
        trialActive.Value = !trialActive.Value;
        // Debug.Log($"trialActive value is now {trialActive.Value}");
    }

    // RPC to update the current trial type value
    [ServerRpc(RequireOwnership=false)]
    public void UpdateTrialTypeServerRPC(string trialType)
    {
        this.trialType.Value = trialType;
        // Debug.LogError($"trialType is now {trialType}");
    }

    [ServerRpc(RequireOwnership=false)]
    public void UpdateFirstTriggerActivationThisTrialServerRPC(bool firstTriggerActivationThisTrial)
    {
        this.firstTriggerActivationThisTrial.Value = firstTriggerActivationThisTrial;
    }

    // RPC to access and update local-client IDs
    [ServerRpc(RequireOwnership=false)]
    public void UpdateClientIdsServerRPC()
    {
        var players = NetworkManager.ConnectedClientsList;
        Debug.Log(NetworkManager.ConnectedClientsList);

        for (int i = 0; i < players.Count; i++)
        {
            if ((ulong)i == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"Local player Id {NetworkManager.Singleton.LocalClientId} contained in client list");
            }
            else
            {
                Debug.Log($"client {i} is connected to the server but is not the local player");
            }
        }
    }


    // ServerRPC to update player scores
    // Currently this is probably only attributing scores to the Host player because this method is run ON the server
    // hence LocalClientId will always be 0 
    [ServerRpc(RequireOwnership=false)]
    public void UpdateScoresServerRPC(int increment, ulong callerClientId)
    {
        // var players = NetworkManager.ConnectedClientsList;

        // for (int i = 0; i < players.Count; i++)
        // {
        //     if ((ulong)i == NetworkManager.Singleton.LocalClientId)
        //     {
        //         Debug.Log($"Local player Id {NetworkManager.Singleton.LocalClientId} contained in client list, updating score");
        //         scores[i] += increment;
        //     }
        //     else
        //     {
        //         Debug.Log($"client {i} is connected to the server but is not the local player, not updating score");
        //     }
        // } 

        scores[(int)callerClientId] += increment; 
    }

    [ServerRpc(RequireOwnership = false)]
    public void IlluminationHighServerRpc(bool isHigh)
    {
        // Call a client Rpc for all clients to set global illumination to isHigh
        trialHandler.IlluminationHighClientRpc(isHigh);
    }

    /* ServerRPC to log data for all clients at slice onset,
    including clientId, gameObject.transform.position, and 
    gameObject.transform.rotation
    */
    // Maybe first try just executing the logging only if this is the server? 
    // in a callback method for activeWalls.OnValueChanged, with
    // if (isServer)
    [ServerRpc(RequireOwnership = false)]
    public void LogClientDataServerRPC()
    {
        // Not implemented
    }




}


