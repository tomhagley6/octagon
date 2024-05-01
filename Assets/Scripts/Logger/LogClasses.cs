using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace LoggingClasses
{
    
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

    }

    public class SliceOnsetLogEvent
    {
        // time
        // description string
        // wall numbers
        // positions
        // head angles
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