# Agent Reference Guidelines

## Problem Solved
Previously, GameObjects were hard-coded in the Inspector to reference specific agent GameObjects by name (e.g., "PlayerAgent"). This caused issues when:
- Using different agent GameObjects (e.g., "PlayerAgentTest" instead of "PlayerAgent")
- Deleting or renaming agent GameObjects
- Setting up new arenas or duplicating existing ones

## Solution: Automatic Agent Discovery

### How It Works
Agents are now automatically discovered at runtime using the following strategy:

1. **Hierarchy Structure**: All agents must be under `ArenaManager > Agents`
2. **Component-Based**: Scripts search for `OctagonAgent` components (not GameObject names)
3. **Tag-Based**: Agents are differentiated by their Unity tags:
   - `"PlayerAgent"` tag for the player
   - `"OpponentAgent"` tag for the opponent (in non-solo mode)

### Implementation Pattern

```csharp
void Start()
{
    // Find the Agents container in the arena hierarchy
    Transform agentsContainer = arenaRoot.Find("Agents");
    
    if (agentsContainer == null)
    {
        Debug.LogError("Could not find 'Agents' container!");
        return;
    }
    
    // Get all OctagonAgent components in the Agents container
    OctagonAgent[] foundAgents = agentsContainer.GetComponentsInChildren<OctagonAgent>(true);
    
    // Assign agents based on their tags
    foreach (var agent in foundAgents)
    {
        if (agent.CompareTag("PlayerAgent"))
        {
            playerAgent = agent;
        }
        else if (agent.CompareTag("OpponentAgent"))
        {
            opponentAgent = agent;
        }
    }
    
    // Validate that required agents were found
    if (playerAgent == null)
    {
        Debug.LogError("PlayerAgent not found!");
    }
}
```

### Scripts Updated
- `OctagonWallTrigger.cs` - Auto-finds player/opponent agents
- `OctagonArenaSettings.cs` - Auto-finds player/opponent agents

### Requirements for New Agents

When creating or modifying agents:

1. **Hierarchy**: Place agent under `ArenaManager > Agents`
2. **Component**: Ensure the GameObject has an `OctagonAgent` component
3. **Tag**: Set the correct Unity tag:
   - Player agent: Tag = `"PlayerAgent"`
   - Opponent agent: Tag = `"OpponentAgent"`
4. **GameObject Name**: Can be anything (e.g., "PlayerAgentTest", "PlayerAgent_V2", etc.)

### Benefits

✅ No manual Inspector assignments needed
✅ GameObject names don't matter - only tags and hierarchy
✅ Easy to swap agent implementations
✅ Prevents "wrong agent" bugs
✅ Works with prefab instances automatically
✅ Clear error messages when agents are missing

### Inspector Fields

Agent reference fields are no longer `[SerializeField]` - they're private and auto-assigned:

```csharp
// ❌ OLD: Manual assignment required
[SerializeField] OctagonAgent playerAgent;

// ✅ NEW: Auto-assigned at runtime
private OctagonAgent playerAgent;
```

### Debugging

If agents aren't found, check the console for error messages:
- `"Could not find 'Agents' container"` - Check hierarchy structure
- `"No OctagonAgent components found"` - Check agent has OctagonAgent component
- `"PlayerAgent not found"` - Check agent has correct tag assigned

### Migration Checklist

When updating existing scenes:

- [ ] Ensure all agents are under `ArenaManager > Agents`
- [ ] Verify agent tags are set correctly
- [ ] Clear any old Inspector references (they'll be ignored)
- [ ] Test that agents are found correctly at runtime
- [ ] Check console for any error messages
