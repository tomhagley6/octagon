using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class NaivePlayerMovement : MonoBehaviour
{
    public Rigidbody rb = new Rigidbody();
    public int directionalForce = new int();

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("w"))
        {
            rb.AddForce(0, 0, directionalForce * Time.deltaTime, ForceMode.VelocityChange);
        }
        else if (Input.GetKey("a"))
        {
            rb.AddForce(-directionalForce * Time.deltaTime, 0, 0, ForceMode.VelocityChange);
        }
        else if (Input.GetKey("d"))
        {
            rb.AddForce(directionalForce * Time.deltaTime, 0, 0, ForceMode.VelocityChange);
        }
        else if (Input.GetKey("s"))
        {
            rb.AddForce(0, 0, -directionalForce * Time.deltaTime, ForceMode.VelocityChange);
        }
    }
}
