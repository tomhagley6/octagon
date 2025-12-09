# Troubleshooting: NullReferenceException in UpdateSensors()

## Error Message
```
NullReferenceException: Object reference not set to an instance of an object
Unity.MLAgents.Agent.UpdateSensors () (at ./Library/PackageCache/com.unity.ml-agents@3.0.0-exp.1/Runtime/Agent.cs:1145)
```

## What This Means

The ML-Agents framework is trying to call `Update()` on a sensor in the agent's sensor list, but one of the sensors is null. This typically happens during the initialization phase when sensors are being created.

## Root Causes & Solutions

### 1. SelectivePassThroughRaycast Component Missing
**Symptom:** Error occurs immediately when entering Play mode.

**Solution:**
- Verify that **both** components are attached to your agent GameObject:
  - `SelectivePassThroughRaycast`
  - `SelectivePassThroughRaycastSensorComponent`
- They should be on the **same GameObject** (your agent)

**How to Check:**
1. Select your agent in the Hierarchy
2. Look in the Inspector
3. You should see both components listed

### 2. Raycast Logic Not Assigned in Inspector
**Symptom:** Error occurs and you see this message in Console:
```
[SelectivePassThroughRaycastSensor] SelectivePassThroughRaycast component not found on [AgentName]
```

**Solution:**
1. Select your agent GameObject
2. Find the `SelectivePassThroughRaycastSensorComponent` in Inspector
3. Drag the `SelectivePassThroughRaycast` component into the "Raycast Logic" field
4. Or click the target icon and select it from the list

### 3. Component Execution Order Issue
**Symptom:** Error occurs intermittently or only sometimes.

**Solution:** The updated code now handles this automatically with:
- Early initialization in `Awake()`
- Manual initialization check in `CalculateObservationSize()`
- Defensive null checks throughout

### 4. Multiple SensorComponents on Same Agent
**Symptom:** Error only occurs when you have other sensor components.

**Solution:**
- Ensure sensor names are unique
- Check that each `SensorComponent` is properly configured
- The default name "SelectivePassThroughRaycastSensor" should work unless you have duplicates

## Debug Steps

### Step 1: Check Console for Initialization Messages
When you enter Play mode, you should see these messages:
```
[SelectivePassThroughRaycast] Initialized with 7 rays on [AgentName]
[SelectivePassThroughRaycast] Observation size calculated: 42 (7 rays × 6 obs/ray)
[SelectivePassThroughRaycastSensor] CreateSensors called on [AgentName]
[SelectivePassThroughRaycastSensor] Creating sensor with observation size: 42
[SelectivePassThroughRaycastSensor] Successfully created sensor with 42 observations on [AgentName]
```

**If you DON'T see these messages:**
- The component might not be attached properly
- Or it's disabled in the Inspector

### Step 2: Verify Component Configuration

**SelectivePassThroughRaycast Settings:**
```
Max Ray Degrees: 66
Rays Per Direction: 3
Ray Length: 60
Sphere Radius: 0
Detection Mask: (Select appropriate layers)
Pass Through Tag: "OpponentAgent"
Detect Pass Through Objects: ✓ (checked)
Detection Tags: (Should have 4 entries)
  - HighWall
  - LowWall
  - HighWallTrigger
  - LowWallTrigger
```

**SelectivePassThroughRaycastSensorComponent Settings:**
```
Raycast Logic: (Should reference SelectivePassThroughRaycast component)
Sensor Name: "SelectivePassThroughRaycastSensor"
```

### Step 3: Check BehaviorParameters

**Important:** Your Vector Observation Space Size should be updated:
- **OLD:** 43 (1 mode + 42 raycast)
- **NEW:** 1 (only mode, raycast handled by sensor)

If you haven't updated this, you'll get observation size mismatch errors.

### Step 4: Clean and Rebuild

Sometimes Unity's cache gets out of sync:
1. Close Unity
2. Delete the `Library` folder in your project
3. Reopen Unity (it will rebuild the Library)
4. Try again

### Step 5: Check Script Compilation

Make sure all scripts compiled successfully:
1. Open Console (Ctrl/Cmd + Shift + C)
2. Click the "Clear" button
3. Check for any compilation errors
4. If you see errors, they must be fixed first

## What Was Fixed in the Updated Code

The improved implementation now includes:

1. **Defensive Null Checks**: Every method checks for null references before accessing them
2. **Early Initialization**: `Awake()` is called early to ensure components are ready
3. **Manual Initialize**: `Initialize()` method can be called explicitly if needed
4. **Graceful Fallbacks**: If something goes wrong, empty arrays are returned instead of null
5. **Better Logging**: Debug messages help identify where the issue occurs
6. **Exception Handling**: Try-catch blocks prevent crashes and log useful error information

## Still Having Issues?

If the error persists after trying the above:

1. **Take a screenshot** of:
   - Your agent's Inspector showing all components
   - The full error message in Console
   - The SelectivePassThroughRaycastSensorComponent settings

2. **Check the Console** for these specific error messages:
   - `SelectivePassThroughRaycast component not found`
   - `Invalid observation size`
   - `Failed to create sensor instance`
   - `Exception during sensor creation`

3. **Try the Debugger Component**:
   - Add `RaycastSensorDebugger` to your agent
   - Enable "Enable Logging"
   - See if observations are being generated

4. **Temporarily Remove** the SensorComponent:
   - Disable `SelectivePassThroughRaycastSensorComponent` in Inspector
   - See if the agent works without raycasts
   - If yes, the issue is in the sensor setup
   - If no, the issue might be elsewhere

## Quick Fix Checklist

- [ ] Both components attached to agent GameObject
- [ ] SelectivePassThroughRaycast has 4 detection tags configured
- [ ] SelectivePassThroughRaycastSensorComponent has raycast logic assigned
- [ ] BehaviorParameters Vector Observation Space Size = 1
- [ ] No compilation errors in Console
- [ ] Initialization messages appear in Console when entering Play mode
- [ ] Detection Mask is configured in SelectivePassThroughRaycast
- [ ] Tags exist in Unity Tag Manager (HighWall, LowWall, etc.)

## Contact/Support

If you've tried everything and still have issues, make sure to provide:
1. Full error stack trace
2. Screenshots of Inspector showing component configuration
3. Console output when entering Play mode
4. Unity version and ML-Agents version

The error should now be resolved with the improved null checking and initialization logic!
