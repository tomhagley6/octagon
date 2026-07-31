# Selective Pass-Through Raycast Sensor System

## Overview

This system provides ML-Agents compatible raycasting that can detect objects while allowing rays to pass through specific tagged objects (e.g., opponent agents). The implementation follows ML-Agents best practices by using the SensorComponent architecture.

## Components

### 1. SelectivePassThroughRaycast.cs
The core raycasting logic component. This MonoBehaviour:
- Performs the actual Physics raycasts
- Sorts and filters hits based on tags
- Caches observations for efficiency
- Provides visualization in the Scene view

**Key Features:**
- Pass-through detection: Rays can detect objects "behind" other objects
- Configurable ray spread, count, and length
- One-hot encoding for detected object tags
- Normalized distance observations
- Observation caching to prevent duplicate raycasts per frame

### 2. SelectivePassThroughRaycastSensor.cs
The ISensor implementation that integrates with ML-Agents. This class:
- Implements the ISensor interface required by ML-Agents
- Calls the raycast logic in `Update()` (before observation collection)
- Caches observations for the `Write()` method
- Provides observation specs to the neural network

### 3. SelectivePassThroughRaycastSensorComponent.cs
The SensorComponent that registers the sensor with ML-Agents. This component:
- Extends `SensorComponent` (required by ML-Agents)
- Creates and registers the sensor during agent initialization
- Automatically integrates with the ML-Agents sensor pipeline

## How It Works

### ML-Agents Integration Flow

```
1. Agent Initialization
   └─> InitializeSensors() called by ML-Agents
       └─> Finds all SensorComponents on the agent
           └─> SelectivePassThroughRaycastSensorComponent.CreateSensors()
               └─> Creates SelectivePassThroughRaycastSensor
                   └─> Sensor added to agent's sensor list

2. Every Decision Step
   └─> UpdateSensors() called by ML-Agents
       └─> SelectivePassThroughRaycastSensor.Update()
           └─> InvalidateCache() on raycast logic
           └─> GetObservations() triggers fresh raycasts
           └─> Observations cached in sensor
   
   └─> CollectObservations() called by ML-Agents
       └─> VectorSensor observations collected
       └─> All ISensor.Write() methods called
           └─> SelectivePassThroughRaycastSensor.Write()
               └─> Writes cached observations to output buffer
   
   └─> Observations sent to neural network
```

### Performance Optimizations

1. **Single Raycast Per Frame**: Raycasts are performed once in `Update()`, not multiple times
2. **Observation Caching**: Results are cached and reused when `GetObservations()` is called multiple times
3. **Array Reuse**: Observation arrays are allocated once and reused
4. **Gizmo Optimization**: Scene view visualization uses cached ray data

## Setup Instructions

### For New Agents

1. **Attach Components to Agent GameObject:**
   ```
   Agent GameObject
   ├─ OctagonAgent (or your Agent script)
   ├─ SelectivePassThroughRaycast
   └─ SelectivePassThroughRaycastSensorComponent
   ```

2. **Configure SelectivePassThroughRaycast:**
   - Max Ray Degrees: Field of view (e.g., 66° for ±66° = 132° total). NOTE (outdated): this example value is stale. As of 2026-06-17 maxRayDegrees was changed to 55 (±55° = 110° total) to match the human player camera's horizontal FoV.
   - Rays Per Direction: Number of rays on each side of center (e.g., 3 = 7 total rays)
   - Ray Length: Maximum detection distance
   - Detection Mask: Layers to detect
   - Pass Through Tag: Tag of objects rays should pass through (e.g., "OpponentAgent")
   - Detection Tags: Tags to identify in observations (e.g., "HighWall", "LowWall")

3. **Configure SelectivePassThroughRaycastSensorComponent:**
   - Raycast Logic: Drag the SelectivePassThroughRaycast component here (auto-assigned if on same GameObject)
   - Sensor Name: Unique name for this sensor (default: "SelectivePassThroughRaycastSensor")

4. **Update BehaviorParameters:**
   - Vector Observation Size should now EXCLUDE raycast observations
   - Raycast observations are handled automatically by the sensor component
   - Only include observations from `CollectObservations(VectorSensor sensor)`

### Migrating from Old Manual System

1. **Remove manual raycast handling:**
   - Delete calls to `raycastSensor.GetObservations()` from `CollectObservations()`
   - Remove `[SerializeField] SelectivePassThroughRaycast raycastSensor;` field
   - Remove raycast sensor initialization code from `Awake()`

2. **Add new components:**
   - Add `SelectivePassThroughRaycastSensorComponent` to the agent GameObject
   - Assign the `SelectivePassThroughRaycast` component to it

3. **Update Vector Observation Size:**
   - Old size = manual observations + raycast observations
   - New size = manual observations only
   - Raycast observations are now separate

## Observation Space

### Per Ray Observations (in order):
1. **Primary Hit Distance** (1 float): Normalized distance to first non-pass-through object (0 if no hit)
2. **Object Type One-Hot** (N floats): One-hot encoding for detected object tag
   - Example: [0, 1, 0, 0] = LowWall detected
   - All zeros = unrecognized or no object
3. **Pass-Through Distance** (1 float): Normalized distance to pass-through object (0 if none)

### Total Observation Size
```
Total observations = (2 * raysPerDirection + 1) * (1 + detectionTags.Count + 1)

Example with raysPerDirection=3 and 4 detection tags:
- Total rays = 7
- Observations per ray = 1 + 4 + 1 = 6
- Total observations = 7 * 6 = 42
```

## Debugging

### Visualization
The system provides Scene view visualization:
- **Green rays**: No wall detected
- **Red rays**: Wall detected
- **Yellow spheres**: Wall hit points
- **Cyan spheres**: Pass-through object hit points

### Common Issues

1. **"SelectivePassThroughRaycast component not found"**
   - Ensure SelectivePassThroughRaycast is attached to the agent GameObject
   - Or assign it manually in SelectivePassThroughRaycastSensorComponent inspector

2. **Observation size mismatch**
   - Check BehaviorParameters Vector Observation Size
   - Should only include manual observations, not raycast observations
   - Raycast observations are handled separately by the sensor component

3. **Rays not detecting objects**
   - Check Detection Mask includes the correct layers
   - Verify objects have colliders
   - Ensure tags are correctly assigned

4. **Pass-through not working**
   - Verify Pass Through Tag matches the object's tag exactly
   - Enable "Detect Pass Through Objects" in inspector
   - Check that pass-through objects are on the Detection Mask

## Advantages Over Manual System

1. ✅ **Proper ML-Agents Integration**: Follows framework architecture
2. ✅ **Better Performance**: Single raycast computation per frame
3. ✅ **Observation Caching**: Prevents duplicate work
4. ✅ **Automatic Registration**: No manual sensor setup needed
5. ✅ **Clean Separation**: Agent code doesn't handle sensor details
6. ✅ **Easier Debugging**: Sensors appear in ML-Agents diagnostics
7. ✅ **Standard Pattern**: Other developers can understand the code

## API Reference

### SelectivePassThroughRaycast

**Public Methods:**
- `float[] GetObservations()`: Returns flattened observation array
- `int CalculateObservationSize()`: Returns total observation count
- `void InvalidateCache()`: Forces fresh raycast computation on next call

**Inspector Settings:**
- `maxRayDegrees`: Maximum angle from forward direction
- `raysPerDirection`: Number of rays on each side of center
- `rayLength`: Maximum ray distance
- `sphereRadius`: Radius for SphereCast (0 = standard Raycast)
- `detectionMask`: Physics layers to detect
- `passThroughTag`: Tag for pass-through objects
- `detectPassThroughObjects`: Enable pass-through detection
- `detectionTags`: List of tags to identify in observations

### SelectivePassThroughRaycastSensor

Implements: `ISensor`

**Required Methods:**
- `void Update()`: Called before observations collected
- `int Write(ObservationWriter writer)`: Writes observations to buffer
- `void Reset()`: Called at episode start
- `ObservationSpec GetObservationSpec()`: Returns observation specifications
- `string GetName()`: Returns sensor name
- `CompressionSpec GetCompressionSpec()`: Returns compression settings

### SelectivePassThroughRaycastSensorComponent

Extends: `SensorComponent`

**Required Methods:**
- `ISensor[] CreateSensors()`: Creates and returns sensor instances

**Inspector Settings:**
- `raycastLogic`: Reference to SelectivePassThroughRaycast component
- `sensorName`: Unique sensor identifier

## Future Enhancements

Possible improvements for future versions:

1. **Self-Collision Exclusion**: Automatically ignore agent's own colliders
2. **Multiple Pass-Through Tags**: Support multiple object types that rays pass through
3. **Sentinel Values**: Use -1 instead of 0 for "no detection"
4. **Debug Mode Toggle**: Runtime flag to enable/disable debug logs
5. **Adaptive Ray Count**: Change ray count based on distance or curriculum
6. **Hit Normal Observations**: Include surface normal information
7. **Material/Texture Detection**: Identify surface properties
8. **Batch Raycasting**: Use Unity Jobs system for parallel raycast computation

## License & Credits

Part of the Octagon project by tomhagley6.
Built with Unity ML-Agents Toolkit.
