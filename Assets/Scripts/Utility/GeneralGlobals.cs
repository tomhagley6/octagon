
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Globals
{
    public static class General
    {

        // // vals

        // trial logic
        public static int highScore = 50;
        public static int lowScore = 10;
        public static string highScoreRewardType = "High";
        public static string lowScoreRewardType = "Low";
        public static List<string> trialTypes = new List<string>{"HighLow", "ForcedHigh", "ForcedLow"};
        public static List<int> trialTypeProbabilities = new List<int>{80, 10, 10};
        public static List<int> wallSeparations = new List<int>{1,2,4};   // index difference between trial walls
                                                                          // random choice within list
        public static Color wallHighColour = Color.red;
        public static Color wallLowColour = Color.blue;

        // timings
        public static float ITIMin = 2f;
        public static float ITIMax = 5f;
        public static float trialEndDuration = 2f;
        public static float trialStartDurationMin = 0.5f;
        public static float trialStartDurationMax = 1.5f; 
        public static float startFirstTrialDelay = 0.75f;
        public static float globalIlluminationLow = 0.6f;
        public static float globalIlluminationHigh = 0.8f;


        // movement
        public static float mouseSensitivity = 1000f;
        public static float neckClampMin = -60f;
        public static float neckClampMax = 90f;

        // leaderboards
        public static string leaderboardId = "Octagon";
        
        // keys
        public static KeyCode toggleMouse = KeyCode.F1;
        public static KeyCode toggleCamera = KeyCode.F2;
        public static KeyCode toggleLeaderboards = KeyCode.F3;
        public static KeyCode togglePlayer = KeyCode.F4;
        public static KeyCode toggleIP = KeyCode.F5;

        // Fixedstring values
        public static FixedString32Bytes highLow = new("HighLow");
        public static FixedString32Bytes forcedHigh = new("ForcedHigh");
        public static FixedString32Bytes forcedLow = new("ForcedLow");

    }
}