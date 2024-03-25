using System.Collections.Generic;
using UnityEngine;
using Logging;
using System;
using Unity.Netcode;
using static GameManager;
using Unity.VisualScripting;

// Pass in the current ordered active triggers and the current trial type (controlled by GameController)
// Then, use the trial type to decide how the trigger should respond to activation
// Also consider which trigger instance has been activated on trigger entry
public class WallTrigger : NetworkBehaviour
{
    public GameManager gameManager; 
    private string trialType = "HighLowTrial"; // replace with global?
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
    
    
    public override void OnNetworkSpawn() 
    {
        Debug.Log("Begin WallTrigger.cs OnNetworkSpawn");

        // GetComponent to return the IdentityAssignment instance for the current GameObject
        // FindObjectOfType for GameManager as there is a single instance per scene
        identityAssignment = gameObject.GetComponent<IdentityAssignment>(); 
        gameManager = GameManager.Instance;
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();

        triggerID = identityAssignment.customID;

        // Subscribe to the change in value for activeWalls NetworkVariable with a method 
        // which will update our class variables for the current active wall1 and wall2
        if (gameManager !=  null) {
            gameManager.activeWalls.OnValueChanged += OnWallChange;
            Debug.Log("WallTrigger.cs subscribed successfully to activeWalls.OnValueChanged with OnWallChange");
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
        
    
        collider = GetComponent<BoxCollider>(); 

        // Subscribe OnTriggerEntered Action with a callback function that 
        // deactivates the walls on this trial
        OnTriggerEntered += DeactivateWall;


    }

    private void OnWallChange(ActiveWalls previousValue, ActiveWalls newValue) {
        if (newValue.wall1 == 0) return;
        
        wallID1 = newValue.wall1;
        wallID2 = newValue.wall2;
        Debug.Log($"WallTrigger.cs has updated the values of local fields to match new wall values {wallID1} and {wallID2}");
        wallIDs = new List<int>(){wallID1, wallID2};
        Debug.Log($"WallIDs list contains values: {String.Join(",", wallIDs)}");


        // set all walls active as triggers at the beginning of a trial (change of walls)
        // any triggers which are irrelevant for the current trial will not take any action
        // within OnTriggerEnter
        if (collider.enabled == false)
        {
            collider.enabled = true;
            Debug.Log($"Collider for wall {triggerID} re-enabled");
        }
    }
    
    
    void Start()
    {
        // Populate a list of all trigger GameObjects at Start time
        foreach (GameObject trigger in GameObject.FindGameObjectsWithTag("WallTrigger"))
        {
            triggers.Add(trigger);
        }
    }
    
    // Method that runs when a Trigger is entered
    // No need to explicitly reference
    void OnTriggerEnter(Collider other)
    {
        
        // Invoke the callback on OnTriggerEntered Action for each wall currently active
        for (int i = 0; i < wallIDs.Count; i++)
        {
            OnTriggerEntered?.Invoke(wallIDs[i]);
            Debug.Log($"Invoked OnTriggerEntered's subscribed method DeactivateWall for wall number {wallIDs[i]}");
        }
        /* // Prevent repeat activation of the trigger
        collider.enabled = false; */
        // Check if the GameObject that entered the trigger was the local client player's
        bool isClient = false;
        if (other.GetComponent<NetworkObject>() != null && other.GetComponent<NetworkObject>().IsLocalPlayer) isClient = true;
        
        // Debug.Log($"Custom ID: {triggerID}");
        Debug.Log("Wall triggers are being triggered.");
        
        switch (trialType)
        {
            case "HighLowTrial":
                Debug.Log($"List at HighLowTrial execution is: {string.Join(",", wallID1, wallID2)}");
                // HighLowTrial(gameManager.activeWalls, triggerID, isClient);
                HighLowTrial(wallID1, wallID2, triggerID, isClient);
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

    // Standard HighLow trial
    // If this wall is designated High, add 50 points to score
    // Else if it is Low, add 25 points to score
    void HighLowTrial(int wallIDHigh, int wallIDLow, int triggerID, bool isClient)
    {
        // int highWallTriggerID = activeWalls[0];
        // int lowWallTriggerID = activeWalls[1];
        int highWallTriggerID = wallIDHigh;
        int lowWallTriggerID = wallIDLow;
        string rewardType = triggerID == highWallTriggerID ? "High" : "Low";

        Debug.Log($"Values HighLowTrial receives for high and low wall IDs are: {highWallTriggerID} and {lowWallTriggerID}");

        if (triggerID == highWallTriggerID)
        {
            // Debug.Log("Active high wall is: " + gameManager.activeWalls[0]);
            Debug.Log("Active high wall is: " + highWallTriggerID);
            Debug.Log($"EndTrial inputs: {highScore}, {highWallTriggerID}, {lowWallTriggerID}, {triggerID}, {rewardType}, {IsLocalPlayer}");
            Debug.Log("High score (50) triggered");
            // if (isClient) {
            trialHandler.EndTrial(highScore, highWallTriggerID, lowWallTriggerID, triggerID, rewardType);
            // }
            diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
                                                                Globals.trialNum,
                                                                Globals.trialType,
                                                                triggerID,
                                                                rewardType,
                                                                highScore,
                                                                isClient));


        }
        else if (triggerID == lowWallTriggerID)
        {
            Debug.Log($"EndTrial inputs: {lowScore}, {highWallTriggerID}, {lowWallTriggerID}, {triggerID}, {rewardType}, {IsLocalPlayer}");
            // if (isClient) {
            trialHandler.EndTrial(highScore, highWallTriggerID, lowWallTriggerID, triggerID, rewardType);
            // }
            diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
                                                    Globals.trialNum,
                                                    Globals.trialType,
                                                    triggerID,
                                                    rewardType,
                                                    lowScore,
                                                    isClient));
            Debug.Log("Low score (25) triggered");
        }
        else Debug.Log("No conditions met for HighLowTrial");
    }
}
