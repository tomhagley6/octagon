using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Logging;

namespace LoggingClasses
{

    // helper data types
    public class PlayerInfo
    {
        // player id
        // player transform.position
        // player transform.rotation
        public ulong clientId;
        public Vector3 position;
        public Quaternion rotation;

        public PlayerInfo(ulong _clientId, Vector3 _position, Quaternion _rotation)
        {
            clientId = _clientId;
            position = _position;
            rotation = _rotation;
        }
    }
    
    // Create log classes
    [Serializable]
    public class StartLogEvent
    {
        // time
        public string Description { get; set; }

        public StartLogEvent(string description)
        {
            Description = description;
        }
    }

    public class TrialStartLogEvent
    {
        // time
        // description string
        // positions
        // head angles
        public string timeLocal;
        public string timeApplication;
        public Dictionary<string, object> data;

        public TrialStartLogEvent(ushort trialNum)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            data = new Dictionary<string, object>()
            {
                {"trialNum", trialNum}
            };
        }

    }

    // For slice onset, 'data' dictionary includes all active wall numbers,
    // and the player position and rotation for each player
    // This is done by having data contain a dictionary of PlayerInfo dictionaries, 
    // each associated with their respective clientIds, and containing position and rotation
    // The dictionaries may not need a clientId field, if I'm adding them to the playerInfoDict
    // with clientId as the key.
    // Trigger the logging on slice onset information when ActiveWalls change, and only on the Server.
    public class SliceOnsetLogEvent
    {
        // time
        // description string
        // wall numbers
        // positions
        // head angles
        public string timeLocal;
        public string timeApplication;
        public string eventDescription;
        public Dictionary<string, object> data;

        public SliceOnsetLogEvent(int wall1, int wall2, Dictionary<string,object> playerInfoDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = "slice onset";
            data = new Dictionary<string, object>()

            {
                {"wall1", wall1},
                {"wall2", wall2},
                {"playerInformation", playerInfoDict}
            };
        }
    }

    public class TriggerActivationLogEvent
    {
        // time 
        // description string
        // wall numbers
        // wall number activated
        // positions
        // head angles
        // winner 
    }

    public class ITIStartLogEvent
    {
        // necessary? 
    }

}