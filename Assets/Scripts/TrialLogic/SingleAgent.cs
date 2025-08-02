// dependencies

using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using System.IO;
using System.Linq;

public class SingleAgent : Agent
{
    // scripts

    // assigned in inspector 
    // each arena has its own copy of each script
    [SerializeField] public WallTrigger wallTrigger;
    [SerializeField] public GameManager gameManager;
    [SerializeField] public TrialHandler trialHandler;
    [SerializeField] public TrialLogic trialLogic;
    [SerializeField] public IdentityManager identityManager;

    // variables and objects

    // agent actions
    // adjust agent speeds as appropriate
    public float moveSpeed = 20f;
    public float turnSpeed = 360f;
    public CharacterController controller;

    // octagon arena
    private Transform arenaRoot;

    // wall trigger objects
    public List<GameObject> allWallTriggers;

    // variable to keep track of shaping reward
    public float totalShapingReward;

    // bool flag to check whether agent is currently being trained
    public bool isTraining = false;
    // path to log file where agent data will be saved
    string logPath;
    // StreamWriter instance used to write agent logs to the file
    StreamWriter logWriter;
    public override void Initialize()
    {
        // checks whether communicator (which allows interaction with python process) is on
        // if off, agent is in inference/heuristic mode
        isTraining = Academy.Instance.IsCommunicatorOn;

        // if communicator is off
        if (!isTraining)
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
        // base.Awake() 
        // empty
    }

    void Start()
    {
        // find all wall triggers from the parent of this script (the agent)

        arenaRoot = transform.parent;

        // searches children of the arena (including inactive ones)
        allWallTriggers = arenaRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("WallTrigger")) // children with tag "WallTrigger"
            .Select(t => t.gameObject) // select the associated game object
            .ToList(); // store in list
    }

    public override void OnEpisodeBegin()
    {
        // check that agent currently running this script is "PlayerAgent"
        // is this the agent assigned to playerAgent in TrialHandler? 
        if (this == trialHandler.playerAgent)
        {
            totalShapingReward = 0; // variable to track shaping rewards for agent

            // missing logic
            // is a trial in process
            // scratch random number logic - one trial per episode
        }
    }

    // observations:

    // visual observations are provided via CameraSensor (Agent component) which is assigned a camera (Agent's child)
    // Sensor component collects image information transforming it into a 3D tensor that can be fed into the CNN (in our case: resnet)
    // optionally add active wall flag as vector observation
}