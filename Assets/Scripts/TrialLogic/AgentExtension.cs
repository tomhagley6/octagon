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

    public float moveSpeed = 20f;
    public float turnSpeed = 90f;
    private float forwardInput;
    private float strafeInput;
    private float rotateInput;

    public CharacterController controller;
    //public Animator animator;

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

    }

    public override void OnEpisodeBegin()
    {
        Debug.Log($"[Agent] Episode begin for {tag}");


        if (CompareTag("PlayerAgent"))
        {
            Debug.Log($"[{tag}] PlayerAgent episode setup");

            // Reset trial state
            // isTrialInProgress = false;

            // relocating trial counter to trial handler extension
            trialHandlerExtension.trialCounter = 0;
            RandomNumber = Random.Range(2, 3); // temporarly capping to one trial per episode for association-learning
            // RandomNumber = Random.Range(5, 10);
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


    public override void OnActionReceived(ActionBuffers actions)
    {

        var continuousActions = actions.ContinuousActions;

        forwardInput = Mathf.Clamp(continuousActions[0], -1f, 1f);
        strafeInput = Mathf.Clamp(continuousActions[1], -1f, 1f);
        rotateInput = Mathf.Clamp(continuousActions[2], -1f, 1f);

        AddReward(-0.0001f); // step penalty
    }
    
    public void FixedUpdate()
    {
        if (controller == null) return;

        //RequestDecision();

        Vector3 targetDirection = transform.forward * forwardInput + transform.right * strafeInput;
        if (targetDirection.magnitude > 1)
            targetDirection.Normalize();

        if (targetDirection.magnitude > 0.01f)
            Debug.DrawRay(transform.position, targetDirection * 2f, Color.red, 0.1f);

        controller.Move(targetDirection * moveSpeed * Time.fixedDeltaTime);

        float targetYRotation = transform.eulerAngles.y + rotateInput * turnSpeed * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);

        //animator.SetBool("isRunning", targetDirection.magnitude > 0.05f);

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



