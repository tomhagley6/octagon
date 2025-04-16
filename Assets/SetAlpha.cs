using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Mathematics;

public class SetAlpha : NetworkBehaviour
{

    public GameObject joints;
    public GameObject surface;
    private Material jointsMat;
    private Material surfaceMat;
    private bool isTransparent = false;
    private Color defaultColorJoints;
    private Color defaultColorBody;

   void Start()
   {
     // Access the material of the joints and body
     joints = FindChildByName(gameObject, "Alpha_Joints");
     surface = FindChildByName(gameObject, "Alpha_Surface");

     Renderer jointsRenderer = joints.GetComponent<Renderer>();
     jointsMat = jointsRenderer.material;


     // surface = transform.Find("Alpha_Surface").gameObject;
     Renderer surfaceRenderer = surface.GetComponent<Renderer>();
     surfaceMat = surfaceRenderer.material;

   }

   void Update()
   {
     // 
    if (Input.GetKeyDown(KeyCode.F2) && IsOwner)
    {
        ToggleMaterialAlpha();
    }
   }

   void ToggleMaterialAlpha()
   {

     float alphaVal;

     if (!isTransparent){alphaVal = 0f; isTransparent=true;}
     else {alphaVal = 255f; isTransparent=false;}

     Color colorJoints = jointsMat.color;
     Color colorSurface = surfaceMat.color;
     colorJoints.a = alphaVal;
     colorSurface.a = alphaVal;
     jointsMat.color = colorJoints;
     surfaceMat.color = colorSurface;
   }

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
