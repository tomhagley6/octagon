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


// Central logic for Octagon Prototype
// Includes functions for starting and ending trials
public class GameManager : SingletonNetwork<GameManager>
{


    // public GameObject player;
    public DiskLogger diskLogger;
    public int score;
    // public List<int> activeWalls;
    // public bool movementEnabled = true; // flag to control whether character controller
    //                                     // should take input


    List<int> walls;
    public IdentityManager identityManager;
    Color defaultWallColor;

    // Setup an event to enable checking that GameManager has completed startup code
    public event Action<bool> OnReadyStateChanged; 
    public bool isReady = false;

    public NetworkVariable<int> activeWallsHighID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> activeWallsLowID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
        // Subscribe to the OnValueChanged delegate with a lambda expression that fulfills the
        // delegate by giving the correct signature (previous and new value), and in the body
        // we can put a Debug statement to ensure we only log the value when it changes
        activeWallsHighID.OnValueChanged += (int previousValue, int newValue) => {
            Debug.Log($"New High wall ID: {newValue}");

        }; 

        activeWallsLowID.OnValueChanged += (int previousValue, int newValue) => {
            Debug.Log($"New Low wall ID: {newValue}");
        };


        // Get identity manager and order the (populated) dictionary
        identityManager = FindObjectOfType<IdentityManager>(); 
        Debug.Log($"identityManager exists and its reference is {identityManager}");
        identityManager.OrderDictionary();  
        Debug.Log("identityManager.OrderDictionary ran without errors");
        // List<int> chosenWalls = SelectNewWalls();  // Begin trial logic sequence
        // UpdateNetworkVariables(chosenWalls);

        Debug.Log("3 seconds have started");
        // Pause script execution for 1 second to allow other scene NetworkObjects to spawn
 
        StartCoroutine(WaitForSpawn());
        Debug.Log("Waiting for coroutine to finish");


        // Here Invoking all subscribed methods of OnReadyStateChanged now that isReady is true
        // Invoke called as a method on an event will trigger all methods subscribed to the event
        // and passes them isReady as an input
        isReady = true;
        OnReadyStateChanged?.Invoke(isReady);
    }


    IEnumerator WaitForSpawn()
    {

        yield return new WaitForSeconds(3f);

        Debug.Log("3 seconds have passed");        

    }

    void Start()
    {
        // // Get identity manager and order the (populated) dictionary
        // identityManager = FindObjectOfType<IdentityManager>(); 
        // identityManager.OrderDictionary();  
        // StartTrial();  // Begin trial logic sequence

        // // This is now done in a Networked script attached to the player prefab
/*         // Start logging data
        diskLogger = FindObjectOfType<DiskLogger>();
        diskLogger.StartLogger();
        StartCoroutine("LogPos");
 */
    }

/*     IEnumerator LogPos()
    {
        while (true)
        {
            diskLogger.Log(String.Format(Globals.posFormat, Globals.posTag,
                                                            Globals.player, 
                                                            player.transform.position.x,
                                                            player.transform.position.z));
            
            yield return new WaitForSeconds(0.2f);

        }
    } */

    
/*     void ColourWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID); 
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject;

        // Save the current colour of the wall before overwriting it 
        defaultWallColor = highWall.GetComponent<Renderer>().material.color;

        // Assign colours to the walls that fit their rewards
        highWall.GetComponent<Renderer>().material.color = Color.red;
        lowWall.GetComponent<Renderer>().material.color = Color.blue;
    } */

    /* void WashWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID);
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject; 

        // Reset wall colours back to their previously-saved defaults
        highWall.GetComponent<Renderer>().material.color = defaultWallColor; 
        lowWall.GetComponent<Renderer>().material.color = defaultWallColor;   
    } */

    // Prevent player movement
    // void RemoveAgency()
    // {
    //     // Debug.Log("Trigger entered");
    //     movementEnabled = false;
    
    // }

    public void AdjustScore(int increment = 0)
    {
        score += increment;

    }

    // Prepare for initiation of a new trial
    void ResetTrial(int highWallTriggerID, int lowWallTriggerID)
    {
        // clear the active walls list
        // activeWalls.Clear(); 

       /*  // reset wall colours
        WashWalls(highWallTriggerID, lowWallTriggerID); */
        
        /* // Halt player movement very briefly while the trial resets (contributes to visual feedback
        // of the trial ending)
        // Asynchronous method which will run without interrupting the main thread of the program
        // This is important, to allow waiting for 0.3 seconds without pausing other program functions
        IEnumerator TrialResetImmobility()
        {

            yield return new WaitForSeconds(0.3f);

            // Re-enable player movement after it was disabled in EndTrial()
            movementEnabled = true;
        }

        /// Run the above-defined coroutine
        StartCoroutine(TrialResetImmobility()); */


    }
    
    // Define and run requirements for a new trial
    // Triggered by GameManager on Start(), and again by ResetTrial()
    void StartTrial()
    {    

        Debug.Log("NEW TRIAL");

        /// Generate wall trigger IDs for a new trial
        /// currently hard coded for testing
        Debug.Log($"Identity manager exists and its reference is: {identityManager}");
        walls = identityManager.ListCustomIDs();

        // activeWalls.Clear();

        // Choose a random wall to reference the trial to 
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

        // assign to the network variable
        activeWallsHighID.Value = highWallTriggerID;
        activeWallsLowID.Value = lowWallTriggerID;


        Debug.Log("Number of walls: " + walls.Count);
        for (int i = 0; i < walls.Count; i++)
        {
            // Debug.Log("Wall number " + i + " has the ID: " + walls[i]);
        }


        Debug.Log("high wall is: " + highWallTriggerID);

        // Order in this list decides which is High and Low
        // activeWalls.Add(highWallTriggerID);
        // activeWalls.Add(lowWallTriggerID);
        // activeWalls.Value = new ActiveWalls {
        //     wall1 = highWallTriggerID,
        //     wall2 = lowWallTriggerID
        // };
        

  /*       // Add colour to the parent walls of each trigger
        ColourWalls(highWallTriggerID, lowWallTriggerID); */

    }

    public void EndTrial(int increment, int highWallTriggerID, int lowWallTriggerID, int triggerID,
                         string rewardType)
    {
        // // Halt movement
        // RemoveAgency();

       /*  // log wall trigger event
        diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
                                                                Globals.trialNum,
                                                                Globals.trialType,
                                                                triggerID,
                                                                rewardType,
                                                                increment)); */

        

        // Adjust score
        // Score.cs accesses the score here to display to the Canvas
        AdjustScore(increment);

        // reset position and walls
        ResetTrial(highWallTriggerID, lowWallTriggerID);

        // Begin StartTrial again with a random ITI
        // Pause code block execution (while allowing other scripts to continue) by running "Invoke"
        // with a random delay duration between the first and second argument
        float ITIvalue = Random.Range(2f,5f);
        Invoke("StartTrial", ITIvalue);
        Debug.Log($"ITI duration for this trial: {ITIvalue}");

    }


    public List<int> SelectNewWalls() {
        Debug.Log("NEW TRIAL");

        /// Generate wall trigger IDs for a new trial
        /// currently hard coded for testing
        Debug.Log($"identityManager exists and its reference is {identityManager}");
        walls = identityManager.ListCustomIDs();

        // activeWalls.Clear();

        // Choose a random wall to reference the trial to 
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

        // assign to the network variable
        activeWallsHighID.Value = highWallTriggerID;
        activeWallsLowID.Value = lowWallTriggerID;


        Debug.Log("Number of walls: " + walls.Count);
        for (int i = 0; i < walls.Count; i++)
        {
            // Debug.Log("Wall number " + i + " has the ID: " + walls[i]);
        }

       
        // Order in this list decides which is High and Low
        // activeWalls.Add(highWallTriggerID);
        // activeWalls.Add(lowWallTriggerID);
        // activeWalls.Value = new ActiveWalls {
        //     wall1 = highWallTriggerID,
        //     wall2 = lowWallTriggerID
        // };
        

        return new List<int>(new int[] {highWallTriggerID, lowWallTriggerID});

    }

    public void UpdateNetworkVariables(List<int> wallList)
    {
        activeWalls.Value = new ActiveWalls {
            wall1 = wallList[0],
            wall2 = wallList[1]
        };

        Debug.Log($"activeWalls value is set with the values {wallList[0]} and {wallList[1]}");
    }

}
