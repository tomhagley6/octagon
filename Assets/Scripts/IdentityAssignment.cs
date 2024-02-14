using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Attach this script to each Octagon wall, and each wall will take its manually-assigned ID
// and add it to the IdentityManager dictionary, linked to its own handle
public class IdentityAssignment : MonoBehaviour
{
    // An ID for the WallTrigger, manually set for each wall, which has its own IdentityAssignment.cs
    public int customID;
    IdentityManager identityManager;

    void Start()
    {

        identityManager = FindObjectOfType<IdentityManager>();
        if (identityManager != null)
        {
            // Add this wall to the wallID, wall GameObject dictionary
            identityManager.AssignIdentifier(gameObject, customID);
            Debug.Log($"Key assigned in Identity Assignment is: {customID}");
        }
        else
        {
            Debug.LogWarning("IdentityManager not found in the scene");
        }
    }
}
