using System.Collections.Generic;
using UnityEngine;


// Logger that implements both a local DiskLogger and a NetworkLogger
// Both classes run their Log functions when DoubleLogger.Log is called
public class DoubleLogger : Logger
{
    private DiskLogger diskLogger;
    private NetworkLogger networkLogger;

    public DoubleLogger(string filename, string ip, int port)
    {
        diskLogger = new DiskLogger();
        networkLogger = new NetworkLogger(ip, port);
    }

    public override void Log(string logEntry)
    {
        diskLogger.Log(logEntry);
        networkLogger.Log(logEntry);

    }
}