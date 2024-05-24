using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine.Perception.GroundTruth;
using System.Collections.Concurrent;
using ImageEncoder = UnityEngine.Perception.GroundTruth.Utilities.ImageEncoder;
using UnityEngine.Perception.GroundTruth.Utilities;


public class CaptureCamera : MonoBehaviour
{
    public Camera captureCamera;
    public RenderTexture renderTexture;
    public int frameRate = 30;
    public int captureWidth = 1280;
    public int captureHeight = 720;

    private int frameCount = 0;
    private ConcurrentQueue<Texture2D> frameQueue = new ConcurrentQueue<Texture2D>();
    private Thread fileWritingThread;
    private bool isCapturing = true;
    private string persistentDataPath;

    private void Start()
    {
        persistentDataPath = Application.persistentDataPath;

        if (captureCamera.targetTexture != renderTexture)
        {
            captureCamera.targetTexture = renderTexture;
        }

        StartCoroutine(CaptureFrames());
        fileWritingThread = new Thread(ProcessFrames);
        fileWritingThread.Start();
    }

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
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        captureCamera.Render();

        Texture2D frame = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        frame.Apply();

        // Enqueue frame for encoding
        frameQueue.Enqueue(frame);

        RenderTexture.active = currentRT;

        frameCount++;
    }

    private void ProcessFrames()
    {
        while (isCapturing || !frameQueue.IsEmpty)
        {
            if (frameQueue.TryDequeue(out Texture2D frame))
            {
                // Encode frame asynchronously
                EncodeFrameAsync(frame);
            }
            Thread.Sleep(10); // Small delay to reduce CPU usage
        }
    }

    private void EncodeFrameAsync(Texture2D frame)
    {
        // Set encoding options (e.g., format, quality)
        ImageEncoder.encodeImagesAsynchronously = true;

        // Encode image asynchronously
        ImageEncoder.EncodeImage(frame, OnEncodingComplete);
    }

    private void OnEncodingComplete(byte[] encodedBytes)
    {
        // Write encoded bytes to file
        string fileName = string.Format("Frame_{0:D04}.png", frameCount);
        File.WriteAllBytes(Path.Combine(persistentDataPath, fileName), encodedBytes);
    }

    private void OnDestroy()
    {
        isCapturing = false;
        if (fileWritingThread != null && fileWritingThread.IsAlive)
        {
            fileWritingThread.Join();
        }
    }
}