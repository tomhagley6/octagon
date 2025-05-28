using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Globals;

public class TrialHandlerExtension : MonoBehaviour
{
    // scripts
    public TrialLogicExtension trialLogicExtension;
    public GameManagerExtension gameManagerExtension;


    // variables
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;
    public string thisTrialType;
    public int trialCounter = 0;
    // public int RandomNumber;

    // agents
    // public MLAgent opponentAgent;
    // public MLAgent playerAgent;
    [SerializeField] private MLAgent opponentAgent;
    [SerializeField] private MLAgent playerAgent;

    // trial loop control
    private bool isTrialLoopRunning = false;


    void Awake()

    // called first before scene starts running. 
    // set up references
    // initialise things that don't depend on other scripts being ready

    {
        if (trialLogicExtension == null) trialLogicExtension = FindObjectOfType<TrialLogicExtension>();
        if (gameManagerExtension == null) gameManagerExtension = FindObjectOfType<GameManagerExtension>();

    }

    void Start()

    // called after awake (only if script is enabled)
    // initialise things that rely on other objects already existing

    {
        opponentAgent = GameObject.FindWithTag("OpponentAgent").GetComponent<MLAgent>();
        playerAgent = GameObject.FindWithTag("PlayerAgent").GetComponent<MLAgent>();

        // StartTrial();

    }

    public void StartTrial()
    {
        // set trial state to in progress
        if (!playerAgent.isTrialInProgress)
        {
            playerAgent.isTrialInProgress = true;
            // select two new wall IDs

            //trialCounter = 0;
            // RandomNumber = Random.Range(20, 30);
            Debug.Log($"[Agent] Trial counter is set to max number of trials: {playerAgent.RandomNumber}");

            trialLogicExtension.AssignNewWalls();
            wallID1 = trialLogicExtension.activeWalls.wall1;
            wallID2 = trialLogicExtension.activeWalls.wall2;
            wallIDs = new List<int> {wallID1, wallID2};
            Debug.Log($"Starting new trial with wall IDs {wallIDs}");

            // select new trial type
            thisTrialType = gameManagerExtension.SelectTrial();

            // colour walls based on wall IDs and trial type
            trialLogicExtension.ColourWalls(wallID1, wallID2, thisTrialType);
            Debug.Log($"walls coloured for trial type {thisTrialType} with wall IDs {wallID1} and {wallID2}");

        }
        else
        {
            Debug.LogWarning("StartTrial() called while a trial is already in progress. Ignoring.");
        }


    }

    public IEnumerator TrialLoop()
    {
        if (isTrialLoopRunning)
        {
            Debug.Log("[TrialLoop] Trial loop already running, ignoring.");
            yield break;
        }
        isTrialLoopRunning = true;
        
        // wait for a short time before initiating the trial loop
        yield return new WaitForSeconds(0.1f);

        // add to trial counter
        //playerAgent.trialCounter++;
        //Debug.Log($"Trial {playerAgent.trialCounter} out of {playerAgent.RandomNumber} started ");
        trialCounter++;
        Debug.Log($"Trial {trialCounter} out of {playerAgent.RandomNumber} started ");

        // stop trial loop if max number of trials reached
        //if (playerAgent.trialCounter >= playerAgent.RandomNumber)
        if (trialCounter >= playerAgent.RandomNumber)
        {
            Debug.Log("[TrialLoop] Max number of trials reached. Ending episode.");
            
            opponentAgent.CustomEndEpisode();
            playerAgent.CustomEndEpisode();

            isTrialLoopRunning = false;

            yield break;
        }

        yield return new WaitForSeconds(Random.Range(General.ITIMin, General.ITIMax));

        StartTrial();
        isTrialLoopRunning = false;

    }
}