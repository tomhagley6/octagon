using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Logger : MonoBehaviour
{

    // Abstract logging method
    // Implemented separately for local file vs network data logging
    public abstract void Log(string data);
}
