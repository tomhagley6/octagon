using System.Collections.Generic;
using UnityEngine;


// Pass in the current ordered active triggers and the current trial type (controlled by GameController)
// Then, use the trial type to decide how the trigger should respond to activation
// Also consider which trigger instance has been activated on trigger entry
public class WallTrigger : MonoBehaviour
{
    public GameManager gameManager; 
    public string trialType = "HighLowTrial"; // replace with global?
    public int highScore = 50; // globals
    public int lowScore = 25; // globals
    IdentityAssignment identityAssignment;
    
    void Start() 
    {
        // GetComponent to return the IdentityAssignment instance for the current GameObject
        // FindObjectOfType for GameManager as there is a single instance per scene
        identityAssignment = gameObject.GetComponent<IdentityAssignment>(); 
        gameManager = FindObjectOfType<GameManager>();
    }
    
    // Method that runs when a Trigger is entered
    // No need to explicitly reference
    void OnTriggerEnter()
    {
        int triggerID = identityAssignment.customID;
        // Debug.Log($"Custom ID: {triggerID}");
        // Debug.Log($"Active walls are: {gameManager.activeWalls[0]} and {gameManager.activeWalls[1]}");

        switch (trialType)
        {
            case "HighLowTrial":
                Debug.Log($"List at HighLowTrial execution is: {string.Join(",", gameManager.activeWalls)}");
                HighLowTrial(gameManager.activeWalls, triggerID);

                break;

            default:
                Debug.Log("Trial type not currently implemented");
                break;

        }
    }

    // Standard HighLow trial
    // If this wall is designated High, add 50 points to score
    // Else if it is Low, add 25 points to score
    void HighLowTrial(List<int> activeWalls, int triggerID)
    {
        int highWallTriggerID = activeWalls[0];
        int lowWallTriggerID = activeWalls[1];

        if (triggerID == highWallTriggerID)
        {
            gameManager.EndTrial(highScore, highWallTriggerID, lowWallTriggerID);
            Debug.Log("High score (50) triggered");
        }
        else if (triggerID == lowWallTriggerID)
        {
            gameManager.EndTrial(lowScore, highWallTriggerID, lowWallTriggerID);
            Debug.Log("Low score (25) triggered");
        }
    }
}
