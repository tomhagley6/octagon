using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; // needed for .Select and .Where
using System.Linq;
using Globals;
using Unity.MLAgents.Policies;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using Mono.CSharp;
//using System.Numerics;

public class MLAgent : Agent
{
    public GameObject octagon;
    BehaviorParameters behaviorParameters;
    [SerializeField] public WallTriggerExtension wallTriggerExtension;
    [SerializeField] public GameManagerExtension gameManagerExtension;
    [SerializeField] public TrialHandlerExtension trialHandlerExtension;
    [SerializeField] public TrialLogicExtension trialLogicExtension;
    [SerializeField] public IdentityManager identityManager;

    public float moveSpeed = 20f;
    public float turnSpeed = 90f;

    public CharacterController controller;
    //public Animator animator;
    [SerializeField] public MLAgent otherAgent;

    public int wallID1;
    public int wallID2;
    public List<int> wallIDs;
    public string thisTrialType;
    public GameObject wall1trigger;
    public GameObject wall2trigger;
    public List<GameObject> allWallTriggers;
    private float forwardInput;
    private float strafeInput;
    private float rotateInput;
    public float distToWall1;
    public float distToWall2;

    public bool wallSetupComplete = false;
    public int RandomNumber;
    private Vector3 cachedToWall1;
    private Vector3 cachedToWall2;
    private float cachedAlignmentWall1;
    private float cachedAlignmentWall2;
    private float fieldOfView;
    private float fovThreshold;
    private Transform arenaRoot;
    //public int StepCount { get; }




    public override void Initialize()
    {
        // agentCamera.enabled = true;
        // Debug.Log($"Agent Camera Enabled: {agentCamera.enabled}");

        //rb = GetComponent<Rigidbody>();
        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        //if (otherAgent != null)
        //{
        //otherRb = otherAgent.GetComponent<Rigidbody>();
        //}

    }
    protected override void Awake()

    // Awake is called when the script instance is being loaded
    // Use Awake to initialize references and set up the environment

    {
        base.Awake();

        // assigned in inspector so that they are unique to that arena (for parallel training)

        if (gameManagerExtension == null) gameManagerExtension = FindObjectOfType<GameManagerExtension>();
        if (trialHandlerExtension == null) trialHandlerExtension = FindObjectOfType<TrialHandlerExtension>();
        if (wallTriggerExtension == null) wallTriggerExtension = FindObjectOfType<WallTriggerExtension>();
        if (identityManager == null) identityManager = FindObjectOfType<IdentityManager >();
        if (trialLogicExtension == null) trialLogicExtension = FindObjectOfType<TrialLogicExtension >();

    }
    void Start()
    {
        //allWallTriggers = new List<GameObject>(GameObject.FindGameObjectsWithTag("WallTrigger"));

        // finds wall triggers from parent of agent script (for parallel training)
        arenaRoot = transform.parent;

        allWallTriggers = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("WallTrigger"))
            .Select(t => t.gameObject)
            .ToList();
    }

    public override void OnEpisodeBegin()
    {
        //Debug.Log($"[Agent] Episode begin for {tag}");


        if (this == trialHandlerExtension.playerAgent)
        {

            if (arenaRoot.name == "OctagonAlcoves (2)")
            {
                Debug.Log($"Agent {this.tag} starts epidose");
                
            }

            //WallManager.HasAssigned = false;

            // temporarily setting bool to false to handle single trial per episode
            trialHandlerExtension.isTrialLoopRunning = false;

            //if (!trialHandlerExtension.isTrialLoopRunning && !wallTriggerExtension.triggerStartsTrial)
            if (!trialHandlerExtension.isTrialLoopRunning)
            {
                //RandomNumber = Random.Range(2, 3);
                RandomNumber = 1;

                trialHandlerExtension.trialCounter = 0;
                trialHandlerExtension.ResetTrial();
                trialHandlerExtension.PlayerSpawn();
                //trialHandlerExtension.StartTrial();
                StartCoroutine(trialHandlerExtension.TrialLoop());


            }
            else
            {
                trialHandlerExtension.trialCounter = 0;
                //RandomNumber = Random.Range(2, 3);
                RandomNumber = 1;
                trialHandlerExtension.PlayerSpawn();
            }
        }
    }

    public void CustomEndEpisode()
    {
        //Debug.Log($"[Agent] CustomEndEpisode() called for {tag}");

        EndEpisode(); // calls base method

    }


    public override void CollectObservations(VectorSensor sensor)
    {

        // direction vectors to high wall, low wall, and other agent
        if (wall1trigger == null || wall2trigger == null)
        {
            //Debug.LogWarning("[CollectObservations] Skipping — references not ready.");
            sensor.AddObservation(Vector3.zero); // vector to wall 1
            sensor.AddObservation(Vector3.zero); // vector to wall 2
            sensor.AddObservation(Vector3.zero); // vector to other
            sensor.AddObservation(0f); // agent y rotation
            sensor.AddObservation(0f); // other agent y rotation
            sensor.AddObservation(0f); // alignment to wall 1
            sensor.AddObservation(0f); // alignment to wall 2
            sensor.AddObservation(0f); // wall 1 identity
            sensor.AddObservation(0f); // wall 2 identity
            return;
        }

        // check wall triggers are correct at this stage for both agents
        // Debug.Log($"Agent {tag} has wall 1 trigger parent name: {wall1trigger.transform.parent.name}");

        // 1. agent y rotation
        sensor.AddObservation(transform.eulerAngles.y / 360f);

        // agent velocity
        //sensor.AddObservation(rb.velocity);

        Vector3 wall1TriggerCentre = wall1trigger.GetComponent<Collider>().bounds.center;
        Vector3 wall2TriggerCentre = wall2trigger.GetComponent<Collider>().bounds.center;

        cachedToWall1 = (wall1TriggerCentre - transform.position).normalized;
        cachedToWall2 = (wall2TriggerCentre - transform.position).normalized;
        //cachedToWall1 = (wall1trigger.transform.position - transform.position).normalized;
        //cachedToWall2 = (wall2trigger.transform.position - transform.position).normalized;
        Vector3 toOther = (otherAgent.transform.position - transform.position).normalized;

        if (Vector3.Distance(transform.position, wall1trigger.transform.position) < 2.5)
        {
            //Debug.Log($"current vector to Wall 1 is {cachedToWall1}");
        }

        // current: alignment to trigger
        // need alignment to coloured walls

        cachedAlignmentWall1 = Vector3.Dot(transform.forward, cachedToWall1);
        cachedAlignmentWall2 = Vector3.Dot(transform.forward, cachedToWall2);
        float alignmentOther = Vector3.Dot(transform.forward, toOther);

        fieldOfView = 110f;
        fovThreshold = Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);

        // 2. vector, alignment to walls, wall IDs

        if (cachedAlignmentWall1 > fovThreshold)
        {
            sensor.AddObservation(cachedToWall1);         // vector 3 to wall 1
            sensor.AddObservation(cachedAlignmentWall1);  // scalar 1 alignment to wall 1 
            sensor.AddObservation(1f);                    // wall identity 1 (high)
            if (arenaRoot.name == "OctagonAlcoves (2)")
            {
                Debug.Log($"Observations are: vector to W1 {cachedToWall1}, alignment to W1 {cachedAlignmentWall1}, Wall ID {1f}");
            }
        }
        else
        {
            sensor.AddObservation(Vector3.zero);         // placeholder obs
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        if (cachedAlignmentWall2 > fovThreshold)
        {
            sensor.AddObservation(cachedToWall2);         // vector 3 to wall 2
            sensor.AddObservation(cachedAlignmentWall2);  // scalar 1 alignment to wall 2
            sensor.AddObservation(2f);                    // wall identity 2 (low)
            if (arenaRoot.name == "OctagonAlcoves (2)")
            {
                Debug.Log($"Observations are: vector to W2 {cachedToWall2}, alignment to W2 {cachedAlignmentWall2}, Wall ID {2f}");
            }
        }
        else
        {
            sensor.AddObservation(Vector3.zero);         // placeholder obs
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // 3. vector to other agent, other agent's y rotation

        sensor.AddObservation(alignmentOther > fovThreshold ? toOther : Vector3.zero);
        sensor.AddObservation(alignmentOther > fovThreshold ? (otherAgent.transform.eulerAngles.y / 360f) : 0f);

        if (arenaRoot.name == "OctagonAlcoves (2)")
        {
            if (cachedAlignmentWall1 > fovThreshold)
            {
                //Debug.Log($"Vector to wall 1: {cachedToWall1}");
            }
            if (alignmentOther > fovThreshold)
            {
                //Debug.Log($"Vector to other: {toOther}");
            }
        }

        // get head angle vector
        // get player-to-alcove vector - or rather player to coloured walls vectors
        // compute dot product between vectors
        // if alignment is in fov range add wall observations 
        // replicate for other agent ob


    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        var continuousActions = actions.ContinuousActions;

        forwardInput = Mathf.Clamp(continuousActions[0], -1f, 1f);
        strafeInput = Mathf.Clamp(continuousActions[1], -1f, 1f);
        rotateInput = Mathf.Clamp(continuousActions[2], -1f, 1f);

        AddReward(-0.0001f); // step penalty


        if (!wallSetupComplete || wall1trigger == null || wall2trigger == null)
        {
            //Debug.Log("[MoveAgent] wall triggers are null — walls not set up yet.");
            return;
        }

        Vector3 wall1TriggerCentre = wall1trigger.GetComponent<Collider>().bounds.center;
        Vector3 wall2TriggerCentre = wall2trigger.GetComponent<Collider>().bounds.center;

        float currentDistWall1 = Vector3.Distance(transform.position, wall1TriggerCentre);
        float currentDistWall2 = Vector3.Distance(transform.position, wall2TriggerCentre);

        float distanceDelta1 = distToWall1 - currentDistWall1;
        float distanceDelta2 = distToWall2 - currentDistWall2;

        //Debug.Log($"[{tag}] Dist to wall1: {currentDistWall1:F2}, Prev: {distToWall1:F2}, Delta: {distanceDelta1:F3}");

        Vector3 dirToWall1 = (wall1TriggerCentre - transform.position).normalized;
        Vector3 dirToWall2 = (wall2TriggerCentre - transform.position).normalized;

        //float currentAlignmentWall1 = Vector3.Dot(transform.forward, dirToWall1);
        //float currentAlignmentWall2 = Vector3.Dot(transform.forward, dirToWall2);

        if ((cachedAlignmentWall1 > fovThreshold) && (distanceDelta1 > 0.01f) && (currentDistWall1 < 20))
        {
            float proximityReward1 = (1f / Mathf.Max(currentDistWall1, 0.1f)) * 0.000001f;
            float alignmentReward1 = cachedAlignmentWall1 * 0.0001f;
            AddReward(proximityReward1 + alignmentReward1);
            if (arenaRoot.name == "OctagonAlcoves (2)")
            {
                Debug.Log($"Rewarding agent for proximity: {proximityReward1} and alignment {alignmentReward1} to wall 1");
            }
        }

        if ((cachedAlignmentWall2 > fovThreshold) && (distanceDelta2 > 0.01f) && (currentDistWall2 < 20))
        {
            float proximityReward2 = (1f / Mathf.Max(currentDistWall2, 0.1f)) * 0.000001f;
            float alignmentReward2 = cachedAlignmentWall2 * 0.0001f;
            AddReward(proximityReward2 + alignmentReward2);
            if (arenaRoot.name == "OctagonAlcoves (2)")
            {
                Debug.Log($"Rewarding agent for proximity: {proximityReward2} and alignment {alignmentReward2} to wall 2");
            }
        }

        if (cachedAlignmentWall1 < fovThreshold && cachedAlignmentWall2 < fovThreshold && rotateInput > 0)
        {
            //AddReward(Mathf.Abs(rotateInput) * 0.001f);
            AddReward(0.0001f);
        }

        //if ((distanceDelta1 > 0.01f) && (currentDistWall1 < 19) && (currentDistWall1 < currentDistWall2))
        //{
        //float movementReward1 = Mathf.Max(distanceDelta1, 0f) * 0.0002f;
        //float proximityReward1 = (1f / Mathf.Max(currentDistWall1, 0.1f)) * 0.00005f;
        //AddReward(movementReward1 + proximityReward1);

        //Debug.Log($"step reward {movementReward1 + proximityReward1}");

        //}

        //if ((distanceDelta2 > 0.01f) && (currentDistWall2 < 19) && (currentDistWall2 < currentDistWall1))
        //{
        //float movementReward2 = Mathf.Max(distanceDelta2, 0f) * 0.0001f;
        //float proximityReward2 = (1f / Mathf.Max(currentDistWall2, 0.1f)) * 0.00005f;
        //AddReward(movementReward2 + proximityReward2);

        //Debug.Log($"step reward {movementReward2 + proximityReward2}");

        //}

        distToWall1 = currentDistWall1;
        distToWall2 = currentDistWall2;

        if (StepCount == 4900)
        {
            float reward = GetCumulativeReward();
            Debug.Log($"Step: {StepCount}, cumulative reward: {reward}");
        }

        //Vector3 toWall1 = (wall1trigger.transform.position - transform.position).normalized;
        //Vector3 moveDir = transform.forward * forwardInput + transform.right * strafeInput;
        //moveDir = moveDir.magnitude > 0.01f ? moveDir.normalized : Vector3.zero;
        //float alignment1 = Vector3.Dot(moveDir, toWall1);

        // add small reward for moving toward active wall
        //if (alignment1 > 0.1f)
        //{
        //AddReward(alignment1 * +0.0001f);
        //}

        //Vector3 toWall2 = (wall2trigger.transform.position - transform.position).normalized;
        //float alignment2 = Vector3.Dot(moveDir, toWall2);

        // add small reward for moving toward active wall
        //if (alignment2 > 0.1f)
        //{
        //AddReward(alignment2 * +0.0001f);
        //}

        //if (alignment1 > 0.9f)
        //{
        //Debug.Log($"alignment to wall 1 is {alignment1}, vector to wall is {toWall1}");
        //}
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


