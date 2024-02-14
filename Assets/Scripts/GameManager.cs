using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


// Central logic for Octagon Prototype
// Includes functions for starting trials and ending trials
public class GameManager : MonoBehaviour
{


    public GameObject Player;
    public TextMeshProUGUI scoreText;
    public int score;
    public List<int> activeWalls;

    List<int> walls;
    IdentityManager identityManager;
    Color defaultWallColor;


    void Start()
    {
        identityManager = FindObjectOfType<IdentityManager>(); 
        identityManager.OrderDictionary();  // Call to order dictionary after it has been populated
        StartTrial();   // Begin sequential trial logic

    }

    void ColourWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID); 
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject;

        // Save the current colour of the wall before overwriting it 
        defaultWallColor = highWall.GetComponent<Renderer>().material.color;

        // Assign colours to the walls that fit their rewards
        highWall.GetComponent<Renderer>().material.color = Color.red;
        lowWall.GetComponent<Renderer>().material.color = Color.blue;
    }

    void WashWalls(int highWallTriggerID, int lowWallTriggerID)
    {
        // Access the actual game object through the ID:GameObject dict in IdentityManager
        GameObject highWallTrigger = identityManager.GetObjectByIdentifier(highWallTriggerID);
        GameObject lowWallTrigger = identityManager.GetObjectByIdentifier(lowWallTriggerID);

        // Get the (parent) octagon wall of each trigger
        GameObject highWall = highWallTrigger.transform.parent.gameObject;
        GameObject lowWall = lowWallTrigger.transform.parent.gameObject; 

        // Reset wall colours back to their previously-saved defaults
        highWall.GetComponent<Renderer>().material.color = defaultWallColor; 
        lowWall.GetComponent<Renderer>().material.color = defaultWallColor;   
    }
    void RemoveAgency()
    {
        Debug.Log("Trigger entered");
        Player.GetComponent<CharacterController>().enabled = false;
    }

    public void AdjustScore(int increment = 0)
    {
        score += increment;

    }

    // Prepare for initiation of a new trial
    void ResetTrial(int highWallTriggerID, int lowWallTriggerID)
    {
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //// Currently not using player position reset
        // Player.transform.position = new Vector3(-80.65f, 2.76f, 44.17f);

        // clear the active walls list
        activeWalls.Clear(); 
        // reset wall colours
        WashWalls(highWallTriggerID, lowWallTriggerID);
        
        // Halt player movement very briefly while the trial resets (contributes to visual feedback
        // of the trial ending)
        // Asynchronous method which will run without interrupting the main thread of the program
        // This is important, to allow waiting for 0.3 seconds without pausing other program functions
        IEnumerator TrialResetImmobility()
        {

            yield return new WaitForSeconds(0.3f);

            // Re-enable player movement after it was disabled in EndTrial()
            Player.GetComponent<CharacterController>().enabled = true;
        }

        /// Run the above-defined coroutine
        StartCoroutine(TrialResetImmobility());


    }
    

    // Unreferenced attempt to pause the game. Superceded by ResetTrial's coroutine
    public void StartPause(float pauseTime)
    {
        // how many seconds to pause the game
        StartCoroutine(PauseGame(pauseTime));
    }
   public IEnumerator PauseGame(float pauseTime)
   {
        Debug.Log ("Inside PauseGame()");
        Time.timeScale = 0f;
        float pauseEndTime = Time.realtimeSinceStartup + pauseTime;
        while (Time.realtimeSinceStartup < pauseEndTime)
        {
            yield return 0;
        }
        Time.timeScale = 1f;
        Debug.Log("Done with my pause");
   }

   void Test()
   {
    Debug.Log("Hello, world");
   }

    // Define and run requirements for a new trial
    // Triggered by GameManager on startup, and again by
    void StartTrial()
    {


        /// Start ITI (range from 2 to 5 seconds randomly)
        /// Pause movement control
        // Generate a random float between 2 and 5 seconds
        // float pauseTime = UnityEngine.Random.Range(2f, 5f);
        // StartPause(pauseTime);

        /// Start ITI (range from 2 to 5 seconds randomly)
        // Pause code block execution (while allowing other scripts to continue) by running "Invoke"
        // on an empty function, delayed by a random float between 2 and 5 seconds. 
        Invoke("Test", UnityEngine.Random.Range(2f,5f));
    

        /// Generate wall trigger IDs for a new trial
        /// currently hard coded for testing
        walls = identityManager.ListCustomIDs();

        activeWalls.Clear();

        // Choose a random wall to reference the trial to 
        int anchorWallIndex = UnityEngine.Random.Range(0, walls.Count);
        // choose a random second wall that is consistent with anchor wall for this trial type
        int wallIndexDiff = new List<int>{-2, 2}[UnityEngine.Random.Range(0, 1)];
        int dependentWallIndex = anchorWallIndex + wallIndexDiff;
        // Account for circular octagon structure
        if (dependentWallIndex < 0)
        {
            dependentWallIndex += walls.Count;
        }
        else if (dependentWallIndex >= walls.Count - 1)
        {
            dependentWallIndex -= walls.Count;
        }
        
        // assign high and low walls with the generated indexes
        int highWallTriggerID = walls[anchorWallIndex];
        int lowWallTriggerID = walls[dependentWallIndex];


        Debug.Log("Number of walls: " + walls.Count);
        for (int i = 0; i < walls.Count; i++)
        {
            Debug.Log("Wall number " + i + " has the ID: " + walls[i]);
        }



        // int highWallTriggerID = walls[0];
        // int lowWallTriggerID = walls[1];

        Debug.Log("high wall is: " + highWallTriggerID);

        // activeWalls.Add(highWallTriggerID);
        // activeWalls.Add(lowWallTriggerID);

        // GameObject[] wallTriggers = GameObject.FindGameObjectsWithTag("WallTrigger");
        // foreach (GameObject wallTrigger in wallTriggers)
        // {
        //     activeWalls.Add(wallTrigger.GetInstanceID());
        // }

        // Order in this list decides which is High and Low
        activeWalls.Add(highWallTriggerID);
        activeWalls.Add(lowWallTriggerID);

        // Add colour to the parent walls of each trigger
        ColourWalls(highWallTriggerID, lowWallTriggerID);

    }

    public void EndTrial(int increment, int highWallTriggerID, int lowWallTriggerID)
    {
        // Halt movement
        RemoveAgency();

        // Adjust score
        // Score.cs accesses the score here to display to the Canvas
        AdjustScore(increment);

        // reset position and walls
        ResetTrial(highWallTriggerID, lowWallTriggerID);

        // PauseGame(UnityEngine.Random.Range(2f,5f));
        Invoke("StartTrial", UnityEngine.Random.Range(2f,5f));

        // create new walls
        // StartTrial();

    }

}
