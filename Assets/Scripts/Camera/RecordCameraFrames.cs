using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using Globals;

public class CaptureCamera : MonoBehaviour
{
    public Camera captureCamera;
    public RenderTexture renderTexture;
    public int frameRate = 30;
    public int captureWidth = 1280;
    public int captureHeight = 720;

    private int frameCount = 0;
    private ConcurrentQueue<FrameData> frameQueue = new ConcurrentQueue<FrameData>(); // Thread safe queue to store frame data
    private Thread fileWritingThread; // Separate thread for writing to file
    private bool isCapturing = true;
    private bool isActive = false;
    private string persistentDataPath; // storage location for output images

    // Begin capturing frames and setup a background filewriting thread
    private void ToggleRecording(bool isActive)
    {
        // Start recording if not already active
        if (!isActive)
        {
            RenderTexture.active = renderTexture;
            persistentDataPath = Application.persistentDataPath;
            Debug.LogError("Video frames saved to " + Application.persistentDataPath);

            if (captureCamera.targetTexture != renderTexture)
            {
                captureCamera.targetTexture = renderTexture;
            }

            isCapturing = true;
            StartCoroutine(CaptureFrames());
            fileWritingThread = new Thread(ProcessFrames);
            fileWritingThread.Start();
            this.isActive = true;
            Debug.LogError("isActive set to " + isActive);
        }
        // Stop recording if already active
        else if (isActive)
        {
            Debug.LogError("isCapturing set to false");
            isCapturing = false;
            StopCoroutine(CaptureFrames());
            if (fileWritingThread != null && fileWritingThread.IsAlive)
            {
                fileWritingThread.Join();
            }
            this.isActive = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(General.toggleRecording))
        {
            Debug.LogError("Toggling recording with isActive equal to " + isActive);
            ToggleRecording(isActive);
        }
    }

    // Keep capturing frames at the specified framerate
    private IEnumerator CaptureFrames()
    {
        float frameInterval = 1.0f / frameRate;

        while (isCapturing)
        {
            yield return new WaitForEndOfFrame();

            CaptureFrame();

            yield return new WaitForSeconds(frameInterval);
        }
    }

    private void CaptureFrame()
    {
        // // Store current RenderTexture value to reapply later
        // RenderTexture currentRT = RenderTexture.active;

        // Replace the current RenderTexture with the one we created
        // This means that the camera will render to the RenderTexture instead of to the screen
        RenderTexture.active = renderTexture;

        // Trigger the camera to render its view
        // which will occur on our RenderTexture
        captureCamera.Render();

        // Initialise a new Texture2D object with our specified dims and 3 8-bit colour channels
        Texture2D frame = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        // Read pixel data from a crop of our current active RenderTexture and apply this read to frame
        frame.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        frame.Apply();

        // Encode to JPEG format byte array on the main thread as required by the Unity API
        byte[] bytes = frame.EncodeToJPG();
        string fileName = string.Format("Frame_{0:D04}.jpg", frameCount); // 4 digit frame-count number

        // Our queue takes a struct of FrameData that includes the full filepath and the JPEG byte array
        frameQueue.Enqueue(new FrameData { FileName = Path.Combine(persistentDataPath, fileName), Bytes = bytes });

        // // Free up memory after use
        // Destroy(frame); 

        // // Restore original render texture so camera renders correctly
        // RenderTexture.active = currentRT;

        frameCount++;
    }


    /* Using a background thread, write JPEG byte arrays to their filepaths
       as specified in each FrameData object */
    private void ProcessFrames()
    {
        while (isCapturing || !frameQueue.IsEmpty)
        {
            if (frameQueue.TryDequeue(out FrameData frameData))
            {
                File.WriteAllBytes(frameData.FileName, frameData.Bytes);
            }
            Thread.Sleep(10); // Small delay to reduce CPU usage
        }
    }


    // Rejoin the background thread after file writing is complete
    private void OnDestroy()
    {
        isCapturing = false;
        if (fileWritingThread != null && fileWritingThread.IsAlive)
        {
            fileWritingThread.Join();
        }
    }

    // Struct format for each entry into the image filewriting queue
    private struct FrameData
    {
        public string FileName;
        public byte[] Bytes;
    }
}