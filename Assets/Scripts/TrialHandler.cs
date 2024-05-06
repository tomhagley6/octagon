using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.CSharp;
using Unity.Netcode;
using UnityEngine;
using static GameManager;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

/* This class exists client-side and accesses the wall number NetworkVariables
that are set by the Singleton GameManager (server control only). 
It then implements all of the logic that can be kept client-side, s.a.
painting and washing walls, score adjust, etc. */
public class TrialHandler : NetworkBehaviour
{
   
   GameManager gameManager;
   public int score;
   IdentityManager identityManager;
   Color defaultWallColour;
   bool isTrialEnderClient = false; // flag to check if current trial was 
                                    // ended by this clientId
   public event Action sliceOnset;  



    // Print current active walls for debugging purposes
    IEnumerator PrintWalls()
    {   
        while (true)
        {
            yield return new WaitForSeconds(2f);

            // Debug.Log($"Activewalls values are {gameManager.activeWalls.Value.wall1} and {gameManager.activeWalls.Value.wall2}");
        }
    }


    public override void OnNetworkSpawn()
    {
        
        // variables
        gameManager = FindObjectOfType<GameManager>();
        identityManager = FindObjectOfType<IdentityManager>();

        // print current active walls
        StartCoroutine(PrintWalls());
        
        // subscriptions
        gameManager.activeWalls.OnValueChanged += ColourWallsOnChange;
        gameManager.OnReadyStateChanged += GameManager_OnReadyStateChangedHandler;


        StartCoroutine(DelayedColourWalls());


        // For case where OnNetworkSpawn occurs after the first trial starts
        if (gameManager.activeWalls == null)
        {
            Debug.Log("activeWalls is null at this point, for some reason");
        }
        if (gameManager.activeWalls.Value.wall1 != 0 && IsClient)
        {
            Debug.Log("activeWalls has a non-zero value and am client");
            // ColourWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);
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

   

    // Due to walls and wall colours not being networked, any late-joining clients will 
    // need to run ColourWalls as they join
    IEnumerator DelayedColourWalls()
    {   
        
        Color myColour = new Color(0,0,0,0);
        Debug.Log($"Current value of default wall color is {defaultWallColour}");
        Debug.Log($"Outcome of myColour == defaultWallColour is {myColour == defaultWallColour}");
        Debug.Log($"DelayedColourWalls conditions are: {WallTrigger.setupComplete}"
                    + $", {gameManager.activeWalls.Value.wall1 != 0}"
                    + $", {defaultWallColour == myColour}");
        
        // coroutine only needs to run while defaultWallColour is not initialised
        if (defaultWallColour != myColour) yield break;

        // Check that wallTriggers have set up, the first trial has started, 
        // and that walls are not already coloured (which implies host)
        yield return new WaitUntil( () => WallTrigger.setupComplete == true 
                                    && gameManager.activeWalls.Value.wall1 != 0
                                    && defaultWallColour == myColour);

        ColourWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);
    }


    public override void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.activeWalls.OnValueChanged -= ColourWallsOnChange;
            gameManager.OnReadyStateChanged -= GameManager_OnReadyStateChangedHandler;
        }
    }


    // Update the colour of walls following value change
    private void ColourWallsOnChange(ActiveWalls previousValue, ActiveWalls newValue) {
        
        // We reset the value of activeWalls to 0 and 0 on each trial. Don't try and colour
        // the walls if they are 0
        if (gameManager.activeWalls.Value.wall1 != 0)
        {
            ColourWalls(gameManager.activeWalls.Value.wall1, gameManager.activeWalls.Value.wall2);
        }
    }   


    // When GameManager is ready, have the server begin the first trial
    public void GameManager_OnReadyStateChangedHandler(bool isReady) {
                
        Debug.Log($"IsServer returns as: {IsServer}");
        if (isReady && IsServer)
        {
            // // Begin trials
            // isTrialEnderClient = true;
            // StartTrial();
            StartCoroutine(StartFirstTrial());
        }
    }

    // Introduce a delay before starting the first trial to allow setup 
    IEnumerator StartFirstTrial()
    {
        yield return new WaitForSeconds(0.75f);


            // Begin trials
            isTrialEnderClient = true;
            StartTrial();
    }

    // Set wall colour for the current trial
    void ColourWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID); 
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject;

        // Save the current colour of the wall before overwriting it 
        // This should be the first material, the wall face
        defaultWallColour = highWall.GetComponent<Renderer>().materials[0].color;
        Debug.Log($"Default wall colour is saved as {defaultWallColour}");
        Debug.Log($"ColourWalls() uses {highWallTriggerID} and {lowWallTriggerID} as wall values");

        // Assign colours to the walls that fit their rewards
        highWall.GetComponent<Renderer>().materials[0].color = Color.red;
        lowWall.GetComponent<Renderer>().materials[0].color = Color.blue;

        // Invoke any callback functions associated with slice onset
        // NB: Slice onset here is defined as when ColourWalls runs on the server
        sliceOnset?.Invoke();
    }
   

   // Reset wall colour to default after a trial
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
        highWall.GetComponent<Renderer>().materials[0].color = defaultWallColour; 
        lowWall.GetComponent<Renderer>().materials[0].color = defaultWallColour;   
    }
    
    
    // Change the score variable, which is accessed by Score.cs to update the UI 
        public void AdjustScore(int increment = 0)
    {
        score += increment;
    }

    
    // Run only by the client which ended the trial, pick new walls for the upcoming
    // trial and update their NetworkVariable
    void StartTrial()
    {    
        StartCoroutine(StartTrialCoroutine());
    }

    public IEnumerator StartTrialCoroutine()
    {
        if (!isTrialEnderClient) {yield return null;}
        
        Debug.Log("isTrialEnderClient is true, and StartTrial has been triggered");

        /* reset triggerActivation values to 0, in case the values between two updates
        are identical, and therefore do not trigger OnValueChanged
        Currently placed in StartTrial, because on a networked client, EndTrial seems
        to run too quickly, and the local client never sees the first update of
        TriggerActivation, only the reset to 0. 
        This is presumably a race condition, and I would need to lock the NetworkVariable
        until both clients had finished with WallTrialInteraction
        For now, this is an easier fix */
        gameManager.UpdateTriggerActivation(0,0);

        List<int> newWalls = gameManager.SelectNewWalls();
        Debug.Log($"The list of ints that is received from GameManager in StartTrail() is {newWalls[0]} and {newWalls[1]}");

        // Change trialActive to true
        gameManager.ToggleTrialActiveServerRPC();

        // Lights up
        GameObject.Find("DirectionalLight").GetComponent<Light>().intensity = 0.82f;

        // Variable delay period before slice onset
        var sliceOnsetDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(sliceOnsetDelay);

        // Activate the chosen walls for this trial (callstack contains ColourWalls(), which is the slice onset trigger)
        // Activate this only after the variable delay
        gameManager.UpdateActiveWalls(newWalls);
        isTrialEnderClient = false;
    }


    /* Run trial wrap-up logic if this client ended the trial
    This can be moved to GameManager when refactoring game logic
    Also, alongside StartTrial, this method be run on the server
    when writing server-authoritative version */
    public void EndTrial(int increment, bool isTrialEnderClient)
    {

        // // Score.cs accesses the score here to display to the Canvas
        // AdjustScore(increment);

        // // Record whether it was this client that ended the current trial
        // this.isTrialEnderClient = isTrialEnderClient;

        // // Begin StartTrial again with a random ITI
        // float ITIvalue = Random.Range(2f,5f);
        
        // // Reset the activeWalls values to 0, to ensure that activeWalls *always*
        // // changes on a new trial
        // List<int> wallReset = new List<int>(){0,0};
        // gameManager.UpdateActiveWalls(wallReset);
        // Debug.Log("Walls now reset to 0 for this trial");
        
        // // // reset triggerActivation values to 0, for the same reason
        // // gameManager.UpdateTriggerActivation(0,0);
        
        // // Pause code block execution (while allowing other scripts to continue) by running "Invoke"
        // // NB no trial logic for the next trial is run until an ITI has ocurred
        // Invoke("StartTrial", ITIvalue);
        // Debug.Log($"ITI duration for this trial: {ITIvalue}");

        StartCoroutine(EndTrialCoroutine(increment, isTrialEnderClient));
        
    }

    // Replacing EndTrial() contents with this coroutine, to allow a 2 second pause prior to starting
    // the ITI
    public IEnumerator EndTrialCoroutine(int increment, bool isTrialEnderClient)
    {
        
        // Update score locally and to the NetworkList
        // Score.cs accesses the score here to display to the Canvas
        AdjustScore(increment);
        gameManager.UpdateScoresServerRPC(increment);

        // Record whether it was this client that ended the current trial
        this.isTrialEnderClient = isTrialEnderClient;

        // Reset the activeWalls values to 0, to ensure that activeWalls *always*
        // changes on a new trial
        List<int> wallReset = new List<int>(){0,0};
        gameManager.UpdateActiveWalls(wallReset);
        Debug.Log("Walls now reset to 0 for this trial");

        // allow a short grace period before the trial ends 
        // Then update the trial active NetworkVaraible
        yield return new WaitForSeconds(1.5f); 
        gameManager.ToggleTrialActiveServerRPC();
        
        // Decrease global illimation
        var light = GameObject.Find("DirectionalLight").GetComponent<Light>();
        light.intensity = 0.6f;

        // Begin StartTrial again with a random ITI
        float ITIvalue = Random.Range(2f,5f);
        // Use Invoke to delay StartTrial until ITI has passed
        // NB no trial logic for the next trial is run until an ITI has ocurred
        Invoke("StartTrial", ITIvalue);
        Debug.Log($"ITI duration for this trial: {ITIvalue}");

    }


}
