using UnityEngine;
using Logging;
using LoggingClasses;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

// Class to handle logging data to file on the local machine
public class DiskLogger : Logger
{   
    // paths
    private string filename;
    private string dataFolder = Globals.logFolder;
    private string filePath;
    
    private bool loggerReady = false;
    
    // // TextWriter replaced with a StreamWriter
    // private TextWriter tw;
    private StreamWriter sw; 

    
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

    public override void Log(string logEntry)
    {
        if (loggerReady)
        {   
            // string toLog = String.Format(Globals.logFormat, DateTime.Now.ToString(Globals.logTimeFormat),
            //                              UnityEngine.Time.time.ToString("f3"), data);

            // var logEntry = new
            // {
            //     LocalTime = DateTime.Now.ToString(Globals.logTimeFormat),
            //     /* Using realtimeSinceStartup to allow to me later create a pause function without 
            //     affecting this time measurement, which is taken as real time from the start of the
            //     application */
            //     ApplicationTime = UnityEngine.Time.realtimeSinceStartupAsDouble.ToString("f3"),
            //     Event = eventDescription,
            //     data
            // };

            // string toLog = JsonConvert.SerializeObject(logEntry);

            lock (logEntries) // prevent multiple threads from reaching this block simultaneously
            {
                logEntries.Add(logEntry);
                // Debug.Log($"{logEntry.ApplicationTime} from Log()");
                // Debug.Log($"{toLog} from Log()");
                Debug.Log("Log entry added to logEntries");
            }
        }
        else
        {
            Debug.Log("Logger not ready");
        }

        Debug.Log("DiskLogger.Log() ran");
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
                    Debug.Log($"{logEntries[0]} from LogToFile coroutine");
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
                    // File.WriteAllText(item, filePath);
                    try 
                    {
                        using (StreamWriter sw = new StreamWriter(filePath, true))
                        {
                            sw.WriteLine(item);
                            Debug.Log(item);

                            // string item2 = JsonUtility.ToJson(
                            //     new {
                            //         Time = "now"
                            //     }
                            // );
                            // sw.WriteLine(item2);

                            
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                    Debug.Log($"item length after writing is {item.Length}");
                    // Debug.Log("TextWriter ran WriteLine for single logEntry");
                    Debug.Log(item);
                }
                logEntries.Clear();

            }
        }

        Debug.Log("EmptyBuffer() ran.");
    }

    private void OnDestroy()
    {
        loggerReady = false;
        StopAllCoroutines();
        if (sw != null)
        {
            sw.Close();
        }
    }

    // Public API
    public void StartLogger()
    {
        
        // Path
        filename = String.Concat(DateTime.Now.ToString(Globals.fileTimeFormat), ".json");
        filePath = Path.Combine(dataFolder, filename);
        Debug.Log("Logger created. Filename: " + filename);

        // // Setup StreamWriter with the current file
        // sw = File.AppendText(filePath);

        loggerReady = true;

        // Record beginning of logger process
        var startEvent = new  
        {
            LocalTime = DateTime.Now.ToString(Globals.logTimeFormat),
            /* Using realtimeSinceStartup to allow to me later create a pause function without 
            affecting this time measurement, which is taken as real time from the start of the
            application */
            ApplicationTime = UnityEngine.Time.realtimeSinceStartupAsDouble.ToString("f3"),
            Event = "Logging started"
        };

        string jsonString = JsonUtility.ToJson(startEvent);
        
        Dictionary<string,object> data = new Dictionary<string, object>
        {
            { "Event", "Logging started (dict)" }
        };
        var toLog = JsonConvert.SerializeObject(data);
        Log(toLog);

        StartLogEvent startLogEvent = new StartLogEvent("testdescription");
        string jsonData = JsonUtility.ToJson(startLogEvent);
        Debug.Log("new attempt at json is "+ jsonData);
        StartLogEvent deserialized = JsonUtility.FromJson<StartLogEvent>(jsonData);
        Debug.Log("and deserialized is " + deserialized.Description);
        if (deserialized.Description == null)
        {
            Debug.Log("deserialized.Description is null");
        }
        else if (deserialized.Description.Length == 0)
        {
            Debug.Log("deserialized.Description is length 0");
        }

        string jsonDataNewtonsoft = JsonConvert.SerializeObject(startLogEvent);
        Debug.Log($"Now trying Newtonsoft: " + jsonDataNewtonsoft);

        Debug.Log($"{startEvent.Event} - {startEvent.LocalTime} - {startEvent.ApplicationTime}");
        


        StartCoroutine(LogToFile());
        Debug.Log("Logging coroutine begun.");
    }

    public void StopLogger()
    {
        Debug.Log("Closing current logger: " + filename);

        // Log("[octagon]:logging end");
        EmptyBuffer();

        loggerReady = false;

        StopCoroutine(LogToFile());

        // Closing TextWriter calls a flush operation
        sw.Close();
    }

} 

    