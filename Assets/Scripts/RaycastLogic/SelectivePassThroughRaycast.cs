using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SelectivePassThroughRaycast : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float maxRayDegrees = 66f; // Field of view is 2 * maxRayDegrees
    [SerializeField] private int raysPerDirection = 3; // 2n+1 total rays
    [SerializeField] private float rayLength = 60f; // Easily long enough for Octagon arena
    [SerializeField] private float sphereRadius = 0f; // No sphere at the ray terminus 
    
    [Header("Detection Settings")]
    [SerializeField] private LayerMask detectionMask; // All layers to detect
    
    [Header("Pass-Through Settings")]
    [SerializeField] private string passThroughTag = "OpponentAgent"; // Rays pass through this tag
    [SerializeField] private bool detectPassThroughObjects = true; // Observe passed-through objects
    
    [Header("Detection Tags")]
    [SerializeField] private List<string> detectionTags = new List<string> 
    { 
        "HighWall",
        "LowWall",
        "HighWallTrigger",   // 260209 Removing these for now to see if agents can learn the interaction zone implicitly
                              // 260210 Added back in for testing, but removed soloMode obs
        "LowWallTrigger",
    };

    private List<RayData> rayDataList;
    private RaycastHit[] hitBuffer = new RaycastHit[20]; // Buffer for raycast hits

    private int totalRays;
    
    void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Ensure the raycast system is initialized.
    /// Called from Awake() and also before first use to handle early initialization.
    /// </summary>
    private void Initialize()
    {
        if (rayDataList == null)
        {
            totalRays = 2 * raysPerDirection + 1;
            rayDataList = new List<RayData>(totalRays);
        }
    }

    /// <summary>
    /// Calculate the total observation size for the sensor.
    /// Used by the SensorComponent during initialization.
    /// </summary>
    public int GetObservationSize()
    {
        int totalRays = 2 * raysPerDirection + 1;
        // Per ray: 1 distance + N tag encodings + 1 opponent distance
        int obsPerRay = 1 + detectionTags.Count + 1;
        return totalRays * obsPerRay;
    }


    private List<RayData> CastRays()
    {
        Initialize(); // Ensure initialized before use
        rayDataList.Clear();
        
        for (int i = 0; i < totalRays; i++)
        {
            float angle = Mathf.Lerp(-maxRayDegrees, maxRayDegrees, (float)i / (totalRays - 1)); // 2+ rays required
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            RaycastHit[] allHits;
            if (sphereRadius > 0)
            {
                allHits = Physics.SphereCastAll(transform.position, sphereRadius, direction, rayLength, detectionMask);
            }
            else
            {
                allHits = Physics.RaycastAll(transform.position, direction, rayLength, detectionMask);
            }
            
            var sortedHits = allHits.OrderBy(h => h.distance).ToList();
            

            // RaycastHit? wallHit = sortedHits.FirstOrDefault(h => !h.collider.CompareTag(passThroughTag));
            // RaycastHit? opponentHit = detectPassThroughObjects
            //  ? sortedHits.FirstOrDefault(h => h.collider.CompareTag(passThroughTag))
            //  : null; // Keeping pass-through detection flexible here

            // Using 'wallHit' here because currently all of the detected objects besides OpponentAgent are 
            // wall related
            /* Changed to Cast the IEnumerable result of .Where to RaycastHit? to avoid
            issues with FirstOrDefault returning default(RaycastHit) which is not nullable,
            and so instead returns a RaycastHit with all fields defaulted (including distance 0) */
            RaycastHit? wallHit = sortedHits
                                    .Where(h => !h.collider.CompareTag(passThroughTag))
                                    .Cast<RaycastHit?>()
                                    .FirstOrDefault();
            
            RaycastHit? opponentHit = detectPassThroughObjects
             ? sortedHits
                .Where(h => h.collider.CompareTag(passThroughTag))
                .Cast<RaycastHit?>()
                .FirstOrDefault()
             : null; // Keeping pass-through detection flexible here
                        
             // Debugging
             if (opponentHit.HasValue && wallHit.HasValue)
            {
                bool isPassingThrough = opponentHit.Value.distance < wallHit.Value.distance; // This logic is sensible assuming from the same ray
                Debug.Log($"Ray {i}: Opponent at {opponentHit.Value.distance:F2}m, " +
                $"Wall at {wallHit.Value.distance:F2}m - " + 
                $"Passing through: {isPassingThrough}");
            }
                        
            rayDataList.Add(new RayData
            {
                angle = angle,
                direction = direction,
                wallHit = wallHit,
                opponentHit = opponentHit
            });
        }
        
        return rayDataList;
    }
    
    // Convert to flat observation array for ML-Agents
    public float[] GetObservations()
    {
        var rayData = CastRays();
        
        // Observation per ray:
        // - 1 float: primary hit distance (normalized, 0 if no hit)
        // - N floats: one-hot encoding for primary hit tag
        // - 1 float: closest pass-through object distance (0 if none)
        /* Currently no one-hot encoding vector for pass-through objects 
        because only OpponentAgent is included. If this changes, an encoding
        vector can be added. */
        int obsPerRay = 1 + detectionTags.Count + 1; 
        float[] observations = new float[totalRays * obsPerRay];
        
        for (int rayIdx = 0; rayIdx < rayData.Count; rayIdx++)
        {
            var ray = rayData[rayIdx];
            int baseIdx = rayIdx * obsPerRay;
            
            // Primary hit distance
            if (ray.wallHit.HasValue)
            {
                // Keep distance normalised for efficient learning
                observations[baseIdx] = ray.wallHit.Value.distance / rayLength; 
                
                // One-hot encode primary hit tag
                string hitTag = ray.wallHit.Value.collider.tag;
                int tagIdx = detectionTags.IndexOf(hitTag);
                if (tagIdx >= 0)
                {   
                    // Any non-distance tag hits go after the distance observation
                    // which is baseIdx + 0
                    // e.g. HighWall is baseIdx + 1 + 0
                    observations[baseIdx + 1 + tagIdx] = 1f; 
                }
            }
            // else all zeros (no primary hit)
            
            // Opponent (pass-through) hit distance
            int opponentIdx = baseIdx + 1 + detectionTags.Count;
            if (ray.opponentHit.HasValue)
            {
                // pass-through object (opponent) distance
                observations[opponentIdx] = ray.opponentHit.Value.distance / rayLength;
            }
            // Else distance is reported as 0 (no pass-through object)
            // Can double check to see if this is a good way to implement for the network!
            else
            {
                observations[opponentIdx] = 0f; // This value means 'no opponent agent'
            }
        }
        
        return observations;
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        var rayData = CastRays();
        
        foreach (var ray in rayData)
        {
            Gizmos.color = ray.wallHit.HasValue ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, ray.direction * rayLength);
            
            if (ray.wallHit.HasValue)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(ray.wallHit.Value.point, 0.3f);
            }
            
            if (ray.opponentHit.HasValue)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(ray.opponentHit.Value.point, 0.2f);
            }
        }
    }

    private struct RayData
    {
        public float angle;
        public Vector3 direction;
        public RaycastHit? wallHit;
        public RaycastHit? opponentHit;
    }
}