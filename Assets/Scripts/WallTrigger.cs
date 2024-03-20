using System.Collections.Generic;
using UnityEngine;
using Logging;
using System;
using Unity.Netcode;
using static GameManager;

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
    private int triggerID; 
    public int wallID1;
    public int wallID2;

    public DiskLogger diskLogger;
    public TrialHandler trialHandler;
    
    public override void OnNetworkSpawn() 
    {
        // GetComponent to return the IdentityAssignment instance for the current GameObject
        // FindObjectOfType for GameManager as there is a single instance per scene
        identityAssignment = gameObject.GetComponent<IdentityAssignment>(); 
        gameManager = GameManager.Instance;
        diskLogger = FindObjectOfType<DiskLogger>();
        trialHandler = FindObjectOfType<TrialHandler>();

        triggerID = identityAssignment.customID;

        // Subscribe to the change in value for activeWalls NetworkVariable with a method 
        // which will update our class variables for the current active wall1 and wall2
        gameManager.activeWalls.OnValueChanged += OnWallChange;
    }

    private void OnWallChange(ActiveWalls previousValue, ActiveWalls newValue) {
        wallID1 = newValue.wall1;
        wallID2 = newValue.wall2;
    }
    
    // Method that runs when a Trigger is entered
    // No need to explicitly reference
    void OnTriggerEnter(Collider other)
    {
        // Check if the GameObject that entered the trigger was the local client player's
        bool isClient = false;
        if (other.GetComponent<NetworkObject>() != null && other.GetComponent<NetworkObject>().IsLocalPlayer) isClient = true;
        
        // Debug.Log($"Custom ID: {triggerID}");
        Debug.Log("Wall triggers are never being triggered.");
        
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

        if (triggerID == highWallTriggerID)
        {
            // Debug.Log("Active high wall is: " + gameManager.activeWalls[0]);
            Debug.Log("Active high wall is: " + highWallTriggerID);
            Debug.Log($"EndTrial inputs: {highScore}, {highWallTriggerID}, {lowWallTriggerID}, {triggerID}, {rewardType}, {IsLocalPlayer}");
            trialHandler.EndTrial(highScore, highWallTriggerID, lowWallTriggerID, triggerID, rewardType);
            diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
                                                                Globals.trialNum,
                                                                Globals.trialType,
                                                                triggerID,
                                                                rewardType,
                                                                highScore,
                                                                isClient));

            Debug.Log("High score (50) triggered");
        }
        else if (triggerID == lowWallTriggerID)
        {
            Debug.Log($"EndTrial inputs: {lowScore}, {highWallTriggerID}, {lowWallTriggerID}, {triggerID}, {rewardType}, {IsLocalPlayer}");
            trialHandler.EndTrial(lowScore, highWallTriggerID, lowWallTriggerID, triggerID, rewardType);
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
