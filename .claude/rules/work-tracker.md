---
paths:
  - "work/**/*.md"
---

# Editing the work tracker

Full rules: CONTRIBUTING.md § Cards, § Completing a card, § Completing an epic.

- Card sections, in order: the metadata table, Summary, Scope, Acceptance criteria, Blocked by, Unblocks, Whitepaper references, then Artifacts, Progress notes and Outcome when they have content. Match existing cards exactly.
- `Status` is one of `Not started`, `Researching`, `Designing`, `Building`, `Done`.
- `Blocked by` and `Unblocks` are mirror images. Edit both cards in the same change.
- Card IDs are permanent and never reused. Never rename a card file; titles can change, filenames cannot.
- Tick an acceptance criterion only with evidence: test output, a measurement, or an inspected artifact.
- Progress notes are append-only, one dated line each: `- YYYY-MM-DD (<Stage>): what was done. Next: …`.
- Status changes also update `work/README.md`: the "In progress" and "Can start now" lists, and ✅ before done cards' links in its tables.
- Move epics with `python3 scripts/work.py move`, never by hand: it rewrites links.
- Run `python3 scripts/work.py check` when done editing.
