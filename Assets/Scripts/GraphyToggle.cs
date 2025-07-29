using System.Collections;
using System.Collections.Generic;
using Globals;
using UnityEngine;

/* A small toggle class subscribed to by GameManager's toggleOverlay event
   to use for toggling the Graphy overlay on and off */

public class GraphyToggle : MonoBehaviour
{
    bool graphyActive = false;

    void Start()
    {
        gameObject.SetActive(false);
        GameManager.toggleOverlay += ToggleOverlayGraphyListener;
    }


    void ToggleOverlayGraphyListener()
    {
        if (graphyActive == true)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
        graphyActive = !graphyActive;
    }
}
