using System.Collections.Generic;
using UnityEngine;


// Network extension of the Logger Class
// Currently unused while using Unity Relay
public class NetworkLogger : Logger
{
    private string ip;
    private int port;

    public NetworkLogger(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }

    public override void Log(string logEntry)
    {
        // TODO

        // ???
        // Should probably implement the server first
    }
}