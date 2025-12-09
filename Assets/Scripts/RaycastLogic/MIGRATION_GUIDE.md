# Migration Guide: Manual Raycast to SensorComponent System

## What Changed

Your raycast system has been upgraded from a manual implementation to a proper ML-Agents SensorComponent architecture.

## Old System (Manual)
```
OctagonAgent.cs:
- Had [SerializeField] SelectivePassThroughRaycast raycastSensor;
- Manually called raycastSensor.GetObservations() in CollectObservations()
- Raycasts performed every time CollectObservations() was called
- Debug logs cluttering console

SelectivePassThroughRaycast.cs:
- Simple MonoBehaviour
- No caching
- CastRays() called multiple times per frame (GetObservations + OnDrawGizmos)
```

## New System (SensorComponent)
```
OctagonAgent.cs:
- No raycast-specific code
- CollectObservations() only handles manual observations
- Cleaner, simpler

SelectivePassThroughRaycast.cs:
- Still a MonoBehaviour (core logic)
- Now has observation caching
- CastRays() called only once per frame
- Debug logs removed

NEW: SelectivePassThroughRaycastSensor.cs
- Implements ISensor interface
- Integrates with ML-Agents pipeline

NEW: SelectivePassThroughRaycastSensorComponent.cs
- Extends SensorComponent
- Auto-registers sensor with ML-Agents
```

## Setup Steps for Your Project

### 1. Update Agent GameObjects in Unity Editor

For each agent (PlayerAgent and OpponentAgent):

**A. Add the new SensorComponent:**
1. Select the agent GameObject
2. Add Component → `SelectivePassThroughRaycastSensorComponent`
3. The component should auto-find the `SelectivePassThroughRaycast` component
4. If not, drag the `SelectivePassThroughRaycast` component to the "Raycast Logic" field

**B. Update BehaviorParameters:**
1. Select the agent GameObject
2. Find the BehaviorParameters component
3. Update "Vector Observation" → "Space Size":
   - OLD: Was (1 + 42) = 43 if you had 1 mode observation + 42 raycast observations
   - NEW: Should be just 1 (only the solo/social mode observation)
   - The 42 raycast observations are now handled automatically by the sensor

### 2. Verify Configuration

**SelectivePassThroughRaycast settings:**
- Max Ray Degrees: 66
- Rays Per Direction: 3 (creates 7 total rays)
- Ray Length: 60
- Detection Mask: Set to include Wall layers
- Pass Through Tag: "OpponentAgent"
- Detect Pass Through Objects: ✓ (checked)
- Detection Tags: 
  - HighWall
  - LowWall
  - HighWallTrigger
  - LowWallTrigger

**SelectivePassThroughRaycastSensorComponent settings:**
- Raycast Logic: (auto-assigned or drag SelectivePassThroughRaycast)
- Sensor Name: "SelectivePassThroughRaycastSensor"

### 3. Optional: Add Debugger

If you want to debug raycast observations:
1. Add Component → `RaycastSensorDebugger` to agent
2. Assign the `SelectivePassThroughRaycast` component
3. Enable "Enable Logging" when needed
4. Adjust "Log Interval" to control frequency

### 4. Test in Unity

**Before training:**
1. Enter Play mode
2. Check Console for errors
3. Verify Scene view shows raycast visualizations (green/red rays, yellow/cyan spheres)
4. If using debugger, check that observations are being logged correctly

**During training:**
1. Start training with your existing ML-Agents setup
2. Monitor TensorBoard for any observation-related issues
3. The observation space should match the new configuration

## Expected Observation Space

With the new system:

**Manual Observations (from CollectObservations):**
- Mode: 1 float (solo=1, social=2)

**Automatic Sensor Observations (from SelectivePassThroughRaycastSensor):**
- 7 rays × 6 observations per ray = 42 floats
  - Per ray: [wall_distance, is_high_wall, is_low_wall, is_high_trigger, is_low_trigger, opponent_distance]

**Total: 1 + 42 = 43 observations**

But you configure BehaviorParameters with:
- Vector Observation Space Size: 1 (manual only)
- The sensor adds its 42 automatically

## Common Issues & Solutions

### Issue 1: "Observation size mismatch"
**Solution:** Update BehaviorParameters Vector Observation Space Size to exclude raycast observations (should be 1).

### Issue 2: "SelectivePassThroughRaycast component not found"
**Solution:** Ensure both SelectivePassThroughRaycast and SelectivePassThroughRaycastSensorComponent are on the same GameObject, or manually assign the reference.

### Issue 3: "No raycast visualizations in Scene view"
**Solution:** Enter Play mode. Gizmos only work during runtime.

### Issue 4: "Training starts but agent doesn't learn"
**Solution:** Check that Detection Mask includes the correct layers and tags are assigned properly.

### Issue 5: "Rays aren't passing through opponent"
**Solution:** Verify the opponent has the exact tag "OpponentAgent" and "Detect Pass Through Objects" is enabled.

## Performance Improvements

The new system provides:
- ✅ 50% reduction in raycast calls (from 2x per frame to 1x)
- ✅ Observation caching prevents redundant computation
- ✅ Follows ML-Agents best practices
- ✅ Better integration with ML-Agents profiling tools

## Rollback (If Needed)

If you need to revert to the old system:

1. Remove `SelectivePassThroughRaycastSensorComponent` from agents
2. Re-add `[SerializeField] SelectivePassThroughRaycast raycastSensor;` to OctagonAgent.cs
3. Restore the old CollectObservations code
4. Update BehaviorParameters to include raycast observations in Vector Observation Space Size

However, the new system is recommended for better performance and maintainability.

## Next Steps

1. ✅ Update all agent GameObjects with the new component
2. ✅ Update BehaviorParameters vector observation sizes
3. ✅ Test in Unity Editor (Play mode)
4. ✅ Run inference test with a trained model
5. ✅ Start new training runs
6. ✅ Monitor for any issues

## Questions?

If you encounter issues:
1. Check Unity Console for error messages
2. Verify all components are properly assigned
3. Compare your setup with the configuration above
4. Use RaycastSensorDebugger to inspect observations
