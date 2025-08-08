using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Globals;
using KaimiraGames;
using Unity.MLAgents;
using UnityEngine;
using UnityEditor;

public class OctagonArea : MonoBehaviour
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
    // assign agents in inspector
    [SerializeField] public OctagonAgent opponentAgent;
    [SerializeField] public OctagonAgent playerAgent;
    [SerializeField] public IdentityManager identityManager;


    void Awake()
    {
        arenaRoot = transform.parent;

        if (identityManager == null) identityManager = arenaRoot.GetComponentInChildren<IdentityManager>();
    }

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
        Debug.Log("[StartTrial] calling arena set-up method");
        SetUpArena();
    }

    public struct ActiveWalls
    {
        public int wall1;
        public int wall2;
    }

    public void SetUpArena()
    {
        Debug.Log("[SetUpArena] arena set-up process initiated.");

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

    public void AssignNewWalls()
    {
        List<int> newWalls = SelectNewWalls();
        activeWalls.wall1 = newWalls[0];
        activeWalls.wall2 = newWalls[1];

        Debug.Log("[AssignNewWalls] New walls for this trial are assigned.");
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

        // assign interaction colour to the centre of the wall

        Transform wall1Centre = wall1.transform.Find("InteractionZone");
        Transform wall2Centre = wall2.transform.Find("InteractionZone");

        var zoneColor = General.wallInteractionZoneColour;
        wall1Centre.GetComponent<Renderer>().materials[0].color = zoneColor;
        wall2Centre.GetComponent<Renderer>().materials[0].color = zoneColor;

        Debug.Log("[ColourWalls] New trial walls are coloured.");
        playerAgent.LogSliceOnsetEvent(wallID1, wallID2, thisTrialType);

    }
    public void TrialLoop()
    {
        isTrialLooping = true;

        StartCoroutine(ITI());
    }
    public IEnumerator ITI()
    {

        Debug.Log($"ITI range: {General.ITIMin} to {General.ITIMax}");
        iti = Random.Range(General.ITIMin, General.ITIMax);

        //Debug.Log($"Waiting for ITI: {iti}");
        yield return new WaitForSeconds(iti);

        Debug.Log("Trial loop started.");

        Debug.Log("About to enable triggers.");
        // ensures that triggers are enabled only after ITI has passed
        EnableTriggers();

        Debug.Log("ITI ended. Triggers re-enabled. Trial now starting.");

        StartTrial();

        if (!soloMode)
        {
            opponentAgent.previousDistanceHigh = Vector3.Distance(opponentAgent.transform.position, wall1Trigger.transform.position);
            opponentAgent.previousDistanceLow = Vector3.Distance(opponentAgent.transform.position, wall2Trigger.transform.position);
        }

        playerAgent.previousDistanceHigh = Vector3.Distance(playerAgent.transform.position, wall1Trigger.transform.position);
        playerAgent.previousDistanceLow = Vector3.Distance(playerAgent.transform.position, wall2Trigger.transform.position);

        Debug.Log($"[OctagonAgent] Agent {playerAgent.tag} starts with distance to high {playerAgent.previousDistanceHigh} and distance to low {playerAgent.previousDistanceLow}.");
        if (!soloMode)
        { Debug.Log($"[OctagonAgent] Agent {opponentAgent.tag} starts with distance to high {opponentAgent.previousDistanceHigh} and distance to low {opponentAgent.previousDistanceLow}."); }

    }
    public void EnableTriggers()
    {
        Debug.Log("Enabling triggers");
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
        Debug.Log("Disabling triggers.");
        foreach (var trigger in allWallTriggers)
        {
            if (trigger.TryGetComponent<BoxCollider>(out var collider))
            {
                collider.enabled = false;
            }
        }
    }

    public void ResetTrial()
    {
        if (wallID1 != 0 && wallID2 != 0)
        {
            WashWalls(wallID1, wallID2);

            Debug.Log("Walls have now been washed");

            GameObject HWT = identityManager.GetObjectByIdentifier(wallID1);
            GameObject LWT = identityManager.GetObjectByIdentifier(wallID2);

            // get the (parent) octagon wall of each trigger
            GameObject HW = HWT.transform.parent.gameObject;
            GameObject LW = LWT.transform.parent.gameObject;

            //Debug.Log($"Tags for high wall and low walls after washing are {HW.tag} and {LW.tag}");

        }


        else if (wallID1 == 0 && wallID2 == 0)
        {
            Debug.Log("First episode. Walls yet to be assigned, nothing to reset.");
        }
    }

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

    public (int score, string rewardType) TrialInteraction(int triggerID, int highWallTriggerID, int lowWallTriggerID, string thisTrialType)
    {
        int score = 0;
        string rewardType = "";

        switch (thisTrialType)
        {
            case var value when value == General.highLow:

                score = triggerID == highWallTriggerID ? General.highScore : General.lowScore;
                rewardType = triggerID == highWallTriggerID ? General.highScoreRewardType : General.lowScoreRewardType;

                break;

            // case var value when value == General.riskyChoice:

            // (score, rewardType) = AssignRiskyReward(triggerID, highWallTriggerID, lowWallTriggerID);

            // break;

            case var value when value == General.forcedHigh:

                score = General.highScore;
                rewardType = General.highScoreRewardType;

                break;

            case var value when value == General.forcedLow:

                score = General.lowScore;
                rewardType = General.lowScoreRewardType;

                break;

                // case var value when value == General.forcedRisky:

                // score = isRiskyWin ? General.highScore : 0;
                // rewardType = isRiskyWin ? General.highScoreRewardType : General.zeroRewardType; 

                // break;
        }

        return (score, rewardType);
    }

}