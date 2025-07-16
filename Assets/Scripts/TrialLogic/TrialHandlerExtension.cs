using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Globals;
using System.Linq;

public class TrialHandlerExtension : MonoBehaviour
{
    private Transform arenaRoot;
    // scripts
    [SerializeField] public TrialLogicExtension trialLogicExtension;
    [SerializeField] public GameManagerExtension gameManagerExtension;
    [SerializeField] public IdentityManager identityManager;
    //[SerializeField] public WallManager wallManager;

    // variables
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;
    public string thisTrialType;
    public GameObject wall1trigger;
    public GameObject wall2trigger;
    [SerializeField] public MLAgent opponentAgent;
    [SerializeField] public MLAgent playerAgent;
    public bool newTrialSetUp = false;
    public Vector3 startPosition;
    public int trialCounter = 0;
    public bool isTrialLoopRunning = false;
    public float endReward;
    //public bool trialIsSetUp = false;




    void Awake()

    // called first before scene starts running. 
    // set up references
    // initialise things that don't depend on other scripts being ready

    // assigned in inspector for parallel training
    {
        arenaRoot = transform.parent;

        if (trialLogicExtension == null) trialLogicExtension = arenaRoot.GetComponentInChildren<TrialLogicExtension>();
        if (gameManagerExtension == null) gameManagerExtension = arenaRoot.GetComponentInChildren<GameManagerExtension>();
        if (identityManager == null) identityManager = arenaRoot.GetComponentInChildren<IdentityManager >();
        //if (wallManager == null) wallManager = FindObjectOfType<WallManager>();
    }

    void Start()

    // called after awake (only if script is enabled)
    // initialise things that rely on other objects already existing

    // assigned in inspector for parallel training
    {
        if (opponentAgent == null)
        {
            //opponentAgent = GameObject.FindWithTag("OpponentAgent").GetComponent<MLAgent>();
            opponentAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("OpponentAgent"))
                ?.GetComponent<MLAgent>();
        }
        if (playerAgent == null)
        {
            //playerAgent = GameObject.FindWithTag("PlayerAgent").GetComponent<MLAgent>();
            playerAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("PlayerAgent"))
                ?.GetComponent<MLAgent>();
        }

    }

    public void StartTrial()
    {
        // Note. agents don't have memory of previous episode
        // to retain memory for previous trials they need to be bundled together

        // set to true at the start of the trial 
        // set back to false upon trigger interaction [WallTriggerExtension]
        SetUpArena();
    }

    public void PlayerSpawn()
    {
        Vector3 arenaCentre = arenaRoot.transform.position;

        float x = arenaCentre.x;
        float z = arenaCentre.z;
        Vector3 currentPosition = transform.position;
        float y = currentPosition.y;

        Vector3 startPosition = new Vector3(Random.Range(x - 2f, x + 2f), y, Random.Range(z - 1f, z + 1f));

        playerAgent.controller.enabled = false;
        opponentAgent.controller.enabled = false;

        playerAgent.transform.position = startPosition;

        Vector3 shift = new Vector3(2f, 0f, 0f);

        float random = Random.Range(0f, 1f);
        if (random < 0.05f)
        {
            opponentAgent.transform.position = startPosition - shift;
            playerAgent.transform.forward = new Vector3(0f, 180f, 0f);
            opponentAgent.transform.forward = new Vector3(0f, 180f, 0f);

        }
        else
        {
            opponentAgent.transform.position = startPosition + shift;
            playerAgent.transform.forward = new Vector3(0f, 0f, 0f);
            opponentAgent.transform.forward = new Vector3(0f, 0f, 0f);
        }

        playerAgent.controller.enabled = true;
        opponentAgent.controller.enabled = true;

        //Debug.Log($"[StartTrial] Set player to {startPosition}, opponent to {startPosition - Vector3.right}");

    }

    public void SetUpArena()
    {

        //if (!WallManager.HasAssigned)
        //{
        //    trialLogicExtension.AssignNewWalls();
        //    WallManager.GlobalWallID1 = trialLogicExtension.activeWalls.wall1;
        //    WallManager.GlobalWallID2 = trialLogicExtension.activeWalls.wall2;
        //    WallManager.GlobalTrialType = gameManagerExtension.SelectTrial();
        //    WallManager.HasAssigned = true;
        //}

        // select two new wall IDs
        trialLogicExtension.AssignNewWalls();
        wallID1 = trialLogicExtension.activeWalls.wall1;
        wallID2 = trialLogicExtension.activeWalls.wall2;
        //wallID1 = WallManager.GlobalWallID1;
        //wallID2 = WallManager.GlobalWallID2;

        //Debug.Log($"Setting wall IDs to {wallID1}, {wallID2}");

        opponentAgent.wallID1 = wallID1;
        opponentAgent.wallID2 = wallID2;

        playerAgent.wallID1 = wallID1;
        playerAgent.wallID2 = wallID2;

        wall1trigger = identityManager.GetObjectByIdentifier(wallID1);
        wall2trigger = identityManager.GetObjectByIdentifier(wallID2);

        opponentAgent.wall1trigger = wall1trigger;
        opponentAgent.wall2trigger = wall2trigger;

        playerAgent.wall1trigger = wall1trigger;
        playerAgent.wall2trigger = wall2trigger;

        // set initial distances so agents can compute deltas correctly
        playerAgent.distToWall1 = Vector3.Distance(playerAgent.transform.position, wall1trigger.transform.position);
        playerAgent.distToWall2 = Vector3.Distance(playerAgent.transform.position, wall2trigger.transform.position);
        opponentAgent.distToWall1 = Vector3.Distance(opponentAgent.transform.position, wall1trigger.transform.position);
        opponentAgent.distToWall2 = Vector3.Distance(opponentAgent.transform.position, wall2trigger.transform.position);

        wallIDs = new List<int> { wallID1, wallID2 };
        //Debug.Log($"Starting new trial with wall IDs {wallIDs}");

        // select new trial type
        thisTrialType = gameManagerExtension.SelectTrial();
        //thisTrialType = WallManager.GlobalTrialType;
        playerAgent.thisTrialType = thisTrialType;
        opponentAgent.thisTrialType = thisTrialType;

        // colour walls based on wall IDs and trial type
        trialLogicExtension.ColourWalls(wallID1, wallID2, thisTrialType);
        //Debug.Log($"walls coloured for trial type {thisTrialType} with wall IDs {wallID1} and {wallID2}");

        playerAgent.wallSetupComplete = true;
        opponentAgent.wallSetupComplete = true;

    }

    // this method bundles trials together
    public IEnumerator TrialLoop()
    {
        // if isTrialLoopRunning is true, do not run the trial loop
        if (isTrialLoopRunning)
        {
            //Debug.Log("Trial Loop is already running");
            yield break;
        }

        isTrialLoopRunning = true;

        trialCounter++;
        //Debug.Log($"Trial {trialCounter} out of {playerAgent.RandomNumber} started");


        // if the current trial number is bigger than the randomly assigned number for this episode, end this episode
        if (trialCounter > playerAgent.RandomNumber)
        {
            endReward = 0.5f;
            opponentAgent.AddReward(endReward);
            playerAgent.AddReward(endReward);

            float opponentReward = opponentAgent.GetCumulativeReward();
            float playerReward = playerAgent.GetCumulativeReward();

            opponentAgent.CustomEndEpisode();
            playerAgent.CustomEndEpisode();

            //Debug.Log($"[TrialLoop] Max number of trials reached. Ending episode. Agents are rewarded {playerReward}, {opponentReward}");

            isTrialLoopRunning = false;

            yield break;
        }

        //if isTrialLoopRunning is false and the current trial number is <= the random max number of trials for this episode
        //run ITI, start new trial, and increase the counter
        //yield return new WaitForSeconds(Random.Range(General.ITIMin, General.ITIMax));
        yield return new WaitForSeconds(0.1f);

        //if (trialCounter > 1)
        if (trialCounter > 2) //setting this to 2 for the time being so it never runs when RandomNumber is 1
        {
            playerAgent.AddReward(0.1f);
            opponentAgent.AddReward(0.1f);
        }

        //ResetTrial();
        StartTrial();

        //trialIsSetUp = true;

        //isTrialLoopRunning = false;
    }
    
    public void ResetTrial()
    {
        //isTrialLoopRunning = false;

        if (wallID1 != 0 && wallID2 != 0)
        {
            trialLogicExtension.WashWalls(wallID1, wallID2);
        }

    }

}