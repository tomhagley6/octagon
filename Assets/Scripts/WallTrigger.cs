using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Pass in the current ordered active triggers and the current trial type (controlled by GameController)
// Then, use the trial type to decide how the trigger should respond to activation
// Also consider which trigger instance has been activated on trigger entry

// Logic to control the response to activating triggers attached to each Octagon wall
// With one copy of this script per wall
// Check this wall's ID against the current trial walls IDs
// Depending on the wall identity, control the score output
// Allow modularity for various trial types
public class WallTrigger : MonoBehaviour
{
    public GameManager gameManager; 
    // public List<int> activeWalls;
    public string trialType = "HighLowTrial";
    public int highScore = 50;
    public int lowScore = 25;
    IdentityAssignment identityAssignment;
    
    void Start() 
    {
        // Making sure to use GetComponent here instead of just looking for 
        // object of type IdentityAssignment. 
        // I think doing it this way somehow avoids confusion between the 
        // IdentityAssignment components of the different WallTrigger prefab
        // instances
        identityAssignment = gameObject.GetComponent<IdentityAssignment>(); 
        gameManager = FindAnyObjectByType<GameManager>();
    }
    void OnTriggerEnter()
    {
        // identityAssignment = gameObject.GetComponent<IdentityAssignment>();
        int triggerID = identityAssignment.customID;
        Debug.Log($"Custom ID: {triggerID}");
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
