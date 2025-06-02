using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; // needed for .Select and .Where
using System.Linq;
using Globals;

public class MLAgent : Agent
{

    public WallTriggerExtension wallTriggerExtension;
    public GameManagerExtension gameManagerExtension;
    public TrialHandlerExtension trialHandlerExtension;
    // public Agent agent;
    public Camera agentCamera;

    public float moveSpeed = 0.5f;
    public float turnSpeed = 1f;
    private Rigidbody rb;
    public bool isTrialInProgress = false;

    // set counter to keep track of how many trials have been completed
    // public int trialCounter = 0;
    public int RandomNumber;
    private Vector3 startPosition;

    // setting this to keep track of steps taken and reward appropriately
    // public new int MaxStep;

    public override void Initialize()
    {
        agentCamera.enabled = true;
        Debug.Log($"Agent Camera Enabled: {agentCamera.enabled}");
    }
    protected override void Awake()

    // Awake is called when the script instance is being loaded
    // Use Awake to initialize references and set up the environment
    // Get rigidbody for agent movement

    {
        base.Awake();

        if (gameManagerExtension == null) gameManagerExtension = FindObjectOfType<GameManagerExtension>();
        if (trialHandlerExtension == null) trialHandlerExtension = FindObjectOfType<TrialHandlerExtension>();
        if (wallTriggerExtension == null) wallTriggerExtension = FindObjectOfType<WallTriggerExtension>();

        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

    }

    public override void OnEpisodeBegin()
    {
        Debug.Log($"[Agent] Episode begin for {tag}");

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (gameManagerExtension != null)
        {
            startPosition = gameManagerExtension.GetValidSpawnPosition();
            Debug.Log($"[{tag}] Spawning at {startPosition}");
        }

        rb.transform.localPosition = CompareTag("OpponentAgent")
            ? startPosition - 1f * Vector3.right
            : startPosition;

        if (CompareTag("PlayerAgent"))
        {
            Debug.Log($"[{tag}] PlayerAgent episode setup");

            // Reset trial state
            // isTrialInProgress = false;

            // relocating trial counter to trial handler extension
            trialHandlerExtension.trialCounter = 0;
            RandomNumber = Random.Range(5, 10);
            // Debug.Log($"[Agent] Trial counter reset. Starting 0 of {RandomNumber}");

            // Start trial loop from TrialHandler
            trialHandlerExtension.StartTrial(); // Let StartTrial handle checking trial state
        }
    }

    public void CustomEndEpisode()
    {
        Debug.Log($"[Agent] CustomEndEpisode() called for {tag}"); 
        // — trial {trialCounter}/{RandomNumber}, isTrialInProgress={isTrialInProgress}");
        
        EndEpisode(); // This calls the base method — even if not override-able
    }


    public void MoveAgent(ActionBuffers actions)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero; 

        var continuousActions = actions.ContinuousActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        rb.velocity = new Vector3(dirToGo.x * moveSpeed, rb.velocity.y, dirToGo.z * moveSpeed);
        transform.Rotate(rotateDir * turnSpeed * Time.deltaTime);

        // AddReward(-0.001f); // Optional step penalty
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions);
        // Debug.Log($"[Agent] StepCount: {StepCount}")
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = 0f;
        continuousActionsOut[2] = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = -1f;
        }

    }

}


