using System.Collections.Generic;
using UnityEngine;


// Logging namespace for Global values associated with logging formats
// and networking values
namespace Globals
{
    public static class Logging
    {
        
        // vals
        public static float loggingFrequency = 0.02f; // currently 50 fps
        
        
        // initialisers
        public static int port = 0000;
        public static string ip = "ip_here";

        // formatting strings
        public static string fileTimeFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss";
        
        // // Example code for creating the txt file path
        // filename = String.Concat(DateTime.Now.ToString(fileTimeFormat), ".txt");
        // Debug.Log("Logger created. Filename: " + filename);
        // fullPath = Path.Combine(folderName, filename);

        public static string logTimeFormat = "HH:mm:ss:fff";

        public static string logFormat = "T{0}:UT {1} {2}"; // timestamp, unitytimestamp, 
                                                               // content


        // // Example code for creating the log
        //         public void AddLogEntry(string log)
        // {
        //     if (loggerReady)
        //     {
        //         string toLog = String.Format(entryFormat, DateTime.Now.ToString(logEntryTimeFormat), UnityEngine.Time.time.ToString("f3"), log);
        //         lock (fileEntries)
        //         {
        //             fileEntries.Add(toLog);
        //         }
        //     }

        // }
        

        // paths
        public static string logFolder = "/home/tom/Unity/data";

        // log formatting
        public static string logEntryFormat = "[{0}]:{1}";  // [tag]:data
        public static string posFormat = "[{0}] {1} {2} {3}"; // tag, player, x, y
        public static string wallTriggerFormat = "[{0}] {1} {2} {3} {4} {5} {6}"; // tag, trial_num,
                                                                                // trail_type, wall_num,
                                                                                // reward_type,
                                                                                // reward_val
                                                                                // IsLocalPlayer

        // // Example logging method usage
        //MiceUILogger.Instance.AddLogEntry(string.Format(Globals.LOG_CONTENT_FORMAT, 
        // Globals.LOG_ID_YIN, String.Format("{0} {1} {2} {3} {4} {5} {6} {7}", "trialNum", 
        // "trialType", "Y_num", "Y_ID", "prev_Y", "orientation", "rewLocation", "rewPresent")));

        
        // tags
        public static string posTag = "pos";
        public static string wallTriggerTag = "wall";
        public static string beginLogging = "logging start";
        public static string trialStart = "trial start";
        public static string sliceOnset = "slice onset";
        public static string triggerActivation = "trigger activation";
        public static string trialEnd = "trial end";
        public static string endLogging = "logging end";
        public static string timeTriggered = "time";

    }

}
