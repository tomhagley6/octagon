

public class OctagonArea : MonoBehaviour
{
    // octagon arena
    private Transform arenaRoot;
    // training mode
    public bool soloMode;
    // active walls
    public ActiveWalls activeWalls;
    // assign agents in inspector
    [SerializeField] public MLAgent opponentAgent;
    [SerializeField] public MLAgent playerAgent;
    [SerializeField] public IdentityManager identityManager;

    void Awake()
    {
        arenaRoot = transform.parent;

        // find scripts
    }

    void Start()
    {
        if (!soloMode && opponentAgent == null)
        {
            opponentAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("OpponentAgent"))
                ?.GetComponent<MLAgent>();
        }
        if (playerAgent == null)
        {
            playerAgent = arenaRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag("PlayerAgent"))
                ?.GetComponent<MLAgent>();
        }
    }

    public void StartTrial()
    {
        SetUpArena();
    }

    public void SetUpArena()
    {
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

    }

    public void AssignNewWalls()
    {
        List<int> newWalls = SelectNewWalls();
        activeWalls.wall1 = newWalls[0];
        activeWalls.wall2 = newWalls[1];
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

                break;

            // case var value when value == General.riskyChoice:
                // wall1.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;
                // wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                // break;
            
            case var value when value == General.forcedHigh:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallHighColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallHighColour;

                break;

            case var value when value == General.forcedLow:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallLowColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

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

    }
}