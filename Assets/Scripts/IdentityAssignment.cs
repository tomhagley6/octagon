using UnityEngine;


// Attach this script to each Octagon wall, and each wall will take its manually-assigned ID
// and add it to the IdentityManager dictionary, linked to its own GameObject handle
public class IdentityAssignment : MonoBehaviour
{
    // An ID for the WallTrigger, manually set for each wall, which has its own IdentityAssignment.cs
    public int customID;
    IdentityManager identityManager;

    // Could this be in Awake()? Doesn't require IdentityManager to run any code, just needs to 
    // access the assignment function
    // Then could use Start for GameManager
    void Start()
    {
        // Avoid requirement to manually set IdentityManager, when there is only one in the scene
        identityManager = FindObjectOfType<IdentityManager>(); 
        if (identityManager != null)
        {
            // Add this wall to the wallID, wall GameObject dictionary
            identityManager.AssignIdentifier(gameObject, customID);
            // Debug.Log($"Key assigned in Identity Assignment is: {customID}");
        }
        else
        {
            Debug.LogWarning("IdentityManager not found in the scene");
        }
    }
}
