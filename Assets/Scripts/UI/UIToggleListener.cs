using UnityEngine;
using Globals;
using System;

public class UIToggleListener : MonoBehaviour
{

    // Because the toggleOverlay event is static, it can be subscribed to by 
    // any other class even if UIToggleListener.cs has not been loaded yet. This
    // is because any attempt to subscribe before UIToggleListener.cs is loaded
    // will act as the trigger to load this class
    public static event Action toggleOverlay;

    void Update()
    {
        // Monitor toggle for diagnostics overlay
        // (This can be converted to the new input system)
        if (Input.GetKeyDown(General.toggleOverlay))
        {
            toggleOverlay();
        }
    }

    // Toggle UI overlay (move this to a different script?)
    public static void ToggleOverlay()
    {
        toggleOverlay();
    }
}
