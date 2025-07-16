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
    private Transform arenaRoot;
    [SerializeField] public TrialLogicExtension trialLogicExtension;
    [SerializeField] public TrialHandlerExtension trialHandlerExtension;
    // public MLAgent agentExtension;
    // public Agent baseAgent;
    public IdentityAssignment identityAssignment;

    public int triggerID;
    public List<GameObject> triggers;
    public int wallID1;
    public int wallID2;
    public List<int> wallIDs; 
    // adding multiplayer logic
    [SerializeField] public MLAgent opponentAgent;
    [SerializeField] public MLAgent playerAgent;
    public List<MLAgent> AgentList = new List<MLAgent>();
    public Transform ground; // assign in inspector
    private Bounds spawnBounds; // specify?
                                //private bool isHandlingTrigger = false;
    //public bool triggerStartsTrial = false;



    //Rigidbody opponentAgentRb;
    //Rigidbody playerAgentRb;


    public List<Collider> wallColliders;
    // public static string thisTrialType;  // trial type for this trial
    // public BoxCollider collider;

    void Awake()
    {
        arenaRoot = transform.parent;

        if (trialLogicExtension == null) trialLogicExtension = arenaRoot.GetComponentInChildren<TrialLogicExtension>();
        if (trialHandlerExtension == null) trialHandlerExtension = arenaRoot.GetComponentInChildren<TrialHandlerExtension>();
        
        identityAssignment = GetComponent<IdentityAssignment>();
        if (identityAssignment != null)
        {
            triggerID = identityAssignment.customID;
        }
        else
        {
            //Debug.LogWarning("IdentityAssignment missing on wall trigger object.");
        }

        //wallColliders = GameObject.FindGameObjectsWithTag("Wall")
        //.Select(go => go.GetComponent<Collider>())
        //.Where(c => c != null)
        //.ToList();

        // for parallel training
        
        wallColliders = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("Wall"))
            .Select(t => t.GetComponent<Collider>())
            .Where(c => c != null)
            .ToList();

    }

   void Start()
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
        // AssignActiveWalls();
        
        //opponentAgentRb = opponentAgent.GetComponent<Rigidbody>();
		//playerAgentRb = playerAgent.GetComponent<Rigidbody>();

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

            //Debug.Log($"[OnTriggerEnter] agent {winnerTag} triggered Wall {triggerID}");

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
        float negReward = -0.01f;

        MLAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        interactor.AddReward(negReward);
        //Debug.Log($"[HandleInactiveTrigger] Agent is negatively rewarded for interacting with inactive wall {triggerID}");

        //isHandlingTrigger = false;
    }
    public void HandleWallTrigger(int triggerID, int wallID1, int wallID2, string winnerTag, string loserTag)
    {

        //triggerStartsTrial = true;
        //Debug.Log($"trigger ID is {triggerID}");

        string thisTrialType = trialHandlerExtension.thisTrialType;
        //Debug.Log($"trial type is {thisTrialType}");

        var (score, rewardType) = trialLogicExtension.TrialInteraction(triggerID, wallID1, wallID2, thisTrialType);
        //Debug.Log($"passed to trial interaction method: triggerID: {triggerID}, wallID1: {wallID1}, wallID2: {wallID2}, trialType: {thisTrialType}");

        float scaledReward = score / 10f; // normalize if needed
        // float receivedReward = scaledReward + 0.1f; // changing scoring system to add small positive bonus for triggering a wall (so that risky reward 0 is still positive)
        //Debug.Log($"Score for this trial is {score}, scaled reward is {scaledReward}");

        MLAgent winner = winnerTag == "PlayerAgent" ? playerAgent : opponentAgent;
        MLAgent loser = winnerTag == "PlayerAgent" ? opponentAgent : playerAgent;

        //redundant but keeping structure in case I change it 
        float rewardWinner = scaledReward != 0 ? scaledReward : 0.1f;
        float rewardLoser = scaledReward != 0 ? -0.1f : -0.1f;

        winner.AddReward(rewardWinner);
        //loser.AddReward(scaledReward != 0 ? -scaledReward : -0.1f);
        loser.AddReward(rewardLoser);

        // winner.AddReward(receivedReward);
        // loser.AddReward(-receivedReward);

        //Debug.Log($"[HandleWallTrigger] ({winner.tag}) + {rewardWinner}, {loser.tag} {rewardLoser} ({rewardType})");

        //float opponentReward = opponentAgent.GetCumulativeReward();
        //float playerReward = playerAgent.GetCumulativeReward();

        //opponentAgent.CustomEndEpisode();
        //playerAgent.CustomEndEpisode();
        //opponentAgent.EndEpisode();
        //playerAgent.EndEpisode();

        //Debug.Log($"[{opponentAgent.name}] Episode ended. Cumulative Reward: {opponentReward}");
        //Debug.Log($"[{playerAgent.name}] Episode ended. Cumulative Reward: {playerReward}");

        trialLogicExtension.WashWalls(wallID1, wallID2); // reset walls

        //trialHandlerExtension.trialIsSetUp = false;

        // sets isTrialLoopRunning back to false to allow new trial set-up in TrialHandlerExtension
        trialHandlerExtension.isTrialLoopRunning = false;

        playerAgent.CustomEndEpisode();
        opponentAgent.CustomEndEpisode();

        // start new trial
        // letting OnEpisodeBegin handle this
        //StartCoroutine(trialHandlerExtension.TrialLoop());
        //isHandlingTrigger = false;
    }
}