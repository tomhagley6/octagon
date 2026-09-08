# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## General Points
- You are allowed to say 'I don't know'
- Use direct quotes for factual grounding
- Verify claims with citations

## Project Overview

Octagonal arena environment for reward-based decision-making tasks (analogous to rodent behavioral experiments). Participants choose between coloured walls (Red=High reward, Blue=Low reward) at varying angular separations.

While this branch is primarily used for training RL agents via Unity ML-Agents, the game is also playable by human participants over the network. The `TrialLogic/` folder contains scripts for both modes: `OctagonAgent.cs` and `OctagonWallTrigger.cs` drive RL agent behaviour, while `WallTrigger.cs` handles networked human-player trigger interactions (using `NetworkObject.IsLocalPlayer`). Human movement/camera control lives in `PlayerControl/` (`PlayerMovement.cs`, `MouseLook.cs`). Shared trial infrastructure (`GameManager.cs`, `TrialHandler.cs`) is used by both modes.

- **Unity version**: 2022.3.13f1
- **ML-Agents**: Python package 1.1.0, Unity package 3.0.0-exp.1
- **Python environment**: conda env `mlagents` (Python 3.10, PyTorch 2.8.0, CUDA-accelerated)

## Training Commands

```bash
# Activate the conda environment
conda activate mlagents

# Solo agent training
mlagents-learn Assets/Scripts/MLConfigFiles/SoloConfig.yaml --run-id=<run-id>

# Social (two-agent self-play) training
mlagents-learn Assets/Scripts/MLConfigFiles/SocialConfig.yaml --run-id=<run-id>

# Curriculum-based solo training
mlagents-learn Assets/Scripts/MLConfigFiles/CurriculumSoloConfig.yaml --run-id=<run-id>

# Resume from checkpoint
mlagents-learn <config.yaml> --run-id=<run-id> --resume

# Monitor training
tensorboard --logdir results
```

After running `mlagents-learn`, open **SoloOctagonStage** in the Unity Editor and press Play to
connect the simulation to the trainer. This is the training scene for both Solo and Social runs
-- see Scenes below for the three settings that switch between the two modes. Make sure the
agent's `m_BehaviorName` matches the behavior ID in the config you launched, or the handshake
will not find the behaviour.

## Architecture

### Scenes

- **SoloOctagonStage** -- THE training scene, for **both** Solo and Social runs. Despite the
  name it is not solo-only; it has two agents in it and you switch modes in the Editor by:
  1. de/re-activating the second agent GameObject (`PlayerAgent2`, tag `OpponentAgent`),
  2. setting `m_BehaviorName` on the agent(s) to `OctagonAgentSolo` or `OctagonAgentSocial`
     to match the behavior ID in the config you are launching,
  3. ticking/unticking `soloMode` on `OctagonArenaSettings` (`OctagonArenaSettings.cs:17`).
  Also used for in-Editor visualisation of a trained model: leave `m_BehaviorType` on
  Default and assign an `.onnx` to `m_Model` -- Default falls back to the assigned model
  when no `mlagents-learn` trainer is attached. NOTE: assigning a model dirties the scene
  file; revert it before committing so the training scene stays model-free.
- **TournamentOctagonStage** -- inference only, never trained. Duplicated from
  SoloOctagonStage in `c32fbb7`. Exists solely because a cross-model tournament needs two
  DISTINCT behavior names so ML-Agents can key two different policies:
  - `CompetitiveAgent1` -- tag `PlayerAgent`, log key `"0"`
  - `CompetitiveAgent2` -- tag `OpponentAgent`, log key `"1"`
  Both are BehaviorType Default so the policies can run in Python. The driver is
  `agent_training/tournament/tournament_func.py`, which stages
  `results/<tournament_id>/<behaviour>/checkpoint.pt` per matchup; the names there are
  hardcoded and must match this scene exactly. These names appear in NO training YAML.
- **SocialOctagonStage** -- UNUSED. Superseded by SoloOctagonStage in social mode (see
  `dc01ebd`). Kept for reference only; do not add to it.

**Keeping the scenes in sync:** SoloOctagonStage and TournamentOctagonStage are near-identical
twins -- same `PlayerAgent.prefab`, same 432 property overrides, differing at HEAD only in
`m_Model`, `m_BehaviorName` and `m_Name`. Make behavioural changes in
`Assets/Prefabs/Player/PlayerAgent.prefab` so they propagate to both automatically. If a
change genuinely must be a scene-level override, apply it to both scenes in the same commit.
No C# branches on scene name, so the scenes are otherwise interchangeable.

### Scene Hierarchy
`ArenaManager > Agents > [PlayerAgent, OpponentAgent]` -- agents are auto-discovered at runtime by `OctagonAgent` component + Unity tags (`PlayerAgent`, `OpponentAgent`), not by GameObject name. See `AGENT_REFERENCE_GUIDELINES.md` for the full pattern.

### Core Scripts (`Assets/Scripts/`)

**Trial Logic** (`TrialLogic/`):
- `OctagonAgent.cs` -- Base ML-Agents `Agent` subclass. 3 discrete actions (forward/back, strafe, rotate). 42 vector observations. Movement: 10 m/s, Turn: 360 deg/s.
- `OctagonArenaSettings.cs` -- Arena config: wall assignment, colouring (red/blue), trial types (HighLow, ForcedHigh, ForcedLow), wall separations (1/2/4 = 45/90/180 deg at 50/25/25% probabilities), ITI timing, `isArenaReady` sync flag.
- `OctagonWallTrigger.cs` -- Collision detection at walls, reward assignment, logging.
- `GameManager.cs` -- Server-authoritative state via Netcode `NetworkVariable`s (active walls, triggers, scores).
- `TrialHandler.cs` -- Client-side trial execution, wall colouring/washing, ITI delays.

**Reward values** (defined in `Utility/GeneralGlobals.cs`):
- High wall: +1.0, Low wall: +0.4, Loss: -0.1, Step penalty: -1e-3 (non-idle)
- RND intrinsic motivation strength: 0.01

**Raycast Sensor** (`RaycastLogic/`):
- Custom `SelectivePassThroughRaycastSensor` -- ISensor implementation with filtered raycasts for wall/opponent/ground observations. MaxRayDegrees: 66.

**Networking** (`Networking/`):
- Netcode for GameObjects with Unity Relay service support.

**Logging** (`Logging/`):
- CSV output: Episode, Step, Wall IDs, Time, Position, Rotation, Reward.
- Output dirs: `SoloRaycastLogs/`, `SocialRaycastLogs/`.

**Utilities** (`Utility/`):
- `GeneralGlobals.cs` -- All global constants (reward values, timings, colours, key bindings).
- `WeightedList.cs` -- Probability sampling for wall separations and trial types.

### Training Configs (`Assets/Scripts/MLConfigFiles/`)

All configs use PPO with LSTM memory (256 size, sequence length 64) and RND curiosity:

| Config | Behavior ID | Max Steps | Buffer | Self-Play |
|--------|------------|-----------|--------|-----------|
| `SoloConfig.yaml` | OctagonAgentSolo | 3M | 10240 | No |
| `SocialConfig.yaml` | OctagonAgentSocial | 4M | 20480 | Yes (window=8, save/swap=150k) |
| `CurriculumSoloConfig.yaml` | OctagonAgentSolo | varies | varies | No (5 progressive lessons) |

The Behavior ID column must match `m_BehaviorName` on the agent in SoloOctagonStage before you
press Play. `CompetitiveAgent1`/`CompetitiveAgent2` deliberately appear in no config here --
they are inference-only names belonging to TournamentOctagonStage.

### Key Unity Tags
`PlayerAgent`, `OpponentAgent`, `Wall`, `WallTrigger`, `HighWall`, `LowWall`, `HighWallTrigger`, `LowWallTrigger`

## Working with Models

- Trained checkpoints: `results/<date_group>/<date>/<run_id>/` and `Assets/Models/<date_group>/`
- Checkpoint format: `.pt` (PyTorch) and `.onnx` (for Unity inference)
- To use a pre-trained model as initialisation, set `init_path` in the training YAML config
- Checkpoints saved every 100k steps

