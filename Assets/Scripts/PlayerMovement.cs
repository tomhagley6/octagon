using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;


// Code to implement general x/y player movement, jumping, and detecting contact with the ground
// Does NOT implement any camera control
public class PlayerMovement : MonoBehaviour
{

    // disable controller.Move error caused by pausing character input in GameController
    // do this until I find a better way to implement character control (see Sebastian Graves tutorials)
    #pragma warning disable 0618

    public CharacterController controller; 
    
    public Transform groundCheck;
    public float groundDistance;
    public LayerMask groundMask;
    public float speed;
    public float gMultiplier = 0.01f;
    // public float jumpVelocity = 500;
    public float jumpHeight = 5f;
    public bool movementEnabled = true;
    
    float gravityVelocity; 
    UnityEngine.Vector3 jumpVector;
    float g = 9.81f;
    bool isGrounded;
    UnityEngine.Vector3 yResultantVelocity;

    void MovementUpdate()
    {
        // Set up a ground check
        // This involves using a Transform-only component grouped into FirstPersonPlayer
        // and comparing the transform vector to the Ground layer (with a distance of groundDistance), 
        // with Physics.CheckSphere
        // Now you have a bool that will tell you whether groundCheck is within a distance radius of the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset velocity when grounded to preventive continuous increase in velocity due to gravity
        // Being directed downward, this velocity would be negative
        if (isGrounded && yResultantVelocity.y < 0) 
        {
            yResultantVelocity.y = 0.01f;
        }
        
        // Unity has default axes which it maps to default inputs. So, without configuration,
        // Horizontal and Vertical axes values will correspond to input from WASD 
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // x/z axis movement
        // maps horizontal and vertical to right/left and forward/back
        UnityEngine.Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // jump
        // If grounded and jump is triggered, add immediate upward velocity boost
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            // UnityEngine.Vector3 jump = transform.up * jumpVelocity;
            // jumpVector.y = Mathf.Sqrt(jumpHeight * 2f *  g);
            // controller.Move(jump * Time.deltaTime);
            // Debug.Log(jumpVector);
            yResultantVelocity.y = Mathf.Sqrt(jumpHeight * 2f *  g);

        }

        // Movement due to gravity
        yResultantVelocity.y -= g * gMultiplier;
        // UnityEngine.Vector3 gravityVector = transform.up * -gravityVelocity;
        
        // y axis movement
        // UnityEngine.Vector3 resultantVector = gravityVector + jumpVector;
        // yResultantVelocity.y += gravityVelocity;
        controller.Move(yResultantVelocity * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {   
        // allow a flag to be inactive when player movement is overrided
        if (movementEnabled)
        {
            MovementUpdate();
        }


    }

    #pragma warning restore 0618

}

