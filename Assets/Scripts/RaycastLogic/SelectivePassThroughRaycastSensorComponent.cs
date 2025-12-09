using UnityEngine;
using Unity.MLAgents.Sensors;

/// <summary>
/// SensorComponent for selective pass-through raycasting.
/// Attach this component to an agent to automatically register the raycast sensor with ML-Agents.
/// </summary>
public class SelectivePassThroughRaycastSensorComponent : SensorComponent
{
    [SerializeField] 
    [Tooltip("Reference to the SelectivePassThroughRaycast component that performs the raycasting logic")]
    private SelectivePassThroughRaycast raycastLogic;

    [SerializeField]
    [Tooltip("Name for this sensor. Must be unique among all sensors on the agent.")]
    private string sensorName = "SelectivePassThroughRaycastSensor";

    // Cache the created sensors to prevent recreation on every Inspector redraw
    private ISensor[] m_CachedSensors;

    /// <summary>
    /// Creates the sensor(s) for this component.
    /// Called by ML-Agents during agent initialization and also by the Editor Inspector.
    /// We cache the result to avoid recreating sensors on every Inspector redraw.
    /// </summary>
    public override ISensor[] CreateSensors()
    {
        // Return cached sensors if they already exist
        if (m_CachedSensors != null && m_CachedSensors.Length > 0)
        {
            Debug.Log($"[{sensorName}] RETURNING CACHED sensor (instance: {m_CachedSensors[0].GetHashCode()}) " +
                     $"- Frame: {Time.frameCount}, Cache valid: {m_CachedSensors[0] != null}");
            return m_CachedSensors;
        }
        
        // Get the raycast logic component if not already assigned
        if (raycastLogic == null)
        {
            raycastLogic = GetComponent<SelectivePassThroughRaycast>();
        }
        
        if (raycastLogic == null)
        {
            Debug.LogError($"SelectivePassThroughRaycast component not found on {gameObject.name}");
            return System.Array.Empty<ISensor>();
        }
        
        // Calculate observation size using the size calculation method
        // Don't call GetObservations() here as it triggers raycasting before scene is ready
        int observationSize = raycastLogic.GetObservationSize();
        
        // Create and cache the sensor
        var sensor = new SelectivePassThroughRaycastSensor(sensorName, raycastLogic, observationSize);
        m_CachedSensors = new ISensor[] { sensor };
        
        Debug.Log($"[{sensorName}] CREATING NEW sensor with {observationSize} observations " +
                 $"(instance: {sensor.GetHashCode()}) on {gameObject.name} - Frame: {Time.frameCount}");
        
        return m_CachedSensors;
    }
    
    /// <summary>
    /// Debug: Monitor when component is disabled
    /// </summary>
    private void OnDisable()
    {
        Debug.LogWarning($"[{sensorName}] OnDisable called on {gameObject.name} - " +
                        $"Frame: {Time.frameCount}, Cache status: {(m_CachedSensors != null ? "EXISTS" : "NULL")}");
        // NOT clearing cache - investigating if this is the issue
        // m_CachedSensors = null;
    }
    
    /// <summary>
    /// Debug: Monitor when component is enabled
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"[{sensorName}] OnEnable called on {gameObject.name} - " +
                 $"Frame: {Time.frameCount}, Cache status: {(m_CachedSensors != null ? "EXISTS" : "NULL")}");
    }
}
