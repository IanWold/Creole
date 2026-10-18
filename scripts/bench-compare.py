#!/usr/bin/env python3
"""Compare two benchmark runs written by benchmarks/Creole.Benchmarks.

Each run is a folder under BenchmarkDotNet.Artifacts/runs/. The script reads every
*-report-full.json in both runs, matches benchmarks by full name, and prints mean time and
allocated bytes for each, with the ratio new/old. Benchmarks in only one run are listed too.

Usage:
    python3 scripts/bench-compare.py <old-run> <new-run>

A run can be given as a path or as a folder name under BenchmarkDotNet.Artifacts/runs/.
"""

import argparse
import json
import sys
from pathlib import Path

RUNS = Path(__file__).resolve().parent.parent / "BenchmarkDotNet.Artifacts" / "runs"


def resolve(run: str) -> Path:
    path = Path(run)
    if not path.is_dir():
        path = RUNS / run
    if not path.is_dir():
        sys.exit(f"bench-compare: no run folder '{run}'")
    return path


def load(run: Path) -> tuple[dict, dict[str, dict]]:
    host, benchmarks = {}, {}
    reports = sorted(run.glob("results/*-report-full.json"))
    if not reports:
        sys.exit(f"bench-compare: no *-report-full.json in {run}/results")
    for report in reports:
        data = json.loads(report.read_text(encoding="utf-8"))
        host = data.get("HostEnvironmentInfo", host)
        for benchmark in data["Benchmarks"]:
            stats = benchmark.get("Statistics") or {}
            memory = benchmark.get("Memory") or {}
            benchmarks[benchmark["FullName"]] = {
                "mean": stats.get("Mean"),
                "allocated": memory.get("BytesAllocatedPerOperation"),
            }
    return host, benchmarks


def time(ns: float | None) -> str:
    if ns is None:
        return "-"
    for unit, scale in (("s", 1e9), ("ms", 1e6), ("us", 1e3)):
        if ns >= scale:
            return f"{ns / scale:.3f} {unit}"
    return f"{ns:.1f} ns"


def size(b: float | None) -> str:
    if b is None:
        return "-"
    for unit, scale in (("MB", 1 << 20), ("KB", 1 << 10)):
        if b >= scale:
            return f"{b / scale:.2f} {unit}"
    return f"{b:.0f} B"


def ratio(old: float | None, new: float | None) -> str:
    if old is None or new is None:
        return "-"
    if old == 0:
        return "-" if new == 0 else "from 0"
    return f"{new / old:.2f}x"


def describe(host: dict) -> str:
    keys = ("RuntimeVersion", "ProcessorName", "OsVersion", "BenchmarkDotNetVersion")
    return ", ".join(str(host[k]) for k in keys if k in host)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("old")
    parser.add_argument("new")
    args = parser.parse_args()

    old_run, new_run = resolve(args.old), resolve(args.new)
    old_host, old = load(old_run)
    new_host, new = load(new_run)

    print(f"old: {old_run.name} ({describe(old_host)})")
    print(f"new: {new_run.name} ({describe(new_host)})")
    if describe(old_host) != describe(new_host):
        print("warning: the runs used different hosts or runtimes; ratios mix machine and code changes")
    print()

    header = ["Benchmark", "Mean (old)", "Mean (new)", "Time", "Alloc (old)", "Alloc (new)", "Alloc"]
    rows = []
    for name in sorted(old.keys() | new.keys()):
        o, n = old.get(name, {}), new.get(name, {})
        rows.append([
            name.removeprefix("Creole.Benchmarks."),
            time(o.get("mean")), time(n.get("mean")), ratio(o.get("mean"), n.get("mean")),
            size(o.get("allocated")), size(n.get("allocated")), ratio(o.get("allocated"), n.get("allocated")),
        ])

    widths = [max(len(r[i]) for r in [header, *rows]) for i in range(len(header))]
    for row in [header, *rows]:
        print("  ".join(cell.ljust(width) for cell, width in zip(row, widths)))


if __name__ == "__main__":
    main()
