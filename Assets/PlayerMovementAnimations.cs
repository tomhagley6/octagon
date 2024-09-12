using System.Collections;
using System.Collections.Generic;
// using AmplifyShaderEditor;
using UnityEngine;

public class PlayerMovementAnimations : MonoBehaviour
{

    public Animator animator;
    private Vector3 previousPosition;


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

        // Update previous position for the next frame
        previousPosition = transform.position;

        // Convert the world movement direction to local space (relative to the object)
        Vector3 localMovementDirection = transform.InverseTransformDirection(worldMovementDirection);

        // Normalise the movement direction to get values between -1 and 1
        Vector3 localMovement = localMovementDirection.normalized;

        // Send movement information to the animator
        UpdateAnimation(localMovement);

    }

    void UpdateAnimation(Vector3 localMovement)
    {
        // Calculate direction (forward, backward, diagonal, etc.)
        float horizontal = localMovement.x;
        float vertical = localMovement.z;

        // Debug.Log($"horizontal is: {horizontal}, vertical is {vertical}");
        

        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("Vertical", vertical);
    }
}
