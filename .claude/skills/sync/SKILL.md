---
name: sync
description: Audit and repair drift across Creole's project state — broken links and anchors, card/README mismatches, dependency asymmetry, stale indexes, cards whose status disagrees with reality, and whitepaper/decision/docs/code conflicts. Use periodically, after large edits, or when something looks out of date.
argument-hint: "[--report-only]"
---

# Sync

Mechanical drift is fixed directly. Semantic drift (disagreement about the design or what is built) is reported, not resolved, because it needs a decision.

## Input

`--report-only`: report everything, fix nothing.

## Workflow

1. **Mechanical checks.** Run `python3 scripts/work.py check`. Fix what it reports: broken links, missing anchors (find the renamed heading), `Blocked by`/`Unblocks` asymmetry, README totals, "In progress" and "Can start now" lists. Re-run until clean.
2. **Indexes.** Every file in `work/docs/research/`, `work/docs/decisions/`, `work/docs/archive/`, `work/ideas/` and `docs/` is listed in its index, and every index row points to a file that exists.
3. **Status reality check.**
   - In-progress cards with no progress note in a long time: ask whether they are still active.
   - Cards whose criteria look satisfied by existing code or docs but are not Done: suggest `/complete`.
   - Done cards without an Outcome, or whose Outcome mentions follow-ups that have no card.
   - Proposed decisions waiting on the maintainer.
   - Epics whose cards are all Done but still in `work/active/`.
4. **Semantic drift.** Report, do not fix:
   - Accepted decisions not reflected in the whitepaper.
   - Whitepaper § Risks and open questions → Still open items that a decision or card has closed.
   - Whitepaper Roadmap and work/README phases or gates that disagree.
   - Once code exists: public API or diagnostics missing from `docs/`, and docs describing behaviour the code does not have.
5. **Report**: fixes made (by category), then semantic issues with a suggested action each (`/decide`, `/complete`, an editorial fix). Suggest a commit message for the fixes: `work: Sync tracker`.
