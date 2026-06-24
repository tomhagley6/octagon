#!/usr/bin/env python3
"""Round-robin tournament driver for Octagon RL models.

Launches a STANDALONE inference build (no ``mlagents-learn`` / no communicator)
once per matchup, overriding each agent's model via ``--player_model`` /
``--opp_model``, and collects per-matchup behavioural logs for later analysis.

Why standalone (not mlagents-learn)?
    The Unity-assigned model is only used when the ML-Agents communicator is OFF.
    With a connected trainer each agent uses a RemotePolicy and the assigned model
    is ignored. Both agents also share ONE behavior name (OctagonAgentSocial), so
    ``mlagents-learn --inference`` would load a single policy for both -- it cannot
    put model A on PlayerAgent and model B on OpponentAgent. Running the build
    standalone lets each agent load its own model (assigned at startup from the CLI
    arg), which is the whole point of a cross-model tournament.
    See Assets/Readme/Tournament_README.md for the full rationale.

The build does NOT quit itself after finishing: OctagonAgent writes ``DONE.txt``
into ``--sim_out`` (after flushing/closing its log) and then disables the agent.
This driver therefore polls for ``DONE.txt`` and terminates the process.

Matchups: by default each unordered pair runs once (plus self-play). The arena is
positionally symmetric, so the mirror ordering is redundant; pass ``--mirror`` to
run both orderings anyway.

Run from the repo root (e.g. /home/tom/Unity/Octagon) in a normal terminal; no
conda env / trainer needed. Example:
    python tournament/run_tournament.py --build builds/build_tournament_test.x86_64 \
        --models OctagonAgentSocial_260126_04 OctagonAgentSocial_260126_05 --episodes 100

Output layout (per matchup), under ``--out``:
    <player>__vs__<opp>/
        <timestamp>.json          behavioural log (DiskLogger)
        tournament_manifest.json  models actually applied per role (TournamentManager)
        DONE.txt                  completion marker (written by the build)
        unity.log                 Unity player log
And at the root: ``tournament_index.csv`` and ``tournament_config.json``.

Stdlib only; no third-party dependencies.
"""

from __future__ import annotations

import argparse
import csv
import itertools
import json
import os
import re
import signal
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass, asdict
from datetime import datetime
from pathlib import Path


@dataclass
class Matchup:
    matchup_id: str
    player_model: str
    opp_model: str
    out_dir: str
    status: str = "pending"      # pending | done | skipped | failed | timeout
    returncode: int | None = None
    seconds: float | None = None


def sanitize(name: str) -> str:
    """Make a model name safe to use as a path segment."""
    return re.sub(r"[^A-Za-z0-9._-]", "_", name)


def load_models(args) -> list[str]:
    models: list[str] = []
    if args.models_file:
        for line in Path(args.models_file).read_text().splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                models.append(line)
    if args.models:
        models.extend(args.models)
    # de-duplicate, preserving order
    seen, unique = set(), []
    for m in models:
        if m not in seen:
            seen.add(m)
            unique.append(m)
    return unique


def build_matchups(models: list[str], out_root: Path, self_play: bool,
                   mirror: bool) -> list[Matchup]:
    if mirror:
        # Both orderings: (A,B) and (B,A); include (A,A) only if self_play.
        pairs = ((a, b) for a, b in itertools.product(models, repeat=2)
                 if self_play or a != b)
    else:
        # Each unordered pair once; with_replacement adds the (A,A) self-play cells.
        pairs = itertools.combinations_with_replacement(models, 2) if self_play \
            else itertools.combinations(models, 2)

    matchups = []
    for player, opp in pairs:
        mid = f"{sanitize(player)}__vs__{sanitize(opp)}"
        matchups.append(Matchup(
            matchup_id=mid,
            player_model=player,
            opp_model=opp,
            out_dir=str(out_root / mid),
        ))
    return matchups


def run_matchup(m: Matchup, args) -> Matchup:
    out_dir = Path(m.out_dir)
    done_path = out_dir / "DONE.txt"

    # Resumable: a finished matchup is left untouched.
    if done_path.exists() and not args.force:
        m.status = "skipped"
        return m

    out_dir.mkdir(parents=True, exist_ok=True)
    # Clear a stale marker from a previous failed/forced attempt.
    if done_path.exists():
        done_path.unlink()

    cmd = [str(args.build)]
    if not args.graphics:
        cmd += ["-batchmode", "-nographics"]
    cmd += [
        "-logFile", str(out_dir / "unity.log"),
        "--sim_eps", str(args.episodes),
        "--sim_out", str(out_dir),
        "--player_model", m.player_model,
        "--opp_model", m.opp_model,
    ]
    cmd += args.extra

    if args.dry_run:
        print("[dry-run]", " ".join(cmd))
        m.status = "pending"
        return m

    start = time.time()
    # New process group so we can reliably kill the player and any children.
    proc = subprocess.Popen(
        cmd,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        start_new_session=True,
    )

    try:
        while True:
            if done_path.exists():
                m.status = "done"
                break
            if proc.poll() is not None:
                # Process exited before writing DONE.txt -> crash / bad args.
                m.status = "done" if done_path.exists() else "failed"
                m.returncode = proc.returncode
                break
            if time.time() - start > args.timeout:
                m.status = "timeout"
                break
            time.sleep(args.poll)
    finally:
        _terminate(proc)
        m.seconds = round(time.time() - start, 1)
        if m.returncode is None:
            m.returncode = proc.returncode

    return m


def _terminate(proc: subprocess.Popen) -> None:
    if proc.poll() is not None:
        return
    try:
        os.killpg(os.getpgid(proc.pid), signal.SIGTERM)
    except ProcessLookupError:
        return
    try:
        proc.wait(timeout=15)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(os.getpgid(proc.pid), signal.SIGKILL)
        except ProcessLookupError:
            pass


def write_index(out_root: Path, matchups: list[Matchup]) -> None:
    index = out_root / "tournament_index.csv"
    with index.open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(asdict(matchups[0]).keys()))
        writer.writeheader()
        for m in matchups:
            writer.writerow(asdict(m))


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--build", required=True, type=Path,
                   help="Path to the standalone Unity player (e.g. builds/build_xxx.x86_64).")
    p.add_argument("--models", nargs="+", metavar="NAME",
                   help="Entrant model names (must match TournamentManager.entrantModels asset names).")
    p.add_argument("--models-file", type=Path,
                   help="Text file of model names, one per line (# comments allowed).")
    p.add_argument("--episodes", type=int, default=100,
                   help="Completed trials per matchup (passed as --sim_eps). Default 100.")
    p.add_argument("--out", type=Path, default=None,
                   help="Output root. Default: simulations/tournaments/<timestamp>.")
    p.add_argument("--parallel", type=int, default=1,
                   help="Concurrent matchups. Keep low unless the build is CPU-light. Default 1.")
    p.add_argument("--timeout", type=float, default=3600,
                   help="Per-matchup wall-clock timeout in seconds. Default 3600.")
    p.add_argument("--poll", type=float, default=2.0,
                   help="DONE.txt poll interval in seconds. Default 2.")
    p.add_argument("--no-self-play", dest="self_play", action="store_false",
                   help="Exclude self-play (i vs i) matchups.")
    p.add_argument("--mirror", action="store_true",
                   help="Also run the reversed ordering of each pair (A-as-player AND "
                        "B-as-player). Default off: the arena is symmetric, so one ordering "
                        "per pair suffices.")
    p.add_argument("--graphics", action="store_true",
                   help="Run with graphics (debugging); default is -batchmode -nographics.")
    p.add_argument("--force", action="store_true",
                   help="Re-run matchups even if DONE.txt already exists.")
    p.add_argument("--dry-run", action="store_true",
                   help="Print the matchup list and commands without launching anything.")
    p.add_argument("--extra", nargs=argparse.REMAINDER, default=[],
                   help="Everything after --extra is appended verbatim to each build command.")
    args = p.parse_args()

    if not args.build.exists() and not args.dry_run:
        p.error(f"build not found: {args.build}")

    models = load_models(args)
    if len(models) < 1:
        p.error("provide at least one model via --models or --models-file")

    if args.out is None:
        stamp = datetime.now().strftime("%y%m%d_%H%M%S")
        args.out = Path("simulations/tournaments") / stamp
    out_root = args.out
    out_root.mkdir(parents=True, exist_ok=True)

    matchups = build_matchups(models, out_root, args.self_play, args.mirror)

    print(f"Models ({len(models)}): {', '.join(models)}")
    print(f"Matchups: {len(matchups)}  |  episodes/matchup: {args.episodes}  "
          f"|  parallel: {args.parallel}  |  mirror: {args.mirror}")
    print(f"Output: {out_root}")

    # Persist the run configuration for provenance.
    (out_root / "tournament_config.json").write_text(json.dumps({
        "build": str(args.build),
        "models": models,
        "episodes": args.episodes,
        "self_play": args.self_play,
        "mirror": args.mirror,
        "graphics": args.graphics,
        "extra": args.extra,
        "created": datetime.now().isoformat(timespec="seconds"),
    }, indent=2))

    if args.dry_run:
        for m in matchups:
            print(f"  {m.matchup_id}")
            run_matchup(m, args)
        return 0

    done = 0
    with ThreadPoolExecutor(max_workers=max(1, args.parallel)) as pool:
        futures = {pool.submit(run_matchup, m, args): m for m in matchups}
        try:
            for fut in as_completed(futures):
                m = fut.result()
                done += 1
                print(f"[{done}/{len(matchups)}] {m.matchup_id}: {m.status}"
                      + (f" ({m.seconds}s)" if m.seconds else ""))
                write_index(out_root, matchups)  # checkpoint after each result
        except KeyboardInterrupt:
            print("\nInterrupted; terminating in-flight matchups...", file=sys.stderr)
            for fut in futures:
                fut.cancel()
            write_index(out_root, matchups)
            return 130

    write_index(out_root, matchups)
    n_done = sum(m.status in ("done", "skipped") for m in matchups)
    n_bad = sum(m.status in ("failed", "timeout") for m in matchups)
    print(f"\nComplete: {n_done} ok, {n_bad} failed/timeout. Index: "
          f"{out_root / 'tournament_index.csv'}")
    return 1 if n_bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
