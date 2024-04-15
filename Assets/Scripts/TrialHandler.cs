using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using static GameManager;
using Debug = UnityEngine.Debug;

// This class exists client-side and accesses the wall number NetworkVariables
// that are set by the Singleton GameManager (server control only). 
// It then implements all of the logic that can be kept client-side, s.a.
// painting and washing walls, score adjust, etc.
public class TrialHandler : NetworkBehaviour
{
   
   
   GameManager gameManager;
   public int score;
   IdentityManager identityManager;
   Color defaultWallColor;
   List<int> walls;
   public NetworkVariable<int> activeWallsLowID;
   public NetworkVariable<int> activeWallsHighID;
   
   // N.B. Difference betweeen isTrialEnderClient and isTrialEnderClient
   bool isTrialEnderClient = false; // flag to check if current trial was 
                          // ended by this client
    // WallTrigger wallTrigger;


    IEnumerator PrintWalls()
    {   
        while (true)
        {
            yield return new WaitForSeconds(1.5f);

            Debug.Log($"Activewalls values are {gameManager.activeWalls.Value.wall1} and {gameManager.activeWalls.Value.wall2}");
        }
    }

    public override void OnNetworkSpawn()
    {
        gameManager = GameManager.Instance;
        identityManager = FindObjectOfType<IdentityManager>();
        // // N.B. This will return the first active loaded WallTrigger object
        // wallTrigger = FindObjectOfType<WallTrigger>();

        StartCoroutine(PrintWalls());
        
        gameManager.activeWalls.OnValueChanged += OnWallChange;
        gameManager.OnReadyStateChanged += GameManager_OnReadyStateChangedHandler;


        StartCoroutine(DelayedColorWalls());


        // For case where OnNetworkSpawn occurs after the first trial starts
        if (gameManager.activeWalls == null)
        {
            Debug.Log("activeWalls is null at this point, for some reason");
        }
        if (gameManager.activeWalls.Value.wall1 != 0 && IsClient)
        {
            Debug.Log("activeWalls has a non-zero value and am client");
            // ColorWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);
        }
        else 
        {
            Debug.Log("Either not IsClient or activeWalls has a 0 value now");
            if (IsClient)
            {
                Debug.Log("Current player IS a client, though");
                Debug.Log($"Active walls are currently {gameManager.activeWalls.Value.wall1}"
                             + $"and {gameManager.activeWalls.Value.wall2}");
            }
            else
            {
                Debug.Log("Current player is NOT a client");
            }
        }
    }

    // Coroutine here is likely unnecessary because ColorWalls will only rely on
    // the physical GameObjects being loaded, and not need OnNetworkSpawn to complete
    // BUT IT WILL NEED THE FIRST TRIAL TO HAVE STARTED SO THAT ACTIVEWALLS IS UPDATED
    IEnumerator DelayedColorWalls()
    {   
        
        // Currently nothing to stop every client from running this coroutine even if they
        // are the host. To avoid this, we can check whether the defaultWallColor is equal 
        // to Color's default initialisation value. If it is, ColorWalls has never been run
        // and therefore should now be run on this client
        Color myColor = new Color(0,0,0,0);
        Debug.Log($"Current value of default wall color is {defaultWallColor}");
        Debug.Log($"Outcome of myColor == defaultWallColor is {myColor == defaultWallColor}");
        Debug.Log($"DelayedColorWalls conditions are: {WallTrigger.setupComplete}"
                    + $", {gameManager.activeWalls.Value.wall1 != 0}"
                    + $", {defaultWallColor == myColor}");
        yield return new WaitUntil( () => WallTrigger.setupComplete == true && gameManager.activeWalls.Value.wall1 != 0
        && defaultWallColor == myColor);

        ColorWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);

    }

    public override void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.activeWalls.OnValueChanged -= OnWallChange;
            gameManager.OnReadyStateChanged -= GameManager_OnReadyStateChangedHandler;
        }
    }

    private void OnWallChange(ActiveWalls previousValue, ActiveWalls newValue) {
        
        /* // Don't wash walls if it is the first trial
        if (previousValue.wall1 != 0)
        {
            // WashWalls(previousValue.wall1, previousValue.wall2);
        }
        // ColorWalls(newValue.wall1, newValue.wall2);
        Debug.Log("New walls coloured"); */


        // We reset the value of activeWalls to 0 and 0 on each trial. Don't try and colour
        // the walls if they are 0
        if (gameManager.activeWalls.Value.wall1 != 0)
        {
            ColorWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);
        }
    }   

    public void GameManager_OnReadyStateChangedHandler(bool isReady) {
        

        // In theory this method should only be invoked when the ready condition
        // is met, so should switch this to a single if-statement when cleaning
        // up the code
        do
        {
            Debug.Log($"IsServer returns as: {IsServer}");
            if (isReady && IsServer)
            {
                // Begin trials
                isTrialEnderClient = true;
                StartTrial();
            }
        }
        while (!isReady);
    }

  

    void ColorWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID); 
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject;

        // Save the current colour of the wall before overwriting it 
        // This should be the first material, the wall face
        defaultWallColor = highWall.GetComponent<Renderer>().materials[0].color;
        Debug.Log($"Default wall colour is saved as {defaultWallColor}");
        Debug.Log($"ColorWalls() uses {highWallTriggerID} and {lowWallTriggerID} as wall values");

        // Assign colours to the walls that fit their rewards
        highWall.GetComponent<Renderer>().materials[0].color = Color.red;
        lowWall.GetComponent<Renderer>().materials[0].color = Color.blue;
    }
   
   public void WashWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        Debug.Log($"WashWalls receives high and low wall trigger IDs as: {highWallTriggerID} and {lowWallTriggerID}");
        
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID);
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject; 

        // Reset wall colours back to their previously-saved defaults
        highWall.GetComponent<Renderer>().material.color = defaultWallColor; 
        lowWall.GetComponent<Renderer>().material.color = defaultWallColor;   
    }

        public void AdjustScore(int increment = 0)
    {
        score += increment;

    }

    // Prepare for initiation of a new trial
    void ResetTrial(int highWallTriggerID, int lowWallTriggerID)
    {
        // clear the active walls list
        // activeWalls.Clear(); 

        // reset wall colours
        WashWalls(highWallTriggerID, lowWallTriggerID);
        
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
    void StartTrial()
    {    
        if (isTrialEnderClient)
        {
            Debug.Log("isTrialEnderClient is true, and StartTrial has been triggered");
            List<int> newWalls = gameManager.SelectNewWalls();
            Debug.Log($"The list of ints that is received from GameManager in StartTrail() is {newWalls[0]} and {newWalls[1]}");
            gameManager.UpdateNetworkVariables(newWalls);
            isTrialEnderClient = false;
        }
        // Add colour to the parent walls of each trigger
        // NB Walls are coloured immediately after the NetworkVariable for the new trial is updated
        // THIS SHOULD BE TRIGGERING WHENEVER START TRIAL IS RUN, BUT STARTTRIAL IS ONLY RUN BY ONE 
        // CLIENT
        // ColorWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);

    }

    public void EndTrial(int increment, int highWallTriggerID, int lowWallTriggerID, int triggerID,
                         string rewardType, bool isTrialEnderClient)
    {

        // Score.cs accesses the score here to display to the Canvas
        AdjustScore(increment);

        // Record whether it was this client that ended the current trial
        this.isTrialEnderClient = isTrialEnderClient;

        // END TRIAL IS ONLY CALLED BY THE TRIAL-ENDING CLIENT, SO WASH WALLS ELSEWHERE
        /* // reset position and walls
        // Wash walls should be moved out of here and instead be triggered for all clients by a trial ending
        Debug.Log("WashWalls() is being triggered in TrialHandler.EndTrial by WallTrigger.cs");
        Debug.Log($"WashWalls() uses {highWallTriggerID} and {lowWallTriggerID} for the walls");
        WashWalls(highWallTriggerID, lowWallTriggerID) */;

        // Begin StartTrial again with a random ITI
        // Pause code block execution (while allowing other scripts to continue) by running "Invoke"
        // with a random delay duration between the first and second argument
        float ITIvalue = Random.Range(2f,5f);
        // NB no trial logic for the next trial is run until an ITI has ocurred
        Invoke("StartTrial", ITIvalue);
        Debug.Log($"ITI duration for this trial: {ITIvalue}");

    }




}
