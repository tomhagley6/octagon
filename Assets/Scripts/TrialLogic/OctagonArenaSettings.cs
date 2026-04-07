using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Globals;
using KaimiraGames;
using Unity.MLAgents;
using UnityEngine;
using UnityEditor;
// using System.Numerics;

public class OctagonArenaSettings : MonoBehaviour
{
    // octagon arena
    private Transform arenaRoot;
    // training mode
    public bool soloMode;
    // active walls
    public ActiveWalls activeWalls;
    // collect all wall triggers
    public List<GameObject> allWallTriggers;
    public int wallID1;
    public int wallID2;
    public GameObject wall1Trigger;
    public GameObject wall2Trigger;
    public string thisTrialType;
    List<int> walls;
    // initial wall colour
    private Color defaultWallColour;
    private float iti;
    public bool isTrialLooping = false;
    
    // NEW: Flag to signal when arena setup is complete and safe for agents to observe/act
    // This prevents OpponentAgent from observing stale wall states before PlayerAgent completes setup
    public bool isArenaReady = false;
    
    // assign agents in inspector
    [SerializeField] public OctagonAgent opponentAgent;
    [SerializeField] public OctagonAgent playerAgent;
    [SerializeField] public IdentityManager identityManager;

    // Agent location
    private Vector3 playerSpawnOffset = new Vector3(0, 1.5f, 0);

    // Training curriculum parameters
    private EnvironmentParameters envParams;
    private float arenaScale;


    // References for the arena and identity manager of the arena walls
    void Awake()
    {
        arenaRoot = transform.parent;

        
        // GameObject playerAgent = Instantiate(playerAgent, arenaRoot, )

        if (identityManager == null) identityManager = arenaRoot.GetComponentInChildren<IdentityManager>();

        arenaScale = arenaRoot.localScale.x; // Do not assume arena scale is 1 by default
    }

    // Get a reference to all triggers and present agents
    void Start()
    {
        allWallTriggers = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("WallTrigger"))
            .Select(t => t.gameObject)
            .ToList();

        if (!soloMode && opponentAgent == null)
        {
            opponentAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("OpponentAgent"))
                ?.GetComponent<OctagonAgent>();
        }
        if (playerAgent == null)
        {
            playerAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("PlayerAgent"))
                ?.GetComponent<OctagonAgent>();
        }
    }

    public void StartTrial()
    {
        #if UNITY_EDITOR
        Debug.Log("[StartTrial] calling arena set-up method");
        #endif
        SetUpArena();
    }

    public struct ActiveWalls
    {
        public int wall1;
        public int wall2;
    }

    // Assign and colour walls for the upcoming trials
    public void SetUpArena()
    {
        #if UNITY_EDITOR
        Debug.Log("[SetUpArena] arena set-up process initiated.");
        #endif

        AssignNewWalls();
        wallID1 = activeWalls.wall1;
        wallID2 = activeWalls.wall2;
        wall1Trigger = identityManager.GetObjectByIdentifier(wallID1);
        wall2Trigger = identityManager.GetObjectByIdentifier(wallID2);

        thisTrialType = SelectTrial();

        // feed wall trigger IDs to agent script
        if (!soloMode)
        {
            opponentAgent.wall1Trigger = wall1Trigger;
            opponentAgent.wall2Trigger = wall2Trigger;
            opponentAgent.thisTrialType = thisTrialType;
        }

        playerAgent.wall1Trigger = wall1Trigger;
        playerAgent.wall2Trigger = wall2Trigger;
        playerAgent.thisTrialType = thisTrialType;

        ColourWalls(wallID1, wallID2, thisTrialType);


        // get the parent object (octagon wall) for each trigger
        GameObject HW = wall1Trigger.transform.parent.gameObject;
        GameObject LW = wall2Trigger.transform.parent.gameObject;

        //Debug.Log($"Tags for high wall and low walls after colouring are {HW.tag} and {LW.tag}");

    }

    // Scale the arena (during the training curriculum)
    public void ApplyArenaScale()
    {
        float scale = envParams.GetWithDefault("arena_scale", 1.0f);

        arenaRoot.localScale = new Vector3(scale, 1f, scale); // Don't scale the
    }


    public void AssignNewWalls()
    {
        List<int> newWalls = SelectNewWalls();
        activeWalls.wall1 = newWalls[0];
        activeWalls.wall2 = newWalls[1];

        #if UNITY_EDITOR
        Debug.Log("[AssignNewWalls] New walls for this trial are assigned.");
        #endif
    }

    public List<int> SelectNewWalls()
    {

        // get wall trigger IDs for a new trial
        walls = identityManager.ListCustomIDs();

        // choose a random anchor wall to reference the trial to 
        int anchorWallIndex = Random.Range(0, walls.Count);

        // create weighted list of wall separation values to draw from 
        WeightedList<int> wallSeparationsWeighted = new();
        for (int i = 0; i < General.wallSeparations.Count; i++)
        {
            wallSeparationsWeighted.Add(General.wallSeparations[i], General.wallSeparationsProbabilities[i]);
        }

        // query the weighted list for this trial's wall separation
        int wallSeparation = wallSeparationsWeighted.Next();


        // choose a random second wall that is consistent with anchor wall for this trial type
        int wallIndexDiff = new List<int> { -wallSeparation, wallSeparation }[Random.Range(0, 2)];

        int dependentWallIndex = anchorWallIndex + wallIndexDiff;

        // account for circular octagon structure
        if (dependentWallIndex < 0)
        {
            dependentWallIndex += walls.Count;
        }
        else if (dependentWallIndex >= walls.Count)
        {
            dependentWallIndex -= walls.Count;
        }

        // assign high and low walls with the generated indexes
        int highWallTriggerID = walls[anchorWallIndex];
        int lowWallTriggerID = walls[dependentWallIndex];

        return new List<int>(new int[] { highWallTriggerID, lowWallTriggerID });
    }

    public string SelectTrial()
    {
        // create weighted list of trial types to draw from 
        WeightedList<string> trialTypeDist = new();
        for (int i = 0; i < General.trialTypes.Count; i++)
        {
            trialTypeDist.Add(General.trialTypes[i], General.trialTypeProbabilities[i]);
        }

        // return trial type for this trial
        return trialTypeDist.Next();
    }

    public void ColourWalls(int wallID1, int wallID2, string thisTrialType)
    {

        // access game object through the ID:GameObject dict in IdentityManager
        GameObject wall1trigger = identityManager.GetObjectByIdentifier(wallID1);
        GameObject wall2trigger = identityManager.GetObjectByIdentifier(wallID2);

        // get the parent object (octagon wall) for each trigger
        GameObject wall1 = wall1trigger.transform.parent.gameObject;
        GameObject wall2 = wall2trigger.transform.parent.gameObject;

        // save original wall colour before overwriting
        defaultWallColour = wall1.GetComponent<Renderer>().materials[0].color;

        // assign colors based on trial type
        switch (thisTrialType)
        {
            case var value when value == General.highLow:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallHighColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                // set tags to high/low wall for raycasts
                // 260209 Trigger raycasts were inactivated
                wall1.tag = "HighWall";
                wall2.tag = "LowWall";

                wall1trigger.tag = "HighWallTrigger";
                wall2trigger.tag = "LowWallTrigger";

                break;

            // case var value when value == General.riskyChoice:
            // wall1.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;
            // wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

            // break;

            case var value when value == General.forcedHigh:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallHighColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallHighColour;

                // set tags to high wall for raycasts
                wall1.tag = "HighWall";
                wall2.tag = "HighWall";

                wall1trigger.tag = "HighWallTrigger";
                wall2trigger.tag = "HighWallTrigger";

                break;

            case var value when value == General.forcedLow:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallLowColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                // set tags to low wall for raycasts
                wall1.tag = "LowWall";
                wall2.tag = "LowWall";

                wall1trigger.tag = "LowWallTrigger";
                wall2trigger.tag = "LowWallTrigger";

                break;

                // case var value when value == General.forcedRisky:
                // wall1.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;
                // wall2.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;

                // break;

        }


        // // Why is this necessary? Shouldn't interaction zone always be consistently coloured? 
        // assign interaction colour to the centre of the wall

        Transform wall1Centre = wall1.transform.Find("InteractionZone");
        Transform wall2Centre = wall2.transform.Find("InteractionZone");

        var zoneColor = General.wallInteractionZoneColour;
        wall1Centre.GetComponent<Renderer>().materials[0].color = zoneColor;
        wall2Centre.GetComponent<Renderer>().materials[0].color = zoneColor;

        #if UNITY_EDITOR
        Debug.Log("[ColourWalls] New trial walls are coloured.");
        #endif
        playerAgent.LogSliceOnsetEvent(wallID1, wallID2, thisTrialType);

    }

    // Is there a loop to trials? Seems like it should be a discrete process with the next 
    // iteration triggered 
    public void TrialLoop()
    {
        isTrialLooping = true;
        
        // NEW: Mark arena as NOT ready during setup - prevents agents from observing stale state
        isArenaReady = false;
        #if UNITY_EDITOR
        Debug.Log("[TrialLoop] Arena setup starting - isArenaReady set to false");
        #endif

        StartCoroutine(ITI());
    }

    // Inititate the ITI and lead into Start Trial logic
    public IEnumerator ITI()
    {
        #if UNITY_EDITOR
        Debug.Log($"ITI range: {General.ITIMin} to {General.ITIMax}");
        #endif
        iti = Random.Range(General.ITIMin, General.ITIMax);
        
        // Use a 2 second fixed EndTrial delay to replicate experimental setup, added 260310
        // Present in builds only from 260408 onwards
        yield return new WaitForSeconds(2f);

        //Debug.Log($"Waiting for ITI: {iti}");
        yield return new WaitForSeconds(iti);

        // Use a 0.5-1.5 second variable length TrialStart delay to replicate experimental setup, added 260310
        // Present in builds only from 260408 onwards
        float trialStartDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(trialStartDelay);

        #if UNITY_EDITOR
        Debug.Log("Trial loop started.");
        Debug.Log("About to enable triggers.");
        #endif
        
        // ensures that triggers are enabled only after ITI has passed
        EnableTriggers();

        #if UNITY_EDITOR
        Debug.Log("ITI ended. Triggers re-enabled. Trial now starting.");
        #endif

        StartTrial(); // This calls SetUpArena() -> ColourWalls() which sets wall tags

        // NEW: Signal that arena setup is complete - safe for OpponentAgent to observe
        isArenaReady = true;
        #if UNITY_EDITOR
        Debug.Log("[ITI] Arena setup complete - isArenaReady set to true");
        #endif

        if (!soloMode)
        {
            opponentAgent.previousDistanceHigh = Vector3.Distance(opponentAgent.transform.position, wall1Trigger.transform.position);
            opponentAgent.previousDistanceLow = Vector3.Distance(opponentAgent.transform.position, wall2Trigger.transform.position);
        }

        playerAgent.previousDistanceHigh = Vector3.Distance(playerAgent.transform.position, wall1Trigger.transform.position);
        playerAgent.previousDistanceLow = Vector3.Distance(playerAgent.transform.position, wall2Trigger.transform.position);

        #if UNITY_EDITOR
        Debug.Log($"[OctagonAgent] Agent {playerAgent.tag} starts with distance to high {playerAgent.previousDistanceHigh} and distance to low {playerAgent.previousDistanceLow}.");
        if (!soloMode)
        { Debug.Log($"[OctagonAgent] Agent {opponentAgent.tag} starts with distance to high {opponentAgent.previousDistanceHigh} and distance to low {opponentAgent.previousDistanceLow}."); }
        #endif

    }

    // Wall collider trigger enabling/disabling methods
    public void EnableTriggers()
    {
        #if UNITY_EDITOR
        Debug.Log("Enabling triggers");
        #endif
        foreach (var trigger in allWallTriggers)
        {
            if (trigger.TryGetComponent<BoxCollider>(out var collider))
            {
                collider.enabled = true;
            }
        }
    }
    public void DisableTriggers()
    {
        #if UNITY_EDITOR
        Debug.Log("Disabling triggers.");
        #endif
        foreach (var trigger in allWallTriggers)
        {
            if (trigger.TryGetComponent<BoxCollider>(out var collider))
            {
                collider.enabled = false;
            }
        }
    }

    // Currently only calls WashWalls on the trial active walls
    // Only if octagonArena.IsTrialLooping is True, from within OnEpisodeBegin
    // Would it make more sense to have this at the end the trial logic, after HandleTriggerEntry has begun?
    public void ResetTrial()
    {
        if (wallID1 != 0 && wallID2 != 0)
        {
            WashWalls(wallID1, wallID2);

            #if UNITY_EDITOR
            Debug.Log("Walls have now been washed");
            #endif

            GameObject HWT = identityManager.GetObjectByIdentifier(wallID1);
            GameObject LWT = identityManager.GetObjectByIdentifier(wallID2);

            // get the (parent) octagon wall of each trigger
            GameObject HW = HWT.transform.parent.gameObject;
            GameObject LW = LWT.transform.parent.gameObject;

            //Debug.Log($"Tags for high wall and low walls after washing are {HW.tag} and {LW.tag}");

        }


        else if (wallID1 == 0 && wallID2 == 0)
        {
            #if UNITY_EDITOR
            Debug.Log("First episode. Walls yet to be assigned, nothing to reset.");
            #endif
        }
    }

    // Why are we changing interaction zone colour? Could this be removed
    public void WashWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID);
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject;

        // reset wall tags
        highWall.tag = "Wall";
        lowWall.tag = "Wall";

        highWallTrigger.tag = "WallTrigger";
        lowWallTrigger.tag = "WallTrigger";

        // reset wall colours back to their previously-saved defaults
        highWall.GetComponent<Renderer>().materials[0].color = defaultWallColour;
        lowWall.GetComponent<Renderer>().materials[0].color = defaultWallColour;

        // reset interaction zone back to full transparency
        GameObject wall1Centre = highWall.transform.Find("InteractionZone").gameObject;
        GameObject wall2Centre = lowWall.transform.Find("InteractionZone").gameObject;

        Color wallCentreColor = wall1Centre.GetComponent<Renderer>().materials[0].color;
        wallCentreColor.a = 0f;
        wall1Centre.GetComponent<Renderer>().materials[0].color = wallCentreColor;
        wall2Centre.GetComponent<Renderer>().materials[0].color = wallCentreColor;

    }
    

    // Identify the outcome score dependent on activated trigger and current trial type
    public (float reward, string rewardType) TrialInteraction(int triggerID, int highWallTriggerID, int lowWallTriggerID, string thisTrialType)
    {
        float reward = 0f;
        string rewardType = "";

        switch (thisTrialType)
        {
            case var value when value == General.highLow:

                reward = triggerID == highWallTriggerID ? General.highScore : General.lowScore;
                rewardType = triggerID == highWallTriggerID ? General.highScoreRewardType : General.lowScoreRewardType;

                break;

            // case var value when value == General.riskyChoice:

            // (score, rewardType) = AssignRiskyReward(triggerID, highWallTriggerID, lowWallTriggerID);

            // break;

            case var value when value == General.forcedHigh:

                reward = General.highScore;
                rewardType = General.highScoreRewardType;

                break;

            case var value when value == General.forcedLow:

                reward = General.lowScore;
                rewardType = General.lowScoreRewardType;

                break;

                // case var value when value == General.forcedRisky:

                // score = isRiskyWin ? General.highScore : 0;
                // rewardType = isRiskyWin ? General.highScoreRewardType : General.zeroRewardType; 

                // break;
        }

        return (reward, rewardType);
    }

}