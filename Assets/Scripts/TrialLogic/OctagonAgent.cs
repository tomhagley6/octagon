using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public class OctagonAgent : Agent
{
    // scripts

    // assigned in inspector 
    // each arena has its own copy of each script
    public GameObject wall1Trigger;
    public GameObject wall2Trigger;
    public string thisTrialType;
    [SerializeField] public OctagonArenaSettings octagonArenaSettings;
    // uncomment once trial logic script is ready
    //[SerializeField] public TrialLogic trialLogic;
    [SerializeField] public IdentityManager identityManager;

    // variables and objects

    // agent actions
    // adjust agent speeds as appropriate
    public float moveSpeed = 10f;
    public float turnSpeed = 360f;
    public CharacterController controller;
    public Animator animator;
    public float previousDistanceHigh;
    public float previousDistanceLow;
    private int episodeCount = 0;
    private int stepCount = 0;

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
        //if (!isTraining && isInference)
        if (!isTraining)
        {
            // get agent tag (PlayerAgent or OpponentAgent)
            string agentTag = this.tag;

            // define path for agent log 
            // stores log in 'AgentLogs' folder in 'Assets' folder
            if (!octagonArenaSettings.soloMode)
            {
                logPath = Application.dataPath + $"/SocialRaycastLogs/log_{agentTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            }
            else
            {
                logPath = Application.dataPath + $"/SoloRaycastLogs/log_{agentTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            }

            logWriter = new StreamWriter(logPath, true); // class for writing text to files
            logWriter.WriteLine("Episode,Step,Wall1,Wall2,Time,PosX,PosY,PosZ,RotY,Reward");

        }
    }

    // Awake is called when the script instance is being loaded
    // use to intialise references
    protected override void Awake()
    {
        // Search upward to find the ArenaManager parent
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "ArenaManager" || current.GetComponent<Transform>().name.Contains("ArenaManager"))
            {
                arenaRoot = current;
                break;
            }
            current = current.parent;
        }

        // Fallback to direct parent if ArenaManager not found
        if (arenaRoot == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"[{gameObject.name}] ArenaManager not found in hierarchy, using direct parent");
            #endif
            arenaRoot = transform.parent;
        }
        else
        {
            #if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] Found ArenaManager: {arenaRoot.name}");
            #endif
        }

        // Find OctagonArenaSettings in the arena hierarchy
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

        // Get character controller component
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("CharacterController component not found on agent.");
            }   
        }

        // Get animator component
        if (animator == null)   
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on agent.");
            }
        }

        // Raycast sensor is now handled automatically by SelectivePassThroughRaycastSensorComponent
        // No need to get a reference here

    }



    void Start()
    {
        // Get references to all wall triggers in the arena
        allWallTriggers = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("WallTrigger"))            .Select(t => t.gameObject)            .ToList(); // store in list
        
        #if UNITY_EDITOR
        if (octagonArenaSettings != null)
        {
            Debug.Log("Octagon area located.");
        }
        
        // Debug: Check sensor initialization state
        Debug.LogWarning($"[OctagonAgent] Start() called on {gameObject.name} - Frame: {Time.frameCount}");
        var agentType = typeof(Agent);
        var sensorsField = agentType.GetField("sensors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sensorsField != null)
        {
            var sensors = sensorsField.GetValue(this);
            if (sensors != null)
            {
                var sensorsList = sensors as System.Collections.Generic.List<ISensor>;
                Debug.LogWarning($"[OctagonAgent] Start() - sensors count: {sensorsList?.Count ?? -1}");
            }
            else
            {
                Debug.LogWarning($"[OctagonAgent] Start() - sensors is NULL");
            }
        }
        #endif
    }

    // Debug OnEnable/OnDisable - Re-adding to track initialization lifecycle

    /// <summary>
    /// CRITICAL: Must call base.OnEnable() to trigger LazyInitialize()
    /// </summary>
    protected override void OnEnable()
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] OnEnable called on {gameObject.name} - Frame: {Time.frameCount}");
        #endif
        base.OnEnable(); // MUST call this - it calls LazyInitialize()
        
        #if UNITY_EDITOR
        // Check if initialization succeeded
        var agentType = typeof(Agent);
        var initializedField = agentType.GetField("m_Initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (initializedField != null)
        {
            bool isInitialized = (bool)initializedField.GetValue(this);
            Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] After OnEnable - m_Initialized: {isInitialized}");
        }
        #endif
    }

    /// <summary>
    /// Debug: Track when agent is disabled
    /// </summary>
    protected override void OnDisable()
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] OnDisable called on {gameObject.name} - Frame: {Time.frameCount}");
        #endif
        base.OnDisable();
    }

    /// <summary>
    /// Debug: Override EndEpisode to check sensor state before the crash
    /// </summary>
    public new void EndEpisode()
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] EndEpisode called on {gameObject.name} (Tag: {tag}) - Frame: {Time.frameCount} - StepCount: {StepCount}/{MaxStep}");
        
        // Check if agent is initialized
        var agentType = typeof(Agent);
        var initializedField = agentType.GetField("m_Initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (initializedField != null)
        {
            bool isInitialized = (bool)initializedField.GetValue(this);
            Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] Agent m_Initialized: {isInitialized}");
        }
        
        // Use reflection to check the internal sensors list
        var sensorsField = agentType.GetField("sensors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (sensorsField != null)
        {
            var sensors = sensorsField.GetValue(this);
            Debug.LogWarning($"[OctagonAgent] sensors field value: {(sensors == null ? "NULL" : "EXISTS")}");
            
            if (sensors != null)
            {
                var sensorsList = sensors as System.Collections.Generic.List<ISensor>;
                Debug.LogWarning($"[OctagonAgent] sensors list count: {sensorsList?.Count ?? -1}");
                
                if (sensorsList != null && sensorsList.Count > 0)
                {
                    for (int i = 0; i < sensorsList.Count; i++)
                    {
                        Debug.LogWarning($"[OctagonAgent]   Sensor {i}: {sensorsList[i]?.GetName() ?? "NULL"} (hash: {sensorsList[i]?.GetHashCode() ?? 0})");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("[OctagonAgent] Could not access sensors field via reflection");
        }
        
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] About to call base.EndEpisode() - Frame: {Time.frameCount}");
        #endif
        
        base.EndEpisode();
        
        #if UNITY_EDITOR
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] Returned from base.EndEpisode() - Frame: {Time.frameCount}");
        #endif
    }


    // We can put all reset code in OnEpisodeBegins, so that regardless of the reason for
    // EndEpisode, we reset the agent and arena the same way
    // ITI Can be still be called here. I think it's needed to keep the trained task
    // spiritually similar to the human task. However, in the curriculum we can move from
    // low variability and low duration, up to the standard ITI
    public override void OnEpisodeBegin()
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"[OctagonAgent {GetInstanceID()}] OnEpisodeBegin called on {gameObject.name} (Tag: {tag}) - Frame: {Time.frameCount}");
        #endif

        // Reset agent-specific state regardless of whether this is PlayerAgent or OpponentAgent
        totalShapingReward = 0;
        episodeCount++;

        #if UNITY_EDITOR
        Debug.Log($"OnEpisodeBegin - trial looping: {octagonArenaSettings.isTrialLooping}");

        // Why is this necessary?
        Debug.Log("Time scale: " + Time.timeScale);
        #endif
        
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1; // Resume normal time
        }

        // Only PlayerAgent should handle arena-wide setup (trial loop, wall coloring, etc.)
        // OpponentAgent just resets its own state above
        if (!this.CompareTag("PlayerAgent"))
        {
            // NEW: OpponentAgent waits for PlayerAgent's setup to complete before continuing
            #if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] OpponentAgent waiting for arena setup...");
            #endif
            StartCoroutine(WaitForArenaSetup());
            return;
        }

        // PlayerAgent-specific arena setup logic below
        if (!octagonArenaSettings.isTrialLooping)
        {
            #if UNITY_EDITOR
            Debug.Log("PlayerAgent found, starting episode");
            #endif

            // disable wall triggers during ITI
            octagonArenaSettings.DisableTriggers();

            // reset arena by washing off active wall colours
            octagonArenaSettings.ResetTrial();

            // start trial ITI and active wall colouring logic
            #if UNITY_EDITOR
            Debug.Log("Starting coroutine...");
            #endif
            octagonArenaSettings.TrialLoop();
            #if UNITY_EDITOR
            Debug.Log("Coroutine has started");
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.Log("please wait for trial loop to be unlocked");
            Debug.Log($"OnEpisodeBegin, Trial looping - Trial looping: {octagonArenaSettings.isTrialLooping}");
            #endif
        }
        //previousDistanceHigh = Vector3.Distance(transform.position, wall1Trigger.transform.position);
        //previousDistanceLow = Vector3.Distance(transform.position, wall2Trigger.transform.position);

        //Debug.Log($"[OctagonAgent] Agent {this.tag} starts with distance to {previousDistanceHigh} and distance to low {previousDistanceLow}.");

    }

    /// <summary>
    /// NEW: Coroutine that blocks OpponentAgent until arena setup is complete.
    /// Prevents OpponentAgent from observing stale/incorrect wall tags and colors.
    /// This ensures both agents observe the same arena state when episodes begin.
    /// </summary>
    private IEnumerator WaitForArenaSetup()
    {
        #if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] OpponentAgent entering WaitForArenaSetup - isArenaReady: {octagonArenaSettings.isArenaReady}");
        #endif
        
        // Wait until PlayerAgent's TrialLoop() -> ITI() -> StartTrial() -> ColourWalls() completes
        yield return new WaitUntil(() => octagonArenaSettings.isArenaReady);
        
        #if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] OpponentAgent arena setup complete - resuming episode");
        #endif
        // Now safe to observe walls with correct tags/colors and begin acting
    }

    // observations:

    // visual observations are provided via CameraSensor (Agent component) which is assigned a camera (Agent's child)
    // Sensor component collects image information transforming it into a 3D tensor that can be fed into the CNN (in our case: resnet)
    // optionally add active wall flag as vector observation

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        stepCount++;

        // record the current time, position, and reward
        float currentTime = Time.time;
        float posX = transform.position.x;
        float posY = transform.position.y;
        float posZ = transform.position.z;
        float rotY = transform.eulerAngles.y;
        float reward = GetCumulativeReward();

        if (!isTraining)
        {
            // log this step's data to the CSV file
            logWriter.WriteLine($"{episodeCount},{stepCount},{octagonArenaSettings.wallID1},{octagonArenaSettings.wallID2},{currentTime},{posX},{posY},{posZ},{rotY},{reward}");

            // flush the writer to ensure data is written in real-time
            logWriter.Flush();
        }

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

        // Animate agent on movement
        if (animator == null)
        {
            Debug.LogError("Animator is not assigned");
            return;
        }
        animator.SetBool("isRunning", targetDirection.magnitude > 0.05f);

        // step penalty
        // Too small compared to final reward? How frequent is one step, and how long is one trial?
        // Changed to -1e-3f from -1e-4f on 260219 
        AddReward(-1e-3f);


        if (wall1Trigger == null || wall2Trigger == null)
        {
            return;
        }

        Vector3 wall1TriggerCentre = wall1Trigger.transform.position;
        Vector3 wall2TriggerCentre = wall2Trigger.transform.position;

        Vector3 toWall1 = (wall1TriggerCentre - transform.position).normalized;
        Vector3 toWall2 = (wall2TriggerCentre - transform.position).normalized;

        // dot product of agent forward direction vector and direction to each wall
        // dot product of two normalised vectors gives cosine of the angle between them
        // These could be used for shaping rewards based on alignment to walls
        // But we might not need alongside curriculum and curiosity
        float alignmentToWall1 = Vector3.Dot(transform.forward, toWall1);
        float alignmentToWall2 = Vector3.Dot(transform.forward, toWall2);

    }

    // Raycast observations are automatically collected by SelectivePassThroughRaycastSensorComponent
    public override void CollectObservations(VectorSensor sensor)
    {
        /* Removed 260210 to try and address the catastrophic forgetting behaviour when transitioning from 
            Solo to social */
        // // Observe mode (solo vs social)
        // sensor.AddObservation(octagonArenaSettings.soloMode ? 1 : 2);
    }

    // Manual agent control for testing
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

    public void LogTriggerActivation(int triggerID, string wallType, string interactorTag)
    {
        if (logWriter != null && !isTraining)
        {
            float currentTime = Time.time;
            logWriter.WriteLine($"{episodeCount},{stepCount},{currentTime},TriggerActivated,{triggerID},{wallType},{interactorTag}");
            logWriter.Flush();
        }
    }

    public void LogSliceOnsetEvent(int highWall, int lowWall, string trialType)
    {
        if (logWriter != null && !isTraining)
        {
            float currentTime = Time.time;
            logWriter.WriteLine($"{episodeCount},{stepCount},{currentTime},SliceOnset,{highWall},{lowWall},{trialType}");
            logWriter.Flush();
        }
    }

    public void LogEpisodeBeginEvent()
    {
        if (logWriter != null && !isTraining)
        {
            float currentTime = Time.time;
            logWriter.WriteLine($"{episodeCount},{stepCount},{currentTime},EpisodeBegin");
            logWriter.Flush();
        }
    }

    public void LogEpisodeEndEvent()
    {
        if (logWriter != null && !isTraining)
        {
            float currentTime = Time.time;
            logWriter.WriteLine($"{episodeCount},{stepCount},{currentTime},EpisodeEnd");
            logWriter.Flush();
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