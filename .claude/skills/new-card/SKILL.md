---
name: new-card
description: Add a new card (or a new epic) to Creole's work tracker in the standard format, with IDs, dependencies wired in both directions, phase and wave placement, and the work README tables and totals updated.
argument-hint: "<description of the work> [--epic <PREFIX>]"
---

# New card

Rules and format: CONTRIBUTING.md § Cards and § Epics, phases, waves and gates.

## Input

`$ARGUMENTS`: what the work is, and optionally the epic prefix.

## Workflow

1. **Check for overlap.** Search existing cards (`grep -ril` over `work/`) for the same scope. If one covers it, propose changing that card instead.
2. **Shape the card with the maintainer** (propose, then confirm):
   - Epic: the best existing one, or a new epic (a new prefix, a new `NN-slug` folder after the highest number, and a section under `## Epics` in work/README.md).
   - ID: next unused number for the prefix. IDs are never reused, including from complete/ and backburner/.
   - Title, Summary, Scope, and acceptance criteria that can be verified by test, measurement or inspection.
   - Size (S, M, L or XL, as defined in work/README.md), Phase, and `Gate` only if it is a gate.
   - Blockers and dependents. Check whether existing cards should now be blocked by it.
   - Whitepaper references. If the work is not in the whitepaper at all, flag it: either the whitepaper needs a decision to include it, or the card is out of scope.
   - If a card is bigger than XL or has more than about six criteria, propose splitting it.
3. **Write the card** in the exact format of existing cards (see any file in `work/active/`). Use relative links like existing cards do.
4. **Wire dependencies both ways**: add it to each blocker's `## Unblocks` and each dependent's `## Blocked by`, replacing "Nothing directly." or "Nothing. This item can start immediately." where needed.
5. **Update work/README.md**: the epic table, the phase table in the right wave (one more than its deepest blocker's wave in that phase), `**Totals:**`, "Can start now" if it is ready, and the critical path if this changes it.
6. Run `python3 scripts/work.py check`, fix anything reported, and suggest a commit message: `work: Add <ID> <title>`.
