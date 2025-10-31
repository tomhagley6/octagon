
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;


/*
 Class to implement a toggle for the visibility of the player avatar
 (both joints and surface materials)
 This class is the first to implement the new input system,
 and can be used as a template for other scripts that listen out for input events.
*/
public class SetAlpha : NetworkBehaviour
{

  public GameObject joints;
  public GameObject surface;
  private Material jointsMat;
  private Material surfaceMat;
  private bool isTransparent = false;
  private Color defaultColorJoints;
  private Color defaultColorBody;
  private PlayerInputActions inputActions;

  void Start()
  {
    // Access the material of the joints and body
    joints = FindChildByName(gameObject, "Alpha_Joints");
    surface = FindChildByName(gameObject, "Alpha_Surface");

    Renderer jointsRenderer = joints.GetComponent<Renderer>();
    jointsMat = jointsRenderer.material;

    Renderer surfaceRenderer = surface.GetComponent<Renderer>();
    surfaceMat = surfaceRenderer.material;

    // Initialize input system only for owner
    if (IsOwner)
    {
      SetupInputActions();
    }
  }

  // Subscribe the ToggleAlpha Action to the OnToggleAlpha method, and enable
  // the relevant ActionMap (Player)
  private void SetupInputActions()
  {
    try
    {
      inputActions = new PlayerInputActions();
      inputActions.Player.ToggleAlpha.performed += OnToggleAlpha;
      inputActions.Player.Enable();
    }
    catch (System.Exception e)
    {
      Debug.LogWarning($"Failed to initialize New Input System: {e.Message}. Falling back to legacy input.");
    }
  }

  // Subscribed to the input Action for toggling alpha, defined in the 
  // Input System's 'Player' ActionMap
  private void OnToggleAlpha(InputAction.CallbackContext context)
  {
    ToggleMaterialAlpha();
  }


  // Cleanly remove the input actions listener if the parent object
  // is destroyed, to prevent memory leaks
  public override void OnDestroy()
  {
    if (inputActions != null)
    {
      inputActions.Player.ToggleAlpha.performed -= OnToggleAlpha;
      inputActions.Dispose();
    }
    base.OnDestroy();
  }

  // Toggle the material visibility for joints and surfaces
  void ToggleMaterialAlpha()
  {

    float alphaVal;

    if (!isTransparent) { alphaVal = 0f; isTransparent = true; }
    else { alphaVal = 255f; isTransparent = false; }

    Color colorJoints = jointsMat.color;
    Color colorSurface = surfaceMat.color;
    colorJoints.a = alphaVal;
    colorSurface.a = alphaVal;
    jointsMat.color = colorJoints;
    surfaceMat.color = colorSurface;
  }

  // Recursive search implementation 
  // (c.f. Transform.Find which only search direct children)
  public GameObject FindChildByName(GameObject parent, string childName)
  {
    if (parent.name == childName)
    {
      return parent;
    }

    // Iterate through each child of the parent
    foreach (Transform child in parent.transform)
    {
      GameObject result = FindChildByName(child.gameObject, childName);

      // If a match is found in recursion, return
      if (result != null)
      {
        return result;
      }
    }

    // Return null if no match is found
    return null;
  }


}
