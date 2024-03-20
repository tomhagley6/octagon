using UnityEngine;
using Logging;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class DiskLogger : Logger
{   
    // paths
    private string filename;
    private string dataFolder = Globals.logFolder;
    private string filePath;
    
    private bool loggerReady = false;
    private TextWriter tw;
    
    // Store log entries in a buffer before writing to file
    private readonly List<string> logEntries = new List<string>();

    public DiskLogger()
    {   // This is not needed if we have filename as a private variable
        // this.filename = filename;
    }

    public void Start()
    {
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
        }
        Debug.Log("DiskLogger Start() ran");
  
    }

    public override void Log(string data)
    {
        if (loggerReady)
        {   
            string toLog = String.Format(Globals.logFormat, DateTime.Now.ToString(Globals.logTimeFormat),
                                         UnityEngine.Time.time.ToString("f3"), data);
            lock (logEntries) // prevent multiple threads from reaching this block simultaneously
            {
                logEntries.Add(toLog);
            }
        }

        // Debug.Log("DiskLogger.Log() ran");
        // Debug.Log($"logEntries is: {logEntries[0]}");
    }

    // We don't care about emptying the entire log buffer in a single Update frame
    // Spread this over multiple frames in a coroutine instead
    private IEnumerator LogToFile()
    {
        Debug.Log("LogToFile Coroutine began");
        while (loggerReady)
        {
            if (logEntries.Count != 0 )
            {
                try
                {
                    EmptyBuffer();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error while emptying logger buffer: " + ex.Message);
                }
            }
            // Waits for the next fixed framerate Update() method
            // Debug.Log("LogToFile Coroutine yielded");
            yield return new WaitForFixedUpdate();
        }
    }

    // LogToFile helper method
    private void EmptyBuffer()
    {
        if (loggerReady)
        {
            lock(logEntries)
            {
                foreach (string item in logEntries)
                {
                    tw.WriteLine(item);
                    // Debug.Log("TextWriter ran WriteLine for single logEntry");
                }
                logEntries.Clear();

            }
        }

        // Debug.Log("EmptyBuffer() ran.");
    }

    private void OnDestroy()
    {
        loggerReady = false;
        StopAllCoroutines();
        tw.Close();
    }

    // Public API
    public void StartLogger()
    {
        
        // Path
        filename = String.Concat(DateTime.Now.ToString(Globals.fileTimeFormat), ".txt");
        filePath = Path.Combine(dataFolder, filename);
        Debug.Log("Logger created. Filename: " + filename);

        // Setup TextWriter with the current file
        tw = File.AppendText(filePath);

        loggerReady = true;

        // Record beginning of logger process
        Log("[octagon]:logging start");

        StartCoroutine(LogToFile());
        Debug.Log("Coroutines begun.");
    }

    public void StopLogger()
    {
        Debug.Log("Closing current logger: " + filename);

        Log("[octagon]:logging end");
        EmptyBuffer();

        loggerReady = false;

        StopCoroutine(LogToFile());

        // Closing TextWriter calls a flush operation
        tw.Close();
    }
} 

