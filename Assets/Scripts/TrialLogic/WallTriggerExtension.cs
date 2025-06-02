using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; // needed for .Select and .Where
using System.Linq;
using Globals;
public class WallTriggerExtension : MonoBehaviour
{
    public TrialLogicExtension trialLogicExtension;
    public TrialHandlerExtension trialHandlerExtension;
    // public MLAgent agentExtension;
    // public Agent baseAgent;
    private IdentityAssignment identityAssignment;

    public int triggerID;
    public List<GameObject> triggers;
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs; 
    // adding multiplayer logic
    public MLAgent opponentAgent;
    public MLAgent playerAgent;
    public List<MLAgent> AgentList = new List<MLAgent>();
    public Transform ground; // assign in inspector
    private Bounds spawnBounds; // specify?
    private bool isHandlingTrigger = false;



    Rigidbody opponentAgentRb;
    Rigidbody playerAgentRb;


    public List<Collider> wallColliders;
    // public static string thisTrialType;  // trial type for this trial
    // public BoxCollider collider;

    void Awake()
    {
        if (trialLogicExtension == null) trialLogicExtension = FindObjectOfType<TrialLogicExtension>();
        if (trialHandlerExtension == null) trialHandlerExtension = FindObjectOfType<TrialHandlerExtension>();
        
        identityAssignment = GetComponent<IdentityAssignment>();
        if (identityAssignment != null)
        {
            triggerID = identityAssignment.customID;
        }
        else
        {
            Debug.LogWarning("IdentityAssignment missing on wall trigger object.");
        }

        wallColliders = GameObject.FindGameObjectsWithTag("Wall")
            .Select(go => go.GetComponent<Collider>())
            .Where(c => c != null)
            .ToList();

        // assign the ground GameObject to the ground field in the Inspector
        // make sure the ground has a Collider component for bounds
        if (ground != null)
        {
            Collider groundCollider = ground.GetComponent<Collider>();
            if (groundCollider != null)
                spawnBounds = groundCollider.bounds;
            else
                Debug.LogWarning("Ground object has no collider.");
        }
        else
        {
            Debug.LogWarning("Ground not assigned — agent spawn bounds unavailable.");
        }

    }

   void Start()
    {
		opponentAgent = GameObject.FindWithTag("OpponentAgent").GetComponent<MLAgent>();
        playerAgent = GameObject.FindWithTag("PlayerAgent").GetComponent<MLAgent>();

        // AssignActiveWalls();
        
        opponentAgentRb = opponentAgent.GetComponent<Rigidbody>();
		playerAgentRb = playerAgent.GetComponent<Rigidbody>();

    }

    void OnTriggerEnter(Collider interactingObject)
    {

        // if GameObject has an MLAgent component attached assign it to the agent variable
        if (!interactingObject.TryGetComponent<MLAgent>(out var agent)) return;

        List<int> wallIDs = trialHandlerExtension.wallIDs;
        wallID1 = trialHandlerExtension.wallID1;
        wallID2 = trialHandlerExtension.wallID2;

        // if (!wallIDs.Contains(triggerID)) return;
        if (wallIDs.Contains(triggerID))
        {
            string winnerTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";
            string loserTag = winnerTag == "PlayerAgent" ? "OpponentAgent" : "PlayerAgent";

            Debug.Log($"[OnTriggerEnter] agent {winnerTag} triggered Wall {triggerID}");

            HandleWallTrigger(triggerID, wallID1, wallID2, winnerTag, loserTag);
        }

        else if (!wallIDs.Contains(triggerID))
        {
            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            HandleInactiveTrigger(triggerID, interactorTag);
        }
    }

    public void HandleInactiveTrigger(int triggerID, string interactorTag)
    {
        //if (isHandlingTrigger)
        //{
        //    return;
        //}

        //isHandlingTrigger = true;
        float negReward = -0.1f;

        MLAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        interactor.AddReward(negReward);
        Debug.Log($"[HandleInactiveTrigger] Agent is negatively rewarded for interacting with inactive wall {triggerID}");

        //isHandlingTrigger = false;
    }
    public void HandleWallTrigger(int triggerID, int wallID1, int wallID2, string winnerTag, string loserTag)
    {

        if (!playerAgent.isTrialInProgress || isHandlingTrigger)
        {
            Debug.Log($"[HandleWallTrigger] Ignoring trigger; trial in progress: {playerAgent.isTrialInProgress}, already handling: {isHandlingTrigger}");
            return;
        }

        isHandlingTrigger = true;
        playerAgent.isTrialInProgress = false;
        Debug.Log($"[HandleWallTrigger] ({playerAgent.isTrialInProgress}) Trial in progress, setting to false and proceeding with logic.");

        Debug.Log($"trigger ID is {triggerID}");

        string thisTrialType = trialHandlerExtension.thisTrialType;
        Debug.Log($"trial type is {thisTrialType}");

        var (score, rewardType) = trialLogicExtension.TrialInteraction(triggerID, wallID1, wallID2, thisTrialType);
        Debug.Log($"passed to trial interaction method: triggerID: {triggerID}, wallID1: {wallID1}, wallID2: {wallID2}, trialType: {thisTrialType}");

        float scaledReward = score / 10f; // normalize if needed
        // float receivedReward = scaledReward + 0.1f; // changing scoring system to add small positive bonus for triggering a wall (so that risky reward 0 is still positive)
        Debug.Log($"Score for this trial is {score}, scaled reward is {scaledReward}");

        trialLogicExtension.WashWalls(wallID1, wallID2); // reset walls

        MLAgent winner = winnerTag == "PlayerAgent" ? playerAgent : opponentAgent;
        MLAgent loser = winnerTag == "PlayerAgent" ? opponentAgent : playerAgent;

        winner.AddReward(scaledReward != 0 ? scaledReward : 0.1f);
        loser.AddReward(scaledReward != 0 ? -scaledReward : -0.1f);

        // winner.AddReward(receivedReward);
        // loser.AddReward(-receivedReward);

        Debug.Log($"[HandleWallTrigger] ({winner.tag}) + {(scaledReward != 0 ? scaledReward : 0.1f)}, {loser.tag} {(scaledReward != 0 ? -scaledReward : -0.1f)} ({rewardType})");

        float endReward = 0.1f;

        Debug.Log($"[HandleWallTrigger] current StepCount is {playerAgent.StepCount} out of {playerAgent.MaxStep}");
        Debug.Log($"[HandleWallTrigger] current trial is {trialHandlerExtension.trialCounter} out of {playerAgent.RandomNumber}");

        if ((playerAgent.StepCount < playerAgent.MaxStep) && (trialHandlerExtension.trialCounter == playerAgent.RandomNumber))
        {
            winner.AddReward(endReward);
            loser.AddReward(endReward);
            Debug.Log($"[HandleWallTrigger] agents completed all trials before max step was reached and receive reward {endReward}");
        }

        StartCoroutine(trialHandlerExtension.TrialLoop());

        isHandlingTrigger = false;
    }
}