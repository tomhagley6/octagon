using UnityEngine;
using System.Collections.Generic;
using Globals;

public class TrialLogicExtension : MonoBehaviour
{
    public ActiveWalls activeWalls;
    public GameManagerExtension gameManagerExtension;
    public IdentityManager identityManager;
    private Color defaultWallColour;
    List<Collider> wallColliders;

    public Vector3 arenaCenter = new Vector3(0, 0, 0);  // set this based on your scene
    public float spawnRadius = 2f; // radius inside which agent can spawn safely


    public List<GameObject> allWallTriggers;

    void Start()
    {
        if (identityManager == null) identityManager = FindObjectOfType<IdentityManager>();
        if (gameManagerExtension == null) gameManagerExtension = FindObjectOfType<GameManagerExtension>();
        
        allWallTriggers = new List<GameObject>(GameObject.FindGameObjectsWithTag("WallTrigger"));

    }



    public struct ActiveWalls
    {
        public int wall1;
        public int wall2;
    }

    public void AssignNewWalls()
    {
        List<int> newWalls = gameManagerExtension.SelectNewWalls();
        activeWalls.wall1 = newWalls[0];
        activeWalls.wall2 = newWalls[1];

        Debug.Log($"New walls are assigned as: {newWalls[0]} and {newWalls[1]}");
    }

    // highWallTriggerID and lowWallTriggerID will be wallID1 and wallID2 obtained from trialLogicExtension.activeWalls as done in the agent script


    public void ColourWalls(int wallID1, int wallID2, string thisTrialType)
    {

        // Access game object through the ID:GameObject dict in IdentityManager
        GameObject wall1trigger = identityManager.GetObjectByIdentifier(wallID1);
        GameObject wall2trigger = identityManager.GetObjectByIdentifier(wallID2);

        // Get the parent object (octagon wall) for each trigger
        GameObject wall1 = wall1trigger.transform.parent.gameObject;
        GameObject wall2 = wall2trigger.transform.parent.gameObject;

        // Save original wall colour before overwriting
        defaultWallColour = wall1.GetComponent<Renderer>().materials[0].color;

        // Assign colors based on trial type
        // thisTrialType is the output of gameManager.SelectTrial() - I am not sure whether this is the correct use of it

        switch (thisTrialType)
        {
            case var value when value == General.highLow:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallHighColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                break;
            
            case var value when value == General.riskyChoice:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                break;
            
            case var value when value == General.forcedHigh:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallHighColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallHighColour;

                break;

            case var value when value == General.forcedLow:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallLowColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallLowColour;

                break;

            case var value when value == General.forcedRisky:
                wall1.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;
                wall2.GetComponent<Renderer>().materials[0].color = General.wallRiskyColour;

                break;

        }

        // Assign interaction colour to the centre of the wall

        Transform wall1Centre = wall1.transform.Find("InteractionZone");
        Transform wall2Centre = wall2.transform.Find("InteractionZone");

        var zoneColor = General.wallInteractionZoneColour;
        wall1Centre.GetComponent<Renderer>().materials[0].color = zoneColor;
        wall2Centre.GetComponent<Renderer>().materials[0].color = zoneColor;

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
        highWall.GetComponent<Renderer>().materials[0].color = defaultWallColour; 
        lowWall.GetComponent<Renderer>().materials[0].color = defaultWallColour;  

        // Reset interaction zone back to full transparency
        GameObject wall1Centre = highWall.transform.Find("InteractionZone").gameObject;
        GameObject wall2Centre = lowWall.transform.Find("InteractionZone").gameObject;
        
        Color wallCentreColor = wall1Centre.GetComponent<Renderer>().materials[0].color;
        wallCentreColor.a = 0f;
        wall1Centre.GetComponent<Renderer>().materials[0].color = wallCentreColor;
        wall2Centre.GetComponent<Renderer>().materials[0].color = wallCentreColor;

        foreach (var trigger in allWallTriggers)
        {
            if (trigger.TryGetComponent<BoxCollider>(out var collider))
            {
                collider.enabled = true; // Re-enable the trigger collider
                Debug.Log($"Collider re-enabled for trigger: {trigger}");
            }
        }


    }

    (int score, string rewardType) AssignRiskyReward(int triggerID, int highWallTriggerID, int lowWallTriggerID)
    {
        float probability = General.probabilityRisky;
        bool isRiskyWin = UnityEngine.Random.value < probability;

        if (triggerID == highWallTriggerID)
        {
            int score = isRiskyWin ? General.highScore : 0;
            string rewardType = isRiskyWin ? General.highScoreRewardType : General.zeroRewardType;
            return (score, rewardType);
        }
        else
        {
            return (General.lowScore, General.lowScoreRewardType);
        }
    }

    // This allows TrialInteraction to return something we can use for assigning correct rewards to agents
    public (int score, string rewardType) TrialInteraction(int triggerID, int highWallTriggerID, int lowWallTriggerID, string thisTrialType)
    {
        int score = 0;
        string rewardType = "";

        float probability = General.probabilityRisky;
        bool isRiskyWin = UnityEngine.Random.value < probability;

        switch (thisTrialType)
        {
            case var value when value == General.highLow:
            
            score = triggerID == highWallTriggerID ? General.highScore : General.lowScore;
            rewardType = triggerID == highWallTriggerID ? General.highScoreRewardType : General.lowScoreRewardType;
        
            break;

            case var value when value == General.riskyChoice:

            (score, rewardType) = AssignRiskyReward(triggerID, highWallTriggerID, lowWallTriggerID);

            break;

            case var value when value == General.forcedHigh:
            
            score = General.highScore;
            rewardType = General.highScoreRewardType;

            break;

            case var value when value == General.forcedLow:

            score = General.lowScore;
            rewardType = General.lowScoreRewardType;

            break;

            case var value when value == General.forcedRisky:

            score = isRiskyWin ? General.highScore : 0;
            rewardType = isRiskyWin ? General.highScoreRewardType : General.zeroRewardType; 

            break;
        }

        return (score, rewardType);
    }

}
