using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters;
using UnityEngine;
using Globals;
using System.ComponentModel;

public class OctagonWallTrigger : MonoBehaviour
{
    // assign parent arena
    private Transform arenaRoot;
    // scripts
    IdentityAssignment identityAssignment;
    [SerializeField] OctagonArenaSettings octagonArenaSettings;
    [SerializeField] OctagonAgent playerAgent;
    [SerializeField] OctagonAgent opponentAgent;
    [SerializeField] private ArenaLogger arenaLogger;
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

        // Search upward to find the ArenaManager parent
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "ArenaManager" || current.name.Contains("ArenaManager"))
            {
                arenaRoot = current;
                break;
            }
            current = current.parent;
        }

        // Fallback to hardcoded parent hierarchy if ArenaManager not found
        if (arenaRoot == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"[{gameObject.name}] ArenaManager not found in hierarchy, using hardcoded parent.parent.parent");
            #endif
            arenaRoot = transform.parent.parent.parent;
        }
        else
        {
            #if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] Found ArenaManager: {arenaRoot.name}");
            #endif
        }

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
        if (octagonArenaSettings == null) 
        {
            octagonArenaSettings = arenaRoot.GetComponentInChildren<OctagonArenaSettings>();
            if (octagonArenaSettings == null)
            {
                Debug.LogError($"[{gameObject.name}] OctagonArenaSettings component not found under {arenaRoot.name}");
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log($"[{gameObject.name}] Found OctagonArenaSettings on {octagonArenaSettings.gameObject.name}");
                #endif
            }
        }

        if (!octagonArenaSettings.soloMode && opponentAgent == null)
        {
            // Only find ACTIVE GameObjects (false parameter excludes inactive objects)
            opponentAgent = arenaRoot.GetComponentsInChildren<Transform>(false)
                .FirstOrDefault(t => t.CompareTag("OpponentAgent"))
                ?.GetComponent<OctagonAgent>();
        }
        if (playerAgent == null)
        {
            // Only find ACTIVE GameObjects (false parameter excludes inactive objects)
            playerAgent = arenaRoot.GetComponentsInChildren<Transform>(false)
                .FirstOrDefault(t => t.CompareTag("PlayerAgent"))
                ?.GetComponent<OctagonAgent>();
            
            #if UNITY_EDITOR
            if (playerAgent != null)
            {
                Debug.Log($"[OctagonWallTrigger] Found PlayerAgent: {playerAgent.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[OctagonWallTrigger] Could not find active PlayerAgent in arena!");
            }
            #endif

            if (arenaLogger == null)
                arenaLogger = FindObjectOfType<ArenaLogger>();
        }

    }

    // Method called when another collider interacts with the trigger collider of the parent game object
    void OnTriggerEnter(Collider interactingObject)
    {
        // if interacting GameObject has an ML-Agent component attached assign it to the agent variable
        if (!interactingObject.TryGetComponent<OctagonAgent>(out var agent)) return;

        wallID1 = octagonArenaSettings.activeWalls.wall1;
        wallID2 = octagonArenaSettings.activeWalls.wall2;
        wallIDs = new List<int> { wallID1, wallID2 };

        if (wallIDs.Contains(triggerID))
        {
            // What effect does this assignment have on trial logic? test without it
            octagonArenaSettings.isTrialLooping = false;
            
            // NEW: Reset arena ready flag when trial ends - prevents agents from acting during next setup
            octagonArenaSettings.isArenaReady = false;
            #if UNITY_EDITOR
            Debug.Log($"[OnTriggerEnter] Wall triggered - isTrialLooping: {octagonArenaSettings.isTrialLooping}, isArenaReady: {octagonArenaSettings.isArenaReady}");
            #endif

            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            arenaLogger?.LogTriggerActivation(triggerID, agent, authorised: true);

            HandleWallTrigger(triggerID, wallID1, wallID2, interactorTag);

            string wallTag = triggerID == wallID1 ? "HighWall" : "LowWall";

        }
        else if (!wallIDs.Contains(triggerID))
        {
            string interactorTag = agent.CompareTag("PlayerAgent") ? "PlayerAgent" : "OpponentAgent";

            arenaLogger?.LogTriggerActivation(triggerID, agent, authorised: false);

            HandleInactiveTrigger(triggerID, interactorTag);
        }
    }

    // Assign a negative penalty for colliding with an inactive wall trigger
    public void HandleInactiveTrigger(int triggerID, string interactorTag)
    {
        // float inactiveWallPenalty = -0.01f;

        // OctagonAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;
        // interactor.AddReward(inactiveWallPenalty);
    }

    // Assign reward and end the current episode, following relevant trigger collision 
    public void HandleWallTrigger(int triggerID, int wallID1, int wallID2, string interactorTag)
    {
        OctagonAgent interactor = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;

        string thisTrialType = interactor.thisTrialType;

        // Identify the outcome score dependent on activated trigger and current trial type
        var (reward, rewardType) = octagonArenaSettings.TrialInteraction(triggerID, wallID1, wallID2, thisTrialType);


        if (!octagonArenaSettings.soloMode && opponentAgent != null)
        {
            OctagonAgent winner = interactorTag == "PlayerAgent" ? playerAgent : opponentAgent;
            OctagonAgent loser = interactorTag == "PlayerAgent" ? opponentAgent : playerAgent;
            
            /* Removed zero-sum rewards and replaced with a small negative reinforcement 260220 */
            // // Equal and negative reward for the loser in competitive play
            // winner.AddReward(reward);
            // loser.AddReward(-reward);

            winner.AddReward(reward);
            // loser.AddReward(Globals.General.loserScore); // All negative reward for the loser is removed 250220 (end-of-day)
            float playerScore = winner == playerAgent ? reward : 0f;
            float opponentScore = winner == opponentAgent ? reward : 0f;
            octagonArenaSettings.SetTrialScores(playerScore, opponentScore);
            octagonArenaSettings.SetSessionScores(playerScore, opponentScore);

            // Capture each agent's full trial reward before EndEpisode() resets it.
            octagonArenaSettings.SetTrialRewards(
                playerAgent.GetCumulativeReward(), opponentAgent.GetCumulativeReward());

            octagonArenaSettings.DisableTriggers();
            #if UNITY_EDITOR
            float plCumulativeReward = playerAgent.GetCumulativeReward();
            Debug.Log($"Player agent reward at the end of this episode is {plCumulativeReward}");
            float oppCumulativeReward = opponentAgent.GetCumulativeReward();
            Debug.Log($"Opponent agent reward at the end of this episode is {oppCumulativeReward}");
            #endif

            playerAgent.EndEpisode();
            opponentAgent.EndEpisode();
            //playerAgent.LogEpisodeEndEvent();
            //opponentAgent.LogEpisodeEndEvent();


        } else if (octagonArenaSettings.soloMode)
        {
            OctagonAgent winner = playerAgent;
            winner.AddReward(reward);

            octagonArenaSettings.SetTrialScores(reward, 0f);
            octagonArenaSettings.SetSessionScores(reward, 0f);

            // Capture the agent's full trial reward before EndEpisode() resets it.
            octagonArenaSettings.SetTrialRewards(playerAgent.GetCumulativeReward(), 0f);

            #if UNITY_EDITOR
            float cumulativeReward = playerAgent.GetCumulativeReward();
            Debug.Log($"Agent reward at the end of this episode is {cumulativeReward}");
            #endif
            playerAgent.EndEpisode();
            //playerAgent.LogEpisodeEndEvent();
        }
    }
}