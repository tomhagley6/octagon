using System.Collections.Generic;
using UnityEngine;
using Logging;
using System;
using Unity.Netcode;
using static GameManager;
using Unity.VisualScripting;

/* Pass in the current ordered active triggers and the current trial type (controlled by GameController)
Then, use the trial type to decide how the trigger should respond to activation
Also consider which trigger instance has been activated on trigger entry */
public class WallTrigger : NetworkBehaviour
{
    public GameManager gameManager; 
    private string trialType = "HighLowTrial"; // replace with globals
    [SerializeField] private int highScore = 50; // globals
    [SerializeField] private int lowScore = 25; // globals
    IdentityAssignment identityAssignment;
    public int triggerID; 
    public List<GameObject> triggers; // Keep a handle on all triggers
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;

    public DiskLogger diskLogger;
    public TrialHandler trialHandler;

    public BoxCollider collider;
    
    // delegate to subscribe to when OnTriggerEnter is called
    public event Action<int> OnTriggerEntered;
    public static bool setupComplete = false;
    
    // Setup to run immediately after joining the network
    public override void OnNetworkSpawn() 
    {
        // GetComponent to return the IdentityAssignment instance for the current GameObject
        // Instance for GameManager as there is a single instance per scene (Singleton class)
        // (Consider checking if these are null or not before running)
        identityAssignment = gameObject.GetComponent<IdentityAssignment>(); 
        gameManager = GameManager.Instance;
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();
        collider = GetComponent<BoxCollider>(); 


        // This wallTrigger's associated wall number
        triggerID = identityAssignment.customID;

        // Read the wallID, for the case that this OnNetworkSpawn runs after the first trial starts
        if (gameManager != null && gameManager.activeWalls.Value.wall1 != 0)
        {
            wallID1 = gameManager.activeWalls.Value.wall1;
            wallID2 = gameManager.activeWalls.Value.wall2;
        }
        
        // Subscribe to the change in value for activeWalls NetworkVariable with a method 
        // which will update our class variables for the current active wall1 and wall2
        if (gameManager !=  null) {
            gameManager.activeWalls.OnValueChanged += OnWallChange;
        }
        else
        {
            Debug.Log("WallTrigger's gameManager is null at delegate subscription");
        }

        // Account for subscribing to GameManager after the first trial has begun
        if (wallID1 == 0)
        {
            // Check the current value 
            wallID1 = gameManager.activeWalls.Value.wall1;
            wallID2 = gameManager.activeWalls.Value.wall2;
            Debug.Log("WallIDs have been corrected in WallTrigger");
        }
        
        // Subscribe OnTriggerEntered Action with a callback function that 
        // deactivates the walls on this trial
        // To prevent re-entry of relevant walls within the same trial
        OnTriggerEntered += DeactivateWall;

        setupComplete = true;

    }


    void Start()
    {
        // Populate a list of all trigger GameObjects at Start time
        foreach (GameObject trigger in GameObject.FindGameObjectsWithTag("WallTrigger"))
        {
            triggers.Add(trigger);
        }
    }


    // Subscriber method for activeWall NetworkVariable value change
    // Update local class fields with new wall values and activate the colliders for these walls
    private void OnWallChange(ActiveWalls previousValue, ActiveWalls newValue) {
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
        if (collider.enabled == false)
        {
            collider.enabled = true;
            Debug.Log($"Collider for wall {triggerID} re-enabled");
        }
    }
    
    

    
    /* Method that runs when a Trigger is entered
    This is a callback method following Unity trigger entry
    This could be changed to invoke an event that methods
    in GameManager subscribe to, if I want to centralise logic */
    void OnTriggerEnter(Collider interactingObject)
    {
        
        Debug.Log("Trigger is entered");
    
        // Check if the GameObject that entered the trigger was the local client player's
        bool isTrialEnderClient = false;
        if (interactingObject.GetComponent<NetworkObject>() != null
         && interactingObject.GetComponent<NetworkObject>().IsLocalPlayer) isTrialEnderClient = true;
        
        // // Debug statements
        // Debug.Log($"interactingObject.GetComponent<NetworkObject>() != null is {interactingObject.GetComponent<NetworkObject>() != null}" + 
        // $"and interactingObject.GetComponent<NetworkObject>().IsLocalPlayer is { interactingObject.GetComponent<NetworkObject>().IsLocalPlayer}");
        // Debug.Log($"isTrialEnderClient = {isTrialEnderClient}");
        // Debug.Log($"Custom ID: {triggerID}");
        
        Debug.Log("Wall triggers are being triggered.");
        
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


    // Deactivate a trigger's collider
    // To use after the first activation of a trigger per trial
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
        
        // Only call EndTrial if this client is the one that ended the trial
        // to prevent multiple calls in multiplayer
        if (isTrialEnderClient) {
            Debug.Log($"EndTrial inputs: {score}, {highWallTriggerID}, {lowWallTriggerID}"
                        + $" {triggerID}, {rewardType}, {isTrialEnderClient}");
            trialHandler.EndTrial(score, isTrialEnderClient);
        }

        // all clients log their own event information
        diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
                                                Globals.trialNum,
                                                Globals.trialType,
                                                triggerID,
                                                rewardType,
                                                score,
                                                isTrialEnderClient));
        Debug.Log($"{rewardType} score ({score}) triggered");
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

}