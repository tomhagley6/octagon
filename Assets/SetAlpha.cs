using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SetAlpha : NetworkBehaviour
{

    private GameObject alphaJoints;
    private Material alphaJointsMat;

   void Start()
   {
        alphaJoints = transform.Find("Alpha_Joints").gameObject;
        alphaJointsMat = alphaJoints.GetComponent<Material>();
        
   }

   void Update()
   {
    if (Input.GetKeyDown(KeyCode.F2) && IsOwner)
    {
        ToggleMaterialAlpha();
    }
   }

   void ToggleMaterialAlpha()
   {
        // alphaJointsMat.color.a = 0;
   }


}
