#!/usr/bin/env python3
"""Run the Phase 1 memory gate with benchmarks/Creole.MemoryGate.

The gate (ADR-0003) parses Example B and a JSON-lines stream at 10 MB, 100 MB and 1 GB, each in its
own process. --inputs runs other inputs of the harness instead, such as json-document, one JSON
array of the JSON-lines records parsed from a pipe (STRM-04), against the same threshold. Each run samples the retained heap after a forced full blocking GC every 1,000 records,
and subtracts the retained heap after a 1 MB warm-up run. The gate passes when, for both inputs, the
peak at the largest size is at most 1.5 times the peak at the smallest.

Usage:
    python3 scripts/memory-gate.py [--sizes N N ...] [--inputs NAME ...]

--sizes replaces the default sizes, in characters; the threshold compares the largest with the
smallest. The script builds the harness in Release, prints each run's JSON line and a table, and warns
if the working tree has uncommitted changes. It exits with 1 when a run fails or the threshold is missed.
"""

import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "benchmarks" / "Creole.MemoryGate"
INPUTS = ["example-b", "json-lines"]
OTHER_INPUTS = ["json-document"]
SIZES = [10_000_000, 100_000_000, 1_000_000_000]
THRESHOLD = 1.5


def git(*arguments: str) -> str | None:
    try:
        result = subprocess.run(["git", *arguments], cwd=ROOT, capture_output=True, text=True, check=True)
    except (OSError, subprocess.CalledProcessError):
        return None
    return result.stdout.strip()


def ratio(retained: int, smallest: int) -> float:
    return retained / smallest if smallest > 0 else float("inf")


def run(input_name: str, size: int) -> dict:
    command = ["dotnet", "run", "-c", "Release", "--no-build", "--project", str(PROJECT), "--", input_name, str(size)]
    result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
    if result.returncode != 0:
        sys.exit(f"memory-gate: {input_name} at {size:,} failed with exit code {result.returncode}\n{result.stderr}")
    line = result.stdout.strip().splitlines()[-1]
    print(line, flush=True)
    return json.loads(line)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the Phase 1 memory gate.")
    parser.add_argument("--sizes", type=int, nargs="+", default=SIZES, help="input sizes in characters")
    parser.add_argument("--inputs", nargs="+", choices=INPUTS + OTHER_INPUTS, default=INPUTS, help="the inputs to run, by default the gate's")
    arguments = parser.parse_args()
    sizes = sorted(arguments.sizes)
    inputs = arguments.inputs
    if len(sizes) < 2:
        sys.exit("memory-gate: give at least two sizes")

    commit = git("rev-parse", "HEAD")
    status = git("status", "--porcelain")
    print(f"Commit {commit or 'unknown'}")
    if status:
        print("warning: the working tree has uncommitted changes, so this run measures no commit")
    elif status is None:
        print("warning: git could not be run, so the commit is unknown")

    build = subprocess.run(["dotnet", "build", "-c", "Release", str(PROJECT)], cwd=ROOT, capture_output=True, text=True)
    if build.returncode != 0:
        sys.exit(f"memory-gate: the build failed\n{build.stdout}")

    runs = {(name, size): run(name, size) for name in inputs for size in sizes}

    print()
    print(f"{'Input':<14} {'Characters':>15} {'Records':>12} {'Samples':>8} {'Retained (bytes)':>17} {'Ratio':>6} {'Seconds':>8}")
    for name in inputs:
        smallest = runs[(name, sizes[0])]["retainedBytes"]
        for size in sizes:
            result = runs[(name, size)]
            print(
                f"{name:<14} {result['characters']:>15,} {result['records']:>12,} {result['samples']:>8,} "
                f"{result['retainedBytes']:>17,} {ratio(result['retainedBytes'], smallest):>6.2f} {result['seconds']:>8.1f}"
            )

    print()
    passed = True
    for name in inputs:
        growth = ratio(runs[(name, sizes[-1])]["retainedBytes"], runs[(name, sizes[0])]["retainedBytes"])
        passed &= growth <= THRESHOLD
        verdict = "pass" if growth <= THRESHOLD else "FAIL"
        print(f"{name}: peak at {sizes[-1]:,} is {growth:.2f}x the peak at {sizes[0]:,} (threshold {THRESHOLD}x): {verdict}")

    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
