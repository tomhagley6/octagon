using UnityEngine;

/* A small toggle class subscribed to by UIToggleListener's toggleOverlay event
   to use for toggling the Graphy overlay on and off */

public class GraphyToggle : MonoBehaviour
{
    bool graphyActive = false;

    void Start()
    {
        gameObject.SetActive(false);
        UIToggleListener.toggleOverlay += ToggleOverlayGraphyListener;
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
