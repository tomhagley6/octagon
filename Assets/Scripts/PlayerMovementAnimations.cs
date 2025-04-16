using System.Collections;
using System.Collections.Generic;
// using AmplifyShaderEditor;
using UnityEngine;

public class PlayerMovementAnimations : MonoBehaviour
{

    public Animator animator;
    private Vector3 previousPosition;
    bool smallDifference = true;


    void Start()
    {
        // store initial position
        previousPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {   
        // calculate movement direction
        Vector3 worldMovementDirection = transform.position - previousPosition;

        // Debug.LogWarning(worldMovementDirection.magnitude);
        if (worldMovementDirection.magnitude > 1e-02)
        {
            smallDifference = false;
            // Debug.LogWarning("smallDifference is false");
        }
        else
        {
            smallDifference = true;
        }

        // Update previous position for the next frame
        previousPosition = transform.position;

        // Convert the world movement direction to local space (relative to the object)
        Vector3 localMovementDirection = transform.InverseTransformDirection(worldMovementDirection);

        // Normalise the movement direction to get values between -1 and 1
        Vector3 localMovement = localMovementDirection.normalized;

        // Send movement information to the animator
        UpdateAnimation(localMovement, smallDifference);

    }

    void UpdateAnimation(Vector3 localMovement, bool smallDifference)
    {
        // Calculate direction (forward, backward, diagonal, etc.)
        float horizontal = localMovement.x;
        float vertical = localMovement.z;

        // Debug.Log($"horizontal is: {horizontal}, vertical is {vertical}");
        
        if (!smallDifference)
        {
            animator.SetFloat("Horizontal", horizontal);
            animator.SetFloat("Vertical", vertical);
        }
        else
        {
            animator.SetFloat("Horizontal", 0);
            animator.SetFloat("Vertical", 0);
        }
    }
}
