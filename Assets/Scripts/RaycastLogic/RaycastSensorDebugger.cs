using UnityEngine;

/// <summary>
/// Optional debugging helper for the Selective Pass-Through Raycast system.
/// Attach this to an agent to log raycast observations in a readable format.
/// </summary>
public class RaycastSensorDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableLogging = false;
    [SerializeField] private float logInterval = 1f; // Log every N seconds
    [SerializeField] private SelectivePassThroughRaycast raycastLogic;

    [Header("Display Options")]
    [SerializeField] private bool logMiddleRayOnly = true;
    [SerializeField] private bool logAllRays = false;

    private float nextLogTime = 0f;

    void Start()
    {
        if (raycastLogic == null)
        {
            raycastLogic = GetComponent<SelectivePassThroughRaycast>();
            if (raycastLogic == null)
            {
                Debug.LogWarning($"[RaycastSensorDebugger] SelectivePassThroughRaycast not found on {gameObject.name}");
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (!enableLogging || raycastLogic == null)
            return;

        if (Time.time >= nextLogTime)
        {
            LogRaycastData();
            nextLogTime = Time.time + logInterval;
        }
    }

    private void LogRaycastData()
    {
        float[] observations = raycastLogic.GetObservations();
        
        if (observations == null || observations.Length == 0)
        {
            Debug.Log("[RaycastDebug] No observations available");
            return;
        }

        // Calculate observation structure
        // This assumes the standard structure: 1 distance + N tags + 1 pass-through distance
        int estimatedTagCount = 4; // Adjust if your setup is different
        int obsPerRay = 1 + estimatedTagCount + 1;
        int totalRays = observations.Length / obsPerRay;

        if (logMiddleRayOnly)
        {
            int middleRayIdx = totalRays / 2;
            LogRay(middleRayIdx, observations, obsPerRay, estimatedTagCount);
        }
        else if (logAllRays)
        {
            for (int i = 0; i < totalRays; i++)
            {
                LogRay(i, observations, obsPerRay, estimatedTagCount);
            }
        }
    }

    private void LogRay(int rayIndex, float[] observations, int obsPerRay, int tagCount)
    {
        int baseIdx = rayIndex * obsPerRay;
        
        if (baseIdx + obsPerRay > observations.Length)
            return;

        float wallDist = observations[baseIdx];
        string detectedTag = "None";
        
        // Check one-hot encoding to find detected tag
        string[] tagNames = { "HighWall", "LowWall", "HighWallTrigger", "LowWallTrigger" };
        for (int i = 0; i < tagCount && i < tagNames.Length; i++)
        {
            if (observations[baseIdx + 1 + i] > 0.5f)
            {
                detectedTag = tagNames[i];
                break;
            }
        }

        float opponentDist = observations[baseIdx + 1 + tagCount];

        Debug.Log($"[RaycastDebug] Ray {rayIndex}: " +
                  $"Wall='{detectedTag}' at {wallDist:F2}, " +
                  $"Opponent at {opponentDist:F2}");
    }

    // Public method to enable/disable logging at runtime
    public void SetLoggingEnabled(bool enabled)
    {
        enableLogging = enabled;
    }

    // Public method to log immediately (useful for testing)
    public void LogNow()
    {
        if (raycastLogic != null)
        {
            LogRaycastData();
        }
    }
}
