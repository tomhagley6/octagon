# Tournament README

> **⚠️ Status (2026-06-24): this Octagon-side approach is DISCONTINUED.**
> The mechanism documented below — a standalone build (no `mlagents-learn`) with a Unity-side
> `TournamentManager` loading ONNX per agent via `--player_model` / `--opp_model` — works, but it
> diverges from the project's established inference pipeline in `agent_training` (it reimplements
> `run_eval`, needs every model manually imported into Unity as an asset so it doesn't scale to
> run_ids on disk, and runs outside `mlagents-learn`). The tournament is being rebuilt **in the
> `agent_training` repo**, driving cross-model inference through `mlagents-learn` (two distinct
> behavior names + per-matchup config patch), reusing `launch_inference_sim` / `run_eval` /
> `patch_agents_yaml` and emitting the output format `octagon_analysis` already consumes.
> See `/home/tom/repos/agent_training/Tournament_plan.md` for the new plan.
>
> This file is **retained for its development changelog** (§8) — the dead ends and fixes recorded
> there carry useful context (notably why `Agent.SetModel` was abandoned). The `TournamentManager.cs`,
> the `OctagonAgent` Awake override, and `tournament/run_tournament.py` are kept on the archived
> branch but are not the path forward.

Documents everything added on the **`tom-dev_raycast-agents-tournament`** branch for
running round-robin tournaments between trained Octagon models, to compare strategies
(e.g. "centre" vs "surround") under competition.

> Scope: this file covers **only** tournament-relevant code. General project docs live
> in the repo-root `CLAUDE.md`.

---

## 1. What a tournament does

Each model plays every other model (and itself) in the two-agent social arena. For every
matchup we run a fixed number of trials headless and log behaviour, then build a payoff
matrix (who beats whom) and label each model's strategy from its position traces. The goal
is to answer: which strategy is strongest under competition, and how do strategies fare
against themselves and each other.

---

## 2. Why standalone inference, not the `mlagents-learn` pipeline

The normal pipeline runs the build as an env under `mlagents-learn`, which keeps the
**communicator on**. Two reasons that cannot drive a cross-model tournament:

1. **`SetModel` is ignored with a communicator.** With a trainer connected, each agent uses
   a `RemotePolicy` (policy from the Python checkpoint); the Unity-assigned model is ignored
   — see `BehaviorParameters.GeneratePolicy` (the `BehaviorType.Default` → `IsCommunicatorOn`
   branch).
2. **One policy per behavior name.** `SocialConfig.yaml` defines a single behavior
   `OctagonAgentSocial` with `self_play`; both agents share it (differing only by `team_id`).
   `mlagents-learn --inference` loads one policy for that behavior, so it would apply the
   **same** model to both agents — it cannot put model A on `PlayerAgent` and model B on
   `OpponentAgent`.

**Therefore the tournament runs the build standalone (communicator OFF).** In that mode each
agent uses its own Sentis model, and `Agent.SetModel` lets a CLI argument assign a *different*
model per agent per matchup — fully scriptable.

**Trade-off:** the policy comes from the exported `.onnx` rather than the `.pt` checkpoint
(behaviourally equivalent — same weights). Set **Deterministic Inference = true** on both
agents so action selection is argmax, not sampled, making runs reproducible. With that, all
trial-to-trial variation comes from the stochastic arena (wall placement / separation), which
is what we want.

Alternatives considered and rejected: two distinct behavior names with per-behavior
checkpoints under `mlagents-learn` (needs a regenerated config + behavior rename per matchup);
manually assigning two ONNX in the Inspector and rebuilding per matchup (not scriptable).

---

## 3. Code changes on this branch

### `Assets/Scripts/Scene/TournamentManager.cs` (new)
Scene-singleton registry of candidate models.
- `entrantModels` (serialized `List<ModelAsset>`) — drag every entrant `.onnx` in once.
  Models are **Sentis `ModelAsset`s** (this ML-Agents 3.0.0-exp.1 build uses Unity Sentis,
  not Barracuda).
- `GetModelByName(name)` resolves an entrant by asset name (the `.onnx` filename without
  extension, e.g. `OctagonAgentSocial_260126_04`).
- Writes `tournament_manifest.json` into `--sim_out`, recording the requested args **and**
  the model actually applied to each role — so analysis can recover (and verify) the matchup
  without parsing folder names.

### `Assets/Scripts/TrialLogic/OctagonAgent.cs` (edited, in `Awake()`)
- Reads `--player_model` / `--opp_model` by the agent's tag, resolves the entrant via
  `TournamentManager`, and assigns it **directly** to `BehaviorParameters.Model`.
- **Why `Awake` + direct assignment, not `Agent.SetModel()`:** `Awake` runs before the policy is
  generated in `LazyInitialize()`, so the agent simply initialises with the chosen model — exactly
  like an Inspector-assigned agent. `SetModel()` is deliberately avoided: it calls
  `NotifyAgentDone()` at runtime, which clears the agent's action buffers and disrupts the first
  episode's arena setup (it left the agent un-positioned, so it walked out of the scene). (See the
  §8 changelog for how this was found.)
- Falls back to the Inspector-assigned model when no arg is supplied (e.g. editor runs).
- Registers the applied model with `TournamentManager` for the manifest.

### `tournament/run_tournament.py` (new, repo root — outside `Assets/`)
The Python driver (see §6).

---

## 4. One-time Unity scene setup (`TournamentOctagonScene`)

1. **TournamentManager**: add an empty GameObject, attach `TournamentManager`, and drag every
   entrant model into `entrantModels`.
2. **Agent behavior type** — choose one:
   - **`InferenceOnly`**: you **must** assign any entrant as the Inspector *fallback* model on
     each agent. `BehaviorParameters.GeneratePolicy` throws *"Can't use Behavior Type
     InferenceOnly without a model"* during `LazyInitialize`, which runs **before**
     `Initialize()`/`SetModel`. The fallback satisfies that; `SetModel` then swaps it.
   - **`Default`** (alternative): no fallback model needed (it falls back to a harmless
     heuristic policy until `SetModel` runs), and the same scene still works under
     `mlagents-learn` for training. Only caveat: if you ever launch it under a trainer it will
     use the Python policy, not the ONNX.
3. **Deterministic Inference = true** on both agents (reproducibility — see §2).
4. Confirm the two agents carry the `PlayerAgent` / `OpponentAgent` tags.

---

## 5. Build + validate (do this before any large sweep)

The tournament scene is a duplicate of `SoloOctagonStage`, which runs inference standalone with no
extra setup (no Netcode/host — that's only for human cross-PC play). Always smoke-test one matchup
before trusting a full sweep.

### 5a. Build the player (Unity Editor)
File → Build Settings → enable **`TournamentOctagonScene`** as the active scene → Build →
save as e.g. `builds/build_tournament_test.x86_64`.

### 5b. Run the checks in a normal terminal (not the editor, no conda/trainer)
From the repo root `/home/tom/Unity/Octagon`. Replace `A`/`B` with two real entrant names.

```bash
cd /home/tom/Unity/Octagon

# Visual check — opens a Unity window so you can watch both agents play
./builds/build_tournament_test.x86_64 --sim_eps 3 \
  --sim_out /tmp/ttest --player_model A --opp_model B -logFile /tmp/ttest/unity.log

# Headless check — no window (this is how the driver runs)
./builds/build_tournament_test.x86_64 -batchmode -nographics -logFile /tmp/ttest2/unity.log \
  --sim_eps 3 --sim_out /tmp/ttest2 --player_model A --opp_model B
```

### 5c. Success criteria
- `/tmp/ttest2/DONE.txt` appears within a minute or two.
- The `<timestamp>.json` log contains time events with **both** clientId `"0"` and `"1"` in
  `playerPosition` (proves both agents are active).
- `tournament_manifest.json` shows `PlayerAgent → A`, `OpponentAgent → B`.
- `trial_end` events carry `playerScores` for both clients.
- `unity.log` shows `[OctagonAgent] Tournament model applied` **twice** and
  `[TournamentManager] Wrote tournament_manifest.json`, with no exceptions.

### 5d. Failure signatures
- Agents shoot out of the arena / no trials run / "Logger not ready" → the arena setup isn't
  completing. The known cause was applying the model via `Agent.SetModel()` at runtime; the model
  is now assigned in `Awake` instead (§3, §8). If it recurs, compare the scene against a plain
  `SoloOctagonStage` run.
- Only clientId `0` ever appears, or only one agent moves → the second agent has no model (assign a
  fallback in the Inspector, and pass its `--opp_model`).
- *"Can't use Behavior Type InferenceOnly without a model"* → assign the Inspector fallback
  model (§4.2).

---

## 6. Running the tournament (`tournament/run_tournament.py`)

Standard library only. Run from the repo root.

```bash
python tournament/run_tournament.py \
  --build builds/build_tournament_test.x86_64 \
  --models OctagonAgentSocial_260126_04 OctagonAgentSocial_260126_05 \
  --episodes 100
```

Key options:

| Flag | Meaning |
|------|---------|
| `--build` | Path to the standalone player. |
| `--models NAME...` / `--models-file FILE` | Entrant names (must match `entrantModels` asset names). |
| `--episodes N` | Completed trials per matchup (`--sim_eps`). |
| `--out DIR` | Output root. Default `simulations/tournaments/<timestamp>`. |
| `--parallel K` | Concurrent matchups (keep low; each is a full Unity process). |
| `--no-self-play` | Exclude i-vs-i matchups. |
| `--mirror` | Also run the reversed ordering of each pair. **Default off** — the arena is symmetric, so one ordering per pair suffices. |
| `--graphics` | Run with a window (debugging). |
| `--force` | Re-run matchups even if `DONE.txt` exists (otherwise they're skipped — runs are resumable). |
| `--dry-run` | Print the matchup list + commands without launching. |
| `--timeout S`, `--poll S` | Per-matchup wall-clock limit / `DONE.txt` poll interval. |

How it works: launches the build standalone per matchup, polls for `DONE.txt` (the build does
**not** self-quit — it disables the agent after writing the marker, with the log already
flushed/closed), then terminates the process. Resumable and checkpointed to
`tournament_index.csv` after every matchup.

### Output layout
```
simulations/tournaments/<timestamp>/
  tournament_config.json            # run parameters / provenance
  tournament_index.csv              # matchup_id, player_model, opp_model, out_dir, status, ...
  <player>__vs__<opp>/
    <timestamp>.json                # behavioural log (DiskLogger)
    tournament_manifest.json        # models actually applied per role
    DONE.txt                        # completion marker
    unity.log                       # Unity player log
```
(`simulations/` is gitignored — outputs stay local.)

---

## 7. Analysis plan (next stage)

Decisions already taken:
- **Trial mix: experimental 50/25/25** across 45/90/180° wall separations (set in
  `GeneralGlobals.cs`), split by separation in analysis.
- One ordering per pair (arena symmetric).

Planned analysis (parser/notebook, TBD):
1. **Strategy label per model** — from the 50 Hz `time_triggered` position traces, mean radial
   distance from arena centre in the trial-onset window. Low = centre, high = surround. Verify
   it's stable across opponents.
2. **Payoff matrix, split by separation** — win-rate (fraction of trials this client took the
   high wall) or mean `trialScores` per (player, opp) cell, separately for 45/90/180°.
3. **Ranking / stability** — rank by mean performance across opponents within each separation;
   optionally Nash of each empirical matrix. Cross with strategy labels to answer: best
   strategy overall, how each does against itself (diagonal), and centre-vs-surround head to
   head.

Join keys for analysis: `tournament_manifest.json` (tag → model per run) + the per-trial
`trial_end` / `slice_onset` events (`clientId`, wall IDs → separation).

---

## 8. Development changelog

A running record of the build-up, including the dead ends — kept so the same walls aren't hit
twice and so the "why" behind the current design is visible.

### 2026-06-23 — Initial tooling
- Added `TournamentManager.cs` (entrant registry + run manifest), the `OctagonAgent` model
  override, and `tournament/run_tournament.py`. Wrote this README.

### 2026-06-23 — Model override design
- Confirmed the override must run **standalone** (communicator off): with `mlagents-learn` the
  policy is a `RemotePolicy` and `SetModel` is ignored, and the single self-play behavior name
  can't host two different models (see §2).
- Confirmed the runtime uses **Unity Sentis**, not Barracuda — the model type is
  `Unity.Sentis.ModelAsset`, and `Agent.SetModel(string, ModelAsset, InferenceDevice)`.

### 2026-06-23 — First smoke test failed: agents stood still
- **Symptom:** in a built tournament scene, both agents stood in the centre, unmoving; the log
  showed `UnityAgentsException: Can't use Behavior Type InferenceOnly without a model` thrown
  from `BehaviorParameters.GeneratePolicy` → `Agent.LazyInitialize` → `OnEnable`.
- **Cause:** the agents were `InferenceOnly` with **no model assigned in the Inspector**.
  `GeneratePolicy` runs during `LazyInitialize` — *before* `Initialize()` (where the override
  was being applied) — and throws when `InferenceOnly` has a null model. No policy → no actions.
- **Fix:** assign any entrant as a **fallback model** on each agent in the Inspector (and
  populate `TournamentManager.entrantModels`). Documented as a hard requirement in §4.

### 2026-06-23 — Second smoke test failed: `ArgumentNullException` flood
- **Symptom:** with the fallback model assigned, `Initialize()` was now reached and `SetModel`
  ran, but it threw a continuous flood of `ArgumentNullException: Value cannot be null`
  (`NotifyAgentDone` → `ActionBuffers.Clear` → `Array.Clear`), and the agents still didn't move.
- **Cause:** `SetModel` calls `NotifyAgentDone()`, which clears the agent's action buffers.
  Those buffers (`m_Info.storedActions`) are allocated **after** `Initialize()` returns inside
  `LazyInitialize()`. Calling `SetModel` from `Initialize()` clears null buffers and throws. The
  earlier "safe in `Initialize()`" assumption only checked the actuator manager, not the buffers.
- **Fix:** split the override — **resolve** the `ModelAsset` in `Initialize()`, but **apply**
  it (`SetModel`) once in the first `OnEpisodeBegin()`, by which point the buffers exist and no
  decision has yet been requested. Added a `using Unity.Sentis;` and four `pendingTournament*`
  fields to carry the resolved override between the two methods.

### 2026-06-23 — Third smoke test: agents flew out of the arena
- **Symptom:** with the fallback model assigned, both models now loaded
  (`[OctagonAgent] Tournament model applied` appeared twice) and the agents acted — but they
  immediately shot out of the arena and out of the scene. No `trial_*` events were logged and
  `ArenaLogger` reported "Logger not ready".
- **False lead (netcode):** initially suspected the trial loop needed a Netcode host that nothing
  started. **Wrong** — standard training/inference uses no Netcode at all (it's only for human
  cross-PC play). The tournament scene is a duplicate of `SoloOctagonStage`, which runs two-model
  inference fine with Inspector-assigned models and no host. The "Logger not ready" came from
  `ArenaLogger.IsReady()` finding `OctagonArenaSettings.playerAgent` null — a *symptom* of the
  arena setup never completing, not a cause.
- **Actual cause:** the only behavioural difference from the working Solo path was applying the
  model via `Agent.SetModel()` in `OnEpisodeBegin()`. `SetModel` calls `NotifyAgentDone()` right at
  episode start, before the `PlayerAgent` runs `TrialLoop()`/`SetUpArena()` — disrupting arena
  setup so the agent was never positioned/reset and walked out. Confirmed by running with no model
  args (no `SetModel`): agents respected walls again.
- **Fix:** stop using `SetModel` entirely. Assign `BehaviorParameters.Model` directly in `Awake()`,
  before `LazyInitialize()` generates the policy. The agent then initialises with the chosen model
  exactly like an Inspector-assigned one — no `NotifyAgentDone`, no arena disruption. This also
  removed the `pendingTournament*` fields, the `OnEpisodeBegin` apply, and the InferenceOnly
  null-model concern from the previous approach.

### Still to verify
- A clean visual run where both agents move, play trials, and resolve them — with the override now
  applied in `Awake` (real per-role models, not the fallback).
- The headless `-batchmode -nographics` run, for the full unattended sweep.
