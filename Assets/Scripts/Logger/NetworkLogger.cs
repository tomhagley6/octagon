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

    public override void Log(string data)
    {
        // TODO

        // ???
        // Should probably implement the server first
    }
}