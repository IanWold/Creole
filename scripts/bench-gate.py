#!/usr/bin/env python3
"""Check the Phase 0 speed gate from benchmark runs written by benchmarks/Creole.Benchmarks.

A gate run is three runs of the gate benchmarks with the default job, on the gate machine
(benchmarks/README.md, "Gate runs"):

    dotnet run -c Release --project benchmarks/Creole.Benchmarks -- --anyCategories Gate

For each gate benchmark and each run, the script divides Creole's mean time and allocation by
Pidgin's and Parlot's. It takes the median of each ratio over the runs, the geometric mean of the
medians over the benchmarks, and compares them with the Phase 0 thresholds in ADR-0003.

Usage:
    python3 scripts/bench-gate.py <run> <run> <run>

A run can be given as a path or as a folder name under BenchmarkDotNet.Artifacts/runs/. The script
stops if the runs measured different commits or Parlot versions. It warns if a run had uncommitted
changes or no run.json, used a job other than the default, or ran on a different host, and if the
number of runs is not three. It exits with 1 when a threshold is missed or the runs cannot be compared.
"""

import argparse
import json
import math
import statistics
import sys
from pathlib import Path

RUNS = Path(__file__).resolve().parent.parent / "BenchmarkDotNet.Artifacts" / "runs"

# (label, benchmark class, parameters, Pidgin method). Parlot's method is always "Parlot".
BENCHMARKS = [
    ("Arithmetic", "ArithmeticBenchmarks", "", "PidginFolds"),
    ("JSON small", "JsonBenchmarks", "Input=small", "Pidgin"),
    ("JSON medium", "JsonBenchmarks", "Input=medium", "Pidgin"),
    ("JSON large", "JsonBenchmarks", "Input=large", "Pidgin"),
]

PIDGIN_GEOMEAN = 1.0
PIDGIN_EACH = 1.25
PARLOT_GEOMEAN = 1.5
ALLOCATION_EACH = 1.0


def resolve(run: str) -> Path:
    path = Path(run)
    if not path.is_dir():
        path = RUNS / run
    if not path.is_dir():
        sys.exit(f"bench-gate: no run folder '{run}'")
    return path


def load(run: Path) -> tuple[dict, dict, dict[tuple[str, str, str], dict]]:
    manifest_path = run / "run.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.is_file() else {}
    host, benchmarks = {}, {}
    for report in sorted(run.glob("results/*-report-full.json")):
        data = json.loads(report.read_text(encoding="utf-8"))
        host = data.get("HostEnvironmentInfo", host)
        for benchmark in data["Benchmarks"]:
            display = benchmark.get("DisplayInfo", "")
            job = display.split(": ", 1)[-1].split(" [", 1)[0].split("(", 1)[0]
            benchmarks[(benchmark["Type"], benchmark["Method"], benchmark.get("Parameters", ""))] = {
                "mean": (benchmark.get("Statistics") or {}).get("Mean"),
                "allocated": (benchmark.get("Memory") or {}).get("BytesAllocatedPerOperation"),
                "job": job,
            }
    if not benchmarks:
        sys.exit(f"bench-gate: no *-report-full.json in {run}/results")
    return manifest, host, benchmarks


def describe(host: dict) -> str:
    keys = ("RuntimeVersion", "ProcessorName", "OsVersion", "BenchmarkDotNetVersion")
    return ", ".join(str(host[k]) for k in keys if k in host)


def ratios(runs: list[dict], key: tuple[str, str, str], baseline: tuple[str, str, str], measure: str) -> list[float] | None:
    values = []
    for benchmarks in runs:
        creole, other = benchmarks.get(key), benchmarks.get(baseline)
        if not creole or not other or creole[measure] is None or not other[measure]:
            return None
        values.append(creole[measure] / other[measure])
    return values


def geomean(values: list[float]) -> float:
    return math.exp(sum(math.log(v) for v in values) / len(values))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("runs", nargs="+")
    args = parser.parse_args()

    folders = [resolve(run) for run in args.runs]
    loaded = [load(folder) for folder in folders]
    manifests = [manifest for manifest, _, _ in loaded]
    hosts = [describe(host) for _, host, _ in loaded]
    runs = [benchmarks for _, _, benchmarks in loaded]

    for field in ("commit", "parlot"):
        values = {manifest.get(field) for manifest in manifests if manifest}
        if len(values) > 1:
            sys.exit(f"bench-gate: the runs measured different {field} values: {', '.join(sorted(map(str, values)))}")

    warnings = []
    if len(folders) != 3:
        warnings.append(f"a gate run is three runs; {len(folders)} given")
    for folder, manifest in zip(folders, manifests):
        if not manifest:
            warnings.append(f"{folder.name} has no run.json, so its commit and Parlot version are unknown")
        elif manifest.get("dirty") is not False:
            warnings.append(f"{folder.name} had uncommitted changes, or git could not tell")
    jobs = {b["job"] for benchmarks in runs for b in benchmarks.values()}
    if jobs != {"DefaultJob"}:
        warnings.append(f"a gate run uses the default job; these runs used {', '.join(sorted(jobs))}")
    if len(set(hosts)) > 1:
        warnings.append("the runs used different hosts or runtimes")

    manifest = next((m for m in manifests if m), {})
    commit = manifest.get("commit") or "unknown"
    print(f"commit: {commit[:12]}, Parlot {manifest.get('parlot', 'unknown')}, Pidgin {manifest.get('pidgin', 'unknown')}")
    print(f"host: {hosts[0]}")
    print(f"runs: {', '.join(folder.name for folder in folders)}")
    for warning in warnings:
        print(f"warning: {warning}")
    print()

    header = ["Benchmark", "/ Pidgin (runs)", "/ Pidgin", "/ Parlot (runs)", "/ Parlot", "Alloc / Pidgin"]
    rows, pidgin, parlot, allocation, missing = [], [], [], [], []
    for label, kind, parameters, pidgin_method in BENCHMARKS:
        key = (kind, "Creole", parameters)
        to_pidgin = ratios(runs, key, (kind, pidgin_method, parameters), "mean")
        to_parlot = ratios(runs, key, (kind, "Parlot", parameters), "mean")
        alloc = ratios(runs, key, (kind, pidgin_method, parameters), "allocated")
        if to_pidgin is None or to_parlot is None or alloc is None:
            missing.append(label)
            continue
        pidgin.append((label, statistics.median(to_pidgin)))
        parlot.append((label, statistics.median(to_parlot)))
        allocation.append((label, statistics.median(alloc)))
        rows.append([
            label,
            " ".join(f"{r:.2f}" for r in to_pidgin), f"{pidgin[-1][1]:.2f}",
            " ".join(f"{r:.2f}" for r in to_parlot), f"{parlot[-1][1]:.2f}",
            f"{allocation[-1][1]:.3f}",
        ])

    if missing:
        sys.exit(f"bench-gate: missing Creole, Pidgin or Parlot results for {', '.join(missing)}")

    pidgin_geomean = geomean([r for _, r in pidgin])
    parlot_geomean = geomean([r for _, r in parlot])
    rows.append(["Geometric mean", "", f"{pidgin_geomean:.2f}", "", f"{parlot_geomean:.2f}", ""])

    widths = [max(len(r[i]) for r in [header, *rows]) for i in range(len(header))]
    for row in [header, *rows]:
        print("  ".join(cell.ljust(width) for cell, width in zip(row, widths)).rstrip())
    print()

    worst_pidgin = max(pidgin, key=lambda r: r[1])
    worst_allocation = max(allocation, key=lambda r: r[1])
    checks = [
        (f"Time, geometric mean <= {PIDGIN_GEOMEAN:.2f}x Pidgin", f"{pidgin_geomean:.2f}", pidgin_geomean <= PIDGIN_GEOMEAN),
        (f"Time, every benchmark <= {PIDGIN_EACH:.2f}x Pidgin", f"{worst_pidgin[1]:.2f} ({worst_pidgin[0]})", worst_pidgin[1] <= PIDGIN_EACH),
        (f"Time, geometric mean <= {PARLOT_GEOMEAN:.2f}x Parlot", f"{parlot_geomean:.2f}", parlot_geomean <= PARLOT_GEOMEAN),
        (f"Allocation, every benchmark <= {ALLOCATION_EACH:.2f}x Pidgin", f"{worst_allocation[1]:.3f} ({worst_allocation[0]})", worst_allocation[1] <= ALLOCATION_EACH),
    ]
    width = max(len(name) for name, _, _ in checks)
    for name, value, passed in checks:
        print(f"{name.ljust(width)}  {value}  {'pass' if passed else 'FAIL'}")

    passed = all(ok for _, _, ok in checks)
    print()
    print(f"Phase 0 speed gate: {'pass' if passed else 'FAIL'}")
    sys.exit(0 if passed else 1)


if __name__ == "__main__":
    main()
