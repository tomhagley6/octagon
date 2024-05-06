using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Logging;

namespace LoggingClasses
{

    // helper data types

    // Helper class for assembling general player info
    public class PlayerInfo
    {
        // player id
        // player transform.position
        // player transform.rotation
        public ulong clientId;
        public PlayerPosition position;
        public PlayerRotation rotation;

        public PlayerInfo(ulong _clientId, PlayerPosition _position, PlayerRotation _rotation)
        {
            clientId = _clientId;
            position = _position;
            rotation = _rotation;
        }
    }

    // Helper class to allow logging of only Transform.position x/y/z values
    public class PlayerPosition 
    {
        // player.position.x/y/z
        public double x;
        public double y;
        public double z;

        public PlayerPosition(double _x, double _y, double _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
        
    }

    // Helper class to allow logging of only Transform.rotation euler angles
    public class PlayerRotation
    {
        // player.rotation.eulerangles.x/y/z
        public double x;
        public double y;
        public double z;

        public PlayerRotation(double _x, double _y, double _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
    }
    
    //// Create log classes
    
    // Log event for the beginning of the log file
    public class StartLoggingLogEvent
    {
        // time
        // description string
        public string timeLocal;
        public string timeApplication;
        public string eventDescription;

        public StartLoggingLogEvent()
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.beginLogging;
        }
    }

    // Log event for every trial start 
    // immediately post-ITI, signalled by increased global illumination
    public class TrialStartLogEvent
    {
        // time
        // description string
        // positions
        // head angles
        public string timeLocal;
        public string timeApplication;
        public string eventDescription;
        public Dictionary<string, object> data;

        public TrialStartLogEvent(ushort trialNum)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.trialStart;
            data = new Dictionary<string, object>()
            {
                {"trialNum", trialNum}
            };
        }

    }


    // Log event for every trial start, triggered by execution of ColourWalls on the server
    public class SliceOnsetLogEvent
    {

        /* For slice onset, 'data' dictionary includes all active wall numbers,
        and the player position and rotation for each player
        This is done by having data contain a dictionary of PlayerInfo dictionaries, 
        each associated with their respective clientIds, and containing position and rotation
        The dictionaries may not need a clientId field, if I'm adding them to the playerInfoDict
        with clientId as the key. */
        
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
            eventDescription = Globals.sliceOnset;
            data = new Dictionary<string, object>()

            {
                {"wall1", wall1},
                {"wall2", wall2},
                {"playerInformation", playerInfoDict}
            };
        }
    }

    // Log event for activation of a trigger, as controlled by a change in the
    // TriggerActivation NetworkVariable
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

    // or 'ITIStartLogEvent', which is better?
    public class EndTrialLogEvent
    {
        // necessary? 
    }

}