using UnityEngine;


public class DoubleLogger : Logger
{
    private DiskLogger diskLogger;
    private NetworkLogger networkLogger;

    public DoubleLogger(string filename, string ip, int port)
    {
        diskLogger = new DiskLogger();
        networkLogger = new NetworkLogger(ip, port);
    }

    public override void Log(string data)
    {
        diskLogger.Log(data);
        networkLogger.Log(data);

    }
}