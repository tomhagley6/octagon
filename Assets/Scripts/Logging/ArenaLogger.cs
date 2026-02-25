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
    [Header("References (auto-filled if null)")]
    [SerializeField] private OctagonArenaSettings octagonArenaSettings;
    [SerializeField] private DiskLogger diskLogger;

    [Header("Options")]
    [SerializeField] private bool enableTimeTriggered = true;
    [SerializeField] private bool logStartStopEvents = true;

    private Coroutine timeCoroutine;

    private const ulong PlayerKey = 0UL;
    private const ulong OpponentKey = 1UL;

    private void Awake()
    {
        if (octagonArenaSettings == null)
            octagonArenaSettings = GetComponentInChildren<OctagonArenaSettings>();

        if (diskLogger == null)
            diskLogger = FindObjectOfType<DiskLogger>();

        if (octagonArenaSettings == null)
            Debug.LogError("[ArenaLogger] OctagonArenaSettings not found.");

        if (diskLogger == null)
            Debug.LogError("[ArenaLogger] DiskLogger not found in scene.");
    }

    private void OnEnable()
    {
        if (octagonArenaSettings == null || diskLogger == null) return;

        octagonArenaSettings.SliceOnset += OnSliceOnset;
        octagonArenaSettings.TrialActiveChanged += OnTrialActiveChanged;

        if (logStartStopEvents)
        {
            diskLogger.loggingStarted += OnLoggingStarted;
            diskLogger.loggingEnded += OnLoggingEnded;

            if (diskLogger.IsRunning)
                OnLoggingStarted();
        }
    }

    private void OnDisable()
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

    // -------------------------
    // Public API (call from OctagonWallTrigger)
    // -------------------------

    public void LogTriggerActivation(int wallTriggered, OctagonAgent activator, bool authorised = true)
    {
        if (!IsReady()) return;

        if (!authorised) return;

        int wall1 = octagonArenaSettings.activeWalls.wall1;
        int wall2 = octagonArenaSettings.activeWalls.wall2;

        ulong triggerClientId = GetStableAgentKey(activator);

        var playerPosDict = BuildPlayerPosDict();

        var ev = new TriggerActivationLogEvent(wall1, wall2, wallTriggered, triggerClientId, playerPosDict)
        {
            eventDescription = Logging.triggerActivationAuthorised
        };

        Write(ev);
    }

    // -------------------------
    // Event handlers
    // -------------------------

    private void OnLoggingStarted()
    {
        Write(new StartLoggingLogEvent());
        if (enableTimeTriggered) StartTimeTriggered();
    }

    private void OnLoggingEnded()
    {
        StopTimeTriggered();
        Write(new StopLoggingLogEvent());
    }

    private void OnTrialActiveChanged(bool prevVal, bool newVal)
    {
        if (prevVal == newVal) return;

        if (!prevVal && newVal) LogTrialStart();
        if (prevVal && !newVal) LogTrialEnd();
    }

    private void OnSliceOnset()
    {
        if (!IsReady()) return;

        int wall1 = octagonArenaSettings.activeWalls.wall1;
        int wall2 = octagonArenaSettings.activeWalls.wall2;

        var trialType = new FixedString32Bytes(octagonArenaSettings.thisTrialType);
        var playerPosDict = BuildPlayerPosDict();

        var ev = new SliceOnsetLogEvent(wall1, wall2, trialType, playerPosDict);
        Write(ev);
    }

    // -------------------------
    // Core log methods
    // -------------------------

    private void LogTrialStart()
    {
        if (!IsReady()) return;

        // Ensure trialNum increments exactly once per false->true transition
        octagonArenaSettings.IncrementTrialNum();

        ushort trialNum = octagonArenaSettings.trialNum;
        var trialType = new FixedString32Bytes(octagonArenaSettings.thisTrialType);

        var playerPosDict = BuildPlayerPosDict();

        var ev = new TrialStartLogEvent(trialNum, trialType, playerPosDict);
        Write(ev);
    }

    private void LogTrialEnd()
    {
        if (!IsReady()) return;

        ushort trialNum = octagonArenaSettings.trialNum;
        var playerPosDict = BuildPlayerPosDict();

        // Match existing schema: playerScores is a dictionary keyed like players.
        // If you don't have a "score", use cumulative reward (still numeric).
        var playerScoresDict = BuildPlayerScoresDict();

        var ev = new TrialEndLogEvent(trialNum, playerPosDict, playerScoresDict);
        Write(ev);
    }

    // -------------------------
    // Time-triggered
    // -------------------------

    private void StartTimeTriggered()
    {
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(TimeTriggeredLoop());
    }

    private void StopTimeTriggered()
    {
        if (timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            timeCoroutine = null;
        }
    }

    private IEnumerator TimeTriggeredLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Logging.loggingFrequency);

            if (!IsReady()) continue;

            var playerPosDict = BuildPlayerPosDict();
            var ev = new TimeTriggeredLogEvent(playerPosDict);
            Write(ev);
        }
    }

    // -------------------------
    // Snapshot helpers (stable keys)
    // -------------------------

    private Dictionary<string, object> BuildPlayerPosDict()
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
    {
        var dict = new Dictionary<string, object>();

        // Must match your analysis expectations: keys "0" and "1"
        dict["0"] = octagonArenaSettings.PlayerTrialScore;

        if (!octagonArenaSettings.soloMode)
            dict["1"] = octagonArenaSettings.OpponentTrialScore;

        return dict;
    }

    private ulong GetStableAgentKey(OctagonAgent agent)
    {
        if (agent == null) return PlayerKey;

        // Prefer tags because you already use them reliably
        if (agent.CompareTag("OpponentAgent")) return OpponentKey;
        return PlayerKey;
    }

    private bool IsReady()
    {
        return octagonArenaSettings != null
               && diskLogger != null
               && octagonArenaSettings.playerAgent != null;
    }

    private void Write(object logEvent)
    {
        string json = JsonConvert.SerializeObject(logEvent, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        diskLogger.Log(json);
    }
}
