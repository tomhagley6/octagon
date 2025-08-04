// dependencies

using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
//using System.Numerics;

public class OctagonAgent : Agent
{
    // scripts

    // assigned in inspector 
    // each arena has its own copy of each script
    public GameObject wall1Trigger;
    public GameObject wall2Trigger;
    public string thisTrialType;
    [SerializeField] public OctagonArea octagonArea;
    // uncomment once trial logic script is ready
    //[SerializeField] public TrialLogic trialLogic;
    [SerializeField] public IdentityManager identityManager;

    // variables and objects

    // agent actions
    // adjust agent speeds as appropriate
    public float moveSpeed = 20f;
    public float turnSpeed = 360f;
    public CharacterController controller;
    public Animator animator;
    private float previousDistanceHigh;
    private float previousDistanceLow;

    // octagon arena
    private Transform arenaRoot;

    // wall trigger objects
    public List<GameObject> allWallTriggers;

    // variable to keep track of shaping reward
    public float totalShapingReward;

    // bool flag to check whether agent is currently being trained
    public bool isTraining = false;
    // flag to check whether behaviour is set to inferene
    public bool isInference = false;
    // path to log file where agent data will be saved
    string logPath;
    // StreamWriter instance used to write agent logs to the file
    StreamWriter logWriter;

    public override void Initialize()
    {
        // checks whether communicator (which allows interaction with python process) is on
        // if off, agent is in inference/heuristic mode
        isTraining = Academy.Instance.IsCommunicatorOn;
        isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;


        // if communicator is off
        if (!isTraining && isInference)
        {
            // get agent tag (PlayerAgent or OpponentAgent)
            string agentTag = this.tag;

            // define path for agent log 
            // stores log in 'AgentLogs' folder in 'Assets' folder 
            logPath = Application.dataPath + $"/AgentLogs/log_{agentTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";

            logWriter = new StreamWriter(logPath, true); // class for writing text to files
            logWriter.WriteLine("Episode,Step,Time,PosX,PosZ,Reward");

        }
    }

    // Awake is called when the script instance is being loaded
    // use to intialise references
    protected override void Awake()
    {
        arenaRoot = transform.parent;

        if (octagonArea == null) octagonArea = arenaRoot.GetComponentInChildren<OctagonArea>();

    }

    void Start()
    {
        // find all wall triggers from the parent of this script (the agent)

        // searches children of the arena (including inactive ones)
        allWallTriggers = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("WallTrigger")) // children with tag "WallTrigger"
            .Select(t => t.gameObject) // select the associated game object
            .ToList(); // store in list
        if (octagonArea != null)
        {
            Debug.Log("Octagon area located.");
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin is called");

        Debug.Log("Time scale: " + Time.timeScale);
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1; // Resume normal time
        }

        // check that agent currently running this script is "PlayerAgent"
        // is this the agent assigned to playerAgent in TrialHandler? 
        //if (this == trialHandler.playerAgent)
        // temporarily setting this directly to suppress error
        if (this.tag == "PlayerAgent")
        {
            Debug.Log("PlayerAgent found, starting episode");
            totalShapingReward = 0; // variable to track shaping rewards for agent

            // disable wall triggers during ITI
            octagonArea.DisableTriggers();

            // reset arena by washing off active wall colours
            octagonArea.ResetTrial();

            // start trial ITI and active wall colouring logic
            //StartCoroutine(octagonArea.ITI());

            Debug.Log("Starting coroutine...");

            octagonArea.TrialLoop();
            //StartCoroutine(octagonArea.ITI());

            Debug.Log("Coroutine has started");
        }

        previousDistanceHigh = Vector3.Distance(transform.position, wall1Trigger.transform.position);
        previousDistanceLow = Vector3.Distance(transform.position, wall2Trigger.transform.position);

        Debug.Log($"[OctagonAgent] Agent {this.tag} starts with distance to {previousDistanceHigh} and distance to low {previousDistanceLow}.");

    }

    // observations:

    // visual observations are provided via CameraSensor (Agent component) which is assigned a camera (Agent's child)
    // Sensor component collects image information transforming it into a 3D tensor that can be fed into the CNN (in our case: resnet)
    // optionally add active wall flag as vector observation

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Extract discrete actions for movement, strafe, and rotation
        int moveAction = actionBuffers.DiscreteActions[0];  // Move (3 choices)
        int strafeAction = actionBuffers.DiscreteActions[1];  // Strafe (3 choices)
        int rotateAction = actionBuffers.DiscreteActions[2];  // Rotate (3 choices)

        // Handle move action
        float moveAmount = 0;
        if (moveAction == 1) moveAmount = moveSpeed;   // Move forward
        else if (moveAction == 2) moveAmount = -moveSpeed; // Move backward

        // Handle strafe action
        float strafeAmount = 0;
        if (strafeAction == 1) strafeAmount = moveSpeed;  // Strafe right
        else if (strafeAction == 2) strafeAmount = -moveSpeed; // Strafe left

        // Handle rotate action
        float rotateAmount = 0;
        if (rotateAction == 1) rotateAmount = turnSpeed;  // Rotate clockwise
        else if (rotateAction == 2) rotateAmount = -turnSpeed;  // Rotate counterclockwise

        // Move and rotate agent using the values derived from actions
        Vector3 targetDirection = transform.forward * moveAmount + transform.right * strafeAmount;
        if (targetDirection.magnitude > 1)
            targetDirection.Normalize();

        controller.Move(targetDirection * moveSpeed * Time.fixedDeltaTime);

        float targetYRotation = transform.eulerAngles.y + rotateAmount * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);


        if (animator == null)
        {
            Debug.LogError("Animator is not assigned");
            return;
        }

        animator.SetBool("isRunning", targetDirection.magnitude > 0.05f);

        // step penalty
        AddReward(-0.0001f);

        // optional shaping rewards

        float fieldOfView = 80f;
        // cosine for half of the field of view angle
        float fovThreshold = Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);

        if (wall1Trigger == null || wall2Trigger == null)
        {
            return;
        }

        Vector3 wall1TriggerCentre = wall1Trigger.transform.position;
        Vector3 wall2TriggerCentre = wall2Trigger.transform.position;
        //Vector3 wall1TriggerCentre = wall1Trigger.GetComponent<Collider>().bounds.center;
        //Vector3 wall2TriggerCentre = wall2Trigger.GetComponent<Collider>().bounds.center;

        Vector3 toWall1 = (wall1TriggerCentre - transform.position).normalized;
        Vector3 toWall2 = (wall2TriggerCentre - transform.position).normalized;

        // dot product of agent forward direction vector and direction to each wall
        // dot product of two normalised vectors gives cosine of the angle between them
        float alignmentToWall1 = Vector3.Dot(transform.forward, toWall1);
        float alignmentToWall2 = Vector3.Dot(transform.forward, toWall2);


        // check whether the alignment value for each wall is below the threshold
        // reward agent for rotating when neither wall is in view
        if ((alignmentToWall1 < fovThreshold) && (alignmentToWall2 < fovThreshold) && Mathf.Abs(rotateAmount) > 0)
        {
            AddReward(0.001f);
            //Debug.Log($"neither wall is visible. Alignments: to wall 1 {alignmentToWall1}, to wall 2 {alignmentToWall2}. Turn input is not 0: {rotateAmount}");
            //Debug.Log($"agent head direction vector is {transform.forward}. Dot product (alignment) between vector to wall 1 and head direction is {alignmentToWall1}");
        }

        if ((alignmentToWall1 > fovThreshold) && (alignmentToWall2 < alignmentToWall1))
        {
            float currentDistanceHigh = Vector3.Distance(transform.position, wall1Trigger.transform.position);

            if (currentDistanceHigh < previousDistanceHigh)
            {
                AddReward(0.001f);
                //Debug.Log($"current distance to high is {currentDistanceHigh} and smaller than in previous step. Agent is rewarded.");
            }

            previousDistanceHigh = currentDistanceHigh;

        }

        if ((alignmentToWall2 > fovThreshold) && (alignmentToWall1 < alignmentToWall2))
        {
            float currentDistanceLow = Vector3.Distance(transform.position, wall2Trigger.transform.position);

            if (currentDistanceLow < previousDistanceLow)
            {
                AddReward(0.001f);
                //Debug.Log($"current distance to low is {currentDistanceLow} and smaller than in previous step. Agent is rewarded.");

            }

            previousDistanceLow = currentDistanceLow;

        }

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        discreteActionsOut[2] = 0;

        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }


        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 2;
        }

        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 1;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 2;
        }

    }


    void OnApplicationQuit()
    {
        if (logWriter != null && !isTraining)
        {
            logWriter.Flush();
            logWriter.Close();
        }
    }
}