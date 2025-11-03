using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Netcode;
using static GameManager;

/* Class to be attached to each individual wall trigger in the scene
   Instances of this class receive the current active triggers and trial type
   and decide how the relevant trigger should respond to activation in each 
   case of collision with another object */
public class WallTrigger : NetworkBehaviour
{
    public GameManager gameManager;
    [SerializeField] private int highScore = 50; // globals
    [SerializeField] private int lowScore = 10; // globals
    IdentityAssignment identityAssignment;
    public int triggerID;
    public List<GameObject> triggers; // Keep a handle on all triggers
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs; // Updated to contain new ActiveWalls IDs
                              // on ActiveWalls.OnValueChanged
    public List<int> prevActiveWallIDs;

    public DiskLogger diskLogger;
    public TrialHandler trialHandler;

    public BoxCollider collider;
        public static bool setupComplete = false;


    // delegate to subscribe to when OnTriggerEnter is called
    public event Action<int> OnTriggerEntered;

    // Setup to run immediately after joining the network
    public override void OnNetworkSpawn()
    {

        /* GetComponent to return the IdentityAssignment component for this WallTrigger
        (Consider checking if these are null or not before running) */
        identityAssignment = gameObject.GetComponent<IdentityAssignment>();
        gameManager = FindObjectOfType<GameManager>();
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
        if (gameManager != null)
        {
            gameManager.activeWalls.OnValueChanged += OnWallChange;
        }
        else
        {
            Debug.Log("WallTrigger's gameManager is null at delegate subscription");
            try
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    Debug.Log("Second time, gameManager is still null");
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        // Account for subscribing to GameManager after the first trial has begun
        if (wallID1 == 0)
        {
            // Check the current value 
            wallID1 = gameManager.activeWalls.Value.wall1;
            wallID2 = gameManager.activeWalls.Value.wall2;
            Debug.Log("WallIDs have been corrected in WallTrigger");
        }



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


    /* This is a callback method that runs once following Unity trigger entry
    This could be changed to invoke an event that methods
    in GameManager subscribe to, if I want to centralise logic */
    void OnTriggerEnter(Collider interactingObject)
    {

        // Check if the GameObject that entered the trigger was the local client player's
        bool isTrialEnderClient = false;
        Debug.Log("IsLocalPlayer of interacting object at time of OnTriggerEnter is: "
                    + interactingObject.GetComponent<NetworkObject>().IsLocalPlayer);
        if (interactingObject.GetComponent<NetworkObject>() != null
         && interactingObject.GetComponent<NetworkObject>().IsLocalPlayer) isTrialEnderClient = true;

        Debug.Log("isTrialEnderClient at time of if statement in OnTriggerEnter is " + isTrialEnderClient);


        // NEW LOGIC TO UPDATE THE NETWORKVARIABLE AND TRIGGER GAMEMANAGER LOGIC
        // // Is this only ever true on the Host? Check behaviour of isLocalPlayer
        if (isTrialEnderClient)
        {
            gameManager.UpdateTriggerActivation(triggerID, NetworkManager.Singleton.LocalClientId);
            Debug.Log("Trigger is entered on local client");
            Debug.Log($"LocalClientId at time of update is {NetworkManager.Singleton.LocalClientId}");
        }
        else
        {
            Debug.Log("As isTrialEnderClient is false, not updating trigger activation");
        }
    }


        /* Method that runs on each frame when a Trigger has collision */
    void OnTriggerStay(Collider interactingObject)
    {

        // if ActiveWalls has changed since previous frame (when this method was last run)
        if (!prevActiveWallIDs.SequenceEqual(wallIDs))
        {
            // If the new ActiveWalls contains the wall that this script is attached to
            if (gameManager.firstTriggerActivationThisTrial.Value && wallIDs.Contains(triggerID))
            {
                // Check if the GameObject present in the trigger at time of change was the local player's
                bool isTrialEnderClient = false;
                Debug.Log("IsLocalPlayer of interacting object at time of OnTriggerStay logic is: "
                            + interactingObject.GetComponent<NetworkObject>().IsLocalPlayer);
                if (interactingObject.GetComponent<NetworkObject>() != null
                && interactingObject.GetComponent<NetworkObject>().IsLocalPlayer) isTrialEnderClient = true;

                Debug.Log("isTrialEnderClient at time of if statement in OnTriggerStay is " + isTrialEnderClient);

                // // Is this only ever true on the Host? Check behaviour of isLocalPlayer
                if (isTrialEnderClient == true)
                {
                    // Update TriggerActivation with the trigger that this player entered and is stil occupying
                    // since the before this trial start
                    gameManager.UpdateTriggerActivation(triggerID, NetworkManager.Singleton.LocalClientId);
                    Debug.Log("Relevant trigger is already occupied on local client");
                    Debug.Log($"LocalClientId at time of update is {NetworkManager.Singleton.LocalClientId}");
                }
                else
                {
                    Debug.Log("As isTrialEnderClient is false, not updating trigger activation");
                }
            }
        }


        // Each frame, update the previous ActiveWall IDs variable to be queried next frame
        // Does this have high overhead? 
        prevActiveWallIDs = wallIDs;
    }

    // Subscriber method for activeWall NetworkVariable value change
    // Update local class fields with new wall values and activate the colliders for these walls
    // Note, this identical to the method in GameManager.cs for the same function
    private void OnWallChange(ActiveWalls previousValue, ActiveWalls newValue)
    {
        if (newValue.wall1 == 0) return;

        wallID1 = newValue.wall1;
        wallID2 = newValue.wall2;
        // Debug.Log($"WallTrigger.cs has updated the values of local fields to match new wall values {wallID1} and {wallID2}");
        wallIDs = new List<int>() { wallID1, wallID2 };
        // Debug.Log($"WallIDs list contains values: {String.Join(",", wallIDs)}");
    }

}