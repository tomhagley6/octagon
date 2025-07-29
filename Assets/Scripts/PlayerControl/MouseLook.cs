using System;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Globals;

/* Class to control rotation of the FirstPersonPlayer (around the y-axis) 
and rotation of the camera (around the x-axis) to allow player camera control.
To be attached to any camera instance */
public class MouseLook : NetworkBehaviour
{

    public Transform playerBody;
    float xRotation = 0f;
    public NetworkManager networkManager;
    public Action toggleMouseLock;
    public bool unlockMouseTrigger = false;


    public void Start()
    {
        // This script is unattached from FirstPersonPlayer Object,
        // so find FirstPersonPlayer through NetworkManager
        networkManager = FindObjectOfType<NetworkManager>();
        playerBody = networkManager.LocalClient.PlayerObject.transform;
        Cursor.lockState = CursorLockMode.Locked;  // Lock cursor within game view

        // subscribe to a key-down-triggered event with mouse lock toggle method
        toggleMouseLock += ToggleMouseLockListener;


    }

    // Toggle mouse lock mode between locked and free
    // to allow interaction with QuantumConsole console asset
    public void ToggleMouseLockListener()
    {
        unlockMouseTrigger = !unlockMouseTrigger;
        Cursor.lockState = unlockMouseTrigger ? CursorLockMode.None : CursorLockMode.Locked;
    }


    void Update()
    {
        // No IsOwner checks here
        // We do not need to check for ownership, because we are querying for the local client's player
        // Mouse X and Mouse Y axes report the movement along these axes in the current frame
        // only (i.e., not the overall position of the mouse along an axis, just velocity)
        float mouseX = Input.GetAxis("Mouse X") * General.mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * General.mouseSensitivity * Time.deltaTime;

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
        xRotation = Mathf.Clamp(xRotation, General.neckClampMin, General.neckClampMax);

        // Set rotation
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);


        // Allow manual toggle of mouse lock state
        if (Input.GetKeyDown(General.toggleMouse))
        {
            toggleMouseLock();
        }
    }


}
