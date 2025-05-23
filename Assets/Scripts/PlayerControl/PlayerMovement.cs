using System;
using Globals;
using Unity.Mathematics;
using Unity.Netcode;
// using Unity.VisualScripting;
// using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;


// Class to implement general x/y player movement, jumping, and detecting contact with the ground
// Does NOT implement any camera control
public class PlayerMovement : NetworkBehaviour
{

    // No need to directly assign Transform and Controller as this script attaches to 
    // each new FirstPersonPlayer Network Prefab
    public CharacterController controller; 
    public GameManager gameManager;
    public NetworkManager networkManager;
    public float speed = 200f; // Changed based on hardware
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float jumpHeight = 4f;
    bool isGrounded = true;
    float gravity = 9.81f;
    Vector3 yAxisVelocity;
    float gravityMultiplier = 2f; // scale gravity accel. based on feel
    
    private bool playerInvisible = false;
    private Transform playerBody;
    private GameObject canvas;
    public Action togglePlayerVisible;
    
    public Animator animator;
 

    public override void OnNetworkSpawn()
    {
        // Can alternatively use networkManager.LocalClient.PlayerObject.transform;
        // from elsewhere
        // probably best not to do this here when refactoring
        // Can then get rid of networkManager reference
        // gameObject.transform.position = new Vector3(0,30,0);
        // networkManager.LocalClient.PlayerObject.transform.position = new Vector3(0,200,0);
    
    }
    
    void Start()
    {   
        // variables
        gameManager = FindObjectOfType<GameManager>();
        networkManager = FindObjectOfType<NetworkManager>();
        networkManager.LocalClient.PlayerObject.transform.position = new Vector3(0,10,0);
        playerBody = gameObject.transform;
        canvas = GameObject.Find("Canvas");
        // animator = GetComponent<Animator>();


        togglePlayerVisible += TogglePlayerVisibleListener;

        CreateCamera(); // TODO, create the camera as this script initiates, which means we have a player body to follow
    }

    void CreateCamera()
    {
        // TODO
        // Similar code to in AssignCamera
    }


    void UpdateMovement()
    {   
        // Check if grounded
        /* This involves using a Transform-only child GO of FirstPersonPlayer
        and comparing the transform vector to the Ground layer (with a distance of groundDistance), 
        using Physics.CheckSphere
        Now you have a bool that will tell you whether groundCheck is within a distance radius of the ground */
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        // Reset yAxisVelocity when grounded
        // Set this slightly negative to help with ground stickiness
        if (isGrounded && yAxisVelocity.y < 0)
            yAxisVelocity.y = -2f;


        // Assign the values of the horizontal and vertical (x and z) default input controls
        // to variables
        // By default, A and D produce Horizontal 1 and -1 respectively,
        // and W and S produce Vertical 1 and -1 respectively
        // These axes also work by default with controllers
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        // Naive attempt
        // // Keep the same vector magnitude when moving diagonally, as horizontall/vertically
        // // By halfing the contribution of each axis to the magnitude, when both are active
        // if (math.abs(inputX) > 0 && math.abs(inputZ) > 0)
        // {
        //     inputX = inputX * (float)math.sqrt(0.5);
        //     inputZ = inputZ * (float)math.sqrt(0.5);
        // }

        // Normalise the resulting vector to 1 
        // NB. GetAxis does a smoothing on input values to give a natural feel to stopping
        // Therefore we only want to normalise resultant vector magnitudes > 1, or all
        // smoothed values between 0 and 1 will be set to 1, replacing gradual slow down 
        // with sudden halt
        Vector3 planarMovement = transform.right * inputX + transform.forward * inputZ;
        if (planarMovement.magnitude > 1) {planarMovement.Normalize();}

        // Use Character Controller Move method to apply a translation defined by move Vector3
        controller.Move(planarMovement * speed * Time.deltaTime);

        if (planarMovement.magnitude > 0)
        {
            animator.SetBool("isRunning", true);
            // Debug.LogWarning("isRunning is true");
        }
        else
        {
            animator.SetBool("isRunning", false);
            // Debug.LogWarning("isRunning is false"); //
        }

        // Apply gravity
        /* SUVAT equation for gravity: v = u + a*t
        n.b. that here Time.deltaTime is the 't' in the equation
        it is NOT the term used to account for framerate differences */ 
        yAxisVelocity.y += -gravity * Time.deltaTime * gravityMultiplier;

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Velocity change required for jump height equation
            // Source: Brackeys - First Person Movement in Unity
            yAxisVelocity.y += Mathf.Sqrt(-2f * jumpHeight * -gravity); 
            animator.SetBool("isJumping", true);
        }
        else
        {
            animator.SetBool("isJumping", false);
        }

        // Now apply resultant y-axis velocity, account for framerate
        controller.Move(yAxisVelocity * Time.deltaTime);

        // Animations
    
    }   

    void Update()
    {   
        // Only move a player object that you own as client
        if (!IsOwner) return;
        
        UpdateMovement();

        UpdateCameraPosition();

        // Allow manual toggle of player visibility
        if (Input.GetKeyDown(General.togglePlayer))
        {
            togglePlayerVisible();
        }
        
    }

    void UpdateCameraPosition()
    {
        // TODO

        // set transform point of camera to be slightly ahead of player
        // https://www.youtube.com/watch?v=naaVpEyr4RA
        // possibly this only needs to be done once?

    }


    /* When visibility is toggled off, disable the renderer of the player GameObject
    Also, teleport it far away, because visibility is not a NetworkVariable but
    player location is.
    This ensures that other clients also do not see the player (for recording purposes) */
        public void TogglePlayerVisibleListener()
    {
        
        playerInvisible = !playerInvisible;
        
        if (playerInvisible)
        {
            playerBody.gameObject.GetComponentInChildren<Renderer>().enabled = false;
            playerBody.position = new Vector3(10000,0,0);
            canvas.SetActive(false); // also remove UI (for recording)
        }
        else
        {
            playerBody.gameObject.GetComponentInChildren<Renderer>().enabled = true;
            playerBody.position = new Vector3(0,0,0);
            canvas.SetActive(true);

        }
    }





}
