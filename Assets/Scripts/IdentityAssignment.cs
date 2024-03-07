using UnityEngine;


// Each Octagon wall instantiates this class, and will take its manually-assigned ID
// and add it to the IdentityManager dictionary, linked to its own GameObject handle
public class IdentityAssignment : MonoBehaviour
{
    
    public int customID; // An ID for the WallTrigger, 
                         // manually set for each wall,
                         // which has its own IdentityAssignment.cs
    IdentityManager identityManager;

    // In Awake() as this can be the first code run
    // to avoid race condition with GameManager
    void Awake()
    {
        // Avoid requirement to manually set IdentityManager, when there is only one in the scene
        identityManager = FindObjectOfType<IdentityManager>(); 
        if (identityManager != null)
        {
            // Add this wall to the [wallID : wall] GameObject dictionary
            identityManager.AssignIdentifier(gameObject, customID);
            // Debug.Log($"Key assigned in Identity Assignment is: {customID}");
        }
        else
        {
            Debug.LogWarning("IdentityManager not found in the scene"); 
        }
    }
}
