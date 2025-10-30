using UnityEngine;
using Globals;
using LoggingClasses;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

/*  Class to handle logging data to file on the local machine
    Initialise the DiskLogger as a StreamWriter, trigger LoggingStart log events,
    periodically log buffer contents to file, and handle logger object destruction  */
public class DiskLogger : Logger
{   
    // paths
    private string filename;
    private string dataFolder = Logging.logFolder;
    private string filePath;
    
    private bool loggerReady = false;
    
    private StreamWriter sw; 
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

    // Main logging coroutine
    // Continuously log to file while loggerReady
    // We don't care about emptying the entire log buffer in a single Update frame
    // Spread this over multiple frames in a coroutine instead
    private IEnumerator LogToFile()
    {
        // Debug.Log("LogToFile Coroutine began");
        while (loggerReady)
        {
            if (logEntries.Count != 0)
            {
                try
                {
                    // Debug.Log($"{logEntries[0]} from LogToFile coroutine");
                    WriteToBuffer();


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


    // Write to streamwriter buffer
    // Buffer emptying to the filestream occurs on Flush() or Close()
    // Make sure to flush the buffer often to avoid loss of buffered data
    private void WriteToBuffer()
    {
        if (loggerReady)
        {
            lock (logEntries) // Prevent use of the logEntries list while writing to file
            {
                foreach (string item in logEntries)
                {
                    try
                    {
                        // // Check whether this is the most efficient way of doing things,
                        // // or whether this will hog resources
                        sw.WriteLine(item + ",");
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
    // Create the first log event (which records time since application start)
    public void StartLogger()
    {

        // Path
        filename = String.Concat(DateTime.Now.ToString(Logging.fileTimeFormat), ".json");
        filePath = Path.Combine(dataFolder, filename);
        Debug.Log("Logger created. Filename: " + filename);

        // Content
        Dictionary<string, object> firstLogEvent = new Dictionary<string, object>
        {
            { "Event", "Logging started (dict)" }
        };
        var toLog = JsonConvert.SerializeObject(firstLogEvent);


        // Initialise the instance of StreamWriter that we will use for this logging session
        // This instance will need to be flushed regularly to avoid data loss on application crash
        // Automatic flush will happen when buffer fills
        // For 20k bytes, will write every ~20000 chars, or roughly 150 lines
        sw = new StreamWriter(filePath, true, Encoding.UTF8, 20000)
        {
            AutoFlush = false // no flush after each WriteLine (for performance)
        };

        // Begin the file with a square bracket to conform to JSON formatting        
        sw.WriteLine("[");

        // Update the flag to show that the logger is ready to use
        loggerReady = true;

        // Trigger the StartLogging log event in LoggingEvents.cs on the Logger GameObject
        loggingStarted?.Invoke();


        StartLoggingLogEvent startLoggingLogEvent = new StartLoggingLogEvent();
        string jsonData = JsonUtility.ToJson(startLoggingLogEvent);
        StartLoggingLogEvent deserialized = JsonUtility.FromJson<StartLoggingLogEvent>(jsonData);
        if (deserialized.eventDescription == null)
        {
            Debug.Log("deserialized.Description is null");
        }
        else if (deserialized.eventDescription.Length == 0)
        {
            Debug.Log("deserialized.Description is length 0");
        }

        // Record beginning of the logging process in the debug console
        var startEvent = new
        {
            LocalTime = DateTime.Now.ToString(Logging.logTimeFormat),
            /* Using realtimeSinceStartup to allow to me later create a pause function without 
            affecting this time measurement, which is taken as real time from the start of the
            application */
            ApplicationTime = UnityEngine.Time.realtimeSinceStartupAsDouble.ToString("f3"),
            Event = "Logging started"
        };
        Debug.Log($"{startEvent.Event} - {startEvent.LocalTime} - {startEvent.ApplicationTime}");


        // Commit the log to file
        StartCoroutine(LogToFile());
        Debug.Log("Logging coroutine begun.");
    }


    public void StopLogger()
    {
        Debug.Log("Closing current logger: " + filename);

        // Write a logging ended event to file to show that logging finished successfully
        loggingEnded?.Invoke();

        // Clear the remaining buffer
        WriteToBuffer();

        // Turn off logging access
        loggerReady = false;

        // Ensure all current logging actions are terminated
        StopAllCoroutines();

        // Close the StreamWriter instance before the application exits
        if (sw != null)
        {
            sw.Close();         
        }

        // Correct the formatting of the file to fit JSON standards (remove final comma)
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            // Read the content of the JSON file
            string jsonContent = File.ReadAllText(filePath);

            if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 1)
            {
                // Find and remove the last comma in the file
                // The file ends with: },\n (Linux) or },\r\n (Windows)
                // We need to find the comma and remove it, leaving the closing brace intact
                int lastCommaIndex = jsonContent.LastIndexOf(',');
                
                if (lastCommaIndex >= 0)
                {
                    // Remove the comma
                    jsonContent = jsonContent.Remove(lastCommaIndex, 1);
                    Debug.Log("Last comma of the JSON string has been removed");
                }
                else
                {
                    Debug.LogWarning("No comma found in JSON file to remove.");
                }

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

    