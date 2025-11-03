using System.Collections;
using System.Collections.Generic;
using Globals;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// Class to display, accept input for, and dynamically update the mouse sensitivity variable
// The GameObject that this class is attached to 
public class MouseSensitivity : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI mouseSensitivityDisplay;
   [SerializeField] private TMP_InputField mouseSensitivityInput;
   private bool isVisible = false;
    

   void Start()
   {

        // // Avoid incurring runtime costs when you can just assign in editor
        // mouseSensitivityDisplay = GameObject.Find("Canvas").transform
        //                                     .Find("MouseSensitivity")
        //                                     .Find("MouseSensitivityDisplay").gameObject
        //                                     .GetComponent<TextMeshProUGUI>();
       
          // Start the game with the overlay inactive
          mouseSensitivityDisplay.gameObject.SetActive(false);
          mouseSensitivityInput.gameObject.SetActive(false);
          
          // Update the mouse sensitivity display when a new value is input by the user
          mouseSensitivityInput.onValueChanged.AddListener(AdjustMouseSensitivity);

          // Subscribe to the toggleOverlay event to toggle the mouse sensitivity UI
          UIToggleListener.toggleOverlay += ToggleOverlayMouseSensitivityListener;
   }

   void Update()
   {
     //    mouseSensitivityDisplay.text = $"Current: {General.mouseSensitivity}";
   }

   // change mouse sensitivity variable in the Globals namespace, and update the display
   public void AdjustMouseSensitivity(string newMouseSensitivity)
   {
        if (float.TryParse(newMouseSensitivity, out float result))
        {
               if (General.mouseSensitivity != result)
               {
                    General.mouseSensitivity = result;
                    mouseSensitivityDisplay.text = $"Current: {General.mouseSensitivity}";

               }
        }
        
   }

   private void ToggleOverlayMouseSensitivityListener()
   {
        if (!isVisible)
        {
            mouseSensitivityDisplay.gameObject.SetActive(true);
            mouseSensitivityInput.gameObject.SetActive(true);
        }
        if (isVisible)
        {
            mouseSensitivityDisplay.gameObject.SetActive(false);
            mouseSensitivityInput.gameObject.SetActive(false);
        }

        isVisible = !isVisible;
   }

}
