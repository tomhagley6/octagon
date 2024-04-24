using System;
using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

/* Class to control rotation of the FirstPersonPlayer (around the y-axis) 
and rotation of the camera (around the x-axis) to allow player camera control.
To be attached to any camera instance */
public class MouseLook : NetworkBehaviour
{

    public float mouseSensitivity = 1000f;

    public Transform playerBody;

    float xRotation = 0f;
    public NetworkManager networkManager;
    public Action toggleMouseLock;
    public Action togglePlayerVisible;
    public bool unlockMouseTrigger = false;
    public bool playerInvisible = false;
    public GameObject canvas;


    public void Start()
    {
        
        networkManager = FindObjectOfType<NetworkManager>();

        // Script is unattached from FirstPersonPlayer Object, so find this through NetworkManager
        playerBody = networkManager.LocalClient.PlayerObject.transform;
        Cursor.lockState = CursorLockMode.Locked;  // Lock cursor within game view

        // subscribe to a key-triggered event with mouse lock toggle method
        toggleMouseLock += ToggleMouseLockListener;
        togglePlayerVisible += TogglePlayerVisibleListener;
        
        canvas = GameObject.Find("Canvas");

    }

    // Toggle mouse lock mode between locked and free
    // to allow interaction with QuantumConsole
    public void ToggleMouseLockListener()
    {
        
        unlockMouseTrigger = !unlockMouseTrigger;
        Cursor.lockState = unlockMouseTrigger ? CursorLockMode.None : CursorLockMode.Locked;
        
    }
        public void TogglePlayerVisibleListener()
    {
        
        playerInvisible = !playerInvisible;
        
        if (playerInvisible)
        {
            playerBody.gameObject.GetComponentInChildren<Renderer>().enabled = false;
            playerBody.position = new Vector3(10000,0,0);
            canvas.SetActive(false);
        }
        else
        {
            playerBody.gameObject.GetComponentInChildren<Renderer>().enabled = true;
            playerBody.position = new Vector3(0,0,0);
            canvas.SetActive(true);

        }
    }

    void Update()
    {   
        // No IsOwner checks here
        // We do not need to own the camera on the server if it only exists client-side

        // Mouse X and Mouse Y axes report the movement along these axes in the current frame
        // only (i.e., not the overall position of the mouse along an axis, just velocity)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // // y-axis
        /* Rotate() rotates the player around the 3 local axes of the transform by the angle
        specified
        Giving a vector [0 1 0] * mouseX angle, so that only the y-axis is rotated around
        and always by the degrees specified in mouseX */
        playerBody.Rotate(Vector3.up * mouseX);

        // // x-axis
        /* For rotation around the x-axis, we need to set a rotation instead of apply a rotation.
        This is because rotation angle must be clamped w/i a range, which requires it to be 
        a tracked value instead of Mouse Y's frame-by-frame readout of mouse axis movement.
        Therefore we use Quaternion.Euler, which returns a rotation value that can be directly
        set for transform.localRotation, in contrast to transform.Rotate which changes the 
        rotation value by the amount specified (applies a rotation). */
        
        // Clamp rotation around the x-axis to prevent neck-breaking
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  

        // Set rotation
        transform.localRotation = UnityEngine.Quaternion.Euler(xRotation, 0f, 0f);


        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(KeyCode.G))
        {
            toggleMouseLock();
        }
        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(KeyCode.T))
        {
            togglePlayerVisible();
        }
    }
}
