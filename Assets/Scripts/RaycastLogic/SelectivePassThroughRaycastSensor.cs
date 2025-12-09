using UnityEngine;
using Unity.MLAgents.Sensors;

/// <summary>
/// ISensor implementation for selective pass-through raycasting.
/// This sensor computes raycast data in Update() and caches it for efficient observation collection.
/// </summary>
public class SelectivePassThroughRaycastSensor : ISensor
{
    private string m_Name;
    private SelectivePassThroughRaycast m_RaycastLogic;
    private float[] m_CachedObservations;
    private ObservationSpec m_ObservationSpec;

    public SelectivePassThroughRaycastSensor(string name, SelectivePassThroughRaycast raycastLogic, int observationSize)
    {
        m_Name = name;
        m_RaycastLogic = raycastLogic;
        m_ObservationSpec = ObservationSpec.Vector(observationSize);
    }

    /// <summary>
    /// Update is called before CollectObservations. This is where we compute the raycast data.
    /// </summary>
    public void Update()
    {
        if (m_RaycastLogic != null)
        {
            m_CachedObservations = m_RaycastLogic.GetObservations();
            // Debug every 60 frames to avoid spam
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{m_Name}] Update() called - Frame {Time.frameCount}, " +
                         $"Observations: {m_CachedObservations?.Length ?? 0}");
            }
        }
        else if (m_CachedObservations == null)
        {
            m_CachedObservations = new float[m_ObservationSpec.Shape[0]];
            Debug.LogWarning($"[{m_Name}] RaycastLogic is null! Creating empty observation array.");
        }
    }

    /// <summary>
    /// Write the cached observations to the output buffer.
    /// </summary>
    public int Write(ObservationWriter writer)
    {
        if (m_CachedObservations != null)
        {
            for (int i = 0; i < m_CachedObservations.Length; i++)
            {
                writer[i] = m_CachedObservations[i];
            }
            return m_CachedObservations.Length;
        }
        Debug.LogError($"[{m_Name}] Write() called but m_CachedObservations is NULL!");
        return 0;
    }

    public void Reset()
    {
        // Reset is called at the start of each episode
        // We don't need to do anything special here since Update() will be called next
    }

    public ObservationSpec GetObservationSpec()
    {
        return m_ObservationSpec;
    }

    public string GetName()
    {
        return m_Name;
    }

    public CompressionSpec GetCompressionSpec()
    {
        return CompressionSpec.Default();
    }

    public byte[] GetCompressedObservation()
    {
        return null;
    }
}
