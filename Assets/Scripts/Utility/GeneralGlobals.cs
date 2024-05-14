
using UnityEngine;

namespace Globals
{
    public static class General
    {

        // // vals

        // trial logic
        public static int highScore = 50;
        public static int lowScore = 10;
        public static string trialType = "HighLowTrial";

        //timings
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

    }
}