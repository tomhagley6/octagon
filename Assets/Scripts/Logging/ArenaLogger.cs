using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEngine;
using LoggingClasses;
using Globals;


public class ArenaLogger : MonoBehaviour
{
    [SerializeField] private OctagonArenaSettings octagonArenaSettings;
    [SerializeField] private DiskLogger diskLogger;
    [SerializeField] private bool enableTimeTriggered = true;
    [SerializeField] private bool logStartStopEvents = true;
    //private Coroutine timeCoroutine;
    private const ulong PlayerKey = 0UL;
    private const ulong OpponentKey = 1UL;
    private bool timeTriggeredActive = false;
    private double nextLogTime = 0.0;

    private void Awake()
    /// Ensures that necessary components are assigned,
    /// either through the inspector or by finding them in the scene.
    /// Logs errors if components are missing.
    {
        if (octagonArenaSettings == null)
        {
            octagonArenaSettings = FindObjectOfType<OctagonArenaSettings>();
        }

        if (diskLogger == null)
        {
            diskLogger = FindObjectOfType<DiskLogger>();
        }

        if (octagonArenaSettings == null)
        {
            Debug.LogError("[ArenaLogger] OctagonArenaSettings not found in the scene.");
        }

        if (diskLogger == null)
        {
            Debug.LogError("[ArenaLogger] DiskLogger not found in the scene.");
        }
    }

    private void OnEnable()
    /// Subscribes to relevant events from OctagonArenaSettings and DiskLogger.
    {
        if (octagonArenaSettings == null || diskLogger == null) return;

        octagonArenaSettings.SliceOnset += OnSliceOnset;
        octagonArenaSettings.TrialActiveChanged += OnTrialActiveChanged;

        if (logStartStopEvents)
        {
            diskLogger.loggingStarted += OnLoggingStarted;
            diskLogger.loggingEnded += OnLoggingEnded;

            if (diskLogger.isRunning)
            {
                OnLoggingStarted();
            }
        }
    }

    private void OnDisable()
    /// Unsubscribes from events to prevent memory leaks and unintended behavior.
    {
        if (octagonArenaSettings != null)
        {
            octagonArenaSettings.SliceOnset -= OnSliceOnset;
            octagonArenaSettings.TrialActiveChanged -= OnTrialActiveChanged;
        }

        if (diskLogger != null && logStartStopEvents)
        {
            diskLogger.loggingStarted -= OnLoggingStarted;
            diskLogger.loggingEnded -= OnLoggingEnded;
        }

        StopTimeTriggered();
    }

    // -----------------------
    // Logging event handlers
    // -----------------------

    private double CurrentApplicationTime()
    {
        return Time.timeAsDouble;
    }

    private void OnLoggingStarted()
    /// Logs the first line stating that logging has started
    {
        Write(new StartLoggingLogEvent(CurrentApplicationTime()));
        if (enableTimeTriggered) StartTimeTriggered();
    }

    private void OnLoggingEnded()
    /// Logs the last line stating that logging has ended
    {
        StopTimeTriggered();
        Write(new StopLoggingLogEvent(CurrentApplicationTime()));
    }

    // ---------------------
    // Trial event handlers
    // ---------------------

    private void OnTrialActiveChanged(bool prevVal, bool newVal)
    /// If TrialActiveChanged boolean value changes from False to True trial start logging method is called.
    /// If boolean changes from True to False trial end logging method is called.
    /// If boolean does not change, no log event is triggered.
    {
        if (prevVal == newVal) return;

        if (!prevVal && newVal) LogTrialStart();
        if (prevVal && !newVal) LogTrialEnd();
    }

    private void LogTrialStart()
    /// Method called on False->True change in trial active bool.
    /// Logs a trial start event, including details about the trial number, the trial type, and player position.
    {
        if (!IsReady()) return;

        // Ensure trialNum increments exactly once per false->true transition
        octagonArenaSettings.IncrementTrialNum();

        ushort trialNum = octagonArenaSettings.trialNum;
        var trialType = new FixedString32Bytes(octagonArenaSettings.thisTrialType);

        var playerPosDict = BuildPlayerPosDict();

        var ev = new TrialStartLogEvent(trialNum, trialType, playerPosDict, CurrentApplicationTime());
        Write(ev);
    }

    private void OnSliceOnset()
    /// Method called when active walls are first coloured for each trial.
    /// Logs a slice onset event, including details about the walls active, the trial type, and player positions.
    {
        if (!IsReady()) return;
        
        int wall1 = octagonArenaSettings.activeWalls.wall1;
        int wall2 = octagonArenaSettings.activeWalls.wall2;

        var trialType = new FixedString32Bytes(octagonArenaSettings.thisTrialType);
        var playerPosDict = BuildPlayerPosDict();

        var ev = new SliceOnsetLogEvent(wall1, wall2, trialType, playerPosDict, CurrentApplicationTime());
        Write(ev);
    }

    public void LogTriggerActivation(int wallTriggered, OctagonAgent activator, bool authorised = true)
    /// Method called when an active wall is triggered.
    /// Logs a trigger activation event, including details about the walls involved, the activator, and player positions.
    {
        if (!IsReady()) return;

        if (!authorised) return;

        int wall1 = octagonArenaSettings.activeWalls.wall1;
        int wall2 = octagonArenaSettings.activeWalls.wall2;

        ulong triggerClientId = GetStableAgentKey(activator);

        var playerPosDict = BuildPlayerPosDict();

        var ev = new TriggerActivationLogEvent(wall1, wall2, wallTriggered, triggerClientId, playerPosDict, CurrentApplicationTime())
        {
            eventDescription = Logging.triggerActivationAuthorised
        };

        Write(ev);
    }

    private void LogTrialEnd()
    /// Method called on True->False change in trial active bool.
    /// Logs a trial end event, including details about the trial number, player position, and player score.
    {
        if (!IsReady()) return;

        ushort trialNum = octagonArenaSettings.trialNum;
        var playerPosDict = BuildPlayerPosDict();

        // Match existing schema: playerScores is a dictionary keyed like players.
        // If you don't have a "score", use cumulative reward (still numeric).
        var playerScoresDict = BuildPlayerScoresDict();

        var ev = new TrialEndLogEvent(trialNum, playerPosDict, playerScoresDict, CurrentApplicationTime());
        Write(ev);
    }

    // ---------------------
    // Time-triggered
    // ---------------------

    private void StartTimeTriggered()
    /// Called at the start of the logging process
    /// Initiates the player position and time logging at chosen frequency
    {
        //if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        //timeCoroutine = StartCoroutine(TimeTriggeredLoop());
        timeTriggeredActive = true;
        nextLogTime = Time.timeAsDouble;
    }
    
    private void StopTimeTriggered()
    /// Called at the end of the logging process
    /// Concludes the frequent logging of player position and time
    {
        //if (timeCoroutine != null)
        //{
        //    StopCoroutine(timeCoroutine);
        //    timeCoroutine = null;
        //}
        timeTriggeredActive = false;
    }

    private void FixedUpdate()
    /// Alternative to coroutine for time-triggered logging using FixedUpdate
    {
        if (!timeTriggeredActive) return;
        if (!IsReady()) return;

        while (Time.timeAsDouble + 1e-9f >= nextLogTime)
        {
            var playerPosDict = BuildPlayerPosDict();
            var ev = new TimeTriggeredLogEvent(playerPosDict, nextLogTime);
            Write(ev);

            nextLogTime += Logging.loggingFrequency; // 0.02
        }
    }

    //private IEnumerator TimeTriggeredLoop()
    ///// Logs player position and time at chosen frequency
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(Logging.loggingFrequency);

    //        if (!IsReady()) continue;

    //        var playerPosDict = BuildPlayerPosDict();
    //        var ev = new TimeTriggeredLogEvent(playerPosDict);
    //        Write(ev);
    //    }
    //}
    // -------------------------
    // Snapshot helpers (stable keys)
    // -------------------------

    private Dictionary<string, object> BuildPlayerPosDict()
    /// Dictionary of player and opponent positions
    {
        var dict = new Dictionary<string, object>();

        // Always add PlayerAgent first as key "0"
        var player = octagonArenaSettings.playerAgent;
        if (player != null)
            dict[PlayerKey.ToString()] = BuildPlayerPosition(PlayerKey, player);

        // Add OpponentAgent second as key "1" (if present)
        if (!octagonArenaSettings.soloMode && octagonArenaSettings.opponentAgent != null)
            dict[OpponentKey.ToString()] = BuildPlayerPosition(OpponentKey, octagonArenaSettings.opponentAgent);

        return dict;
    }

    private PlayerPosition BuildPlayerPosition(ulong stableId, OctagonAgent agent)
    /// Gets agent position from the agent object
    {
        Vector3 loc = agent.transform.position;
        Vector3 bodyEuler = agent.transform.rotation.eulerAngles;

        float camX = 0f, camZ = 0f;
        var cam = agent.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            camX = cam.transform.rotation.eulerAngles.x;
            camZ = cam.transform.rotation.eulerAngles.z;
        }

        var playerLocation = new PlayerLocation(loc.x, loc.y, loc.z);
        var playerRotation = new PlayerRotation(camX, bodyEuler.y, camZ);
        return new PlayerPosition(stableId, playerLocation, playerRotation);
    }

    private Dictionary<string, object> BuildPlayerScoresDict()
    /// Dictionary of player and opponents scores
    {
        var dict = new Dictionary<string, object>();

        // Must match your analysis expectations: keys "0" and "1"
        //dict["0"] = octagonArenaSettings.PlayerTrialScore;
        dict["0"] = octagonArenaSettings.PlayerCumulativeScore;

        if (!octagonArenaSettings.soloMode)
            //dict["1"] = octagonArenaSettings.OpponentTrialScore;
            dict["1"] = octagonArenaSettings.OpponentCumulativeScore;

        return dict;
    }

    private ulong GetStableAgentKey(OctagonAgent agent)
    /// Maps runtime agent instances to fixed ulong keys used in logging,
    /// ensuring consistency across all log events regardless of object instance IDs
    /// or scene reloads.
    {
        if (agent == null) return PlayerKey;

        if (agent.CompareTag("OpponentAgent")) return OpponentKey;
        return PlayerKey;
    }

    private bool IsReady()
    /// Boolean value to check that the objects required for logging are present in the scene (octagonArenaSettings, diskLogger, playerAgent)
    {
        return octagonArenaSettings != null
               && diskLogger != null
               && octagonArenaSettings.playerAgent != null;
    }

    private void Write(object logEvent)
    /// Writes the logs in json format
    {
        string json = JsonConvert.SerializeObject(logEvent, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        diskLogger.Log(json);
    }
}