using System.Collections.Generic;
using UnityEngine;

public class NetworkLogger : Logger
{
    private string ip;
    private int port;

    public NetworkLogger(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }

    public override void Log(Dictionary<string,object> data, string eventDescription)
    {
        // TODO

        // ???
        // Should probably implement the server first
    }
}