using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Logging;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random=UnityEngine.Random;


/* Class to control generation and updating of NetworkVariable
values for trials
NB: GameManager does not contain or trigger StartTrial or EndTrial
methods */
public class GameManager : SingletonNetwork<GameManager>
{

    public DiskLogger diskLogger;
    public TrialHandler trialHandler;

    public int score;
    List<int> walls;
    public IdentityManager identityManager;
    public List<GameObject> triggers; // Keep a handle on all triggers

    // Setup an event to enable checking that GameManager has completed startup code
    public event Action<bool> OnReadyStateChanged; 
    public bool isReady = false;
    [SerializeField] private int highScore = 50; // globals
    [SerializeField] private int lowScore = 25; // globals
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;

     private string trialType = "HighLowTrial"; // replace with globals

    // delegate to subscribe to when OnTriggerEnter is called
    public event Action<int> OnTriggerEntered;

    // Winning player should update the server following trigger entry
    // Create new NetworkVariable triggerActivation
    public NetworkVariable<TriggerActivation> triggerActivation = new NetworkVariable<TriggerActivation>(

            new TriggerActivation {
                triggerID = 777,
                activatorClientId = 777
            }

    );
    
    public struct TriggerActivation : INetworkSerializable {
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
    public NetworkVariable<int> trialNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); */

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


    public override void OnNetworkSpawn() {
        /* Subscribe to the OnValueChanged delegate with a lambda expression that fulfills the
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
        OnReadyStateChanged?.Invoke(isReady);

        // // Subscribe to changes in the triggerID NetworkVariable value
        // triggerID.OnValueChanged += TriggerIDHandler_DeactivateWalls;
        // triggerID.OnValueChanged += TriggerIDHandler_TriggerEntry;

        
        /// WALL NETWORKVARIABLE
        // Subscribe to the change in value for activeWalls NetworkVariable with a method 
        // which will update our class variables for the current active wall1 and wall2
        activeWalls.OnValueChanged += ActiveWallsHandler_OnWallChange;

        // Read the wallID, for the case that this OnNetworkSpawn runs after the first trial starts
        wallID1 = activeWalls.Value.wall1;
        wallID2 = activeWalls.Value.wall2;

        /* Subscribe OnTriggerEntered Action with a callback method that 
        deactivates the walls on this trial
        To prevent re-entry of relevant walls within the same trial */
        OnTriggerEntered += DeactivateWall;

        /* Subscribe triggerActivation with a callback method that runs the trial
        logic for a wall interaction */
        triggerActivation.OnValueChanged += TriggerActivationHandler_TriggerEntry;

        

    }

    void Start()
    {
        // Populate a list of all trigger GameObjects at Start time
        foreach (GameObject trigger in GameObject.FindGameObjectsWithTag("WallTrigger"))
        {
            triggers.Add(trigger);
        }
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
    public void TriggerActivationHandler_TriggerEntry(TriggerActivation prevValue, TriggerActivation newValue)
    {   
        Debug.Log($"triggerActivation value received as triggerID {newValue.triggerID} and clientID {newValue.activatorClientId}");
        if (newValue.triggerID == 0) return;

        bool isTrialEnderClient;
        int triggerID;

        // Check client ids to see if this client ended the current trial
        isTrialEnderClient = newValue.activatorClientId == NetworkManager.Singleton.LocalClientId ? true : false;
        Debug.Log($"isTrialEnderClient returns as {isTrialEnderClient} on this client");
        Debug.Log($"LocalClientId returns as {NetworkManager.Singleton.LocalClientId} on this client");
        triggerID = newValue.triggerID;

        switch (trialType)
        {
            case "HighLowTrial":
                Debug.Log($"List at HighLowTrial execution is: {string.Join(",", wallID1, wallID2)}");
                HighLowTrial(wallID1, wallID2, triggerID, isTrialEnderClient);
                break;

            default:
                Debug.Log("Trial type not currently implemented");
                break;

        }

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
                    collider.enabled = true;
                    Debug.Log($"Collider for wall {trigger.GetComponent<WallTrigger>().triggerID} re-enabled");
                }
            }
        }
        else
        {
            Debug.Log("Triggers array in GameManager is null at time of ActiveWallsHandler_OnWallChange. Therefore, cannot change collider status");
        }
    }
    

    
    // Standard HighLow trial
    void HighLowTrial(int wallIDHigh, int wallIDLow, int triggerID, bool isTrialEnderClient)
    {
        Debug.Log("HighLowTrial running");
        
        int highWallTriggerID = wallIDHigh;
        int lowWallTriggerID = wallIDLow;

        Debug.Log("Values HighLowTrial receives for high and low wall IDs are: "
        + $"{highWallTriggerID} and {lowWallTriggerID}");
        
        // If this is a relevant wall for the current trial
        if (triggerID == highWallTriggerID || triggerID == lowWallTriggerID)
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
            WallTrialInteraction(triggerID, highWallTriggerID, lowWallTriggerID, isTrialEnderClient);
        }
        else Debug.Log("No conditions met for HighLowTrial");
    
    }


    void WallTrialInteraction(int triggerID, int highWallTriggerID,
                                int lowWallTriggerID, bool isTrialEnderClient)
    {
        
        Debug.Log("Entered WallTrialInteraction");

        // LVs
        int score = triggerID == highWallTriggerID ? highScore : lowScore;
        string rewardType = triggerID == highWallTriggerID ? "High" : "Low";

        // All clients wash their own walls, making use of the wall number NetworkObject
        // Bugs that prevent triggers triggering on other clients will prevent this code from running
        trialHandler.WashWalls(highWallTriggerID, lowWallTriggerID);
        
        // Debug statement
        Debug.Log($"End of trial values: {score}, {highWallTriggerID}, {lowWallTriggerID}"
            + $" {triggerID}, {rewardType}, {isTrialEnderClient}");

        // Only call EndTrial if this client is the one that ended the trial
        // to prevent multiple calls in multiplayer
        if (isTrialEnderClient) {
            Debug.Log($"EndTrial inputs: {score}, {highWallTriggerID}, {lowWallTriggerID}"
                        + $" {triggerID}, {rewardType}, {isTrialEnderClient}");
            trialHandler.EndTrial(score, isTrialEnderClient);
        }


        // // all clients log their own event information
        // diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
        //                                         Globals.trialNum,
        //                                         Globals.trialType,
        //                                         triggerID,
        //                                         rewardType,
        //                                         score,
        //                                         isTrialEnderClient));
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
                wallTrigger.collider.enabled = false;
            }
        }
    }
    
    public List<int> SelectNewWalls() {
        Debug.Log("NEW TRIAL");

        // Generate wall trigger IDs for a new trial
        walls = identityManager.ListCustomIDs();

        // Choose a random anchor wall to reference the trial to 
        int anchorWallIndex = UnityEngine.Random.Range(0, walls.Count);
        // choose a random second wall that is consistent with anchor wall for this trial type
        int wallIndexDiff = new List<int>{-2, 2}[UnityEngine.Random.Range(0, 1)];
        int dependentWallIndex = anchorWallIndex + wallIndexDiff;
        
        // Account for circular octagon structure
        if (dependentWallIndex < 0)
        {
            dependentWallIndex += walls.Count;
        }
        else if (dependentWallIndex >= walls.Count - 1)
        {
            dependentWallIndex -= walls.Count;
        }
        
        // assign high and low walls with the generated indexes
        int highWallTriggerID = walls[anchorWallIndex];
        int lowWallTriggerID = walls[dependentWallIndex];   

        return new List<int>(new int[] {highWallTriggerID, lowWallTriggerID});
    }
    

    public void UpdateActiveWalls(List<int> wallList)
    {
        
        // Update activeWalls with new wall values
        UpdateWallsServerRpc(wallList[0], wallList[1]);

        Debug.Log($"activeWalls value is set with the values {wallList[0]} and {wallList[1]}");
    }


    // RPC to update activeWalls on the server and not the client
    [ServerRpc(RequireOwnership=false)]
    public void UpdateWallsServerRpc(int _wall1, int _wall2)
    {
        // This will cause a change over the network
        // and ultimately invoke `OnValueChanged` on all receivers
        activeWalls.Value = new ActiveWalls {
            wall1 = _wall1,
            wall2 = _wall2
        };
    }

    public void UpdateTriggerActivation(int triggerID, ulong activatorClientId)
    {
        
        // Update activeWalls with new wall values
        UpdateTriggerActivationServerRPC(triggerID, activatorClientId);

    }


    // RPC to update activeWalls on the server and not the client
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

    // RPC to access and update local-client IDs

}
