using UnityEngine;

/* Class to provide animator input for player movement animations
   Identify the direction and magnitude of movement, and communicate to the animator
   if the magnitude is above threshold */
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

    void Update()
    {
        // calculate current movement direction
        Vector3 worldMovementDirection = transform.position - previousPosition;
        // Debug.LogWarning(worldMovementDirection.magnitude);

        if (worldMovementDirection.magnitude > 1e-02)
        {
            smallDifference = false;
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
        // Communicate direction to the animator
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
