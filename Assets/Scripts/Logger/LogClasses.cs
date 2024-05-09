using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Logging;
using Unity.Netcode;

namespace LoggingClasses
{


    

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

        public TrialStartLogEvent(ushort trialNum, Dictionary<string,object> playerPosDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.trialStart;
            data = new Dictionary<string, object>()
            {
                {"trialNum", trialNum},
                {"playerInfo", playerPosDict}
            };
        }

    }


    // Log event for every trial start, triggered by execution of ColourWalls on the server
    public class SliceOnsetLogEvent
    {

        /* For slice onset, 'data' dictionary includes all active wall numbers,
        and the player position and rotation for each player
        This is done by having data contain a dictionary of PlayerPosition dictionaries, 
        each associated with their respective clientIds, and containing position and rotation
        The dictionaries may not need a clientId field, if I'm adding them to the playerPosDict
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

        public SliceOnsetLogEvent(int wall1, int wall2, Dictionary<string,object> playerPosDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.sliceOnset;
            data = new Dictionary<string, object>()

            {
                {"wall1", wall1},
                {"wall2", wall2},
                {"playerPosition", playerPosDict}
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
        // trigger activating client 
        public string timeLocal;
        public string timeApplication;
        public string eventDescription;
        public Dictionary<string, object> data;

        public TriggerActivationLogEvent(int wall1, int wall2, int wallTriggered, ulong triggerClientId, Dictionary<string,object> playerPosDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.triggerActivation;
            data = new Dictionary<string, object>()
            {
                {"wall1", wall1},
                {"wall2", wall2},
                {"wallTriggered", wallTriggered},
                {"triggerClient", triggerClientId},
                {"playerPosition", playerPosDict}
            };
        }

    }

    // or 'ITIStartLogEvent', which is better?
    // These are identical if I have a short 'trial outcome' epoch at the end 
    // Otherwise, these are both irrelevant as ITI should start within milliseconds of
    // trigger activation
    public class TrialEndLogEvent
    {
        // time
        // description string
        // positions
        // head angles
        // current scores
        // score changes(?) <-- this could just be done in post I think 

        public string timeLocal;
        public string timeApplication;
        public string eventDescription;
        public Dictionary<string, object> data;

        public TrialEndLogEvent(ushort trialNum, Dictionary<string, object> playerPosDict, Dictionary<string,object> playerScoresDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.endTrial;
            data = new Dictionary<string, object>()
            {   
                {"trialNum", trialNum},
                {"playerPosition", playerPosDict},
                {"playerScores", playerScoresDict}

            };
        }
    }

   public class TimeTriggeredLogEvent
   {
        // time
        // description string
        // positions
        // head angles

        public string timeLocal;
        public string timeApplication;
        public string eventDescription;
        public Dictionary<string,object> data;

        public TimeTriggeredLogEvent(Dictionary<string,object> playerPosDict)
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.timeTriggered;
            data = new Dictionary<string,object>()
            {
                {"playerPosition", playerPosDict}
            };
        }

   }
   
    public class StopLoggingLogEvent
    {
        // time
        // description string
        public string timeLocal;
        public string timeApplication;
        public string eventDescription;

        public StopLoggingLogEvent()
        {
            timeLocal = DateTime.Now.ToString(Globals.logTimeFormat);
            timeApplication = Time.realtimeSinceStartupAsDouble.ToString("f3");
            eventDescription = Globals.endLogging;
        }
    }

    // // helper classes
    
    // Helper class for assembling general player info
    public class PlayerPosition
    {
        // player id
        // player transform.position
        // player transform.rotation
        public ulong clientId;
        public PlayerLocation location;
        public PlayerRotation rotation;

        public PlayerPosition(ulong _clientId, PlayerLocation _location, PlayerRotation _rotation)
        {
            clientId = _clientId;
            location = _location;
            rotation = _rotation;
        }
    }

    // Helper class to allow logging of only Transform.position x/y/z values
    public class PlayerLocation 
    {
        // player.position.x/y/z
        public double x;
        public double y;
        public double z;

        public PlayerLocation(double _x, double _y, double _z)
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

}