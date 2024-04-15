using Unity.Netcode;
using UnityEngine;


// Class to implement general x/y player movement, jumping, and detecting contact with the ground
// Does NOT implement any camera control
public class PlayerMovement : NetworkBehaviour
{

    // No need to directly assign Transform and Controller as this script attaches to 
    // each new FirstPersonPlayer Network Prefab
    public CharacterController controller; 
    public GameManager gameManager;
    public float speed = 200f; // Changed based on hardware
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float jumpHeight = 4f;
    bool isGrounded = true;
    float gravity = 9.81f;
    Vector3 yAxisVelocity;
    float gravityMultiplier = 2f; // scale gravity accel. based on feel

    public override void OnNetworkSpawn()
    {
        // Can alternatively use networkManager.LocalClient.PlayerObject.transform;
        // from elsewhere
        gameObject.transform.position = new Vector3(0,10,0);
    }
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
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

        // Create overall vector for current frame movement
        Vector3 planarMovement = transform.right * inputX + transform.forward * inputZ;

        // Use Character Controller Move method to apply a translation defined by move Vector3
        controller.Move(planarMovement * speed * Time.deltaTime);

        // It's time to stop defying gravity (I think I'll try applying gravity)
        /* SUVAT equation for gravity: v = u + a*t
        n.b. that here Time.deltaTime is the 't' in the equation
        it is NOT the term used to account for framerate differences */ 
        yAxisVelocity.y += -gravity * Time.deltaTime * gravityMultiplier;

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
            // Velocity change required for jump height equation
            // Source: Brackeys - First Person Movement in Unity
            yAxisVelocity.y += Mathf.Sqrt(-2f * jumpHeight * -gravity); 

        // Now apply resultant y-axis velocity, account for framerate
        controller.Move(yAxisVelocity * Time.deltaTime);
    
    }   

    void Update()
    {   
        // Only move a player object that you own as client
        if (!IsOwner) return;
        
        UpdateMovement();
        
    }

}
