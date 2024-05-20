using UnityEngine;
using Globals;
using LoggingClasses;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

// Class to handle logging data to file on the local machine
public class DiskLogger : Logger
{   
    // paths
    private string filename;
    private string dataFolder = Logging.logFolder;
    private string filePath;
    
    private bool loggerReady = false;
    
    // // TextWriter replaced with a StreamWriter
    // private TextWriter tw;
    private StreamWriter sw; 
    private bool isFirstLine = true;
    private string firstLine;
    public Logger logger;

    
    // Store log entries in a buffer before writing to file
    private readonly List<string> logEntries = new List<string>();

    public event Action loggingStarted;
    public event Action loggingEnded;

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

        logger = FindObjectOfType<Logger>();

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
                // Debug.Log("Log entry added to logEntries");
            }
        }
        else
        {
            Debug.Log("Logger not ready");
        }

        // Debug.Log("DiskLogger.Log() ran");
        // Debug.Log($"logEntries is: {logEntries[0]}");
    }

    // We don't care about emptying the entire log buffer in a single Update frame
    // Spread this over multiple frames in a coroutine instead
    private IEnumerator LogToFile()
    {
        // Debug.Log("LogToFile Coroutine began");
        while (loggerReady)
        {
            if (logEntries.Count != 0 )
            {
                try
                {
                    // Debug.Log($"{logEntries[0]} from LogToFile coroutine");
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

    //// Pretty sure this is writing TO the buffer, not emptying it!
    //// Buffer emptying to the filestream should only occur on Flush() or Close()
    //// Should I keep this code at all, or just write directly to buffer with Log()? 
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
                        // // Check whether this is the most efficient way of doing things,
                        // // or whether this will hog resources
                        // firstLine = isFirstLine == true ? "[" : "";
                        sw.WriteLine(item + ",");
                        // Debug.Log(item);

                        // string item2 = JsonUtility.ToJson(
                        //     new {
                        //         Time = "now"
                        //     }
                        // );
                        // sw.WriteLine(item2);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                    // Debug.Log($"item length after writing is {item.Length}");
                    // Debug.Log("TextWriter ran WriteLine for single logEntry");
                    // Debug.Log(item);
                }
                logEntries.Clear();

            }
        }

        // Debug.Log("EmptyBuffer() ran.");
    }

    private void OnDestroy()
    {
        StopLogger();
    }

    // Public API
    public void StartLogger()
    {
        
        // Path
        filename = String.Concat(DateTime.Now.ToString(Logging.fileTimeFormat), ".json");
        filePath = Path.Combine(dataFolder, filename);
        Debug.Log("Logger created. Filename: " + filename);

        // Initialise the instance of StreamWriter that we will use for this logging session
        // This instance will need to be flushed regularly to avoid data loss on application crash
        // Automatic flush will happen when buffer fills
        // For 20k bytes, will write every ~20000 chars, or roughly 150 lines
        sw = new StreamWriter(filePath, true, Encoding.UTF8, 20000)
        {
            AutoFlush = false
        };

        // Begin the file with a square bracket to conform to JSON formatting        
        sw.WriteLine("[");

        // // Setup StreamWriter with the current file
        // sw = File.AppendText(filePath);

        loggerReady = true;

        // Record beginning of logger process
        var startEvent = new  
        {
            LocalTime = DateTime.Now.ToString(Logging.logTimeFormat),
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
        // Log(toLog);

        // FINAL VERSION
        // Trigger the StartLogging log event in LoggingEvents.cs on the Logger GameObject
        // var logger = FindObjectOfType<Logger>().GetComponent<LoggingEvents>();
        // logger.StartLogging();
        loggingStarted?.Invoke();


        StartLoggingLogEvent startLoggingLogEvent = new StartLoggingLogEvent();
        string jsonData = JsonUtility.ToJson(startLoggingLogEvent);
        // Debug.Log("new attempt at json is "+ jsonData);
        StartLoggingLogEvent deserialized = JsonUtility.FromJson<StartLoggingLogEvent>(jsonData);
        // Debug.Log("and deserialized is " + deserialized.eventDescription);
        if (deserialized.eventDescription == null)
        {
            Debug.Log("deserialized.Description is null");
        }
        else if (deserialized.eventDescription.Length == 0)
        {
            Debug.Log("deserialized.Description is length 0");
        }

        string jsonDataNewtonsoft = JsonConvert.SerializeObject(startLoggingLogEvent);
        // Debug.Log($"Now trying Newtonsoft: " + jsonDataNewtonsoft);

        Debug.Log($"{startEvent.Event} - {startEvent.LocalTime} - {startEvent.ApplicationTime}");
        


        StartCoroutine(LogToFile());
        Debug.Log("Logging coroutine begun.");
    }


    public void StopLogger()
    {
        Debug.Log("Closing current logger: " + filename);

        // Write a logging ended event to file to show that logging finished successfully
        loggingEnded?.Invoke();

        EmptyBuffer();

        loggerReady = false;

        StopAllCoroutines();

        // Be careful to close the StreamWriter instance before the application exits
        if (sw != null)
        {
            sw.Close();         
        }

 

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            // Read the content of the JSON file
            string jsonContent = File.ReadAllText(filePath);

            if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 1)
            {
                // Remove the last character
                // jsonContent = jsonContent.Remove(jsonContent.Length - 1);
                jsonContent = jsonContent.Remove(jsonContent.Length - 3, 1);
                Debug.Log("Last character of the JSON string has been removed");

                // Write the modified content back to the file
                File.WriteAllText(filePath, jsonContent);
            }
            else
            {
                Debug.LogWarning("JSON file is empty or has only one character. No action taken.");
            }
        }
        else
        {
            Debug.LogWarning("JSON file path is invalid or does not exist. No action taken.");
        }
        
        // Finish the JSON file by writing a square bracket to the end 
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine("]");
        }
    }

} 

    