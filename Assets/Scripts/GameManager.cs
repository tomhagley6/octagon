using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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

    public int score;
    List<int> walls;
    public IdentityManager identityManager;
    public List<GameObject> triggers; // Keep a handle on all triggers

    Color defaultWallColor;
    // Setup an event to enable checking that GameManager has completed startup code
    public event Action<bool> OnReadyStateChanged; 
    public bool isReady = false;

    // winning player should update the server following trigger entry
    public NetworkVariable<int> triggerActivation = new NetworkVariable<int>(0);
    
    public struct TriggerActivation : INetworkSerializable {
        public int triggerID;
        public ulong OwnerClientId;
    }

    /* trialNum int to act as a trigger for events to run on each trial start
    Instead of relying on activeWalls changing value for all of my logic, define logic based on epoch boundaries
    Create events for e.g. trial start, slice onset (which could be activeWalls)
    This will be initially useful for implementing my variable trial start to slice onset time
    public NetworkVariable<int> trialNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); */

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


        // Get identity manager and order the (populated) dictionary
        identityManager = FindObjectOfType<IdentityManager>(); 
        // Debug.Log($"identityManager exists and its reference is {identityManager}");
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

    }

    void Start()
    {
        // Populate a list of all trigger GameObjects at Start time
        foreach (GameObject trigger in GameObject.FindGameObjectsWithTag("WallTrigger"))
        {
            triggers.Add(trigger);
        }
    }

    public void TriggerIDHandler_DeactivateWalls(int prevVal, int newVal)
    {
        DeactivateWall(activeWalls.Value.wall1);
        DeactivateWall(activeWalls.Value.wall2);
    }

    public void TriggerIDHandler_TriggerEntry(int prevVal, int newVal)
    {

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
    

    public void UpdateNetworkVariables(List<int> wallList)
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

}
