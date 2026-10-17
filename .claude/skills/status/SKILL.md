---
name: status
description: Report where the Creole project stands — in-progress cards, cards ready to start, phase progress, decisions awaiting the maintainer, tracker health — and recommend what to do next. Use at the start of a session or when asked "where are we", "what's next" or "what should I work on".
argument-hint: "[phase or epic prefix, optional]"
allowed-tools: Bash(python3 scripts/work.py *)
---

# Project status

Give the maintainer a short, accurate picture of the project and a recommendation for what to do next. Read-only: do not edit files.

## Current tracker state

!`python3 scripts/work.py status`

## Input

`$ARGUMENTS` may narrow the report to a phase (`0`–`4`, `spike`) or an epic prefix (`CORE`, `LAY`, …). Empty means the whole project.

## Workflow

1. Start from the tracker state above. If it reports errors, run `python3 scripts/work.py check` and lead the report with them.
2. For each in-progress card, read its latest progress note and its `## Artifacts`.
3. List `work/ideas/` entries if the folder exists.
4. From the ready list, pick up to three recommended next cards. Prefer, in order: cards on the critical path to 1.0 (work/README.md § Critical path), cards that unblock the most others (`python3 scripts/work.py card <ID>`), and cards that close out a wave.
5. Report:

```text
In progress: <ID> [stage] title — last note, next step
Waiting on you: proposed decisions, criteria awaiting verification
Ready: <count>; recommended next: <ID> (why), …
Phase progress: Phase 0 n/m, …
Tracker health: clean | <n> problems (run /sync)
External blockers: e.g. C# 15 GA for FND-07
```

Keep it under 25 lines.
