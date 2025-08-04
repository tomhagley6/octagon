using UnityEngine;
using Globals;

public class UIToggleListener : MonoBehaviour
{
    void Update()
    {
        // Monitor toggle for diagnostics overlay
        // (This can be converted to the new input system)
        if (Input.GetKeyDown(General.toggleOverlay))
        {
            GameManager.ToggleOverlay();
        }
    }
}
