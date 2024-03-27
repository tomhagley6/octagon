using Unity.Netcode;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{

    public float mouseSensitivity = 1000f;

    public Transform playerBody;

    float xRotation = 0f;
    public NetworkManager networkManager;


    public void Start()
    {
        
        networkManager = FindObjectOfType<NetworkManager>();
        // playerBody = transform.parent;
        playerBody = networkManager.LocalClient.PlayerObject.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {   
        if (!IsOwner)
        {
            Debug.Log("Not owner. Skipping body.");
        }
        if (!IsOwner) return; 
        

        // Debug.Log("IsOwner so running camera update");
        // Mouse X and Mouse Y axes report the movement along these axes in the current frame
        // only (i.e., not the overall position of the mouse along an axis, just velocity)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        

        // Clamp rotation around the x-axis to prevent neck-breaking
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  

        // Rotate rotates the player around the 3 local axes of the transform by the angle
        // specified
        // Giving a vector [0 1 0] * mouseX angle, so that only the y-axis is rotated around
        // and always by the degrees specified in mouseX
        playerBody.Rotate(Vector3.up * mouseX);

        // For rotation around the x-axis, movement angle is clamped. As mouse Y is a frame-by-frame
        // read-out of mouse axis movement, the only way to clamp this is to keep a tracker for
        // axis position, and apply that value to the transform rotation on each frame
        // Therefore we use Quaternion.Euler, which returns a rotation value that can be directly
        // set for transform.localRotation, in contrast to transform.Rotate which changes the 
        // rotation value by the amount specified (applies a rotation).
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    }
}
