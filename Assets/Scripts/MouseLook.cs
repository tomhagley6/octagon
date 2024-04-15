using System;
using Unity.Netcode;
using UnityEngine;


// Class to control rotation of the FirstPersonPlayer (around the y-axis) 
// and rotation of the camera (around the x-axis) to allow player camera control
// To be attached to any camera instance
public class MouseLook : NetworkBehaviour
{

    public float mouseSensitivity = 1000f;

    public Transform playerBody;

    float xRotation = 0f;
    public NetworkManager networkManager;
    public Action toggleMouseLock;
    public bool unlockMouseTrigger = false;


    public void Start()
    {
        
        networkManager = FindObjectOfType<NetworkManager>();
        // playerBody = transform.parent;

        // Script is unattached from FirstPersonPlayer Object, so find this through NetworkManager
        playerBody = networkManager.LocalClient.PlayerObject.transform;
        Cursor.lockState = CursorLockMode.Locked;  // Lock cursor within game view

        // subscribe to a key-triggered event with mouse lock toggle method
        toggleMouseLock += ToggleMouseLockListener;

    }

    // Toggle mouse lock mode between locked and free
    // to allow interaction with QuantumConsole
    public void ToggleMouseLockListener()
    {
        
        unlockMouseTrigger = !unlockMouseTrigger;
        Cursor.lockState = unlockMouseTrigger ? CursorLockMode.None : CursorLockMode.Locked;
        
    }

    void Update()
    {   
        // Currently skipping, because we do not need to own the camera on the server if it only
        // exists client-side
        /* if (!IsOwner)
        {
            Debug.Log("Not owner. Skipping body.");
        }
        if (!IsOwner) return; */ 
        

        // Debug.Log("IsOwner so running camera update");
        // Mouse X and Mouse Y axes report the movement along these axes in the current frame
        // only (i.e., not the overall position of the mouse along an axis, just velocity)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // // y-axis
        // Rotate rotates the player around the 3 local axes of the transform by the angle
        // specified
        // Giving a vector [0 1 0] * mouseX angle, so that only the y-axis is rotated around
        // and always by the degrees specified in mouseX
        playerBody.Rotate(Vector3.up * mouseX);

        // // x-axis
        // Clamp rotation around the x-axis to prevent neck-breaking
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  


        // For rotation around the x-axis, movement angle must be clamped. As mouse Y is a 
        // frame-by-frame read-out of mouse axis movement, the only way to clamp this is to keep
        // a tracker for axis position, and apply that value to the transform rotation on each frame
        // Therefore we use Quaternion.Euler, which returns a rotation value that can be directly
        // set for transform.localRotation, in contrast to transform.Rotate which changes the 
        // rotation value by the amount specified (applies a rotation).
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);


        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(KeyCode.G))
        {
            toggleMouseLock();
        }
    }
}
