using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OctagonWallTrigger : MonoBehaviour
{
    // assign parent arena
    private Transform arenaRoot;
    // scripts
    IdentityAssignment identityAssignment;
    [SerializeField] OctagonArea octagonArea;
    [SerializeField] OctagonAgent playerAgent;
    [SerializeField] OctagonAgent opponentAgent;
    // variables
    public int triggerID;
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;
    public List<Collider> wallColliders;

    void Awake()
    {   
        // Get the custom ID assigned to this wall trigger
        identityAssignment = gameObject.GetComponent<IdentityAssignment>();
        if (identityAssignment != null)
        {
            triggerID = identityAssignment.customID;
        }

        arenaRoot = transform.parent;

        // get all wall colliders
        wallColliders = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("Wall"))
            .Select(t => t.GetComponent<Collider>())
            .Where(c => c != null)
            .ToList();
    }

    // Get references to player and opponent agents 
    void Start()
    {
        if (!octagonArea.soloMode && opponentAgent == null)
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

    // Method called when another collider interacts with the trigger collider of the parent game object
    void OnTriggerEnter(Collider interactingObject)
    {
        // if interacting GameObject has an ML-Agent component attached assign it to the agent variable
        if (!interactingObject.TryGetComponent<OctagonAgent>(out var agent)) return;

        wallID1 = octagonArea.activeWalls.wall1;
        wallID2 = octagonArea.activeWalls.wall2;
        wallIDs = new List<int> { wallID1, wallID2 };

        if (wallIDs.Contains(triggerID))
        {
            // What effect does this assignment have on trial logic? test without it
            octagonArea.isTrialLooping = false;
            Debug.Log($"Wall trigger - trial looping: {octagonArea.isTrialLooping}");

            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            HandleWallTrigger(triggerID, wallID1, wallID2, interactorTag);

            string wallTag = triggerID == wallID1 ? "HighWall" : "LowWall";

            playerAgent.LogTriggerActivation(wallID1, wallTag, interactorTag);

            if (!octagonArea.soloMode)
            {
                opponentAgent.LogTriggerActivation(wallID1, wallTag, interactorTag);

            }

        }
        else if (!wallIDs.Contains(triggerID))
        {
            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            HandleInactiveTrigger(triggerID, interactorTag);
        }
    }

    // Assign a negative penalty for colliding with an inactive wall trigger
    public void HandleInactiveTrigger(int triggerID, string interactorTag)
    {
        float inactiveWallPenalty = -0.01f;

        OctagonAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;
        interactor.AddReward(inactiveWallPenalty);
    }

    // Assign reward and end the current episode, following relevant trigger collision 
    public void HandleWallTrigger(int triggerID, int wallID1, int wallID2, string interactorTag)
    {
        OctagonAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        string thisTrialType = interactor.thisTrialType;

        // Identify the outcome score dependent on activated trigger and current trial type
        var (score, rewardType) = octagonArea.TrialInteraction(triggerID, wallID1, wallID2, thisTrialType);

        // Set reward at 1/10 point value for this trial outcome
        // This might be way too high? Compared to other rewards
        float scaledReward = score / 10f;

        if (!octagonArea.soloMode && opponentAgent != null)
        {
            OctagonAgent winner = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;
            OctagonAgent loser = interactorTag == "PlayerAgent" ? opponentAgent : playerAgent;

            // Equal and negative reward for the loser in competitive play
            winner.AddReward(scaledReward);
            loser.AddReward(-scaledReward);

            octagonArea.DisableTriggers();
            float plCumulativeReward = playerAgent.GetCumulativeReward();
            Debug.Log($"Player agent reward at the end of this episode is {plCumulativeReward}");
            float oppCumulativeReward = opponentAgent.GetCumulativeReward();
            Debug.Log($"Opponent agent reward at the end of this episode is {oppCumulativeReward}");

            playerAgent.EndEpisode();
            opponentAgent.EndEpisode();
            //playerAgent.LogEpisodeEndEvent();
            //opponentAgent.LogEpisodeEndEvent();


        }
        else if (octagonArea.soloMode)
        {
            OctagonAgent winner = playerAgent;
            winner.AddReward(scaledReward);
            float cumulativeReward = playerAgent.GetCumulativeReward();
            Debug.Log($"Agent reward at the end of this episode is {cumulativeReward}");
            playerAgent.EndEpisode();
            //playerAgent.LogEpisodeEndEvent();
        }
    }
}